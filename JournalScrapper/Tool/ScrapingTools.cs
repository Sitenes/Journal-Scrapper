using JournalScrapper;
using OpenQA.Selenium;
using System.Net;
using System.Text;

public static class ScrapingTool
{
    public static string GetElementValueSafe(this IWebElement? element)
    {
        if (element == null)
            return string.Empty;

        try
        {
            var text = element.GetAttribute("innerText") ?? element.Text;
            return text.Trim().Replace("ي", "ی").Replace("ك", "ک");
        }
        catch (Exception) { }

        return string.Empty;
    }
}