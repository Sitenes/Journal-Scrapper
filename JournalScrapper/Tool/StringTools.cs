using JournalScrappers;
using Microsoft.IdentityModel.Tokens;
using OpenQA.Selenium;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

public static class StringTool
{
    public static int? ToInt(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var match = Regex.Match(input, @"\d+");
        if (match.Success && int.TryParse(match.Value, out int result))
            return result;

        return null;
    }
    public static long ToLong(this string input)
    {
        return Convert.ToInt64(input.IsNullOrEmpty() ? 0 : new string(input.Where(c => char.IsDigit(c)).ToArray()));
    }
    public static string RemoveNonLettersWithSpace(this string input)
    {
        return (new string(input.Where(c => char.IsLetter(c) || c == ' ').ToArray())).Trim().ToLower();
    }
    public static bool? ContainsPersianCharacters(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        string persianPattern = @"[\u0600-\u06FF\uFB8A\uFB8B\uFB8C\uFB8D\uFB8E\uFB8F\uFB90-\uFBFF]";
        string englishPattern = @"[a-zA-Z]";

        int persianCount = Regex.Matches(input, persianPattern).Count;
        int englishCount = Regex.Matches(input, englishPattern).Count;

        // اگر تعداد کاراکترهای فارسی بیشتر باشد true بازگردانده می‌شود و در غیر این صورت false
        return persianCount > englishCount;
    }
    public static double CalculateSimilarity(this string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0.0;

        int[,] matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++) matrix[i, 0] = i;
        for (int j = 0; j <= s2.Length; j++) matrix[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
            for (int j = 1; j <= s2.Length; j++)
            {
                int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }

        int maxLength = Math.Max(s1.Length, s2.Length);
        return 1.0 - (double)matrix[s1.Length, s2.Length] / maxLength;
    }

    // Normalize text for comparison (handles Persian and English)
    public static string NormalizeText(this string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        // Remove diacritics and normalize Persian/Arabic characters
        text = Regex.Replace(text, @"[\u064B-\u065F\u0670]", ""); // Remove Arabic diacritics
        text = text.Replace("ي", "ی").Replace("ك", "ک"); // Normalize Persian characters
        return text.Trim().ToLower().ToLowerInvariant();
    }

    public static string ToIdentifierText(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // 1. فقط حروف فارسی، انگلیسی و فاصله مجاز هستند
        string cleaned = Regex.Replace(input, @"[^آ-یa-zA-Z\s]", " ");

        // 2. حذف فاصله‌های پشت سر هم (بیش از یکی)
        cleaned = Regex.Replace(cleaned, @"\s{2,}", " ");

        // 3. جایگزینی تمام فاصله‌ها با "-"
        cleaned = cleaned.Trim(); // حذف فاصله از ابتدا و انتها
        cleaned = cleaned.Replace(" ", "-");

        return cleaned;
    }

}