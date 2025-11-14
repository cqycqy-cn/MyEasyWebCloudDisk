﻿// -*- coding: utf-8 -*-
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public static class RegCodeHelper
{
    private static readonly string CodesPath = @"C:\Users\Administrator\Desktop\myclouddat\codes\";
    private static readonly string AvailableFile = Path.Combine(CodesPath, "available_codes.txt");
    private static readonly string UsedFile = Path.Combine(CodesPath, "used_codes.txt");

    static RegCodeHelper()
    {
        try
        {
            if (!Directory.Exists(CodesPath)) 
                Directory.CreateDirectory(CodesPath);
            
            // 修复：使用 File.AppendAllText 而不是 File.Create，避免文件被锁定
            if (!File.Exists(AvailableFile)) 
                File.WriteAllText(AvailableFile, "");
            
            if (!File.Exists(UsedFile)) 
                File.WriteAllText(UsedFile, "");
        }
        catch (Exception ex)
        {
            LogHelper.Log("RegCodeHelper 初始化错误: " + ex.Message);
        }
    }

    // 定义返回结果的类
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
    }

    public static ValidationResult ValidateRegCode(string regCode)
    {
        try
        {
            // 检查格式：3大写字母 + 6数字
            if (!Regex.IsMatch(regCode, @"^[A-Z]{3}\d{6}$"))
            {
                return new ValidationResult { IsValid = false, Message = "注册码格式错误，应为3位大写字母+6位数字（如：ABC123456）" };
            }

            // 检查频率限制
            if (!RateLimitHelper.CheckRateLimit("regcode_validate", 10, 60))
            {
                return new ValidationResult { IsValid = false, Message = "验证尝试次数过多，请1小时后再试" };
            }

            // 检查是否在可用列表中
            var availableCodes = File.ReadAllLines(AvailableFile)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Trim())
                .ToList();

            if (!availableCodes.Contains(regCode))
            {
                return new ValidationResult { IsValid = false, Message = "注册码不存在" };
            }

            // 检查是否已被使用
            if (!File.Exists(UsedFile))
            {
                File.WriteAllText(UsedFile, "");
            }

            var usedCodes = File.ReadAllLines(UsedFile)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Split('|')[0].Trim()) // 格式：注册码|使用时间|群组名
                .ToList();

            if (usedCodes.Contains(regCode))
            {
                return new ValidationResult { IsValid = false, Message = "注册码已被使用" };
            }

            return new ValidationResult { IsValid = true, Message = "注册码有效" };
        }
        catch (Exception ex)
        {
            LogHelper.Log("Reg code validation error: " + regCode + " - " + ex.Message);
            return new ValidationResult { IsValid = false, Message = "系统错误，请稍后重试" };
        }
    }

    public static bool MarkRegCodeUsed(string regCode, string groupName)
    {
        try
        {
            // 确保文件存在
            if (!File.Exists(UsedFile))
            {
                File.WriteAllText(UsedFile, "");
            }

            // 从可用文件移除
            if (File.Exists(AvailableFile))
            {
                var availableCodes = File.ReadAllLines(AvailableFile)
                    .Where(line => !string.IsNullOrWhiteSpace(line) && line.Trim() != regCode)
                    .ToList();
                File.WriteAllLines(AvailableFile, availableCodes);
            }

            // 添加到已使用文件
            string usedEntry = string.Format("{0}|{1:yyyy-MM-dd HH:mm:ss}|{2}", regCode, DateTime.Now, groupName);
            File.AppendAllText(UsedFile, usedEntry + Environment.NewLine);

            LogHelper.Log("注册码: " + regCode + " 被用于创建群组: " + groupName);
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Log("注册代码使用错误: " + regCode + " - " + ex.Message);
            return false;
        }
    }
}