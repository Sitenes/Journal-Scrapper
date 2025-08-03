using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DataLayer;
using Entities.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Serilog;

namespace JournalScrappers.Scrap.ISC.Journals;

public class JournalCoverScrapper
{
    private readonly WebDriver _webDriver;
    private readonly DynamicDbContext _context;
    private readonly ILogger<JournalCoverScrapper> _logger;

    public JournalCoverScrapper(DynamicDbContext context, ILogger<JournalCoverScrapper> logger)
    {
        _context = context;
        _logger = logger;

        var options = new ChromeOptions();
        //options.AddArgument("--headless=new");
        options.AddArgument("--ignore-certificate-errors");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1920,1080");
        options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
        options.AddAdditionalOption("useAutomationExtension", false);
        options.AddArgument("--disable-blink-features=AutomationControlled");

        _webDriver = new ChromeDriver(options);
        _webDriver.Manage().Window.Maximize();
        ((IJavaScriptExecutor)_webDriver).ExecuteScript("document.body.style.zoom='50%';");
    }

    public async Task ScrapAllJournalCoversAsync()
    {
        var journals = _context.Journals.Where(x => x.URL != "" && (x.CoverImagePath == "" || x.CoverImagePath == null)).ToList();

        foreach (var journal in journals)
        {
            try
            {
                var urls = StringTool.SplitAndCleanUrls(journal.URL);

                foreach (var url in urls)
                {
                    try
                    {
                        _logger.LogInformation("Scraping cover for journal: Id={Id}, Title={Title}, URL={URL}",
                            journal.Id, journal.Title_Fa ?? journal.Title_EN, url);
                        await ScrapeCoverOfJournalAsync(url, journal);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e,
                            "Failed to scrape journal cover: Id={Id}, Title={Title}, URL={URL}",
                            journal.Id, journal.Title_Fa ?? journal.Title_EN, url);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "Failed to scrape journal cover: Id={Id}, Title={Title}, URL={URL}",
                    journal.Id, journal.Title_Fa ?? journal.Title_EN, journal.URL);

                WebScraper.WriteFailedCsv($"Journal Failed -> id:{journal.Id},link:{journal.URL}", e);
            }
        }
    }

    public async Task ScrapeCoverOfJournalAsync(string url, Journal journal)
    {
        try
        {
            _webDriver.NavigateWithScrollAndZoom(url);
            Thread.Sleep(3000);

            var title = journal.Title_Fa?.Trim() ?? journal.Title_EN?.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                return;
            }

            var imageElement = _webDriver.FindElements(By.TagName("img"))
                .FirstOrDefault(img =>
                {
                    var alt = img.GetAttribute("alt")?.Trim();
                    return !string.IsNullOrEmpty(alt) && (alt.Contains("Main Image", StringComparison.OrdinalIgnoreCase) || alt.Contains("Journal Homepage Image", StringComparison.OrdinalIgnoreCase));
                });

            if (imageElement == null)
                imageElement = _webDriver.FindElements(By.TagName("img"))
                       .FirstOrDefault(img =>
                       {
                           var alt = img.GetAttribute("alt")?.Trim();
                           if (string.IsNullOrEmpty(alt)) return false;

                           return alt.NormalizeText().CalculateSimilarity(title.NormalizeText()) >= 0.6;
                       });

            // Fallback to publisher if no match found
            if (imageElement == null && !journal.Publisher.IsNullOrEmpty())
            {
                imageElement = _webDriver.FindElements(By.TagName("img"))
                    .FirstOrDefault(img =>
                    {
                        var alt = img.GetAttribute("alt")?.Trim();
                        if (string.IsNullOrEmpty(alt)) return false;

                        return alt.NormalizeText().CalculateSimilarity(journal.Publisher.NormalizeText()) >= 0.6;
                    });
            }

            // Fallback to second image from entire site if still null
            if (imageElement == null)
            {
                var images = _webDriver.FindElements(By.TagName("img")).ToList();
                imageElement = images.Count >= 2 ? images[1] : images[0];
            }



            //if (imageElement == null)
            //{
            //    imageElement = _webDriver.FindElements(By.TagName("img"))
            //        .FirstOrDefault(img =>
            //        {
            //            var alt = img.GetAttribute("alt")?.Trim();
            //            return !string.IsNullOrEmpty(alt) && alt.Contains(title, StringComparison.OrdinalIgnoreCase);
            //        });
            //}

            if (imageElement == null)
            {
                _logger.LogError("No matching image found for title: {Title} at URL: {URL}", title, url);
                return;
            }

            string imageUrl = imageElement.GetAttribute("src") ?? "";
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                _logger.LogError("Image src is empty for title: {Title} at URL: {URL}", title, url);
                return;
            }

            if (!imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                Uri baseUri = new Uri(url);
                Uri fullUri = new Uri(baseUri, imageUrl);
                imageUrl = fullUri.ToString();
            }

            var guidFileName = $"{Guid.NewGuid()}.jpg";
            var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "covers");
            Directory.CreateDirectory(wwwrootPath);
            var localPath = Path.Combine(wwwrootPath, guidFileName);

            await DownloadImageAsync(imageUrl, localPath);

            //_logger.LogInformation("Downloaded image from {ImageUrl} to {LocalPath}", imageUrl, localPath);

            //var journal = _context.Journals.FirstOrDefault(j => j.URL == url);
            if (journal != null)
            {
                journal.CoverImagePath = $"upload/journal/journalcovers/{guidFileName}";
                _context.SaveChanges();

                _logger.LogInformation("Updated journal cover path for Id={Id} to {Path}", journal.Id, journal.CoverImagePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting image for URL: {URL} | Journal Id: {Title}", url, journal.Id);
            WebScraper.WriteFailedCsv($"Image scrape failed -> {url}", ex);
        }
    }
    public async Task<bool> DownloadImageAsync(string imageUrl, string savePath)
    {
        try
        {
            using var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 10,
                Proxy = null,
                UseProxy = false,
                AutomaticDecompression = DecompressionMethods.All,

                // ❗ این خط بررسی SSL را غیرفعال می‌کند
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            using var response = await client.GetAsync(imageUrl);

            if (response.IsSuccessStatusCode)
            {
                var contentType = response.Content.Headers.ContentType?.MediaType;
                if (contentType != null && contentType.StartsWith("image"))
                {
                    await using var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write);
                    await response.Content.CopyToAsync(fs);
                    Log.Information("✅ Image downloaded successfully | URL: {ImageUrl} | Path: {Path}", imageUrl, savePath);
                    return true;
                }
                else
                {
                    var preview = await response.Content.ReadAsStringAsync();
                    Log.Warning("⚠️ Not an image | ContentType: {ContentType} | URL: {ImageUrl} | Preview: {Preview}",
                        contentType, imageUrl, preview.Substring(0, Math.Min(200, preview.Length)));
                }
            }
            else
            {
                Log.Warning("⚠️ Failed to download image | Status: {StatusCode} | URL: {ImageUrl}", response.StatusCode, imageUrl);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Exception while downloading image | URL: {ImageUrl}", imageUrl);
        }

        return false;
    }

    
}
