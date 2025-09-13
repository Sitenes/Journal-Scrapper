using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DataLayer;
using Entities.Models.Entities;
using JournalScrappers;
using JournalScrappers.Scrap.ISC.Articles;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OpenQA.Selenium;

namespace JournalScrapper.Scrap.ISC.Articles.CrawlerLinks
{
    public class JournalsCrawler : IDisposable
    {
        private readonly DynamicDbContext _context;
        private readonly WebScraper _scraper;
        private readonly ILogger<ExtractArticles> _logger;
        private readonly CrawlXml _extractXml;
        private readonly HashSet<string> _visitedUrls;
        private readonly HashSet<string> _visitedXmls;
        private readonly Queue<(string Url, int Depth)> _urlQueue;
        private readonly SemaphoreSlim _crawlSemaphore;
        private bool _navElementsClicked;

        public JournalsCrawler(DynamicDbContext context, WebScraper Driver, ILogger<ExtractArticles> logger, CrawlXml extractXml)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            this._scraper = Driver;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _extractXml = extractXml ?? throw new ArgumentNullException(nameof(extractXml));
            _visitedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _visitedXmls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _urlQueue = new Queue<(string Url, int Depth)>();
            _crawlSemaphore = new SemaphoreSlim(2, 2); // محدود کردن تعداد درخواست‌های همزمان
            _navElementsClicked = false;
        }

        public async Task ScrapArticlesAsync()
        {
            var journals = _context.Journals
                .Where(x => !string.IsNullOrWhiteSpace(x.URL) && x.IsIsc && x.Language == "فارسی").OrderBy(x => x.Id)
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

                    await CrawlWebsiteAsync(journal.URL, journal.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطا در پردازش ژورنال: شناسه {JournalId}, لینک {Url}", journal.Id, journal.URL);
                    _scraper.WriteFailedCsv($"Journal Failed -> id:{journal.Id},link:{journal.URL}", ex);
                }
            }
        }

        private async Task CrawlWebsiteAsync(string startUrl, int journalId)
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

                if (currentDepth > 4)
                {
                    continue;
                }

                try
                {
                    _scraper.OpenUrl(currentUrl);
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
                        Task.Delay(100).Wait();
                    }

                    // Collect all links on the page - بهینه شده برای پرفورمنس
                    var linkElements = _scraper.Driver?.FindElements(By.XPath("//a[@href]"));
                    var links = new List<string>();
                    if (linkElements?.Count > 1000)
                        continue;
                    if (linkElements != null)
                    {
                        var queueUrls = new HashSet<string>(_urlQueue.Select(q => q.Url));

                        foreach (var element in linkElements)
                        {
                            try
                            {
                                var href = element.GetAttribute("href")?.Split("#").FirstOrDefault();
                                if (href?.EndsWith("/") ?? false)
                                    href = href.Remove(href.Count() - 1);
                                if (href != null &&
                                    IsValidLink(href, startUrl) &&
                                    !_visitedUrls.Contains(href) &&
                                    !queueUrls.Contains(href))
                                {
                                    links.Add(href);
                                }
                            }
                            catch (Exception e)
                            {
                            }
                        }
                    }

                    // Check for XML links first
                    var xmlLinks = FindXmlLinks().ToHashSet(StringComparer.OrdinalIgnoreCase);
                    foreach (var link in links)
                    {
                        if (!link.IsNullOrEmpty() && !xmlLinks.Contains(link!)) // اگر در xmlLinks نبود
                        {
                            _visitedUrls.Add(link!);
                            _urlQueue.Enqueue((link!, currentDepth + 1));
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
                                await _extractXml.ProcessFromUrl(xmlLink, journalId);
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

        private bool IsValidLink(string? href, string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(href))
                return false;

            return !href.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) &&
                   !href.Contains("linkedin", StringComparison.OrdinalIgnoreCase) &&
                   !href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) &&
                   !href.Contains("xml", StringComparison.OrdinalIgnoreCase) &&
                   !href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) &&
                   IsSameDomain(href, baseUrl);
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

        public void Dispose()
        {
            _crawlSemaphore?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
