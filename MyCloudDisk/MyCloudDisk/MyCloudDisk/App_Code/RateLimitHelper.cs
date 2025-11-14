// -*- coding: utf-8 -*-
using System;
using System.IO;
using System.Linq;
using System.Web;

public static class RateLimitHelper
{
    private static readonly string RateLimitPath = @"C:\Users\Administrator\Desktop\myclouddat\ratelimit\";

    static RateLimitHelper()
    {
        if (!Directory.Exists(RateLimitPath)) Directory.CreateDirectory(RateLimitPath);
    }

    public static bool CheckRateLimit(string operationType, int maxAttempts, int minutes)
    {
        try
        {
            string clientIP = GetClientIP();
            string fileName = string.Format("{0}_{1}.txt", operationType, clientIP.Replace(":", "_"));
            string filePath = Path.Combine(RateLimitPath, fileName);

            // 读取现有记录
            var attempts = File.Exists(filePath) ? 
                File.ReadAllLines(filePath).Select(line => DateTime.Parse(line)).ToList() : 
                new System.Collections.Generic.List<DateTime>();

            // 移除过期的记录
            var cutoffTime = DateTime.Now.AddMinutes(-minutes);
            attempts = attempts.Where(time => time > cutoffTime).ToList();

            // 检查是否超过限制
            if (attempts.Count >= maxAttempts)
            {
                return false;
            }

            // 添加新记录
            attempts.Add(DateTime.Now);
            File.WriteAllLines(filePath, attempts.Select(time => time.ToString("yyyy-MM-dd HH:mm:ss")));

            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Log("Rate limit error: " + operationType + " - " + ex.Message);
            return true; // 出错时不限制，避免影响正常用户
        }
    }

    private static string GetClientIP()
    {
        string ip = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
        if (string.IsNullOrEmpty(ip))
        {
            ip = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
        }
        return ip ?? "unknown";
    }

    public static void CleanupOldRecords()
    {
        try
        {
            var cutoffTime = DateTime.Now.AddHours(-2); // 清理2小时前的记录
            foreach (string file in Directory.GetFiles(RateLimitPath))
            {
                if (File.GetLastWriteTime(file) < cutoffTime)
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.Log("Rate limit cleanup error: " + ex.Message);
        }
    }
}