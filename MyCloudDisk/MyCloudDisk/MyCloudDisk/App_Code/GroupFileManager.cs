﻿// -*- coding: utf-8 -*-
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

public static class GroupFileManager
{
    private static readonly string BaseGroupPath = @"C:\Users\Administrator\Desktop\myclouddat\qunzudata\";
    private static readonly string SystemKeyPassword = "CloudDiskSystemKey2024!"; // 系统级密钥密码
    
    // 固定的系统密钥盐值
    private static readonly byte[] SystemKeySalt = new byte[] { 
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 
    };

    static GroupFileManager()
    {
        if (!Directory.Exists(BaseGroupPath)) Directory.CreateDirectory(BaseGroupPath);
    }

    public class UploadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class FileListResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Dictionary<string, string> Files { get; set; }
    }

    public class DownloadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string TempFilePath { get; set; }
    }

    public class KeyResult
    {
        public byte[] Key { get; set; }
        public bool Success { get; set; }
    }

    public static UploadResult CreateGroup(string groupName, string uploadPassword, string adminPassword, string regCode)
    {
        try
        {
            if (!RateLimitHelper.CheckRateLimit("create_personal_folder", 3, 60))
            {
                return new UploadResult { Success = false, Message = "创建文件夹次数过多，请1小时后再试" };
            }

            if (string.IsNullOrWhiteSpace(groupName) || groupName.Length > 15)
                return new UploadResult { Success = false, Message = "群组名称必须1-15个字符" };

            if (groupName.Any(c => Path.GetInvalidFileNameChars().Contains(c)))
                return new UploadResult { Success = false, Message = "群组名称包含非法字符" };

            if (string.IsNullOrWhiteSpace(uploadPassword) || uploadPassword.Length < 8)
                return new UploadResult { Success = false, Message = "公共密码至少8位" };

            if (!uploadPassword.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c)))
                return new UploadResult { Success = false, Message = "公共密码必须包含至少一个特殊字符" };

            if (string.IsNullOrWhiteSpace(adminPassword) || adminPassword.Length < 8)
                return new UploadResult { Success = false, Message = "管理密码至少8位" };

            if (!adminPassword.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c)))
                return new UploadResult { Success = false, Message = "管理密码必须包含至少一个特殊字符" };

            string groupPath = Path.Combine(BaseGroupPath, groupName);
            if (Directory.Exists(groupPath))
                return new UploadResult { Success = false, Message = "群组名称已存在" };

            var regCodeResult = RegCodeHelper.ValidateRegCode(regCode);
            if (!regCodeResult.IsValid)
                return new UploadResult { Success = false, Message = regCodeResult.Message };

            Directory.CreateDirectory(groupPath);

            var uploadKeyAndSalt = CryptoHelper.DeriveKeyFromPassword(uploadPassword);
            var adminKeyAndSalt = CryptoHelper.DeriveKeyFromPassword(adminPassword);

            byte[] uploadKeyHash = CryptoHelper.ComputeHash(uploadKeyAndSalt.Key);
            byte[] adminKeyHash = CryptoHelper.ComputeHash(adminKeyAndSalt.Key);

            // 加密存储管理密钥
            byte[] encryptedAdminKey = EncryptAdminKey(adminKeyAndSalt.Key);
            
            if (encryptedAdminKey == null)
            {
                return new UploadResult { Success = false, Message = "创建群组失败：密钥加密错误" };
            }

            string configContent = string.Format("{0}|{1}|{2}|{3}|{4}",
                Convert.ToBase64String(uploadKeyAndSalt.Salt),
                Convert.ToBase64String(uploadKeyHash),
                Convert.ToBase64String(adminKeyAndSalt.Salt),
                Convert.ToBase64String(adminKeyHash),
                Convert.ToBase64String(encryptedAdminKey));
            File.WriteAllText(Path.Combine(groupPath, "config.txt"), configContent);

            var emptyIndex = new Dictionary<string, string>();
            string indexJson = new JavaScriptSerializer().Serialize(emptyIndex);
            string tempIndexFile = Path.GetTempFileName();
            File.WriteAllText(tempIndexFile, indexJson);
            CryptoHelper.EncryptFile(tempIndexFile, Path.Combine(groupPath, "index.enc"), adminKeyAndSalt.Key);
            File.Delete(tempIndexFile);

            RegCodeHelper.MarkRegCodeUsed(regCode, groupName);

            LogHelper.LogGroupOperation("创建群组", groupName, "注册码: " + regCode);
            return new UploadResult { Success = true, Message = "群组创建成功" };
        }
        catch (Exception ex)
        {
            LogHelper.LogGroupOperation("创建群组错误", groupName, ex.Message);
            return new UploadResult { Success = false, Message = "系统错误: " + ex.Message };
        }
    }

    public static UploadResult UploadToGroup(string groupName, string uploadPassword, HttpFileCollection files)
    {
        try
        {
            string groupPath = Path.Combine(BaseGroupPath, groupName);
            if (!Directory.Exists(groupPath))
                return new UploadResult { Success = false, Message = "群组不存在" };

            // 验证公共密码
            var uploadKeyResult = GetUploadKey(groupName, uploadPassword);
            if (!uploadKeyResult.Success) 
                return new UploadResult { Success = false, Message = "公共密码错误" };

            // 获取存储的管理密钥
            var adminKeyResult = GetAdminKeyFromConfig(groupName);
            if (!adminKeyResult.Success) 
                return new UploadResult { Success = false, Message = "无法访问群组配置" };

            var fileIndex = GetFileIndex(groupName, adminKeyResult.Key);

            for (int i = 0; i < files.Count; i++)
            {
                HttpPostedFile file = files[i];
                if (file.ContentLength > 0)
                {
                    try
                    {
                        string originalFileName = Path.GetFileName(file.FileName);
                        string encryptedFileName = Guid.NewGuid().ToString() + ".enc";

                        string finalOriginalName = originalFileName;
                        int counter = 1;
                        while (fileIndex.ContainsValue(finalOriginalName))
                        {
                            string nameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
                            string extension = Path.GetExtension(originalFileName);
                            finalOriginalName = nameWithoutExt + "(" + counter + ")" + extension;
                            counter++;
                        }

                        string tempFilePath = Path.GetTempFileName();
                        file.SaveAs(tempFilePath);
                        CryptoHelper.EncryptFile(tempFilePath, Path.Combine(groupPath, encryptedFileName), adminKeyResult.Key);
                        File.Delete(tempFilePath);

                        fileIndex[encryptedFileName] = finalOriginalName;
                        LogHelper.LogGroupOperation("上传文件", groupName, "文件: " + finalOriginalName);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogGroupOperation("上传文件错误", groupName, file.FileName + " - " + ex.Message);
                        return new UploadResult { Success = false, Message = "文件 " + file.FileName + " 上传失败: " + ex.Message };
                    }
                }
            }

            SaveFileIndex(groupName, adminKeyResult.Key, fileIndex);
            return new UploadResult { Success = true, Message = "文件上传成功" };
        }
        catch (Exception ex)
        {
            LogHelper.LogGroupOperation("上传文件错误", groupName, ex.Message);
            return new UploadResult { Success = false, Message = "系统错误: " + ex.Message };
        }
    }

    public static FileListResult GetGroupFileList(string groupName, string adminPassword)
    {
        try
        {
            string groupPath = Path.Combine(BaseGroupPath, groupName);
            if (!Directory.Exists(groupPath))
                return new FileListResult { Success = false, Message = "群组不存在", Files = null };

            var keyResult = GetAdminKey(groupName, adminPassword);
            if (!keyResult.Success) return new FileListResult { Success = false, Message = "管理密码错误", Files = null };

            var fileIndex = GetFileIndex(groupName, keyResult.Key);
            LogHelper.LogGroupOperation("查看文件列表", groupName);
            return new FileListResult { Success = true, Message = "成功", Files = fileIndex };
        }
        catch (Exception ex)
        {
            LogHelper.LogGroupOperation("获取文件列表错误", groupName, ex.Message);
            return new FileListResult { Success = false, Message = "系统错误: " + ex.Message, Files = null };
        }
    }

    public static DownloadResult PrepareGroupFileDownload(string groupName, string adminPassword, string encryptedFileName, string originalFileName)
    {
        try
        {
            string groupPath = Path.Combine(BaseGroupPath, groupName);
            string encryptedFilePath = Path.Combine(groupPath, encryptedFileName);

            if (!File.Exists(encryptedFilePath))
                return new DownloadResult { Success = false, Message = "文件不存在", TempFilePath = null };

            var keyResult = GetAdminKey(groupName, adminPassword);
            if (!keyResult.Success) return new DownloadResult { Success = false, Message = "管理密码错误", TempFilePath = null };

            string tempFileName = Guid.NewGuid().ToString() + "_" + originalFileName;
            string tempFilePath = Path.Combine(@"C:\Users\Administrator\Desktop\myclouddat\", "lsdata", tempFileName);

            CryptoHelper.DecryptFile(encryptedFilePath, tempFilePath, keyResult.Key);

            LogHelper.LogGroupOperation("下载文件", groupName, "文件: " + originalFileName);
            return new DownloadResult { Success = true, Message = "成功", TempFilePath = tempFilePath };
        }
        catch (Exception ex)
        {
            LogHelper.LogGroupOperation("下载文件错误", groupName, encryptedFileName + " - " + ex.Message);
            return new DownloadResult { Success = false, Message = "解密错误: " + ex.Message, TempFilePath = null };
        }
    }

    public static DownloadResult PrepareGroupAllFilesDownload(string groupName, string adminPassword)
    {
        string tempZipPath = "";
        
        try
        {
            string groupPath = Path.Combine(BaseGroupPath, groupName);
            if (!Directory.Exists(groupPath))
                return new DownloadResult { Success = false, Message = "群组不存在", TempFilePath = null };

            var keyResult = GetAdminKey(groupName, adminPassword);
            if (!keyResult.Success) return new DownloadResult { Success = false, Message = "管理密码错误", TempFilePath = null };

            // 获取文件列表
            var fileIndex = GetFileIndex(groupName, keyResult.Key);
            if (fileIndex.Count == 0)
                return new DownloadResult { Success = false, Message = "群组中没有文件", TempFilePath = null };

            // 创建临时目录
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            // 解密所有文件到临时目录
            foreach (var fileEntry in fileIndex)
            {
                string encryptedFilePath = Path.Combine(groupPath, fileEntry.Key);
                string decryptedFilePath = Path.Combine(tempDir, fileEntry.Value);
                
                if (File.Exists(encryptedFilePath))
                {
                    CryptoHelper.DecryptFile(encryptedFilePath, decryptedFilePath, keyResult.Key);
                }
            }

            // 创建ZIP文件
            string zipFileName = string.Format("{0}_{1:yyyyMMdd_HHmmss}.zip", groupName, DateTime.Now);
            tempZipPath = Path.Combine(@"C:\Users\Administrator\Desktop\myclouddat\", "lsdata", zipFileName);
            
            // 使用System.IO.Compression创建ZIP
            CreateZipFile(tempDir, tempZipPath);

            // 清理临时目录
            Directory.Delete(tempDir, true);

            LogHelper.LogGroupOperation("打包下载", groupName, "文件数: " + fileIndex.Count);
            return new DownloadResult { Success = true, Message = "打包下载准备完成，共" + fileIndex.Count + "个文件", TempFilePath = tempZipPath };
        }
        catch (Exception ex)
        {
            // 清理临时文件
            try
            {
                if (!string.IsNullOrEmpty(tempZipPath) && File.Exists(tempZipPath))
                    File.Delete(tempZipPath);
            }
            catch { }
            
            LogHelper.LogGroupOperation("打包下载错误", groupName, ex.Message);
            return new DownloadResult { Success = false, Message = "打包下载失败: " + ex.Message, TempFilePath = null };
        }
    }

    public static UploadResult DeleteGroupFile(string groupName, string adminPassword, string encryptedFileName, string originalFileName)
    {
        try
        {
            string groupPath = Path.Combine(BaseGroupPath, groupName);
            string encryptedFilePath = Path.Combine(groupPath, encryptedFileName);

            if (!File.Exists(encryptedFilePath))
                return new UploadResult { Success = false, Message = "文件不存在" };

            var keyResult = GetAdminKey(groupName, adminPassword);
            if (!keyResult.Success) return new UploadResult { Success = false, Message = "管理密码错误" };

            var fileIndex = GetFileIndex(groupName, keyResult.Key);
            
            if (fileIndex.ContainsKey(encryptedFileName))
            {
                fileIndex.Remove(encryptedFileName);
                SaveFileIndex(groupName, keyResult.Key, fileIndex);
                File.Delete(encryptedFilePath);
                
                LogHelper.LogGroupOperation("删除文件", groupName, "文件: " + originalFileName);
                return new UploadResult { Success = true, Message = "文件删除成功" };
            }
            else
            {
                return new UploadResult { Success = false, Message = "文件不在索引中" };
            }
        }
        catch (Exception ex)
        {
            LogHelper.LogGroupOperation("删除文件错误", groupName, encryptedFileName + " - " + ex.Message);
            return new UploadResult { Success = false, Message = "删除错误: " + ex.Message };
        }
    }

    public static UploadResult DeleteGroup(string groupName, string adminPassword)
    {
        try
        {
            string groupPath = Path.Combine(BaseGroupPath, groupName);
            
            if (!Directory.Exists(groupPath))
                return new UploadResult { Success = false, Message = "群组不存在" };

            var keyResult = GetAdminKey(groupName, adminPassword);
            if (!keyResult.Success) return new UploadResult { Success = false, Message = "管理密码错误" };

            Directory.Delete(groupPath, true);
            LogHelper.LogGroupOperation("删除群组", groupName);
            return new UploadResult { Success = true, Message = "群组删除成功" };
        }
        catch (Exception ex)
        {
            LogHelper.LogGroupOperation("删除群组错误", groupName, ex.Message);
            return new UploadResult { Success = false, Message = "删除错误: " + ex.Message };
        }
    }

    private static KeyResult GetUploadKey(string groupName, string uploadPassword)
    {
        return GetKey(groupName, uploadPassword, 0, 1);
    }

    private static KeyResult GetAdminKey(string groupName, string adminPassword)
    {
        return GetKey(groupName, adminPassword, 2, 3);
    }

    private static KeyResult GetAdminKeyFromConfig(string groupName)
    {
        try
        {
            string groupPath = Path.Combine(BaseGroupPath, groupName);
            string configFile = Path.Combine(groupPath, "config.txt");

            if (!File.Exists(configFile)) return new KeyResult { Key = null, Success = false };

            string[] configParts = File.ReadAllText(configFile).Split('|');
            if (configParts.Length != 5) return new KeyResult { Key = null, Success = false };

            // 解密存储的管理密钥
            byte[] encryptedAdminKey = Convert.FromBase64String(configParts[4]);
            byte[] adminKey = DecryptAdminKey(encryptedAdminKey);
            
            if (adminKey == null) return new KeyResult { Key = null, Success = false };

            return new KeyResult { Key = adminKey, Success = true };
        }
        catch (Exception ex)
        {
            LogHelper.Log("GetAdminKeyFromConfig 错误: " + ex.Message);
            return new KeyResult { Key = null, Success = false };
        }
    }

    private static KeyResult GetKey(string groupName, string password, int saltIndex, int hashIndex)
    {
        string groupPath = Path.Combine(BaseGroupPath, groupName);
        string configFile = Path.Combine(groupPath, "config.txt");

        if (!File.Exists(configFile)) return new KeyResult { Key = null, Success = false };

        string[] configParts = File.ReadAllText(configFile).Split('|');
        if (configParts.Length != 5) return new KeyResult { Key = null, Success = false };

        byte[] salt = Convert.FromBase64String(configParts[saltIndex]);
        byte[] storedKeyHash = Convert.FromBase64String(configParts[hashIndex]);

        byte[] derivedKey = CryptoHelper.DeriveKeyWithSalt(password, salt);
        byte[] derivedKeyHash = CryptoHelper.ComputeHash(derivedKey);

        if (!derivedKeyHash.SequenceEqual(storedKeyHash)) return new KeyResult { Key = null, Success = false };

        return new KeyResult { Key = derivedKey, Success = true };
    }

    private static Dictionary<string, string> GetFileIndex(string groupName, byte[] key)
    {
        string groupPath = Path.Combine(BaseGroupPath, groupName);
        string indexFile = Path.Combine(groupPath, "index.enc");

        if (!File.Exists(indexFile)) return new Dictionary<string, string>();

        string tempIndexFile = Path.GetTempFileName();
        CryptoHelper.DecryptFile(indexFile, tempIndexFile, key);

        string indexJson = File.ReadAllText(tempIndexFile);
        File.Delete(tempIndexFile);

        var serializer = new JavaScriptSerializer();
        return serializer.Deserialize<Dictionary<string, string>>(indexJson) ?? new Dictionary<string, string>();
    }

    private static void SaveFileIndex(string groupName, byte[] key, Dictionary<string, string> fileIndex)
    {
        string groupPath = Path.Combine(BaseGroupPath, groupName);
        
        var serializer = new JavaScriptSerializer();
        string indexJson = serializer.Serialize(fileIndex);

        string tempIndexFile = Path.GetTempFileName();
        File.WriteAllText(tempIndexFile, indexJson);

        CryptoHelper.EncryptFile(tempIndexFile, Path.Combine(groupPath, "index.enc"), key);
        File.Delete(tempIndexFile);
    }

    private static void CreateZipFile(string sourceDirectory, string zipPath)
    {
        // 使用 ZipArchive 手动创建ZIP文件
        using (var fileStream = new FileStream(zipPath, FileMode.Create))
        {
            using (var archive = new System.IO.Compression.ZipArchive(fileStream, System.IO.Compression.ZipArchiveMode.Create))
            {
                foreach (var file in Directory.GetFiles(sourceDirectory))
                {
                    var entryName = Path.GetFileName(file);
                    var entry = archive.CreateEntry(entryName, System.IO.Compression.CompressionLevel.Fastest);
                    
                    using (var entryStream = entry.Open())
                    using (var fileStreamToCompress = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        fileStreamToCompress.CopyTo(entryStream);
                    }
                }
            }
        }
    }

    // 加密管理密钥
    private static byte[] EncryptAdminKey(byte[] adminKey)
    {
        try
        {
            // 使用固定盐值派生系统密钥
            byte[] systemKey = CryptoHelper.DeriveKeyWithSalt(SystemKeyPassword, SystemKeySalt);
            
            using (var aes = System.Security.Cryptography.Aes.Create())
            {
                aes.Key = systemKey;
                aes.GenerateIV();
                
                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new System.IO.MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (var cs = new System.Security.Cryptography.CryptoStream(ms, encryptor, System.Security.Cryptography.CryptoStreamMode.Write))
                    {
                        cs.Write(adminKey, 0, adminKey.Length);
                        cs.FlushFinalBlock();
                    }
                    return ms.ToArray();
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.Log("加密管理密钥错误: " + ex.Message);
            return null;
        }
    }

    // 解密管理密钥
    private static byte[] DecryptAdminKey(byte[] encryptedAdminKey)
    {
        try
        {
            // 使用固定盐值派生系统密钥
            byte[] systemKey = CryptoHelper.DeriveKeyWithSalt(SystemKeyPassword, SystemKeySalt);
            
            using (var aes = System.Security.Cryptography.Aes.Create())
            {
                aes.Key = systemKey;
                
                byte[] iv = new byte[16];
                System.Array.Copy(encryptedAdminKey, 0, iv, 0, iv.Length);
                aes.IV = iv;
                
                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new System.IO.MemoryStream())
                {
                    using (var cs = new System.Security.Cryptography.CryptoStream(ms, decryptor, System.Security.Cryptography.CryptoStreamMode.Write))
                    {
                        cs.Write(encryptedAdminKey, iv.Length, encryptedAdminKey.Length - iv.Length);
                        cs.FlushFinalBlock();
                    }
                    return ms.ToArray();
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.Log("解密管理密钥错误: " + ex.Message);
            return null;
        }
    }
}