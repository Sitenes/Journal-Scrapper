using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Threading;
using System;
using System.IO;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Formats.Asn1;
using System.Xml;
using JournalScrapper.Tool.Scraping;
using JournalScrappers;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System.Text.RegularExpressions;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using JournalScrapper.Tool;


public class WebScraper : IDisposable
{
    public IWebDriver Driver;
    private static int pageCounter = 0;

    #region WebScraper
    private readonly ILogger<ExtractArticles> _logger;
    private bool isDriverInitialized;
    private const int DefaultWait = 1000;

    public WebScraper(ILogger<ExtractArticles> logger)
    {
        this._logger = logger;
        CreateDriver();
    }
    public void ClickAcceptCookiesAsync()
    {
        try
        {
            // پیدا کردن دکمه Accept cookies
            var acceptButton = Driver.FindElement(By.XPath("//button[normalize-space(text())='Accept all cookies']"));

            if (acceptButton != null && acceptButton.Displayed)
            {
                acceptButton.Click();
                Console.WriteLine("✅ دکمه Accept cookies کلیک شد.");
                return;
            }
        }
        catch (Exception ex)
        { }
    }

    public void ResolveTabligh()
    {
        ClickAcceptCookiesAsync();
        if (Driver.Url.Contains("google_vignette"))
        {
            Actions actions = new Actions(Driver);
            actions.MoveByOffset(40, 40).Click().Perform();
            Thread.Sleep(300);
        }
    }

