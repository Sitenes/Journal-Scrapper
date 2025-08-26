using DataLayer;
using Entities.Models.Entities;
using JournalScrappers;
using JournalScrappers.Scrap.ISC.Articles;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace JournalScrapper.Scrap.ISC.Articles.CrawlerLinks
{
    public class JournalsCrawler
    {
        private readonly DynamicDbContext _context;
        private readonly WebScraper _scraper;
        private readonly ILogger<ExtractArticles> _logger;
        private readonly CrawlXml _extractXml;
        private readonly HashSet<string> _visitedUrls;
        private readonly HashSet<string> _visitedXmls;
        private readonly Queue<(string Url, int Depth)> _urlQueue;
        private bool _navElementsClicked;

        public JournalsCrawler(DynamicDbContext context, WebScraper Driver, ILogger<ExtractArticles> logger, CrawlXml extractXml)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            this._scraper = Driver;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _extractXml = extractXml ?? throw new ArgumentNullException(nameof(extractXml));
            _visitedUrls = new HashSet<string>();
            _visitedXmls = new HashSet<string>();
            _urlQueue = new Queue<(string Url, int Depth)>();
            _navElementsClicked = false;
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
                    _scraper.WriteFailedCsv($"Journal Failed -> id:{journal.Id},link:{journal.URL}", ex);
                }
            }
        }

        private void CrawlWebsite(string startUrl, int journalId)
        {
            _urlQueue.Clear();
            _visitedUrls.Clear();
            _visitedXmls.Clear();
            _navElementsClicked = false;
            _urlQueue.Enqueue((startUrl, 0));
            _visitedUrls.Add(startUrl);

            while (_urlQueue.Count > 0)
            {
                var (currentUrl, currentDepth) = _urlQueue.Dequeue();

                if (currentDepth > 3)
                {
                    continue;
                }

                try
                {
                    _scraper.GetPageContent(currentUrl);
                    // Find and process navigation elements only once
                    if (!_navElementsClicked)
                    {
                        var navElements = _scraper.Driver.FindElements(By.XPath(
                            "//*[contains(@class, 'plus') or contains(@class, 'angle-down') or contains(@class, 'pull-right') or contains(@class, 'more') or contains(@class, 'expand')]"));

                        foreach (var navElement in navElements)
                        {
                            try
                            {
                                var jsExecutor = (IJavaScriptExecutor)_scraper.Driver;
                                jsExecutor.ExecuteScript("arguments[0].click();", navElement);
                                
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "خطا در کلیک روی عنصر ناوبری: ژورنال {JournalId}", journalId);
                            }
                        }
                        _navElementsClicked = true;
                        Task.Delay(500).Wait();
                    }

                    // Collect all links on the page
                    var links = _scraper.Driver.FindElements(By.XPath("//a[@href]"))
                        .Select(x => x.GetAttribute("href"))
                        .ToList().Distinct().Where(x => !string.IsNullOrWhiteSpace(x) &&
                                  !x.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) &&
                                  !x.Contains("linkedin", StringComparison.OrdinalIgnoreCase) &&
                                  !x.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) &&
                                  !x.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) &&
                                  !_visitedUrls.Contains(x) &&
                                  !_urlQueue.Any(q => q.Url == x) &&
                                  IsSameDomain(x, startUrl)
                                  );

                    // Check for XML links first
                    var xmlLinks = FindXmlLinks().ToHashSet(StringComparer.OrdinalIgnoreCase);
                    foreach (var link in links)
                    {
                        if (!xmlLinks.Contains(link)) // اگر در xmlLinks نبود
                        {
                            _visitedUrls.Add(link);
                            _urlQueue.Enqueue((link, currentDepth + 1));
                        }
                    }
                    if (xmlLinks.Any())
                    {
                        foreach (var xmlLink in xmlLinks)
                        {
                            if (_visitedXmls.Contains(xmlLink))
                                continue;
                            try
                            {
                                _extractXml.ProcessFromUrl(xmlLink, journalId);
                                _visitedXmls.Add(xmlLink);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "خطا در استخراج XML: لینک {XmlLink}, ژورنال {JournalId}", xmlLink, journalId);
                                _scraper.WriteFailedCsv($"ExtractXML Failed -> xml:{xmlLink}", ex);
                            }
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
                var elements = _scraper.Driver.FindElements(By.XPath(
                    "//a[" +
                    "contains(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'xml') or " +
                    "contains(translate(string(.), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'xml')" +
                    "]"));

                xmlLinks.AddRange(elements
                    .Select(x => x.GetAttribute("href"))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()!);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "خطا در یافتن لینک‌های XML");
            }
            return xmlLinks;
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