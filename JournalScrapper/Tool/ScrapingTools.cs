using JournalScrapper;
using OpenQA.Selenium;
using System.Net;
using System.Text;

public static class ScrapingTool
{
    public static string GetElementValueSafe(this IWebElement? element)
    {
        return element?.Text?.Trim().Replace("ي", "ی").Replace("ك", "ک") ?? string.Empty;
    }
}