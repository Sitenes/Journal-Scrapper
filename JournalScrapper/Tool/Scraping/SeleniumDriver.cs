using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using AngleSharp.Dom;
using CsvHelper;
using CsvHelper.Configuration;
using JournalScrapper.Tool;
using JournalScrapper.Tool.Scraping;
using JournalScrappers;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

public class WebScraper : IDisposable
{
    public IWebDriver? Driver; // nullable for safer re-init
    private static int pageCounter = 0;
    private ChromeDriverService? _service; // nullable
    private readonly ILogger<ExtractArticles> _logger;
    private bool isDriverInitialized;
    private const int DefaultWait = 1000;
    private string _uniqueUserDataDir;
    private static readonly object _driverLock = new object();

    public WebScraper(ILogger<ExtractArticles> logger)
    {
        this._logger = logger;
        // Generate a unique user-data-dir for this instance
        var tempBase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp");
        var uniqueId = Guid.NewGuid().ToString("N");
        _uniqueUserDataDir = Path.Combine(tempBase, $"ChromeUserData_{uniqueId}");
        Directory.CreateDirectory(_uniqueUserDataDir);
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

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
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
        int debugPort = GetFreeTcpPort();
        chromeOptions.AddArgument($"--remote-debugging-port={debugPort}");
        chromeOptions.AddArgument("--no-first-run");
        chromeOptions.AddArgument("--no-default-browser-check");
        // Optional headless via env var
        //var headlessEnv = Environment.GetEnvironmentVariable("SCRAPER_HEADLESS");
        //if (!string.IsNullOrWhiteSpace(headlessEnv) && (headlessEnv == "1" || headlessEnv!.Equals("true", StringComparison.OrdinalIgnoreCase)))
            chromeOptions.AddArgument("--headless=new");

        chromeOptions.AddArgument("--disable-dev-shm-usage"); // Useful to prevent memory issues in limited environments

        chromeOptions.AddArgument("--disable-blink-features=AutomationControlled");
        chromeOptions.AddExcludedArgument("enable-automation");
        chromeOptions.AddArgument($"--user-data-dir={_uniqueUserDataDir}");

        chromeOptions.AddUserProfilePreference("download.prompt_for_download", false);
        chromeOptions.AddUserProfilePreference("download.directory_upgrade", true);
        chromeOptions.AddUserProfilePreference("plugins.always_open_pdf_externally", false);
        chromeOptions.AddUserProfilePreference("profile.default_content_settings.popups", 0);
        chromeOptions.AddUserProfilePreference("safebrowsing.enabled", true);
        chromeOptions.AddUserProfilePreference("download_restrictions", 3);
        chromeOptions.AddArgument("--ignore-certificate-errors");
        chromeOptions.AddArgument("--ignore-ssl-errors");
        chromeOptions.AddArgument("--allow-insecure-localhost");
        chromeOptions.AddArgument("--disable-web-security");
        chromeOptions.AddArgument("--allow-running-insecure-content");
        chromeOptions.AddArgument("--remote-allow-origins=*");
        chromeOptions.AddArgument("--disable-search-engine-choice-screen");
        chromeOptions.AddArgument("--disk-cache-size=0");
        chromeOptions.AddArgument("--disable-application-cache");
        chromeOptions.AddArgument($"user-agent={UserAgents.GetRandomUserAgent()}");
        chromeOptions.AddArgument("Accept=*/*");
        chromeOptions.AddArgument("Accept-Encoding=gzip, deflate, br");
        chromeOptions.AddArgument("Accept-Charset=ISO-8859-1,utf-8;q=0.7,*;q=0.3");
        chromeOptions.AddArgument("Connection=keep-alive");
        chromeOptions.AddArgument($"X-user-agent={UserAgents.GetRandomUserAgent()}");
        chromeOptions.AddArgument("Content-Type=application/json");
        chromeOptions.AddArgument("Pragma=no-cache");
        chromeOptions.AddArgument("Cache-Control=no-cache");
        chromeOptions.AddAdditionalOption("useAutomationExtension", false);
        chromeOptions.PageLoadStrategy = PageLoadStrategy.Eager;

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
        lock (_driverLock)
        {
            int maxAttempts = 3;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    if (isDriverInitialized && Driver != null)
                        return Driver;

                    _service = ChromeDriverService.CreateDefaultService();
                    _service.HideCommandPromptWindow = true;
                    var options = GetChromeOptions();
                    Log($"[CreateDriver] Attempt {attempt} creating Chrome instance (UserDataDir={_uniqueUserDataDir})", LogLevel.Information, "CreateDriver");
                    Driver = new ChromeDriver(_service, options);
                    isDriverInitialized = true;
                    Log($"Browser initialized with user-data-dir: {_uniqueUserDataDir}", LogLevel.Information, "CreateDriver");
                    return Driver;
                }
                catch (Exception ex)
                {
                    Log($"Attempt {attempt} failed to create driver: {ex.Message}", LogLevel.Error, "CreateDriver", ex);
                    SafeKillService();
                    if (attempt == maxAttempts)
                        throw;
                    Thread.Sleep(1200);
                }
            }
            throw new InvalidOperationException("Could not initialize Chrome driver after retries");
        }
    }

    private void SafeKillService()
    {
        try
        {
            if (_service != null)
            {
                var pid = _service.ProcessId;
                if (pid > 0)
                {
                    try
                    {
                        var proc = Process.GetProcessById(pid);
                        if (!proc.HasExited)
                            proc.Kill(true);
                    }
                    catch { }
                }
                _service.Dispose();
            }
        }
        catch (Exception ex)
        {
            Log($"Error during SafeKillService: {ex.Message}", LogLevel.Warning, "SafeKillService", ex);
        }
        finally
        {
            _service = null;
        }
    }

    public void OpenUrl(string url, string? checkSelector = null)
    {
        // Prevent navigation to downloadable files
        if (url.Contains(".pdf") || url.Contains(".rar") || url.Contains(".zip") || url.Contains(".txt"))
            return;
        if (url.Contains("semanticscholar.org") || url.Contains("sciencedirect.com") || url.Contains("researchgate.net") ||
            url.Contains("link.springer.com") || url.Contains("sid.ir") || url.Contains("magiran.com") ||
            url.Contains("scholar.google.com/scholar?cluster") || url.Contains("ieeexplore.ieee.org"))
            return;

        try
        {
            if (!isDriverInitialized)
            {
                Driver = CreateDriver();
                Log("Browser initialized", LogLevel.Information, "OpenUrlAsync");
            }
            Driver.Navigate().GoToUrl(url);

            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            try
            {
                if (!checkSelector.IsNullOrEmpty())
                    wait.Until(d => !string.IsNullOrEmpty(checkSelector) ? d.FindElement(By.CssSelector(checkSelector)) != null : true);
            }
            catch (WebDriverTimeoutException ex)
            {
                Log($"Timeout waiting for page load at URL: {url}", LogLevel.Warning, "OpenUrlAsync", ex);
                OpenUrl(url);
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
                    Thread.Sleep(50);
                }

                for (int i = steps; i >= 0; i--)
                {
                    long scrollPosition = stepSize * i;
                    ((IJavaScriptExecutor)Driver).ExecuteScript($"window.scrollTo(0, {scrollPosition});");
                    Thread.Sleep(50);
                }
                Thread.Sleep(50);
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
        try
        {
            CloseBrowser();
            isDriverInitialized = false;
            Driver = null;
            Driver = CreateDriver();
            OpenUrl(url);
        }
        catch (Exception ex)
        {
            Log($"RestartDriver fatal error: {ex.Message}", LogLevel.Error, "RestartDriver", ex);
        }
    }
    public void CloseBrowser()
    {
        try
        {
            if (isDriverInitialized && Driver != null)
            {
                try { Driver.Quit(); } catch { }
                try { Driver.Dispose(); } catch { }
            }
        }
        catch (Exception ex)
        {
            Log($"Error closing browser: {ex.Message}", LogLevel.Error, "CloseBrowser", ex);
        }
        finally
        {
            isDriverInitialized = false;
            Driver = null;
            SafeKillService();
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
        // Clean up the unique user-data-dir after browser closes
        try
        {
            if (Directory.Exists(_uniqueUserDataDir))
            {
                Directory.Delete(_uniqueUserDataDir, true);
            }
        }
        catch (Exception ex)
        {
            Log($"Error cleaning up user-data-dir: {_uniqueUserDataDir} - {ex.Message}", LogLevel.Warning, "Dispose", ex);
        }
    }

    public async Task<string> GetFullURL(string subUrl, string fullUrl, bool absolute = false)
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