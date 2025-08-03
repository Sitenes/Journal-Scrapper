using DataLayer;
using Entities.Models.Entities;
using JournalScrappers.Scrap.ISC.Articles;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using System;
using System.Linq;
using System.Threading;

namespace JournalScrappers
{
    public class JournalsCrawler
    {
        private readonly DynamicDbContext _context;
        private readonly ILogger<ExtractArticles> _logger;
        private readonly ExtractXml _extractXml;
        private readonly HashSet<string> _visitedUrls;
        private readonly Queue<string> _urlQueue;

        public JournalsCrawler(DynamicDbContext context, ILogger<ExtractArticles> logger, ExtractXml extractXml)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _extractXml = extractXml ?? throw new ArgumentNullException(nameof(extractXml));
            _visitedUrls = new HashSet<string>();
            _urlQueue = new Queue<string>();
        }

        public void ScrapArticles()
        {
            var journals = _context.Journals
                .Where(x => !string.IsNullOrWhiteSpace(x.URL) && x.IsIsc && x.Language == "فارسی")
                .ToList();

            foreach (var journal in journals)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(journal.URL))
                    {
                        _logger.LogInformation("ژورنال رد شد: شناسه {JournalId}, لینک {Url}", journal.Id, journal.URL);
                        continue;
                    }

                    CrawlWebsite(journal.URL, journal.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطا در پردازش ژورنال: شناسه {JournalId}, لینک {Url}", journal.Id, journal.URL);
                    WebScraper.WriteFailedCsv($"Journal Failed -> id:{journal.Id},link:{journal.URL}", ex);
                }
            }
        }

        private void CrawlWebsite(string startUrl, int journalId)
        {
            _urlQueue.Clear();
            _visitedUrls.Clear();
            _urlQueue.Enqueue(startUrl);
            _visitedUrls.Add(NormalizeUrl(startUrl));

            while (_urlQueue.Count > 0)
            {
                var currentUrl = _urlQueue.Dequeue();
                try
                {
                    WebScraper.GetPageContent(currentUrl);

                    // Check for XML links first
                    var xmlLinks = FindXmlLinks();
                    if (xmlLinks.Any())
                    {
                        foreach (var xmlLink in xmlLinks)
                        {
                            try
                            {
                                _extractXml.ExtractXML(xmlLink, journalId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "خطا در استخراج XML: لینک {XmlLink}, ژورنال {JournalId}", xmlLink, journalId);
                                WebScraper.WriteFailedCsv($"ExtractXML Failed -> xml:{xmlLink}", ex);
                            }
                        }
                    }

                    // Find and process navigation elements
                    var navElements = WebScraper.driver.FindElements(By.XPath(
                        "//*[contains(@class, 'plus') or contains(@class, 'angle-down') or contains(@class, 'pull-right') or contains(@class, 'more') or contains(@class, 'expand')]"));

                    foreach (var navElement in navElements)
                    {
                        try
                        {
                            var jsExecutor = (IJavaScriptExecutor)WebScraper.driver;
                            jsExecutor.ExecuteScript("arguments[0].click();", navElement);
                            Thread.Sleep(200);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "خطا در کلیک روی عنصر ناوبری: ژورنال {JournalId}", journalId);
                        }
                    }

                    // Collect all links on the page
                    var links = WebScraper.driver.FindElements(By.XPath("//a[@href]"))
                        .Select(x => x.GetAttribute("href"))
                        .Where(x => !string.IsNullOrWhiteSpace(x) &&
                                  !x.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) &&
                                  !x.Contains("linkedin", StringComparison.OrdinalIgnoreCase) &&
                                  !x.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) &&
                                  !x.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                        .Select(x => NormalizeUrl(x))
                        .Distinct()
                        .ToList();

                    foreach (var link in links)
                    {
                        if (!_visitedUrls.Contains(link) && IsSameDomain(link, startUrl))
                        {
                            _visitedUrls.Add(link);
                            _urlQueue.Enqueue(link);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "خطا در پردازش صفحه: {Url}, ژورنال {JournalId}", currentUrl, journalId);
                }
            }
        }

        private List<string> FindXmlLinks()
        {
            var xmlLinks = new List<string>();
            try
            {
                var elements = WebScraper.driver.FindElements(By.XPath(
                    "//a[contains(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), '.xml') or " +
                    "contains(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'xml')]"));

                xmlLinks.AddRange(elements
                    .Select(x => x.GetAttribute("href"))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => NormalizeUrl(x))
                    .Distinct());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "خطا در یافتن لینک‌های XML");
            }
            return xmlLinks;
        }

        private string NormalizeUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.GetLeftPart(UriPartial.Path).ToLower();
            }
            catch
            {
                return url.ToLower();
            }
        }

        private bool IsSameDomain(string url, string baseUrl)
        {
            try
            {
                var uri = new Uri(url);
                var baseUri = new Uri(baseUrl);
                return uri.Host.Equals(baseUri.Host, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}