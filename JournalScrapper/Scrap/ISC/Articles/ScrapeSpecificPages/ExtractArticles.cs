using System;
using System.Linq;
using System.Threading;
using DataLayer;
using Entities.Models.Entities;
using JournalScrappers.Scrap.ISC.Articles;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;

namespace JournalScrappers
{
    public class ExtractArticles
    {
        private readonly DynamicDbContext _context;
        private readonly ILogger<ExtractArticles> _logger;
        private readonly ExtractXml _extractXml;
        private readonly WebScraper _scraper;

        public ExtractArticles(DynamicDbContext context, ILogger<ExtractArticles> logger, ExtractXml extractXml, WebScraper scraper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _extractXml = extractXml ?? throw new ArgumentNullException(nameof(extractXml));
            this._scraper = scraper;
        }

        public async Task ScrapArticlesAsync()
        {
            var journals = _context.Journals
                .Where(x => !string.IsNullOrWhiteSpace(x.URL) && x.IsIsc).ToList();

            foreach (var journal in journals)
            {

                if (string.IsNullOrWhiteSpace(journal.URL) /*|| _context.Articles.Any(x => x.JournalId == journal.Id)*/)
                {
                    _logger.LogInformation("ژورنال رد شد: شناسه {JournalId}, لینک {Url}", journal.Id, journal.URL);
                    continue;
                }
                var urls = StringTool.SplitAndCleanUrls(journal.URL);
                if (urls.Count > 1)
                {
                    journal.URL = string.Join("|", urls);
                    _context.SaveChanges();
                }
                foreach (var link in urls)
                {

                    try
                    {
                        _scraper.OpenUrl(journal.URL);

                        var plusXpath = By.XPath("//*[not(self::a or self::button) and (contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'plus') or contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'angle-down') or contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'pull-right'))]");
                        var plusElements = _scraper.Driver.FindElements(plusXpath);
                        var journalUrl = _scraper.Driver.Url;

                        foreach (var plusElement in plusElements)
                        {
                            try
                            {
                                var jsExecutor = (IJavaScriptExecutor)_scraper.Driver;
                                jsExecutor.ExecuteScript("arguments[0].click();", plusElement);

                                if (!_scraper.Driver.Url.Contains(journalUrl))
                                {
                                    _scraper.Driver.Navigate().Back();
                                    plusElements = _scraper.Driver.FindElements(plusXpath);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "خطا در کلیک روی عنصر plus برای ژورنال {JournalId}", journal.Id);
                            }
                        }

                        Task.Delay(500).Wait();

                        var issues = _scraper.Driver.FindElements(By.XPath("//a[contains(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'issue')]"))
                            ?.Select(x => x.GetAttribute("href"))
                            .ToList();

                        if (issues == null || !issues.Any())
                        {
                            _logger.LogWarning("هیچ شماره‌ای برای ژورنال {JournalId} یافت نشد", journal.Id);
                            continue;
                        }

                        foreach (var issue in issues)
                        {
                            try
                            {
                                _scraper.OpenUrl(issue);

                                var articles = _scraper.Driver.FindElements(By.XPath("//a[contains(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'article') and not(ancestor::footer)]"))
                                    ?.Select(x => x.GetAttribute("href"))
                                    .Distinct()
                                    .Where(x => !x.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) &&
                                                !x.Contains("linkedin", StringComparison.OrdinalIgnoreCase))
                                    .ToList();

                                if (articles == null || !articles.Any())
                                {
                                    _logger.LogWarning("هیچ مقاله‌ای برای شماره {Issue} یافت نشد", issue);
                                    continue;
                                }

                                foreach (var article in articles)
                                {
                                    try
                                    {
                                        await _extractXml.ExtractXMLAsync(article, journal.Id);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "خطا در استخراج مقاله: لینک {ArticleLink}, ژورنال {JournalId}", article, journal.Id);
                                        _scraper.WriteFailedCsv($"ExtractXML Failed -> article:{article}", ex);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "خطا در پردازش شماره: {Issue}, ژورنال {JournalId}", issue, journal.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "خطا در پردازش ژورنال: شناسه {JournalId}, لینک {Url}", journal.Id, journal.URL);
                        _scraper.WriteFailedCsv($"Journal Failed -> id:{journal.Id},link:{journal.URL}", ex);
                    }
                }

            }
        }
    }
}