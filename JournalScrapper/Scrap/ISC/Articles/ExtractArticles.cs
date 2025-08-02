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
    public class ExtractArticles
    {
        private readonly DynamicDbContext _context;
        private readonly ILogger<ExtractArticles> _logger;
        private readonly ExtractXml _extractXml;

        public ExtractArticles(DynamicDbContext context, ILogger<ExtractArticles> logger, ExtractXml extractXml)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _extractXml = extractXml ?? throw new ArgumentNullException(nameof(extractXml));
        }

        public void ScrapArticles()
        {
            var journals = _context.Journals
                .Where(x => !string.IsNullOrWhiteSpace(x.URL) && x.IsIsc && x.Language == "فارسی")
                .ToList().Reverse<Journal>();

            foreach (var journal in journals)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(journal.URL) /*|| _context.Articles.Any(x => x.JournalId == journal.Id)*/)
                    {
                        _logger.LogInformation("ژورنال رد شد: شناسه {JournalId}, لینک {Url}", journal.Id, journal.URL);
                        continue;
                    }

                    WebScraper.GetPageContent(journal.URL);

                    var plusXpath = By.XPath("//*[not(self::a or self::button) and (contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'plus') or contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'angle-down') or contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'pull-right'))]");
                    var plusElements = WebScraper.driver.FindElements(plusXpath);
                    var journalUrl = WebScraper.driver.Url;

                    foreach (var plusElement in plusElements)
                    {
                        try
                        {
                            var jsExecutor = (IJavaScriptExecutor)WebScraper.driver;
                            jsExecutor.ExecuteScript("arguments[0].click();", plusElement);

                            if (!WebScraper.driver.Url.Contains(journalUrl))
                            {
                                WebScraper.driver.Navigate().Back();
                                plusElements = WebScraper.driver.FindElements(plusXpath);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "خطا در کلیک روی عنصر plus برای ژورنال {JournalId}", journal.Id);
                        }
                    }

                    Thread.Sleep(500);

                    var issues = WebScraper.driver.FindElements(By.XPath("//a[contains(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'issue')]"))
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
                            WebScraper.GetPageContent(issue);

                            var articles = WebScraper.driver.FindElements(By.XPath("//a[contains(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'article') and not(ancestor::footer)]"))
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
                                    _extractXml.ExtractXML(article, journal.Id);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "خطا در استخراج مقاله: لینک {ArticleLink}, ژورنال {JournalId}", article, journal.Id);
                                    WebScraper.WriteFailedCsv($"ExtractXML Failed -> article:{article}", ex);
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
                    WebScraper.WriteFailedCsv($"Journal Failed -> id:{journal.Id},link:{journal.URL}", ex);
                }
            }
        }
    }
}