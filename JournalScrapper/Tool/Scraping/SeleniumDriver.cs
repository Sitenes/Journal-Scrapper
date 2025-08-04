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


public class WebScraper
{
    public static IWebDriver? driver;
    private static int pageCounter = 0;




    public static string GetPageContent(string url)
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

        if (driver == null)
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

            string extraFolderPath = FindDirectoryInParents();
            //string chromeDriverPath = Path.Combine(extraFolderPath, "chromedriver.exe");
            driver = new ChromeDriver(options);
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(7);
        }

        try
        {
            if (pageCounter >= 100 || WebScraper.driver == null || WebScraper.driver.WindowHandles.Count == 0)
            {
                driver?.Quit();
                driver?.Dispose();
                driver = null;
                pageCounter = 0;
                return GetPageContent(url);
            }
            pageCounter++;

            UriBuilder uriBuilder = new UriBuilder(url);
            uriBuilder.Scheme = "http";
            uriBuilder.Port = -1;  // حذف پورت پیش‌فرض
            url = uriBuilder.ToString();
            
            driver!.Navigate().GoToUrl(url);
            try
            {
                driver.Manage().Window.Maximize(); // ابتدا maximize کنیم
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                js.ExecuteScript("document.body.style.zoom='50%'");
                js.ExecuteScript("document.body.style.transform='scale(0.5)'");
                js.ExecuteScript("document.body.style.transformOrigin='0 0'");
                Task.Delay(100).Wait();
            }
            catch (Exception jsEx)
            {
                Console.WriteLine("Could not set zoom: " + jsEx.Message);
            }
            return driver.PageSource;
        }
        catch (Exception e)
        {
            try
            {
                UriBuilder uriBuilder = new UriBuilder(url);
                uriBuilder.Scheme = "https";
                uriBuilder.Port = -1;
                url = uriBuilder.ToString();

                driver!.Navigate().GoToUrl(url);
                try
                {
                    driver.Manage().Window.Maximize(); // ابتدا maximize کنیم
                    IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                    js.ExecuteScript("document.body.style.transform='scale(0.25)'");
                    js.ExecuteScript("document.body.style.transformOrigin='0 0'");
                }
                catch (Exception jsEx)
                {
                    Console.WriteLine("Could not set zoom: " + jsEx.Message);
                }
                return driver.PageSource;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Site cannot load: " + url + " - " + ex.Message);
            }
        }

        // long startTime = System.currentTimeMillis();
        // long maxLoadTime = 10000; // 10 ثانیه

        // WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(1));
        // wait.Until(d =>
        // {
        //     IWebElement element = d.FindElement(By.TagName("body"));
        //     return element != null;
        // });

        // Thread.Sleep(randomWait);

        return "";
    }
    public static string FindDirectoryInParents(string directoryName = "Extra")
    {
        string currentDirectory = Directory.GetCurrentDirectory();

        while (!string.IsNullOrEmpty(currentDirectory))
        {
            string potentialPath = Path.Combine(currentDirectory, directoryName);
            if (Directory.Exists(potentialPath))
            {
                return potentialPath;
            }

            currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
        }

        return "";

    }
    public static string GetFullURL(string subUrl, string fullUrl, bool absolute = false)
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
    public static void WriteFailedCsv(string content = "", Exception? ex = null)
    {
        try
        {
            //var needHeader = !File.Exists(failedFile) || new FileInfo(failedFile).Length == 0;
            var failedFile = Path.Combine(FindDirectoryInParents(), "Failed.csv");
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
