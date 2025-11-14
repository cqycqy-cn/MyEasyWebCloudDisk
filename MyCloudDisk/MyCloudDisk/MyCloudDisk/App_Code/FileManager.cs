using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

public static class FileManager
{
    public static readonly string BaseDataPath = @"C:\Users\Administrator\Desktop\myclouddat\";
    private static readonly string TempFilePath = Path.Combine(BaseDataPath, "lsdata");

    static FileManager()
    {
        if (!Directory.Exists(BaseDataPath)) Directory.CreateDirectory(BaseDataPath);
        if (!Directory.Exists(TempFilePath)) Directory.CreateDirectory(TempFilePath);
    }

    // 定义返回结果的类
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

    public static UploadResult CreateFolderAndUpload(string folderName, string password, HttpFileCollection files)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(folderName) || folderName.Length > 15)
                return new UploadResult { Success = false, Message = "Folder name must be 1-15 characters" };

            if (folderName.Any(c => Path.GetInvalidFileNameChars().Contains(c)))
                return new UploadResult { Success = false, Message = "Folder name contains invalid characters" };

            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return new UploadResult { Success = false, Message = "Password must be at least 8 characters" };

            if (!password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c)))
                return new UploadResult { Success = false, Message = "Password must contain at least one special character" };

            string folderPath = Path.Combine(BaseDataPath, folderName);
            
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                
                var keyAndSalt = CryptoHelper.DeriveKeyFromPassword(password);
                byte[] key = keyAndSalt.Key;
                byte[] salt = keyAndSalt.Salt;
                byte[] keyHash = CryptoHelper.ComputeHash(key);
                
                string configContent = Convert.ToBase64String(salt) + "|" + Convert.ToBase64String(keyHash);
                File.WriteAllText(Path.Combine(folderPath, "config.txt"), configContent);
                
                var emptyIndex = new Dictionary<string, string>();
                string indexJson = new JavaScriptSerializer().Serialize(emptyIndex);
                string tempIndexFile = Path.GetTempFileName();
                File.WriteAllText(tempIndexFile, indexJson);
                CryptoHelper.EncryptFile(tempIndexFile, Path.Combine(folderPath, "index.enc"), key);
                File.Delete(tempIndexFile);
                
                LogHelper.Log("新建文件夹: " + folderName);
            }

            return UploadFiles(folderName, password, files);
        }
        catch (Exception ex)
        {
            LogHelper.Log("Folder creation error: " + folderName + " - " + ex.Message);
            return new UploadResult { Success = false, Message = "System error: " + ex.Message };
        }
    }

    private static UploadResult UploadFiles(string folderName, string password, HttpFileCollection files)
    {
        string folderPath = Path.Combine(BaseDataPath, folderName);
        
        if (!Directory.Exists(folderPath))
            return new UploadResult { Success = false, Message = "Folder does not exist" };

        var keyResult = GetEncryptionKey(folderName, password);
        if (!keyResult.Success) return new UploadResult { Success = false, Message = "Wrong password or config damaged" };

        var fileIndex = GetFileIndex(folderName, keyResult.Key);
        
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
                    CryptoHelper.EncryptFile(tempFilePath, Path.Combine(folderPath, encryptedFileName), keyResult.Key);
                    File.Delete(tempFilePath);

                    fileIndex[encryptedFileName] = finalOriginalName;
                    
                    LogHelper.Log("上传文件: " + folderName + "/" + finalOriginalName);
                }
                catch (Exception ex)
                {
                    LogHelper.Log("File upload error: " + folderName + "/" + file.FileName + " - " + ex.Message);
                    return new UploadResult { Success = false, Message = "File " + file.FileName + " upload failed: " + ex.Message };
                }
            }
        }

        SaveFileIndex(folderName, keyResult.Key, fileIndex);
        
        return new UploadResult { Success = true, Message = "File upload successful" };
    }

    public static FileListResult GetFileList(string folderName, string password)
    {
        try
        {
            CleanTempFiles();

            string folderPath = Path.Combine(BaseDataPath, folderName);
            if (!Directory.Exists(folderPath))
                return new FileListResult { Success = false, Message = "Folder does not exist", Files = null };

            var keyResult = GetEncryptionKey(folderName, password);
            if (!keyResult.Success) return new FileListResult { Success = false, Message = "Wrong password", Files = null };

            var fileIndex = GetFileIndex(folderName, keyResult.Key);
            LogHelper.Log("被访问的文件夹: " + folderName);

            return new FileListResult { Success = true, Message = "Success", Files = fileIndex };
        }
        catch (Exception ex)
        {
            LogHelper.Log("Get file list error: " + folderName + " - " + ex.Message);
            return new FileListResult { Success = false, Message = "System error: " + ex.Message, Files = null };
        }
    }

    public static DownloadResult PrepareFileDownload(string folderName, string password, string encryptedFileName, string originalFileName)
    {
        try
        {
            string folderPath = Path.Combine(BaseDataPath, folderName);
            string encryptedFilePath = Path.Combine(folderPath, encryptedFileName);

            if (!File.Exists(encryptedFilePath))
                return new DownloadResult { Success = false, Message = "File does not exist", TempFilePath = null };

            var keyResult = GetEncryptionKey(folderName, password);
            if (!keyResult.Success) return new DownloadResult { Success = false, Message = "Wrong password", TempFilePath = null };

            string tempFileName = Guid.NewGuid().ToString() + "_" + originalFileName;
            string tempFilePath = Path.Combine(TempFilePath, tempFileName);

            CryptoHelper.DecryptFile(encryptedFilePath, tempFilePath, keyResult.Key);

            LogHelper.Log("准备好下载文件: " + folderName + "/" + originalFileName);
            return new DownloadResult { Success = true, Message = "Success", TempFilePath = tempFilePath };
        }
        catch (Exception ex)
        {
            LogHelper.Log("File download preparation error: " + folderName + "/" + encryptedFileName + " - " + ex.Message);
            return new DownloadResult { Success = false, Message = "Decryption error: " + ex.Message, TempFilePath = null };
        }
    }

    public static UploadResult DeleteFile(string folderName, string password, string encryptedFileName, string originalFileName)
    {
        try
        {
            string folderPath = Path.Combine(BaseDataPath, folderName);
            string encryptedFilePath = Path.Combine(folderPath, encryptedFileName);

            if (!File.Exists(encryptedFilePath))
                return new UploadResult { Success = false, Message = "File does not exist" };

            var keyResult = GetEncryptionKey(folderName, password);
            if (!keyResult.Success) return new UploadResult { Success = false, Message = "Wrong password" };

            // 读取当前文件索引
            var fileIndex = GetFileIndex(folderName, keyResult.Key);
            
            // 从索引中移除文件
            if (fileIndex.ContainsKey(encryptedFileName))
            {
                fileIndex.Remove(encryptedFileName);
                
                // 保存更新后的索引
                SaveFileIndex(folderName, keyResult.Key, fileIndex);
                
                // 删除加密文件
                File.Delete(encryptedFilePath);
                
                LogHelper.Log("File deleted: " + folderName + "/" + originalFileName);
                return new UploadResult { Success = true, Message = "File deleted successfully" };
            }
            else
            {
                return new UploadResult { Success = false, Message = "File not found in index" };
            }
        }
        catch (Exception ex)
        {
            LogHelper.Log("File deletion error: " + folderName + "/" + encryptedFileName + " - " + ex.Message);
            return new UploadResult { Success = false, Message = "Deletion error: " + ex.Message };
        }
    }

    public static UploadResult DeleteFolder(string folderName, string password)
    {
        try
        {
            string folderPath = Path.Combine(BaseDataPath, folderName);
            
            if (!Directory.Exists(folderPath))
                return new UploadResult { Success = false, Message = "Folder does not exist" };

            var keyResult = GetEncryptionKey(folderName, password);
            if (!keyResult.Success) return new UploadResult { Success = false, Message = "Wrong password" };

            // 删除整个文件夹及其所有内容
            Directory.Delete(folderPath, true);
            
            LogHelper.Log("删除文件夹: " + folderName);
            return new UploadResult { Success = true, Message = "Folder and all files deleted successfully" };
        }
        catch (Exception ex)
        {
            LogHelper.Log("Folder deletion error: " + folderName + " - " + ex.Message);
            return new UploadResult { Success = false, Message = "Folder deletion error: " + ex.Message };
        }
    }

    private static KeyResult GetEncryptionKey(string folderName, string password)
    {
        string folderPath = Path.Combine(BaseDataPath, folderName);
        string configFile = Path.Combine(folderPath, "config.txt");

        if (!File.Exists(configFile)) return new KeyResult { Key = null, Success = false };

        string[] configParts = File.ReadAllText(configFile).Split('|');
        if (configParts.Length != 2) return new KeyResult { Key = null, Success = false };

        byte[] salt = Convert.FromBase64String(configParts[0]);
        byte[] storedKeyHash = Convert.FromBase64String(configParts[1]);

        byte[] derivedKey = CryptoHelper.DeriveKeyWithSalt(password, salt);
        byte[] derivedKeyHash = CryptoHelper.ComputeHash(derivedKey);

        if (!derivedKeyHash.SequenceEqual(storedKeyHash)) return new KeyResult { Key = null, Success = false };

        return new KeyResult { Key = derivedKey, Success = true };
    }

    private static Dictionary<string, string> GetFileIndex(string folderName, byte[] key)
    {
        string folderPath = Path.Combine(BaseDataPath, folderName);
        string indexFile = Path.Combine(folderPath, "index.enc");

        if (!File.Exists(indexFile)) return new Dictionary<string, string>();

        string tempIndexFile = Path.GetTempFileName();
        CryptoHelper.DecryptFile(indexFile, tempIndexFile, key);

        string indexJson = File.ReadAllText(tempIndexFile);
        File.Delete(tempIndexFile);

        var serializer = new JavaScriptSerializer();
        return serializer.Deserialize<Dictionary<string, string>>(indexJson) ?? new Dictionary<string, string>();
    }

    private static void SaveFileIndex(string folderName, byte[] key, Dictionary<string, string> fileIndex)
    {
        string folderPath = Path.Combine(BaseDataPath, folderName);
        
        var serializer = new JavaScriptSerializer();
        string indexJson = serializer.Serialize(fileIndex);

        string tempIndexFile = Path.GetTempFileName();
        File.WriteAllText(tempIndexFile, indexJson);

        CryptoHelper.EncryptFile(tempIndexFile, Path.Combine(folderPath, "index.enc"), key);
        File.Delete(tempIndexFile);
    }

    private static void CleanTempFiles()
    {
        try
        {
            var cutOffTime = DateTime.Now.AddMinutes(-30);
            foreach (string file in Directory.GetFiles(TempFilePath))
            {
                if (File.GetCreationTime(file) < cutOffTime)
                {
                    File.Delete(file);
                    LogHelper.Log("清理临时文件: " + Path.GetFileName(file));
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.Log("Clean temp files error: " + ex.Message);
        }
    }
}