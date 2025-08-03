using JournalScrappers;
using Microsoft.IdentityModel.Tokens;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Serilog;
using System.Net;
using System.Text;
using System.Threading;

public static class ScrapingTool
{
    public static string GetElementValueSafe(this IWebElement? element)
    {
        if (element == null)
        {
            Log.Warning("GetElementValueSafe called with null IWebElement.");
            return string.Empty;
        }

        try
        {
            var text = element.GetAttribute("innerText") ?? element.Text;
            var cleanText = text.Trim().Replace("ي", "ی").Replace("ك", "ک");
            return cleanText;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "GetElementValueSafe ابتدا خطا داشت، تلاش مجدد با تاخیر");
            Thread.Sleep(300);

            try
            {
                var text = element.GetAttribute("innerText") ?? element.Text;
                var cleanText = text.Trim().Replace("ي", "ی").Replace("ك", "ک");
                Log.Information("GetElementValueSafe تلاش دوم موفق بود. مقدار: {Text}", cleanText);
                return cleanText;
            }
            catch (Exception retryEx)
            {
                Log.Error(retryEx, "GetElementValueSafe تلاش دوم هم شکست خورد.");
                return string.Empty;
            }
        }
    }

    public static IList<IWebElement>? FindElementsSafe(this IWebElement element, By by)
    {
        try
        {
            var elements = element.FindElements(by);
            return elements;
        }
        catch (NoSuchElementException)
        {
            Log.Warning("FindElementsSafe اولین تلاش ناموفق بود، تلاش مجدد با تاخیر. Selector: {Selector}", by);
            try
            {
                Thread.Sleep(300);
                var elements = element.FindElements(by);
                Log.Information("FindElementsSafe تلاش دوم موفق بود. تعداد المنت‌ها: {Count}, Selector: {Selector}", elements.Count, by);
                return elements;
            }
            catch (NoSuchElementException ex)
            {
                Log.Error(ex, "FindElementsSafe تلاش دوم هم ناموفق بود. Selector: {Selector}", by);
                return null;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "FindElementsSafe با استثنا مواجه شد. Selector: {Selector}", by);
            return null;
        }
    }

    public static IWebElement? FindElementSafe(this IWebElement element, By by)
    {
        try
        {
            var found = element.FindElement(by);
            return found;
        }
        catch (NoSuchElementException)
        {
            Log.Warning("FindElementSafe اولین تلاش ناموفق بود، تلاش مجدد با تاخیر. Selector: {Selector}", by);
            try
            {
                Thread.Sleep(500);
                var found = element.FindElement(by);
                Log.Information("FindElementSafe تلاش دوم موفق بود. Selector: {Selector}", by);
                return found;
            }
            catch (NoSuchElementException ex)
            {
                Log.Error(ex, "FindElementSafe تلاش دوم هم ناموفق بود. Selector: {Selector}", by);
                return null;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "FindElementSafe با استثنا مواجه شد. Selector: {Selector}", by);
            return null;
        }
    }

    public static IList<IWebElement>? FindElementsSafe(this IWebDriver driver, By by)
    {
        try
        {
            var elements = driver.FindElements(by);
            return elements;
        }
        catch (NoSuchElementException)
        {
            Log.Warning("Driver.FindElementsSafe اولین تلاش ناموفق بود، تلاش مجدد با تاخیر. Selector: {Selector}", by);
            try
            {
                Thread.Sleep(300);
                var elements = driver.FindElements(by);
                Log.Information("Driver.FindElementsSafe تلاش دوم موفق بود. تعداد المنت‌ها: {Count}, Selector: {Selector}", elements.Count, by);
                return elements;
            }
            catch (NoSuchElementException ex)
            {
                Log.Error(ex, "Driver.FindElementsSafe تلاش دوم هم ناموفق بود. Selector: {Selector}", by);
                return null;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Driver.FindElementsSafe با استثنا مواجه شد. Selector: {Selector}", by);
            return null;
        }
    }

    public static IWebElement? FindElementSafe(this IWebDriver driver, By by)
    {
        try
        {
            var element = driver.FindElement(by);
            return element;
        }
        catch (NoSuchElementException)
        {
            Log.Warning("Driver.FindElementSafe اولین تلاش ناموفق بود، تلاش مجدد با تاخیر. Selector: {Selector}", by);
            try
            {
                Thread.Sleep(300);
                var element = driver.FindElement(by);
                Log.Information("Driver.FindElementSafe تلاش دوم موفق بود. Selector: {Selector}", by);
                return element;
            }
            catch (NoSuchElementException ex)
            {
                Log.Error(ex, "Driver.FindElementSafe تلاش دوم هم ناموفق بود. Selector: {Selector}", by);
                return null;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Driver.FindElementSafe با استثنا مواجه شد. Selector: {Selector}", by);
            return null;
        }
    }

    public static IWebDriver NavigateWithScrollAndZoom(this IWebDriver driver, string url)
    {
        try
        {
            driver.Navigate().GoToUrl(url);
            ((IJavaScriptExecutor)driver).ExecuteScript("document.body.style.zoom='50%';");
            Thread.Sleep(200);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "NavigateWithScrollAndZoom با خطا مواجه شد. آدرس: {Url}", url);
            throw; // اگر بخواهی خطا را به بیرون هم منتقل کنی
        }

        return driver;
    }

    public static IWebElement ScrollToElement(this IWebDriver driver, IWebElement element)
    {
        try
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
            Thread.Sleep(10);
            return element;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ScrollToElement با خطا مواجه شد.");
            throw;
        }
    }

    public static void WaitUntilElementDisplayed(this IWebDriver driver, By by, int appearTimeoutSeconds = 10)
    {
        try
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(appearTimeoutSeconds));
            wait.Until(d => d.FindElements(by).Count > 0);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "WaitUntilElementDisplayed با خطا مواجه شد. Selector: {Selector}", by);
            Thread.Sleep(1000);
        }
    }

    public static void WaitUntilTextDisplayed(this IWebDriver driver, By by, int appearTimeoutSeconds = 10)
    {
        try
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(appearTimeoutSeconds));
            wait.Until(d => !d.FindElement(by).Text.IsNullOrEmpty());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "WaitUntilTextDisplayed با خطا مواجه شد. Selector: {Selector}", by);
            Thread.Sleep(1000);
        }
    }
}
