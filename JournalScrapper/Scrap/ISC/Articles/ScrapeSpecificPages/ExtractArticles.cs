using System;
using System.Linq;
using System.Threading;
using AngleSharp.Dom;
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
                .Where(x => !string.IsNullOrWhiteSpace(x.URL) && x.IsIsc && x.Language == "فارسی"
                ).ToList();

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
                        List<string> issues = new List<string>();

                        // حالت یکتاوب: سایت‌هایی که دکمه "تمام شماره‌ها" یا "آرشیو مقالات" دارند
                        // bool isYektaweb = TryScrapYektaweb(journal, out issues);
                        bool isYektaweb = false;

                        // اگر یکتاوب نبود، سیناوب را اجرا کن
                        if (!isYektaweb)
                        {
                            issues = ScrapSinaweb(journal);
                        }

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

                                var articles = _scraper.Driver?.FindElements(By.XPath("//a[contains(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'article') and not(ancestor::footer)]"))
                                    ?.Select(x => x.GetAttribute("href"))
                                    .Distinct()
                                    .Where(x => !string.IsNullOrWhiteSpace(x) && !x.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) &&
                                                !x.Contains("linkedin", StringComparison.OrdinalIgnoreCase))
                                    .ToList();

                                if (articles == null || !articles.Any())
                                {
                                    _logger.LogWarning("هیچ مقاله‌ای برای شماره {Issue} یافت نشد", issue);
                                    continue;
                                }

                                foreach (var article in articles)
                                {
                                    if (string.IsNullOrWhiteSpace(article))
                                        continue;

                                    try
                                    {
                                        // ابتدا وارد صفحه مقاله شو
                                        _scraper.OpenUrl(article);

                                        // همه تگ های a که متن یا href آنها شامل xml است (case-insensitive)
                                        var xmlLinks = _scraper.Driver?.FindElements(By.XPath(
                                            "//a[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'xml') or contains(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'xml')]"
                                        ))?.Select(x => x.GetAttribute("href"))
                                         .Where(x => !string.IsNullOrWhiteSpace(x))
                                         .Distinct()
                                         .ToList();

                                        if (xmlLinks != null && xmlLinks.Any())
                                        {
                                            foreach (var xmlLink in xmlLinks)
                                            {
                                                try
                                                {
                                                    await _extractXml.ExtractXMLAsync(xmlLink!, journal.Id);
                                                }
                                                catch (Exception exXml)
                                                {
                                                    _logger.LogError(exXml, "خطا در استخراج XML: لینک {XmlLink}, ژورنال {JournalId}", xmlLink, journal.Id);
                                                    _scraper.WriteFailedCsv($"ExtractXML Failed -> xml:{xmlLink}", exXml);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            _logger.LogWarning("هیچ لینک XML برای مقاله {ArticleLink} یافت نشد (ژورنال {JournalId})", article, journal.Id);
                                        }
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

        /// <summary>
        /// حالت یکتاوب: سایت‌هایی که دکمه "تمام شماره‌ها" یا "آرشیو مقالات" دارند (مانند http://jbums.org/)
        /// </summary>
        private bool TryScrapYektaweb(Journal journal, out List<string> issues)
        {
            issues = new List<string>();

            try
            {
                // جستجوی المنت با alt="تمام شماره" یا متن "تمام شماره‌ها" یا "آرشیو مقالات"
                var archiveElement = _scraper.Driver?.FindElements(By.XPath(
                    "//img[contains(@alt, 'تمام شماره')] | " +
                    "//a[contains(text(), 'تمام شماره')] | " +
                    "//a[contains(text(), 'آرشیو مقالات')] | " +
                    "//a[contains(text(), 'Archive')] | " +
                    "//a[contains(@href, 'browse.php')] | " +
                    "//a[contains(@href, 'archive')]"
                ))?.FirstOrDefault();

                if (archiveElement != null)
                {
                    _logger.LogInformation("حالت یکتاوب شناسایی شد برای ژورنال {JournalId}", journal.Id);

                    // کلیک روی المنت
                    try
                    {
                        if (_scraper.Driver != null)
                        {
                            var jsExecutor = (IJavaScriptExecutor)_scraper.Driver;
                            jsExecutor?.ExecuteScript("arguments[0].click();", archiveElement);
                            Task.Delay(1000).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "خطا در کلیک روی المنت آرشیو برای ژورنال {JournalId}", journal.Id);
                        // اگر کلیک با جاوااسکریپت کار نکرد، از href استفاده کن
                        var href = archiveElement.GetAttribute("href");
                        if (!string.IsNullOrWhiteSpace(href))
                        {
                            _scraper.OpenUrl(href);
                        }
                        else
                        {
                            return false;
                        }
                    }

                    Task.Delay(500).Wait();

                    // استخراج لینک‌های issue ها
                    // جستجو در صفحه آرشیو برای لینک‌های شماره‌ها
                    var issueLinks = _scraper.Driver?.FindElements(By.XPath(
                        "//a[contains(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'browse.php')] | " +
                        "//a[contains(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'issue')] | " +
                        "//a[contains(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'vol')] | " +
                        "//a[img[contains(@alt, 'دوره') or contains(@alt, 'شماره')]]"
                    ))?.Select(x => x.GetAttribute("href"))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

                    if (issueLinks != null && issueLinks.Any())
                    {
                        issues = issueLinks.Where(x => x != null).Select(x => x!).ToList();
                        _logger.LogInformation("تعداد {Count} شماره برای ژورنال {JournalId} در حالت یکتاوب یافت شد", issues.Count, journal.Id);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "خطا در حالت یکتاوب برای ژورنال {JournalId}", journal.Id);
                return false;
            }
        }

        /// <summary>
        /// حالت سیناوب: روش قدیمی - جستجوی المنت‌های plus و استخراج لینک‌های issue
        /// </summary>
        private List<string> ScrapSinaweb(Journal journal)
        {
            try
            {
                _logger.LogInformation("حالت سیناوب اجرا می‌شود برای ژورنال {JournalId}", journal.Id);

                // حالت اول: جستجوی تگ‌هایی که onclick با loadIssues دارند
                var loadIssuesXpath = By.XPath("//*[contains(@onclick, 'loadIssues')]");
                var plusElements = _scraper.Driver?.FindElements(loadIssuesXpath);

                // اگر المنتی با loadIssues پیدا نشد، از روش دوم استفاده کن
                if (plusElements == null || !plusElements.Any())
                {
                    _logger.LogInformation("المنت loadIssues پیدا نشد، استفاده از روش جستجوی کلاس‌ها برای ژورنال {JournalId}", journal.Id);

                    // حالت دوم: جستجوی المنت‌هایی که شامل plus, angle-down یا pull-right هستند
                    var classBasedXpath = By.XPath(
                        "//*[" +
                        "contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'plus') or " +
                        "contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'angle') or " +
                        "contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'pull')" +
                        "]"
                    );
                    plusElements = _scraper.Driver?.FindElements(classBasedXpath);
                }
                else
                {
                    _logger.LogInformation("تعداد {Count} المنت با loadIssues پیدا شد برای ژورنال {JournalId}", plusElements.Count, journal.Id);
                }

                var journalUrl = _scraper.Driver?.Url;

                if (plusElements != null && journalUrl != null)
                {
                    foreach (var plusElement in plusElements)
                    {
                        try
                        {
                            if (_scraper.Driver != null)
                            {
                                var jsExecutor = (IJavaScriptExecutor)_scraper.Driver;
                                jsExecutor?.ExecuteScript("arguments[0].click();", plusElement);

                                // اگر به صفحه دیگری رفتیم، برگردیم و دوباره المنت‌ها رو بگیریم
                                if (_scraper.Driver?.Url != null && !_scraper.Driver.Url.Contains(journalUrl))
                                {
                                    _scraper.Driver.Navigate().Back();
                                    // دوباره همون xpath که استفاده شد رو اجرا کن
                                    var reloadXpath = By.XPath("//*[contains(@onclick, 'loadIssues')]");
                                    var reloadedElements = _scraper.Driver.FindElements(reloadXpath);
                                    if (reloadedElements == null || !reloadedElements.Any())
                                    {
                                        reloadXpath = By.XPath(
                                            "//*[" +
                                            "contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'plus') or " +
                                            "contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'angle-down') or " +
                                            "contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'pull-right')" +
                                            "]"
                                        );
                                        reloadedElements = _scraper.Driver.FindElements(reloadXpath);
                                    }
                                    plusElements = reloadedElements;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "خطا در کلیک روی عنصر plus برای ژورنال {JournalId}", journal.Id);
                        }
                    }
                }

                Task.Delay(1000).Wait();

                var issues = _scraper.Driver?.FindElements(By.XPath("//a[contains(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'issue')]"))
                    ?.Select(x => x.GetAttribute("href"))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!)
                    .ToList();

                return issues ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در حالت سیناوب برای ژورنال {JournalId}", journal.Id);
                return new List<string>();
            }
        }
    }
}