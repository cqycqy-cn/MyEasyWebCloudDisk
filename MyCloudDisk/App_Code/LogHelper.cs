// -*- coding: utf-8 -*-
using System;
using System.IO;

public static class LogHelper
{
    private static readonly string LogPath = @"C:\Users\Administrator\Desktop\myclouddat\operation_log.txt";

    public static void Log(string message)
    {
        try
        {
            string logEntry = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - " + message + Environment.NewLine;
            File.AppendAllText(LogPath, logEntry);
        }
        catch
        {
            // Ignore log errors
        }
    }

    public static void LogGroupOperation(string operation, string groupName, string details = "")
    {
        string message = "群组操作 [" + operation + "] - 群组: " + groupName;
        if (!string.IsNullOrEmpty(details))
        {
            message += " - " + details;
        }
        Log(message);
    }

    public static void LogRegCodeOperation(string operation, string regCode, string details = "")
    {
        string message = "注册码操作 [" + operation + "] - 注册码: " + regCode;
        if (!string.IsNullOrEmpty(details))
        {
            message += " - " + details;
        }
        Log(message);
    }

    public static void LogRateLimit(string operation, string clientIP, string details = "")
    {
        string message = "频率限制 [" + operation + "] - IP: " + clientIP;
        if (!string.IsNullOrEmpty(details))
        {
            message += " - " + details;
        }
        Log(message);
    }
}