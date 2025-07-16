using JournalScrapper;
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
}