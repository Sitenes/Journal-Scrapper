using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System.Text.RegularExpressions;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using System.Diagnostics;
using JournalScrapper.Tool.Scraping;
using JournalScrapper.Tool;
using JournalScrappers;
using AngleSharp.Dom;

public class WebScraper : IDisposable
{
    public IWebDriver Driver;
    private static int pageCounter = 0;
    private ChromeDriverService _service;
    private readonly ILogger<ExtractArticles> _logger;
    private bool isDriverInitialized;
    private const int DefaultWait = 1000;

    public WebScraper(ILogger<ExtractArticles> logger)
    {
        this._logger = logger;
        Driver = CreateDriver();
    }

    public void ClickAcceptCookiesAsync()
    {
        try
        {
            // Find Accept cookies button
            var acceptButton = Driver.FindElement(By.XPath("//button[normalize-space(text())='Accept all cookies']"));

            if (acceptButton != null && acceptButton.Displayed)
            {
                acceptButton.Click();
                Log("Accept cookies button clicked.", LogLevel.Information, "ClickAcceptCookiesAsync");
                return;
            }
        }
        catch (Exception ex)
        {
        }
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
        //chromeOptions.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        //chromeOptions.AddArgument($"--user-agent={userAgent.GetRandomUserAgent()}");
        chromeOptions.AddArgument("--disable-infobars");
        chromeOptions.AddArgument("--disable-notifications");
        chromeOptions.AddArgument("--lang=en-US");
        chromeOptions.AddArgument("--disable-extensions");

        chromeOptions.AddArgument("--disable-webgl");
        chromeOptions.AddArgument("--disable-canvas-aa");

        chromeOptions.AddArgument("--disable-logging");
        chromeOptions.AddArgument("--log-level=3");

        //chromeOptions.AddArgument("--start-maximized"); // Remove if headless

        //chromeOptions.AddArgument("--headless=new");
        chromeOptions.AddArgument("--accept-lang=en-US,en;q=0.9");
        chromeOptions.AddUserProfilePreference("intl.accept_languages", "en-US,en");
        chromeOptions.AddArgument("--force-device-scale-factor=0.7");

        //chromeOptions.AddArgument(@"user-data-dir=C:\Users\hp\AppData\Local\Google\Chrome\User Data");
        //chromeOptions.AddArgument("--profile-directory=\"محمد امین آقاکبیری\"");

        chromeOptions.AddArgument("--no-sandbox"); // Essential for UI-less servers
        chromeOptions.AddArgument("--disable-gpu"); // Prevent graphical issues in headless
        chromeOptions.AddArgument("--disable-software-rasterizer"); // Optimize rendering
        chromeOptions.AddArgument("--remote-debugging-port=9209"); // For remote debugging if needed
        chromeOptions.AddArgument("--disable-dev-shm-usage"); // Useful to prevent memory issues in limited environments

        chromeOptions.AddArgument("--disable-blink-features=AutomationControlled");
        chromeOptions.AddExcludedArgument("enable-automation");

        return chromeOptions;
    }

    public void Log(string message, LogLevel level, string context, Exception? ex = null)
    {
        // Add context to message if provided
        var fullMessage = string.IsNullOrWhiteSpace(context) ? message : $"[{context}] {message}";
        _logger.Log(level, ex, fullMessage);
    }
    public void Log(string message, LogLevel level, Exception? ex = null)
    {
        Log(message, level, ex?.Message ?? "", ex);
    }
    public void Log(LogLevel level, Exception? ex = null)
    {
        Log(ex?.Message ?? "", level, ex);
    }
    public void Log(Exception? ex = null)
    {
        Log(LogLevel.Error, ex);
    }
    public IWebDriver CreateDriver()
    {
        try
        {
            IWebDriver? driver = null;
            if (!isDriverInitialized || Driver == null)
            {
                _service = ChromeDriverService.CreateDefaultService();
                _service.HideCommandPromptWindow = true;
                driver = new ChromeDriver(_service, GetChromeOptions());
                isDriverInitialized = true;
                Log("Browser initialized", LogLevel.Information, "CreateDriver");
            }

            return driver;
        }
        catch (Exception ex)
        {
            Log($"Error creating driver: {ex.Message}", LogLevel.Error, "CreateDriver", ex);
            throw;
        }
    }

