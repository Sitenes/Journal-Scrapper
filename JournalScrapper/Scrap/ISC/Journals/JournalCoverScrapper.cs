using System.Net;
using System.Xml.Linq;
using DataLayer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

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
        options.AddArgument("--headless=new");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1920,1080");
        options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
        options.AddAdditionalOption("useAutomationExtension", false);
        options.AddArgument("--disable-blink-features=AutomationControlled");

        _webDriver = new ChromeDriver(options);
        _webDriver.Manage().Window.Maximize();
        ((IJavaScriptExecutor)_webDriver).ExecuteScript("document.body.style.zoom='50%';");
    }

    public void ScrapAllJournalCovers()
    {
        var journals = _context.Journals.ToList();

        foreach (var journal in journals)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(journal.URL) ||
                    _context.Articles.Any(x => x.JournalId == journal.Id))
                    continue;

                _logger.LogInformation("Scraping cover for journal: Id={Id}, Title={Title}, URL={URL}",
                    journal.Id, journal.Title_Fa ?? journal.Title_EN, journal.URL);

                ScrapeCoverOfJournal(journal.URL, journal.Title_Fa ?? journal.Title_EN);
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

    public void ScrapeCoverOfJournal(string url, string? title)
    {
        try
        {
            _webDriver.NavigateWithScrollAndZoom(url);
            Thread.Sleep(3000);

            title = title?.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                return;
            }

            var imageElement = _webDriver.FindElements(By.TagName("img"))
                .FirstOrDefault(img =>
                {
                    var alt = img.GetAttribute("alt")?.Trim();
                    return !string.IsNullOrEmpty(alt) && alt.Contains("Main Image", StringComparison.OrdinalIgnoreCase);
                });

            if (imageElement == null)
            {
                imageElement = _webDriver.FindElements(By.TagName("img"))
                    .FirstOrDefault(img =>
                    {
                        var alt = img.GetAttribute("alt")?.Trim();
                        return !string.IsNullOrEmpty(alt) && alt.Contains(title, StringComparison.OrdinalIgnoreCase);
                    });
            }

            if (imageElement == null)
            {
                _logger.LogWarning("No matching image found for title: {Title} at URL: {URL}", title, url);
                return;
            }

            string imageUrl = imageElement.GetAttribute("src") ?? "";
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                _logger.LogWarning("Image src is empty for title: {Title} at URL: {URL}", title, url);
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

            using (WebClient client = new WebClient())
            {
                client.Proxy = null;
                client.DownloadFile(imageUrl, localPath);
            }

            _logger.LogInformation("Downloaded image from {ImageUrl} to {LocalPath}", imageUrl, localPath);

            var journal = _context.Journals.FirstOrDefault(j => j.URL == url);
            if (journal != null)
            {
                journal.CoverImagePath = $"/covers/{guidFileName}";
                _context.SaveChanges();

                _logger.LogInformation("Updated journal cover path for Id={Id} to {Path}", journal.Id, journal.CoverImagePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting image for URL: {URL} | Title: {Title}", url, title);
            WebScraper.WriteFailedCsv($"Image scrape failed -> {url}", ex);
        }
    }
}