    public int? AreStringsSimilar(List<string> string1, string string2)
    {
        int index = 0;
        double lastScore = 0;
        for (int i = 0; i < string1.Count; i++)
        {
            var words1 = string1[i].ToLower().Split(new[] { ' ', ',', '.', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            var words2 = string2.ToLower().Split(new[] { ' ', ',', '.', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

            int commonWordCount = words1.Intersect(words2, StringComparer.OrdinalIgnoreCase).Count();
            int totalWords = Math.Max(words1.Length, words2.Length);
            double similarityPercentage = (double)commonWordCount / totalWords * 100;

            if (lastScore < similarityPercentage)
            {
                index = i;
                lastScore = similarityPercentage;
            }
        }

        return lastScore < 60 ? null : index;
    }

    private ChromeOptions GetChromeOptions()
    {
        var chromeOptions = new ChromeOptions();

        //chromeOptions.AddArgument($"--user-agent={userAgent.GetRandomUserAgent()}");
        chromeOptions.AddArgument("--disable-infobars");
        chromeOptions.AddArgument("--disable-notifications");
        chromeOptions.AddArgument("--lang=en-US");
        chromeOptions.AddArgument("--disable-extensions");

        chromeOptions.AddArgument("--disable-webgl");
        chromeOptions.AddArgument("--disable-canvas-aa");

        chromeOptions.AddArgument("--disable-logging");
        chromeOptions.AddArgument("--log-level=3");

        chromeOptions.AddArgument("--start-maximized"); // اگر headless باشه، این رو بردارید یا شرطی کنید

        ////chromeOptions.AddArgument("--headless=new");
        chromeOptions.AddArgument("--accept-lang=en-US,en;q=0.9");
        chromeOptions.AddUserProfilePreference("intl.accept_languages", "en-US,en");

        //chromeOptions.AddArgument(@"user-data-dir=C:\Users\hp\AppData\Local\Google\Chrome\User Data");
        //chromeOptions.AddArgument("--profile-directory=\"محمد امین آقاکبیری\"");

        chromeOptions.AddArgument("--no-sandbox"); // ضروری برای سرور بدون UI
        chromeOptions.AddArgument("--disable-gpu"); // جلوگیری از مشکلات گرافیکی در headless
        chromeOptions.AddArgument("--disable-software-rasterizer"); // بهینه‌سازی رندرینگ
        chromeOptions.AddArgument("--remote-debugging-port=9222"); // برای دیباگ remote اگر نیاز باشه
        chromeOptions.AddArgument("--disable-dev-shm-usage"); // مفید برای جلوگیری از memory issues در محیط‌های محدود

        chromeOptions.AddArgument("--disable-blink-features=AutomationControlled");
        chromeOptions.AddExcludedArgument("enable-automation");

        return chromeOptions;
    }

    public void Log(string message, LogLevel level, string context = "", Exception? ex = null)
    {
        // اگه context داده شده باشه، به پیام اضافه می‌کنیم
        var fullMessage = string.IsNullOrWhiteSpace(context) ? message : $"[{context}] {message}";

        // لاگ بر اساس سطح
        switch (level)
        {
            case LogLevel.Trace:
                _logger.LogTrace(ex, fullMessage);
                break;
            case LogLevel.Debug:
                _logger.LogDebug(ex, fullMessage);
                break;
            case LogLevel.Information:
                _logger.LogInformation(ex, fullMessage);
                break;
            case LogLevel.Warning:
                _logger.LogWarning(ex, fullMessage);
                break;
            case LogLevel.Error:
                _logger.LogError(ex, fullMessage);
                break;
            case LogLevel.Critical:
                _logger.LogCritical(ex, fullMessage);
                break;
            default:
                _logger.Log(level, ex, fullMessage);
                break;
        }
    }

    public IWebDriver CreateDriver()
    {
        try
        {
            IWebDriver? driver = null;
            if (!isDriverInitialized || Driver == null)
            {
                var service = ChromeDriverService.CreateDefaultService();
                service.HideCommandPromptWindow = true;
                driver = new ChromeDriver(service, GetChromeOptions());
                isDriverInitialized = true;
                Log("Browser initialized", LogLevel.Information, "OpenUrl");
            }
            return driver;
        }
        catch (Exception ex)
        {
            Log($"Error CreateDriver : {ex.Message}", LogLevel.Error, "OpenUrl", ex);
            throw;
        }
    }

    public async Task OpenUrlAsync(string url, string? checkSelector = null)
    {
        try
        {
            if (!isDriverInitialized)
            {
                var service = ChromeDriverService.CreateDefaultService();
                service.HideCommandPromptWindow = true;
                Driver = new ChromeDriver(service, GetChromeOptions());
                isDriverInitialized = true;
                Log("Browser initialized", LogLevel.Information, "OpenUrl");
            }
            Driver.Navigate().GoToUrl(url);

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(4));
            try
            {
                if (!checkSelector.IsNullOrEmpty())
                    wait.Until(d => !string.IsNullOrEmpty(checkSelector) ? d.FindElement(By.CssSelector(checkSelector)) != null : true);
            }
            catch (WebDriverTimeoutException)
            {
                Log("Timeout waiting for page load", LogLevel.Warning);
                RestartDriver(url);
                // retry logic
            }
            var random = new Random();

            //int time = 0;
            while (true)
            {
                //if (time > 8)
                //{
                //    throw new Exception();
                //}
                //try
                //{
                //    if (!checkSelector.IsNullOrEmpty() && Driver.FindElement(By.CssSelector(checkSelector)) == null)
                //    {
                //        time++;
                //        Thread.Sleep(100);
                //        continue;
                //    }
                //}
                //catch
                //{
                //    time++;
                //    Thread.Sleep(100);
                //    continue;
                //}

                //for (int i = 0; i < random.Next(1, 3); i++)
                //{
                //await SimulateHumanBehaviorAsync();
                //await Task.Delay(random.Next(200, 500));
                //}

                ResolveTabligh();

                long totalHeight = (long)((IJavaScriptExecutor)Driver).ExecuteScript("return document.body.scrollHeight;");
                int steps = 5;
                long stepSize = totalHeight / steps;

                for (int i = 1; i <= steps; i++)
                {
                    long scrollPosition = stepSize * i;
                    ((IJavaScriptExecutor)Driver).ExecuteScript($"window.scrollTo(0, {scrollPosition});");
                    await Task.Delay(50);
                }

                for (int i = steps; i >= 0; i--)
                {
                    long scrollPosition = stepSize * i;
                    ((IJavaScriptExecutor)Driver).ExecuteScript($"window.scrollTo(0, {scrollPosition});");
                    await Task.Delay(50);
                }
                await Task.Delay(50);
                //Driver.Manage().Cookies.DeleteAllCookies();
                ((IJavaScriptExecutor)Driver).ExecuteScript(
    "Object.defineProperty(navigator, 'webDriver', {get: () => undefined})");
                ((IJavaScriptExecutor)Driver).ExecuteScript(
    "Object.defineProperty(navigator, 'platform', {get: () => 'Win32'})");
                break;

            }
            Log($"Navigated to {url}", LogLevel.Information, "OpenUrl");
        }
        catch (Exception ex)
        {
            Log($"Error navigating to {url}: {ex.Message}", LogLevel.Error, "OpenUrl");
            throw;
        }
    }

    private async Task SimulateHumanBehaviorAsync()
    {
        try
        {
            var actions = new Actions(Driver);
            var random = new Random();
            switch (random.Next(0, 4))
            {
                case 0:
                    var images = Driver.FindElements(By.TagName("img"));
                    if (images.Any())
                        actions.MoveToElement(images[random.Next(images.Count)]).Pause(TimeSpan.FromMilliseconds(random.Next(200, 500))).Perform();
                    break;
                case 1:
                    ((IJavaScriptExecutor)Driver).ExecuteScript($"window.scrollBy(0, {random.Next(100, 500)});");
                    await Task.Delay(random.Next(500, 1500));
                    break;
                case 2:
                    var width = Convert.ToInt32(((IJavaScriptExecutor)Driver).ExecuteScript("return window.innerWidth"));
                    var height = Convert.ToInt32(((IJavaScriptExecutor)Driver).ExecuteScript("return window.innerHeight"));
                    //	actions.MoveByOffset(random.Next(0, width), random.Next(0, height)).Pause(TimeSpan.FromMilliseconds(random.Next(100, 400))).Perform();
                    break;
                case 3:
                    var elements = Driver.FindElements(By.CssSelector("p, div"));
                    break;
            }
            Log("Human behavior simulated", LogLevel.Information, "SimulateHumanBehavior");
        }
        catch (Exception ex)
        {
            Log($"Error simulating human behavior: {ex.Message}", LogLevel.Warning, "SimulateHumanBehavior");
        }
    }

    public async Task SwitchTabAsync(int tabNumber)
    {
        try
        {
            var tabs = Driver.WindowHandles.ToList();
            if (tabNumber >= 0 && tabNumber < tabs.Count)
            {
                Driver.SwitchTo().Window(tabs[tabNumber]);
                await SimulateHumanBehaviorAsync();
                Log($"Switched to tab {tabNumber}", LogLevel.Information, "SwitchTab");
            }
        }
        catch (Exception ex)
        {
            Log($"Error switching tab: {ex.Message}", LogLevel.Error, "SwitchTab");
        }
    }

    public async Task CloseTabAsync()
    {
        try
        {
            if (Driver.WindowHandles.Count > 1)
            {
                Driver.Close();
                Driver.SwitchTo().Window(Driver.WindowHandles.Last());
                Log("Tab closed", LogLevel.Information, "CloseTab");
            }
            else
                await CloseBrowserAsync();
        }
        catch (Exception ex)
        {
            Log($"Error closing tab: {ex.Message}", LogLevel.Error, "CloseTab");
        }
    }
    private async void RestartDriver(string url)
    {
        try
        {
            Driver?.Quit();
        }
        catch { }
        isDriverInitialized = false;
        Log("Driver restarted after error", LogLevel.Warning, "RestartDriver");
        await OpenUrlAsync(url);
    }
    public async Task CloseBrowserAsync()
    {
        try
        {
            if (isDriverInitialized)
            {
                Driver.Quit();
                isDriverInitialized = false;
                Log("Browser closed", LogLevel.Information, "CloseBrowser");
            }
        }
        catch (Exception ex)
        {
            Log($"Error closing browser: {ex.Message}", LogLevel.Error, "CloseBrowser");
        }
        finally
        {
            Driver?.Dispose();
        }
    }
    public string ToIdentifierText(string input)
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
    public async Task<bool> ClickElementAsync(string selector)
    {
        try
        {
            ResolveTabligh();
            var element = Driver.FindElement(By.CssSelector(selector));
            new Actions(Driver).MoveToElement(element).Click().Perform();
            await Task.Delay(DefaultWait);
            Log($"Clicked on {selector}", LogLevel.Information, "ClickElement");
            return true;
        }
        catch (Exception ex)
        {
            try
            {
                ResolveTabligh();
                var element = Driver.FindElement(By.CssSelector(selector));
                new Actions(Driver).MoveToElement(element).Click().Perform();
                await Task.Delay(DefaultWait);
                Log($"Clicked on {selector}", LogLevel.Information, "ClickElement");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public string GetElementText(string selector)
    {
        try
        {
            ResolveTabligh();
            var text = Driver.FindElement(By.CssSelector(selector)).Text.Trim();
            //LogAsync($"Text of {selector}: {text}", LogLevel.Information, "GetElementText");
            return text;
        }
        catch (Exception ex)
        {
            try
            {
                ResolveTabligh();
                var text = Driver.FindElement(By.CssSelector(selector)).Text.Trim();
                //LogAsync($"Text of {selector}: {text}", LogLevel.Information, "GetElementText");
                return text;
            }
            catch
            {
                Log($"Error reading text for {selector}: {ex.Message}", LogLevel.Warning, "GetElementText");
            }
        }
        return "";
    }

    public async Task<string> GetElementTextAsync(IWebElement parentElement, string cssSelector)
    {
        try
        {
            ResolveTabligh();
            var element = parentElement.FindElement(By.CssSelector(cssSelector));
            return element?.Text?.Trim() ?? string.Empty;
        }
        catch (Exception ex)
        {
            try
            {
                ResolveTabligh();
                var element = parentElement.FindElement(By.CssSelector(cssSelector));
                return element?.Text?.Trim() ?? string.Empty;
            }
            catch
            {
                Log($"Error reading text for {cssSelector}: {ex.Message}", LogLevel.Warning, "GetElementText");
                return string.Empty;
            }
        }
    }

    public async Task<List<string>> GetElementsTextAsync(string selector, string attribute = null)
    {
        try
        {
            ResolveTabligh();
            var texts = attribute != null
                ? Driver.FindElements(By.CssSelector(selector)).Select(e => e.GetAttribute(attribute)).Where(t => !string.IsNullOrEmpty(t)).ToList()
                : Driver.FindElements(By.CssSelector(selector)).Select(e => e.Text).Where(t => !string.IsNullOrEmpty(t)).ToList();
            //LogAsync($"Found {texts.Count} texts for {selector}", LogLevel.Information, "GetElementsText");
            return texts;
        }
        catch (Exception ex)
        {
            try
            {
                ResolveTabligh();
                var texts = attribute != null
                    ? Driver.FindElements(By.CssSelector(selector)).Select(e => e.GetAttribute(attribute)).Where(t => !string.IsNullOrEmpty(t)).ToList()
                    : Driver.FindElements(By.CssSelector(selector)).Select(e => e.Text).Where(t => !string.IsNullOrEmpty(t)).ToList();
                //LogAsync($"Found {texts.Count} texts for {selector}", LogLevel.Information, "GetElementsText");
                return texts;
            }
            catch
            {
                Log($"Error getting texts for {selector}: {ex.Message}", LogLevel.Warning, "GetElementsText");
                return new List<string>();
            }
        }
    }
    public async Task<List<string>> GetElementsTextByXPathAsync(string selector, string attribute = null)
    {
        try
        {
            ResolveTabligh();
            var texts = attribute != null
                ? Driver.FindElements(By.XPath(selector)).Select(e => e.GetAttribute(attribute) ?? "").Where(t => !string.IsNullOrEmpty(t)).ToList()
                : Driver.FindElements(By.XPath(selector)).Select(e => e.Text).Where(t => !string.IsNullOrEmpty(t)).ToList();
            //LogAsync($"Found {texts.Count} texts for {selector}", LogLevel.Information, "GetElementsText");
            return texts;
        }
        catch (Exception ex)
        {
            try
            {
                ResolveTabligh();
                var texts = attribute != null
                    ? Driver.FindElements(By.XPath(selector)).Select(e => e.GetAttribute(attribute) ?? "").Where(t => !string.IsNullOrEmpty(t)).ToList()
                    : Driver.FindElements(By.XPath(selector)).Select(e => e.Text).Where(t => !string.IsNullOrEmpty(t)).ToList();
                //LogAsync($"Found {texts.Count} texts for {selector}", LogLevel.Information, "GetElementsText");
                return texts;
            }
            catch
            {
                Log($"Error getting texts for {selector}: {ex.Message}", LogLevel.Warning, "GetElementsText");
                return new List<string>();
            }
        }
    }
    public string? GetElementTextByXPath(string selector, string attribute = "")
    {
        try
        {
            ResolveTabligh();
            var texts = !attribute.IsNullOrEmpty()
                ? Driver.FindElement(By.XPath(selector)).GetAttribute(attribute)
                : Driver.FindElement(By.XPath(selector)).Text.Trim();
            //LogAsync($"Found {texts} text for {selector}", LogLevel.Information, "GetElementsText");
            return texts;
        }
        catch (Exception ex)
        {
            try
            {
                ResolveTabligh();
                var texts = !attribute.IsNullOrEmpty()
                ? Driver.FindElement(By.XPath(selector)).GetAttribute(attribute)
                : Driver.FindElement(By.XPath(selector)).Text.Trim();
                //LogAsync($"Found {texts} texts for {selector}", LogLevel.Information, "GetElementsText");
                return texts;
            }
            catch
            {
                Log($"Error getting texts for {selector}: {ex.Message}", LogLevel.Warning, "GetElementsText");
                return "";
            }
        }
    }
    public IWebElement? FindElementWithRetry(By by, int maxRetries = 3, int delayS = 3)
    {
        IWebElement? element = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                Wait(by, delayS);
                element = FindOneAsync(by);

                if (element != null)
                    return element;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ تلاش {attempt} ناموفق: {ex.Message}");
            }

            Driver.Navigate().Refresh();
        }

        return null;
    }
    public IWebElement Wait(By by, int delayS = 3)
    {
        ResolveTabligh();
        var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(delayS));
        return wait.Until(x => x.FindElement(by));
    }
    public IWebElement? FindOneAsync(By selector)
    {
        try
        {
            ResolveTabligh();
            var element = Driver.FindElement(selector);
            //LogAsync($"Element {selector} found", LogLevel.Information, "FindOne");
            return element;
        }
        catch
        {
            try
            {
                ResolveTabligh();
                var element = Driver.FindElement(selector);
                //LogAsync($"Element {selector} found", LogLevel.Information, "FindOne");
                return element;
            }
            catch (Exception ex)
            {
                Log($"Error finding {selector}: {ex.Message}", LogLevel.Warning, "FindOne");
            }
            return null;
        }
    }
    public IWebElement? FindOne(string selector)
    {
        try
        {
            ResolveTabligh();
            var element = Driver.FindElement(By.CssSelector(selector));
            //Log($"Element {selector} found", LogLevel.Information, "FindOne");
            return element;
        }
        catch
        {
            try
            {
                ResolveTabligh();
                var element = Driver.FindElement(By.CssSelector(selector)));
                //LogAsync($"Element {selector} found", LogLevel.Information, "FindOne");
                return element;
            }
            catch (Exception ex)
            {
                Log($"Error finding {selector}: {ex.Message}", LogLevel.Error, "FindOne");
                return null;
            }
        }
    }
    public IWebElement? FindOneWithin(IWebElement parent, string selector)
    {
        try
        {
            ResolveTabligh();
            var element = parent.FindElement(By.CssSelector(selector));
            Log($"Element {selector} found inside parent", LogLevel.Information, "FindOneWithin");
            return element;
        }
        catch (Exception ex)
        {
            Log($"Error finding element {selector} inside parent: {ex.Message}", LogLevel.Error, "FindOneWithin");
            return null;
        }
    }

    public async Task<List<IWebElement>> FindManyWithinAsync(IWebElement parent, string selector)
    {
        try
        {
            ResolveTabligh();
            var elements = parent.FindElements(By.CssSelector(selector)).ToList();
            Log($"Found {elements.Count} elements for {selector} inside parent", LogLevel.Information, "FindManyWithin");
            return elements;
        }
        catch (Exception ex)
        {
            try
            {
                ResolveTabligh();
                var elements = parent.FindElements(By.CssSelector(selector)).ToList();
                Log($"Found {elements.Count} elements for {selector} inside parent", LogLevel.Information, "FindManyWithin");
                return elements;
            }
            catch
            {
                Log($"Error finding elements for {selector} inside parent: {ex.Message}", LogLevel.Error, "FindManyWithin");
                return new List<IWebElement>();
            }
        }
    }

    public async Task<List<IWebElement>> FindManyAsync(string selector)
    {
        try
        {
            ResolveTabligh();
            var elements = Driver.FindElements(By.CssSelector(selector)).ToList();
            Log($"Found {elements.Count} elements for {selector}", LogLevel.Information, "FindMany");
            return elements;
        }
        catch (Exception ex)
        {
            try
            {
                ResolveTabligh();
                var elements = Driver.FindElements(By.CssSelector(selector)).ToList();
                Log($"Found {elements.Count} elements for {selector}", LogLevel.Information, "FindMany");
                return elements;
            }
            catch
            {
                Log($"Error finding elements for {selector}: {ex.Message}", LogLevel.Error, "FindMany");
                return new List<IWebElement>();
            }
        }
    }

    public async Task<Dictionary<string, IWebElement>> FindWithLabelAsync(string elementSelector, string labelSelector)
    {
        try
        {
            ResolveTabligh();
            var element = Driver.FindElement(By.CssSelector(elementSelector));
            var label = Driver.FindElement(By.CssSelector(labelSelector));
            var result = new Dictionary<string, IWebElement> { { label.Text.Trim(), element } };
            Log($"Element with label {label.Text} found", LogLevel.Information, "FindWithLabel");
            return result;
        }
        catch (Exception ex)
        {
            Log($"Error finding element with label: {ex.Message}", LogLevel.Error, "FindWithLabel");
            return new Dictionary<string, IWebElement>();
        }
    }

    public async Task<List<Dictionary<string, IWebElement>>> FindManyWithLabelsAsync(string elementSelector, string labelSelector)
    {
        try
        {
            ResolveTabligh();
            var elements = Driver.FindElements(By.CssSelector(elementSelector));
            var labels = Driver.FindElements(By.CssSelector(labelSelector));
            var result = new List<Dictionary<string, IWebElement>>();
            for (int i = 0; i < Math.Min(elements.Count, labels.Count); i++)
            {
                result.Add(new Dictionary<string, IWebElement> { { labels[i].Text.Trim(), elements[i] } });
            }
            Log($"Found {result.Count} elements with labels", LogLevel.Information, "FindManyWithLabels");
            return result;
        }
        catch (Exception ex)
        {
            Log($"Error finding elements with labels: {ex.Message}", LogLevel.Error, "FindManyWithLabels");
            return new List<Dictionary<string, IWebElement>>();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        CloseBrowserAsync().GetAwaiter().GetResult();
    }
    #endregion

    public string GetPageContent(string url)
    {
        // Random random = new Random();
        // int randomWait = random.Next(500, 3000); // Adjust the bounds

        if (url.Contains(".pdf") || url.Contains(".rar") || url.Contains(".zip") || url.Contains(".txt"))
        {
            // Console.WriteLine("Site is file");
            return "";
        }

        if (url.Contains("semanticscholar.org") || url.Contains("sciencedirect.com") || url.Contains("researchgate.net") ||
            url.Contains("link.springer.com") || url.Contains("sid.ir") || url.Contains("magiran.com") ||
            url.Contains("scholar.google.com/scholar?cluster") || url.Contains("ieeexplore.ieee.org"))
        {
            // Console.WriteLine("Skipping site");
            return "";
        }

        if (Driver == null)
        {
            ChromeOptions options = new ChromeOptions();
            options.AddUserProfilePreference("download_restrictions", 3);
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("--ignore-ssl-errors");
            options.AddArgument("--allow-insecure-localhost");
            options.AddArgument("--disable-web-security");
            options.AddArgument("--allow-running-insecure-content");
            // options.AddArgument("--remote-debugging-port=" + DEBUGGING_PORT);
            options.AddArgument("--remote-allow-origins=*");
            options.AddArgument("--disable-search-engine-choice-screen");
            options.AddArgument("--disk-cache-size=0");
            options.AddArgument("--disable-application-cache");
            options.AddArgument("user-agent=" + UserAgents.GetRandomUserAgent());
            // options.AddArgument("Origin=" + origin);
            // options.AddArgument("Host=" + host);
            // options.AddArgument("Referer=" + url);
            options.AddArgument("Accept=*/*");
            options.AddArgument("Accept-Encoding=gzip, deflate, br");
            // options.AddArgument("Accept-Language=en-US,en;q=0.9");
            options.AddArgument("Accept-Charset=ISO-8859-1,utf-8;q=0.7,*;q=0.3");
            options.AddArgument("Connection=keep-alive");
            options.AddArgument("X-user-agent=" + UserAgents.GetRandomUserAgent());
            options.AddArgument("Content-Type=application/json");
            options.AddArgument("Pragma=no-cache");
            options.AddArgument("Cache-Control=no-cache");
            // options.AddArgument("disable-infobars");
            // options.AddArgument("user-data-dir=" + chromeProfile);
            // options.AddArgument("profile-directory=Default");
            options.AddArgument("--disable-notifications");

            #region Headless
            //options.AddArgument("--headless=new");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--window-size=1920,1080");
            options.AddAdditionalOption("useAutomationExtension", false);
            options.AddArgument("--disable-blink-features=AutomationControlled");
            #endregion

            options.PageLoadStrategy = PageLoadStrategy.Eager;

            try
            {
                // string killCommand = "taskkill /F /IM chrome.exe /T";
                // Runtime.getRuntime().exec(killCommand);
            }
            catch (Exception e) { }

            string extraFolderPath = FileTools.FindDirectoryInParents();
            //string chromeDriverPath = Path.Combine(extraFolderPath, "chromeDriver.exe");
            Driver = new ChromeDriver(options);
            Driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(7);
        }

        try
        {
            if (pageCounter >= 100 || Driver == null || Driver.WindowHandles.Count == 0)
            {
                Driver?.Quit();
                Driver?.Dispose();
                Driver = null;
                pageCounter = 0;
                return GetPageContent(url);
            }
            pageCounter++;

            UriBuilder uriBuilder = new UriBuilder(url);
            uriBuilder.Scheme = "http";
            uriBuilder.Port = -1;  // حذف پورت پیش‌فرض
            url = uriBuilder.ToString();

            Driver!.Navigate().GoToUrl(url);
            try
            {
                Driver.Manage().Window.Maximize(); // ابتدا maximize کنیم
                IJavaScriptExecutor js = (IJavaScriptExecutor)Driver;
                js.ExecuteScript("document.body.style.zoom='50%'");
                js.ExecuteScript("document.body.style.transform='scale(0.5)'");
                js.ExecuteScript("document.body.style.transformOrigin='0 0'");
                Task.Delay(100).Wait();
            }
            catch (Exception jsEx)
            {
                Console.WriteLine("Could not set zoom: " + jsEx.Message);
            }
            return Driver.PageSource;
        }
        catch (Exception e)
        {
            try
            {
                UriBuilder uriBuilder = new UriBuilder(url);
                uriBuilder.Scheme = "https";
                uriBuilder.Port = -1;
                url = uriBuilder.ToString();

                Driver!.Navigate().GoToUrl(url);
                try
                {
                    Driver.Manage().Window.Maximize(); // ابتدا maximize کنیم
                    IJavaScriptExecutor js = (IJavaScriptExecutor)Driver;
                    js.ExecuteScript("document.body.style.transform='scale(0.25)'");
                    js.ExecuteScript("document.body.style.transformOrigin='0 0'");
                }
                catch (Exception jsEx)
                {
                    Console.WriteLine("Could not set zoom: " + jsEx.Message);
                }
                return Driver.PageSource;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Site cannot load: " + url + " - " + ex.Message);
            }
        }

        // long startTime = System.currentTimeMillis();
        // long maxLoadTime = 10000; // 10 ثانیه

        // WebDriverWait wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(1));
        // wait.Until(d =>
        // {
        //     IWebElement element = d.FindElement(By.TagName("body"));
        //     return element != null;
        // });

        // Thread.Sleep(randomWait);

        return "";
    }

    public string GetFullURL(string subUrl, string fullUrl, bool absolute = false)
    {
        var url = new Uri(fullUrl);

        if (!absolute)
        {
            Uri absoluteUrl = new Uri(url, subUrl);
            return absoluteUrl.ToString();
        }

        if (subUrl.StartsWith("/"))
            subUrl = subUrl.Substring(1);

        if (!subUrl.Contains(url.Host))
            subUrl = url.Host + "/" + subUrl;

        if (!subUrl.Contains("http"))
            subUrl = url.Scheme + "://" + subUrl;

        return subUrl;
    }
    public void WriteFailedCsv(string content = "", Exception? ex = null)
    {
        try
        {
            //var needHeader = !File.Exists(failedFile) || new FileInfo(failedFile).Length == 0;
            var failedFile = Path.Combine(FileTools.FindDirectoryInParents(), "Failed.csv");
            if (!File.Exists(failedFile))
                File.Create(failedFile).Dispose();

            var writer = new StreamWriter(failedFile, append: true, Encoding.UTF8);

            using (var csvWriter = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                //if (needHeader)
                //{
                //    csvWriter.WriteField("Id");
                //    csvWriter.WriteField("Title");
                //    csvWriter.WriteField("Writer Name");
                //    csvWriter.WriteField("Article Link");
                //    csvWriter.WriteField("Exception");
                //    csvWriter.NextRecord();
                //}


                csvWriter.WriteField(content);
                if (ex != null)
                    csvWriter.WriteField(ex.ToString());
                csvWriter.NextRecord();
            }
        }
        catch (IOException e)
        {
            Console.WriteLine(e.ToString());
        }
    }
}
