using JournalScrapper;
using Microsoft.IdentityModel.Tokens;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
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
    public static IList<IWebElement>? FindElementsSafe(this IWebElement element, By by)
    {
        try
        {
            return element.FindElements(by);
        }
        catch (NoSuchElementException)
        {
            try
            {
                Thread.Sleep(300);
                return element.FindElements(by);
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }
    }
    public static IWebElement? FindElementSafe(this IWebElement element, By by)
    {
        try
        {
            return element.FindElement(by);
        }
        catch (NoSuchElementException)
        {
            try
            {
                Thread.Sleep(500);
                return element.FindElement(by);
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }
    }
    public static IList<IWebElement>? FindElementsSafe(this IWebDriver element, By by)
    {
        try
        {
            return element.FindElements(by);
        }
        catch (NoSuchElementException)
        {
            try
            {
                Thread.Sleep(300);
                return element.FindElements(by);
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }
    }
    public static IWebElement? FindElementSafe(this IWebDriver element, By by)
    {
        try
        {
            return element.FindElement(by);
        }
        catch (NoSuchElementException)
        {
            try
            {
                Thread.Sleep(300);
                return element.FindElement(by);
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }
    }
    public static IWebDriver NavigateWithScrollAndZoom(this IWebDriver _webDriver, string url)
    {
        _webDriver.Navigate().GoToUrl(url);
        ((IJavaScriptExecutor)_webDriver).ExecuteScript("document.body.style.zoom='50%';");
        Thread.Sleep(200);
        //((IJavaScriptExecutor)_webDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
        //Thread.Sleep(500);
        //((IJavaScriptExecutor)_webDriver).ExecuteScript("window.scrollTo(0, 0);");
        //Thread.Sleep(500);
        //((IJavaScriptExecutor)_webDriver).ExecuteScript("document.body.style.zoom='50%';");
        //Thread.Sleep(200);
        return _webDriver;
    }

    public static IWebElement ScrollToElement(this IWebDriver _webDriver, IWebElement element)
    {
        ((IJavaScriptExecutor)_webDriver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
        Thread.Sleep(100);
        return element;
    }
    public static void WaitUntilElementDisplayed(this IWebDriver driver, By by, int appearTimeoutSeconds = 10)
    {
        // صبر کن تا المنت ظاهر شود
        var appearWait = new WebDriverWait(driver, TimeSpan.FromSeconds(appearTimeoutSeconds));
        appearWait.Until(d => d.FindElements(by).Count > 0);
    }
    public static void WaitUntilTextDisplayed(this IWebDriver driver, By by, int appearTimeoutSeconds = 10)
    {
        // صبر کن تا المنت ظاهر شود
        var appearWait = new WebDriverWait(driver, TimeSpan.FromSeconds(appearTimeoutSeconds));
        appearWait.Until(d => !d.FindElement(by).Text.IsNullOrEmpty());
    }
}