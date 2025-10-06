using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class CryptoHelper
{
    private const int PBKDF2_ITERATIONS = 15000;
    private const int SALT_SIZE = 16;
    private const int KEY_SIZE = 32;
    private const int IV_SIZE = 16;

    // 用于返回密钥和盐的辅助类
    public class KeyAndSalt
    {
        public byte[] Key { get; set; }
        public byte[] Salt { get; set; }
    }

    /// <summary>
    /// 使用密码派生密钥
    /// </summary>
    public static KeyAndSalt DeriveKeyFromPassword(string password)
    {
        using (var rng = new RNGCryptoServiceProvider())
        {
            byte[] salt = new byte[SALT_SIZE];
            rng.GetBytes(salt);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, PBKDF2_ITERATIONS))
            {
                byte[] key = pbkdf2.GetBytes(KEY_SIZE);
                return new KeyAndSalt { Key = key, Salt = salt };
            }
        }
    }

    /// <summary>
    /// 使用已有的盐和密码重新派生密钥
    /// </summary>
    public static byte[] DeriveKeyWithSalt(string password, byte[] salt)
    {
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, PBKDF2_ITERATIONS))
        {
            return pbkdf2.GetBytes(KEY_SIZE);
        }
    }

    /// <summary>
    /// 加密文件
    /// </summary>
    public static void EncryptFile(string inputFile, string outputFile, byte[] key)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.GenerateIV();

            using (var inputStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            using (var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            {
                outputStream.Write(aes.IV, 0, aes.IV.Length);

                using (var cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    inputStream.CopyTo(cryptoStream);
                }
            }
        }
    }

    /// <summary>
    /// 解密文件
    /// </summary>
    public static void DecryptFile(string inputFile, string outputFile, byte[] key)
    {
        using (var inputStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
        using (var aes = Aes.Create())
        {
            aes.Key = key;

            byte[] iv = new byte[IV_SIZE];
            inputStream.Read(iv, 0, iv.Length);
            aes.IV = iv;

            using (var cryptoStream = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
            using (var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            {
                cryptoStream.CopyTo(outputStream);
            }
        }
    }

    /// <summary>
    /// 计算字节数组的SHA256哈希值
    /// </summary>
    public static byte[] ComputeHash(byte[] data)
    {
        using (var sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(data);
        }
    }
}