    public async Task OpenUrlAsync(string url, string? checkSelector = null)
    {
        try
        {
            if (!isDriverInitialized)
            {
                Driver = CreateDriver();
                Log("Browser initialized", LogLevel.Information, "OpenUrlAsync");
            }
            Driver.Navigate().GoToUrl(url);

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(4));
            try
            {
                if (!checkSelector.IsNullOrEmpty())
                    wait.Until(d => !string.IsNullOrEmpty(checkSelector) ? d.FindElement(By.CssSelector(checkSelector)) != null : true);
            }
            catch (WebDriverTimeoutException ex)
            {
                Log($"Timeout waiting for page load at URL: {url}", LogLevel.Warning, "OpenUrlAsync", ex);
                await OpenUrlAsync(url);
                return;
            }
            var random = new Random();

            while (true)
            {
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

                //((IJavaScriptExecutor)Driver).ExecuteScript($"document.body.style.zoom='40%'");
                break;

            }
            Log($"Navigated to URL: {url}", LogLevel.Information, "OpenUrlAsync");
        }
        catch (Exception ex)
        {
            Log($"Error navigating to URL: {url}, Selector: {checkSelector ?? "None"} - {ex.Message}", LogLevel.Error, "OpenUrlAsync", ex);
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
        }
        catch (Exception ex)
        {
            Log($"Error simulating human behavior: {ex.Message}", LogLevel.Warning, "SimulateHumanBehaviorAsync", ex);
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
                Log($"Switched to tab {tabNumber}", LogLevel.Information, "SwitchTabAsync");
            }
        }
        catch (Exception ex)
        {
            Log($"Error switching to tab {tabNumber}: {ex.Message}", LogLevel.Error, "SwitchTabAsync", ex);
        }
    }

    public void CloseTab()
    {
        try
        {
            if (Driver.WindowHandles.Count > 1)
            {
                Driver.Close();
                Driver.SwitchTo().Window(Driver.WindowHandles.Last());
                Log("Tab closed", LogLevel.Information, "CloseTabAsync");
            }
            else
                CloseBrowser();
        }
        catch (Exception ex)
        {
            Log($"Error closing tab: {ex.Message}", LogLevel.Error, "CloseTabAsync", ex);
        }
    }
    private async void RestartDriver(string url)
    {
        if (Driver != null)
        {
            int servicePid = _service.ProcessId;

            try
            {
                Driver.Quit();
            }
            catch (Exception ex)
            {
                Log($"Error quitting driver during restart for URL: {url} - {ex.Message}", LogLevel.Warning, "RestartDriver", ex);
            }

            try
            {
                Driver.Dispose();
            }
            catch (Exception ex)
            {
                Log($"Error disposing driver during restart for URL: {url} - {ex.Message}", LogLevel.Warning, "RestartDriver", ex);
            }

            try
            {
                var proc = Process.GetProcessById(servicePid);
                if (!proc.HasExited)
                    proc.Kill(true);
            }
            catch (Exception ex)
            {
                Log($"Error killing chromedriver process during restart for URL: {url} - {ex.Message}", LogLevel.Warning, "RestartDriver", ex);
            }

            Driver = null;
            _service = null;
        }
        isDriverInitialized = false;
        Log("Driver restarted after error", LogLevel.Warning, "RestartDriver");
        await OpenUrlAsync(url);
    }
    public void CloseBrowser()
    {
        try
        {
            if (isDriverInitialized)
            {
                Driver.Quit();
                isDriverInitialized = false;
                Log("Browser closed", LogLevel.Information, "CloseBrowserAsync");
            }
        }
        catch (Exception ex)
        {
            Log($"Error closing browser: {ex.Message}", LogLevel.Error, "CloseBrowserAsync", ex);
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

        // 1. Only Persian letters, English letters, and spaces are allowed
        string cleaned = Regex.Replace(input, @"[^آ-یa-zA-Z\s]", " ");

        // 2. Remove consecutive spaces (more than one)
        cleaned = Regex.Replace(cleaned, @"\s{2,}", " ");

        // 3. Replace all spaces with "-"
        cleaned = cleaned.Trim(); // Remove leading and trailing spaces
        cleaned = cleaned.Replace(" ", "-");

        return cleaned;
    }
    public async Task<bool> ClickElementAsync(string selector)
    {
        return await ClickElementAsync(By.CssSelector(selector));
    }
    public async Task<bool> ClickElementAsync(By by)
    {
        try
        {
            ResolveTabligh();
            var element = Driver.FindElement(by);
            new Actions(Driver).MoveToElement(element).Click().Perform();
            await Task.Delay(DefaultWait);
            Log($"Clicked on selector: {by}", LogLevel.Information, "ClickElementAsync");
            return true;
        }
        catch (Exception ex)
        {
            try
            {
                Thread.Sleep(200);
                ResolveTabligh();
                var element = Driver.FindElement(by);
                new Actions(Driver).MoveToElement(element).Click().Perform();
                await Task.Delay(DefaultWait);
                Log($"Clicked on selector: {by}", LogLevel.Information, "ClickElementAsync");
                return true;
            }
            catch (Exception)
            {
                Log($"Retry error clicking on selector: {by} - {ex.Message}", LogLevel.Warning, "ClickElementAsync", ex);
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
            return text;
        }
        catch (Exception ex)
        {
            try
            {
                Thread.Sleep(200);
                ResolveTabligh();
                var text = Driver.FindElement(By.CssSelector(selector)).Text.Trim();
                return text;
            }
            catch (Exception)
            {
                Log($"Retry error reading text for selector: {selector} - {ex.Message}", LogLevel.Warning, "GetElementText", ex);
            }
        }
        return "";
    }

    public string GetElementText(IWebElement parentElement, string cssSelector)
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
                Thread.Sleep(200);
                ResolveTabligh();
                var element = parentElement.FindElement(By.CssSelector(cssSelector));
                return element?.Text?.Trim() ?? string.Empty;
            }
            catch (Exception)
            {
                Log($"Retry error reading text for CSS selector: {cssSelector} in parent element - {ex.Message}", LogLevel.Warning, "GetElementTextAsync", ex);
                return string.Empty;
            }
        }
    }

    public List<string> GetElementsText(string selector, string attribute = null)
    {
        try
        {
            ResolveTabligh();
            var texts = attribute != null
                ? Driver.FindElements(By.CssSelector(selector)).Select(e => e.GetAttribute(attribute)).Where(t => !string.IsNullOrEmpty(t)).ToList()
                : Driver.FindElements(By.CssSelector(selector)).Select(e => e.Text).Where(t => !string.IsNullOrEmpty(t)).ToList();
            return texts;
        }
        catch (Exception ex)
        {
            try
            {
                Thread.Sleep(200);
                ResolveTabligh();
                List<string> texts = attribute != null
                    ? Driver.FindElements(By.CssSelector(selector)).Select(e => e.GetAttribute(attribute)).Where(t => !string.IsNullOrEmpty(t)).ToList()
                    : Driver.FindElements(By.CssSelector(selector)).Select(e => e.Text).Where(t => !string.IsNullOrEmpty(t)).ToList();
                return texts;
            }
            catch (Exception)
            {
                Log($"Retry error getting texts for selector: {selector}, Attribute: {attribute ?? "None"} - {ex.Message}", LogLevel.Warning, "GetElementsTextAsync", ex);
                return new List<string>();
            }
        }
    }
    public List<string> GetElementsTextByXPath(string selector, string attribute = null)
    {
        try
        {
            ResolveTabligh();
            var texts = attribute != null
                ? Driver.FindElements(By.XPath(selector)).Select(e => e.GetAttribute(attribute) ?? "").Where(t => !string.IsNullOrEmpty(t)).ToList()
                : Driver.FindElements(By.XPath(selector)).Select(e => e.Text).Where(t => !string.IsNullOrEmpty(t)).ToList();
            return texts;
        }
        catch (Exception ex)
        {
            try
            {
                Thread.Sleep(200);
                ResolveTabligh();
                var texts = attribute != null
                    ? Driver.FindElements(By.XPath(selector)).Select(e => e.GetAttribute(attribute) ?? "").Where(t => !string.IsNullOrEmpty(t)).ToList()
                    : Driver.FindElements(By.XPath(selector)).Select(e => e.Text).Where(t => !string.IsNullOrEmpty(t)).ToList();
                return texts;
            }
            catch (Exception)
            {
                Log($"Retry error getting texts by XPath selector: {selector}, Attribute: {attribute ?? "None"} - {ex.Message}", LogLevel.Warning, "GetElementsTextByXPathAsync", ex);
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
            return texts;
        }
        catch (Exception ex)
        {
            try
            {
                Thread.Sleep(200);
                ResolveTabligh();
                var texts = !attribute.IsNullOrEmpty()
                ? Driver.FindElement(By.XPath(selector)).GetAttribute(attribute)
                : Driver.FindElement(By.XPath(selector)).Text.Trim();
                return texts;
            }
            catch (Exception)
            {
                Log($"Retry error getting text by XPath selector: {selector}, Attribute: {attribute ?? "None"} - {ex.Message}", LogLevel.Warning, "GetElementTextByXPath", ex);
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
                element = Wait(by, delayS);

                if (element != null)
                    return element;
            }
            catch (Exception ex)
            {
                Log($"Attempt {attempt} failed for locator: {by} - {ex.Message}", LogLevel.Warning, "FindElementWithRetry", ex);
            }

            Driver.Navigate().Refresh();
        }

        return null;
    }
    public IWebElement? Wait(By by, int delayS = 3)
    {
        try
        {
            ResolveTabligh();
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(delayS));
            var element = wait.Until(x => x.FindElement(by));
            Thread.Sleep(50);
            return element;
        }
        catch (Exception)
        { }

        return null;
    }
    public IWebElement? FindOne(By selector)
    {
        try
        {
            ResolveTabligh();
            var element = Driver.FindElement(selector);
            return element;
        }
        catch (Exception ex)
        {
            Log($"Error finding element by selector: {selector} - {ex.Message}", LogLevel.Warning, "FindOneAsync", ex);
            try
            {
                Thread.Sleep(200);
                ResolveTabligh();
                var element = Driver.FindElement(selector);
                return element;
            }
            catch (Exception)
            {
                Log($"Retry error finding element by selector: {selector} - {ex.Message}", LogLevel.Warning, "FindOneAsync", ex);
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
            return element;
        }
        catch (Exception ex)
        {
            try
            {
                Thread.Sleep(200);
                ResolveTabligh();
                var element = Driver.FindElement(By.CssSelector(selector));
                return element;
            }
            catch (Exception)
            {
                Log($"Retry error finding element by CSS selector: {selector} - {ex.Message}", LogLevel.Warning, "FindOne", ex);
                return null;
            }
        }
    }
    public IWebElement? FindOneWithin(IWebElement? parent, string selector)
    {
        return FindOneWithin(parent, By.CssSelector(selector));
    }
    public IWebElement? FindOneWithin(IWebElement? parent, By selector)
    {
        if (parent == null)
            return null;
        try
        {
            ResolveTabligh();
            var element = parent.FindElement(selector);
            return element;
        }
        catch (Exception ex)
        {
            Log($"Error finding element inside parent by selector: {selector} - {ex.Message}", LogLevel.Warning, "FindOneWithin", ex);
            return null;
        }
    }

    public List<IWebElement> FindManyWithin(IWebElement? parent, By by)
    {
        var result = new List<IWebElement>();
        if (parent == null)
            return result;
        try
        {
            ResolveTabligh();
            result = parent.FindElements(by).ToList();
            return result;
        }
        catch (Exception ex)
        {
            Log($"Error finding elements inside parent by selector: {by} - {ex.Message}", LogLevel.Error, "FindManyWithinAsync", ex);
            try
            {
                ResolveTabligh();
                result = parent.FindElements(by).ToList();
                return result;
            }
            catch (Exception)
            {
                Log($"Retry error finding elements inside parent by selector: {by} - {ex.Message}", LogLevel.Error, "FindManyWithinAsync", ex);
                return result;
            }
        }
    }

    public List<IWebElement> FindMany(string selector)
    {
        try
        {
            ResolveTabligh();
            var elements = Driver.FindElements(By.CssSelector(selector)).ToList();
            return elements;
        }
        catch (Exception ex)
        {
            try
            {
                ResolveTabligh();
                var elements = Driver.FindElements(By.CssSelector(selector)).ToList();
                Log($"Found {elements.Count} elements by selector: {selector}", LogLevel.Information, "FindManyAsync");
                return elements;
            }
            catch (Exception)
            {
                Log($"Retry error finding elements by selector: {selector} - {ex.Message}", LogLevel.Error, "FindManyAsync", ex);
                return new List<IWebElement>();
            }
        }
    }

    public Dictionary<string, IWebElement> FindWithLabel(string elementSelector, string labelSelector)
    {
        try
        {
            ResolveTabligh();
            var element = Driver.FindElement(By.CssSelector(elementSelector));
            var label = Driver.FindElement(By.CssSelector(labelSelector));
            var result = new Dictionary<string, IWebElement> { { label.Text.Trim(), element } };
            return result;
        }
        catch (Exception ex)
        {
            Log($"Error finding element with label by selectors: Element={elementSelector}, Label={labelSelector} - {ex.Message}", LogLevel.Error, "FindWithLabelAsync", ex);
            return new Dictionary<string, IWebElement>();
        }
    }

    public List<Dictionary<string, IWebElement>> FindManyWithLabels(string elementSelector, string labelSelector)
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
            return result;
        }
        catch (Exception ex)
        {
            Log($"Error finding elements with labels by selectors: Element={elementSelector}, Label={labelSelector} - {ex.Message}", LogLevel.Error, "FindManyWithLabelsAsync", ex);
            return new List<Dictionary<string, IWebElement>>();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        CloseBrowser();
    }

    public string GetPageContent(string url)
    {
        if (url.Contains(".pdf") || url.Contains(".rar") || url.Contains(".zip") || url.Contains(".txt"))
        {
            return "";
        }

        if (url.Contains("semanticscholar.org") || url.Contains("sciencedirect.com") || url.Contains("researchgate.net") ||
            url.Contains("link.springer.com") || url.Contains("sid.ir") || url.Contains("magiran.com") ||
            url.Contains("scholar.google.com/scholar?cluster") || url.Contains("ieeexplore.ieee.org"))
        {
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
            catch (Exception e)
            {
                Log($"Error killing chrome processes: {e.Message}", LogLevel.Warning, "GetPageContent", e);
            }

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
            uriBuilder.Port = -1;  // Remove default port
            url = uriBuilder.ToString();

            Driver!.Navigate().GoToUrl(url);
            try
            {
                Driver.Manage().Window.Maximize(); // Maximize first
                IJavaScriptExecutor js = (IJavaScriptExecutor)Driver;
                js.ExecuteScript("document.body.style.zoom='50%'");
                js.ExecuteScript("document.body.style.transform='scale(0.5)'");
                js.ExecuteScript("document.body.style.transformOrigin='0 0'");
                Task.Delay(100).Wait();
            }
            catch (Exception jsEx)
            {
                Log($"Could not set zoom for URL: {url} - {jsEx.Message}", LogLevel.Warning, "GetPageContent", jsEx);
            }
            return Driver.PageSource;
        }
        catch (Exception e)
        {
            Log($"Error loading page with HTTP for URL: {url} - {e.Message}", LogLevel.Error, "GetPageContent", e);
            try
            {
                UriBuilder uriBuilder = new UriBuilder(url);
                uriBuilder.Scheme = "https";
                uriBuilder.Port = -1;
                url = uriBuilder.ToString();

                Driver!.Navigate().GoToUrl(url);
                try
                {
                    Driver.Manage().Window.Maximize(); // Maximize first
                    IJavaScriptExecutor js = (IJavaScriptExecutor)Driver;
                    js.ExecuteScript("document.body.style.transform='scale(0.25)'");
                    js.ExecuteScript("document.body.style.transformOrigin='0 0'");
                }
                catch (Exception jsEx)
                {
                    Log($"Could not set zoom for URL: {url} - {jsEx.Message}", LogLevel.Warning, "GetPageContent", jsEx);
                }
                return Driver.PageSource;
            }
            catch (Exception ex)
            {
                Log($"Site cannot load for URL: {url} - {ex.Message}", LogLevel.Error, "GetPageContent", ex);
            }
        }

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
            var failedFile = Path.Combine(FileTools.FindDirectoryInParents(), "Failed.csv");
            if (!File.Exists(failedFile))
                File.Create(failedFile).Dispose();

            var writer = new StreamWriter(failedFile, append: true, Encoding.UTF8);

            using (var csvWriter = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csvWriter.WriteField(content);
                if (ex != null)
                    csvWriter.WriteField(ex.ToString());
                csvWriter.NextRecord();
            }
        }
        catch (IOException e)
        {
            Log($"IO error writing to Failed.csv with content: {content} - {e.Message}", LogLevel.Error, "WriteFailedCsv", e);
        }
        catch (Exception generalEx)
        {
            Log($"General error writing to Failed.csv with content: {content} - {generalEx.Message}", LogLevel.Error, "WriteFailedCsv", generalEx);
        }
    }
}