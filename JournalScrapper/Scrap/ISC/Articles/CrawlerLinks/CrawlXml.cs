using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using DataLayer;
using Entities.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace JournalScrappers.Scrap.ISC.Articles
{
    /// <summary>
    /// Provides functionality to crawl and extract article information from XML sources
    /// </summary>
    public class CrawlXml : IDisposable
    {
        private XDocument? xmlDoc;
        private readonly DynamicDbContext _context;
        private readonly ILogger<CrawlXml> _logger;
        private readonly WebScraper _webScraper;
        private static readonly HttpClient _httpClient = CreateHttpClient();

        /// <summary>
        /// Initializes a new instance of the CrawlXml class
        /// </summary>
        /// <param name="context">Database context for data operations</param>
        /// <param name="logger">Logger instance for logging operations</param>
        /// <param name="webScraper">Web scraper instance for web operations</param>
        /// <exception cref="ArgumentNullException">Thrown when context or logger is null</exception>
        public CrawlXml(DynamicDbContext context, ILogger<CrawlXml> logger, WebScraper webScraper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._webScraper = webScraper;
        }

        /// <summary>
        /// Creates and configures a static HttpClient with optimal settings for performance
        /// </summary>
        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.All,
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 10,
                UseCookies = false,
                Proxy = null,
                UseProxy = false,
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "application/xml,text/xml,application/xhtml+xml,text/html;q=0.9,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9,fa;q=0.8");
            client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            client.DefaultRequestHeaders.ConnectionClose = false;

            return client;
        }

        /// <summary>
        /// Processes XML content from a given URL and extracts article information
        /// </summary>
        /// <param name="xmlUrl">The URL containing XML data</param>
        /// <param name="journalId">The journal identifier to associate articles with</param>
        /// <returns>True if processing was successful, false otherwise</returns>
        public async Task<bool> ProcessFromUrl(string xmlUrl, int journalId)
        {
            if (string.IsNullOrWhiteSpace(xmlUrl))
            {
                _logger.LogError("Invalid XML URL provided");
                return false;
            }

            try
            {
                // دانلود محتوا از URL
                string xmlContent = await GetContentOfUrlAsync(xmlUrl);
                if (string.IsNullOrWhiteSpace(xmlContent))
                {
                    _logger.LogError("Failed to download XML content from URL: {XmlUrl}", xmlUrl);
                    return false;
                }

                xmlDoc = XDocument.Parse(xmlContent);
                bool hasPublisherName = xmlDoc?.Descendants("PublisherName").Any() == true;

                // گرفتن لیست همه مقالات - بهینه شده برای پرفورمنس
                var articleElements = xmlDoc?
                    .Root?
                    .Descendants()
                    .Where(x => string.Equals(x.Name.LocalName, "article", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (articleElements == null || articleElements.Count == 0)
                {
                    _logger.LogWarning("No articles found in XML from URL: {XmlUrl}", xmlUrl);
                    return false;
                }

                // پردازش متوالی مقالات - بهینه شده برای پرفورمنس بدون تغییر منطق
                foreach (var articleElement in articleElements)
                {
                    try
                    {
                        Article? articleInfo = null;
                        bool isNewArticle = false;
                        // اگر مقاله وجود ندارد، ایجاد جدید
                        if (hasPublisherName)
                        {
                            articleInfo = ExtractArticleInfo(new XDocument(articleElement), journalId, xmlUrl);
                        }
                        else
                        {
                            articleInfo = ExtractSimpleArticleInfo(articleElement, journalId, xmlUrl);
                        }

                        if (articleInfo == null)
                        {
                            _logger.LogError("Failed to extract article info for one article in XML: {XmlUrl}", xmlUrl);
                            continue; // برو سراغ مقاله بعدی
                        }
                        var oldArticleInfo = await FindExistingArticleAsync(articleInfo.Doi, articleInfo.IscArticleId, xmlUrl, articleInfo.FullTextUrlIsc);

                        if (oldArticleInfo != null)
                        {
                            UpdateArticleInfo(oldArticleInfo, articleInfo);
                        }
                        else
                        {
                            isNewArticle = true;
                            _context.Articles.Add(articleInfo);
                            await _context.SaveChangesAsync();
                        }

                        if (oldArticleInfo != null)
                            articleInfo = oldArticleInfo;

                        // استخراج نویسنده‌ها و کلیدواژه‌ها
                        if (hasPublisherName)
                        {
                            await ExtractAuthorsAsync(new XDocument(articleElement), new XDocument(), articleInfo.Id, string.Empty, string.Empty);
                            await ExtractKeywordsAsync(new XDocument(articleElement), articleInfo.Id);
                        }
                        else
                        {
                            await ExtractSimpleAuthorsAndKeywordsAsync(new XDocument(articleElement), articleInfo.Id);
                        }

                        _logger.LogInformation(
                            "Article processed successfully from URL: {XmlUrl}. Title: {Title}, New: {IsNew}",
                            xmlUrl,
                            articleInfo.TitleEn ?? articleInfo.TitleFa,
                            isNewArticle
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process a single article in XML: {XmlUrl}", xmlUrl);
                        continue; // خطای یک مقاله نباید کل پروسه رو متوقف کنه
                    }
                }

                // ذخیره نهایی تغییرات باقی‌مانده
                if (_context.ChangeTracker.HasChanges())
                {
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process XML from URL: {XmlUrl}", xmlUrl);
                return false;
            }
        }

        /// <summary>
        /// Finds an existing article in the database based on various identifiers
        /// </summary>
        /// <param name="doi">Digital Object Identifier</param>
        /// <param name="iscId">ISC Article Identifier</param>
        /// <param name="xmlUrl">Source XML URL</param>
        /// <param name="fullTextUrlIsc">Full text URL from ISC</param>
        /// <returns>Existing article if found, null otherwise</returns>
        private async Task<Article?> FindExistingArticleAsync(string? doi, string? iscId, string? xmlUrl, string? fullTextUrlIsc)
        {
            var xmlDomain = StringTool.GetDomainFromUrl(xmlUrl ?? "");

            // بهینه‌سازی: بررسی DOI ابتدا (سریع‌ترین شناساگر)
            if (!string.IsNullOrWhiteSpace(doi))
            {
                var candidate = await _context.Articles.FirstOrDefaultAsync(x => x.Doi == doi);
                if (candidate != null) return candidate;
            }

            // بررسی ISC ID
            if (!string.IsNullOrWhiteSpace(iscId))
            {
                var candidate = await _context.Articles.FirstOrDefaultAsync(x =>
                    x.IscArticleId == iscId &&
                    !string.IsNullOrWhiteSpace(x.PageUrlIsc) &&
                    x.PageUrlIsc.Contains(xmlDomain));
                if (candidate != null) return candidate;
            }

            // بررسی Full Text URL
            if (!string.IsNullOrWhiteSpace(fullTextUrlIsc))
            {
                var candidate = await _context.Articles.FirstOrDefaultAsync(x => x.FullTextUrlIsc == fullTextUrlIsc);
                if (candidate != null) return candidate;
            }

            // Title-based fallback (آخرین گزینه - کندترین)
            var titleFa = GetTagValue("VernacularTitle") ?? GetTagValue("title_fa");
            var titleEn = GetTagValue("ArticleTitle") ?? GetTagValue("title_en");

            if (!string.IsNullOrWhiteSpace(titleFa))
            {
                var candidate = await _context.Articles.FirstOrDefaultAsync(x => x.TitleFa == titleFa);
                if (candidate != null) return candidate;
            }

            if (!string.IsNullOrWhiteSpace(titleEn))
            {
                var candidate = await _context.Articles.FirstOrDefaultAsync(x => x.TitleEn == titleEn);
                if (candidate != null) return candidate;
            }

            return null;
        }

        /// <summary>
        /// Parses a string to integer, returning null if parsing fails
        /// </summary>
        /// <param name="input">String to parse</param>
        /// <returns>Parsed integer or null</returns>
        private int? ParseInt(string? input) => int.TryParse(input, out var value) ? value : null;

        /// <summary>
        /// Updates existing article information with new data
        /// </summary>
        /// <param name="oldArticle">Existing article to update</param>
        /// <param name="newArticle">New article data</param>
        private void UpdateArticleInfo(Article oldArticle, Article newArticle)
        {
            try
            {
                bool updated = false;

                // فیلدهای فارسی
                if (string.IsNullOrEmpty(oldArticle.TitleFa) && !string.IsNullOrEmpty(newArticle.TitleFa))
                {
                    oldArticle.TitleFa = newArticle.TitleFa;
                    updated = true;
                }

                if (string.IsNullOrEmpty(oldArticle.AbstractFa) && !string.IsNullOrEmpty(newArticle.AbstractFa))
                {
                    oldArticle.AbstractFa = newArticle.AbstractFa;
                    updated = true;
                }

                // فیلدهای انگلیسی
                if (string.IsNullOrEmpty(oldArticle.TitleEn) && !string.IsNullOrEmpty(newArticle.TitleEn))
                {
                    oldArticle.TitleEn = newArticle.TitleEn;
                    updated = true;
                }

                if (string.IsNullOrEmpty(oldArticle.AbstractEn) && !string.IsNullOrEmpty(newArticle.AbstractEn))
                {
                    oldArticle.AbstractEn = newArticle.AbstractEn;
                    updated = true;
                }

                // سایر فیلدها
                if (string.IsNullOrEmpty(oldArticle.VolumeEn) && !string.IsNullOrEmpty(newArticle.VolumeEn))
                {
                    oldArticle.VolumeEn = newArticle.VolumeEn;
                    updated = true;
                }

                if (string.IsNullOrEmpty(oldArticle.IssueEn) && !string.IsNullOrEmpty(newArticle.IssueEn))
                {
                    oldArticle.IssueEn = newArticle.IssueEn;
                    updated = true;
                }

                // اگر فیلدی آپدیت شد، تاریخ آخرین بروزرسانی را تغییر بده
                if (updated)
                {
                    oldArticle.LastUpdate = DateTime.Now;
                    _logger.LogInformation("Updated article fields for Article ID: {ArticleId}", oldArticle.Id);
                }
                else
                {
                    _logger.LogInformation("No fields updated for Article ID: {ArticleId}", oldArticle.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update article info for Article ID: {ArticleId}", oldArticle.Id);
            }
        }

        /// <summary>
        /// Extracts comprehensive article information from XML document
        /// </summary>
        /// <param name="xmlDoc">XML document containing article data</param>
        /// <param name="journalId">Journal identifier</param>
        /// <param name="xmlUrl">Source XML URL</param>
        /// <returns>Extracted article information or null if extraction fails</returns>
        private Article? ExtractArticleInfo(XDocument xmlDocument, int journalId, string xmlUrl)
        {
            try
            {
                xmlDoc = xmlDocument;
                var articleInfo = new Article
                {
                    VolumeEn = GetTagValue("Volume"),
                    IssueEn = GetTagValue("Issue"),
                    PageStart = int.TryParse(GetTagValue("FirstPage"), out int firstPage) ? firstPage : null,
                    PageEnd = int.TryParse(GetTagValue("LastPage"), out int lastPage) ? lastPage : null,
                    Type = GetTagValue("PublicationType"),
                    IscArticleId = GetTagValue("ELocationID", attributes: new Dictionary<string, string> { { "EIdType", "pii" } }),
                    Doi = GetTagValue("ELocationID", attributes: new Dictionary<string, string> { { "EIdType", "doi" } }),
                    JournalId = journalId,
                    PageUrlIsc = xmlUrl,
                    IsIsc = true,
                    LastUpdate = DateTime.Now,
                    FullTextUrlIsc = GetTagValue("ArchiveCopySource"),
                    OriginalLanguage = GetTagValue("Language"),
                    SourceType = "ISC",
                };

                // Process titles
                var articleTitle = GetTagValue("ArticleTitle");
                var vernacularTitle = GetTagValue("VernacularTitle");

                if (articleTitle.ContainsPersianCharacters() == true)
                {
                    articleInfo.TitleFa = articleTitle;
                    articleInfo.TitleEn = vernacularTitle;
                }
                else if (vernacularTitle.ContainsPersianCharacters() == true)
                {
                    articleInfo.TitleFa = vernacularTitle;
                    articleInfo.TitleEn = articleTitle;
                }
                else
                {
                    // Default assignment if language detection fails
                    articleInfo.TitleEn = articleTitle;
                    articleInfo.TitleFa = vernacularTitle;
                }

                // Process abstracts
                var abstractText = GetTagValue("Abstract");
                var otherAbstract = GetTagValue("OtherAbstract");

                if (abstractText.ContainsPersianCharacters() == true)
                {
                    articleInfo.AbstractFa = abstractText;
                    articleInfo.AbstractEn = otherAbstract;
                }
                else if (otherAbstract.ContainsPersianCharacters() == true)
                {
                    articleInfo.AbstractFa = otherAbstract;
                    articleInfo.AbstractEn = abstractText;
                }
                else
                {
                    // Default assignment if language detection fails
                    articleInfo.AbstractEn = abstractText;
                    articleInfo.AbstractFa = otherAbstract;
                }

                var pubDateElem = xmlDoc.Descendants("PubDate").FirstOrDefault();
                if (pubDateElem != null)
                {
                    articleInfo.PublicationYear = int.TryParse(pubDateElem.Element("Year")?.Value?.Trim(), out var y) ? y : null;
                    articleInfo.PublicationMonth = int.TryParse(pubDateElem.Element("Month")?.Value?.Trim(), out var m) ? m : null;
                    articleInfo.PublicationDay = int.TryParse(pubDateElem.Element("Day")?.Value?.Trim(), out var d) ? d : null;
                }

                if (!articleInfo.TitleEn.IsNullOrEmpty())
                    articleInfo.ArticleIdentifier = articleInfo.TitleEn.ToIdentifierText();
                else
                    articleInfo.ArticleIdentifier = articleInfo.TitleFa.ToIdentifierText();

                return articleInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract article info from XML");
                return null;
            }
        }

        /// <summary>
        /// Extracts simplified article information from XML element
        /// </summary>
        /// <param name="documentArticle">XML element containing article data</param>
        /// <param name="journalId">Journal identifier</param>
        /// <param name="xmlUrl">Source XML URL</param>
        /// <returns>Extracted article information or null if extraction fails</returns>
        private Article? ExtractSimpleArticleInfo(XElement? documentArticle, int journalId, string xmlUrl)
        {
            if (documentArticle == null)
            {
                _logger.LogError("Article element not found in XML");
                return null;
            }

            try
            {
                var articleInfo = new Article
                {
                    Doi = GetTagValue("article_id_doi", documentArticle) ?? "",
                    IscArticleId = GetTagValue("article_id_pii", documentArticle) ?? "",
                    Type = GetTagValue("content_type", documentArticle) ?? "",
                    PageStart = int.TryParse(GetTagValue("start_page", documentArticle), out var start) ? start : null,
                    PageEnd = int.TryParse(GetTagValue("end_page", documentArticle), out var end) ? end : null,
                    FullTextUrlIsc = GetTagValue("web_url", documentArticle) ?? "",
                    JournalId = journalId,
                    PageUrlIsc = xmlUrl,
                    IsIsc = true,
                    LastUpdate = DateTime.Now
                };

                // Language
                articleInfo.OriginalLanguage = GetTagValue("language", documentArticle) ?? "en";

                // Process titles
                var titleFa = GetTagValue("title_fa", documentArticle) ?? "";
                var titleEn = GetTagValue("title", documentArticle) ?? "";

                if (titleFa.ContainsPersianCharacters() ?? false)
                {
                    articleInfo.TitleFa = titleFa;
                    articleInfo.TitleEn = titleEn;
                }
                else if (titleEn.ContainsPersianCharacters() ?? false)
                {
                    articleInfo.TitleFa = titleEn;
                    articleInfo.TitleEn = titleFa;
                }
                else
                {
                    articleInfo.TitleFa = titleFa;
                    articleInfo.TitleEn = titleEn;
                }

                // Process abstracts
                var abstractFa = GetTagValue("abstract_fa", documentArticle) ?? "";
                var abstractEn = GetTagValue("abstract", documentArticle) ?? "";

                if (abstractFa.ContainsPersianCharacters() ?? false)
                {
                    articleInfo.AbstractFa = abstractFa;
                    articleInfo.AbstractEn = abstractEn;
                }
                else if (abstractEn.ContainsPersianCharacters() ?? false)
                {
                    articleInfo.AbstractFa = abstractEn;
                    articleInfo.AbstractEn = abstractFa;
                }
                else
                {
                    articleInfo.AbstractFa = abstractFa;
                    articleInfo.AbstractEn = abstractEn;
                }

                return articleInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract simple article info from XML");
                return null;
            }
        }

        /// <summary>
        /// Extracts authors and keywords from simplified XML format
        /// </summary>
        /// <param name="xmlDoc">XML document containing author and keyword data</param>
        /// <param name="articleId">Article identifier to associate data with</param>
        private async Task ExtractSimpleAuthorsAndKeywordsAsync(XDocument xmlDoc, int articleId)
        {
            try
            {
                var authorElements = xmlDoc.Descendants("author").ToList();
                foreach (var (authorElement, index) in authorElements.Select((elem, i) => (elem, i)))
                {
                    string firstNameFa = GetTagValue("first_name_fa", 0, authorElement.Document) ?? "";
                    string lastNameFa = GetTagValue("last_name_fa", 0, authorElement.Document) ?? "";
                    string firstNameEn = GetTagValue("first_name", 0, authorElement.Document) ?? "";
                    string lastNameEn = GetTagValue("last_name", 0, authorElement.Document) ?? "";
                    string affiliationFa = GetTagValue("affiliation_fa", 0, authorElement.Document) ?? "";
                    string affiliationEn = GetTagValue("affiliation", 0, authorElement.Document) ?? "";
                    string identifier = GetTagValue("orcid", 0, authorElement.Document) ?? "";

                    // --- چک کردن پروفسور بر اساس نام و دانشگاه
                    Professor? professor = null;
                    if ((affiliationFa?.Contains("دانشگاه اصفهان") == true) ||
                        (affiliationEn?.Contains("University of Isfahan") == true))
                    {
                        // اولویت با ORCID اگر وجود دارد
                        if (!string.IsNullOrEmpty(identifier))
                        {
                            professor = _context.Professors.FirstOrDefault(x =>
                                (x.FirstNameFa == firstNameFa && x.LastNameFa == lastNameFa) ||
                                (x.FirstNameEn == firstNameEn && x.LastNameEn == lastNameEn));
                        }

                        // اگر پروفسور با ORCID پیدا نشد → جستجو با نام فارسی
                        if (professor == null && !string.IsNullOrEmpty(firstNameFa) && !string.IsNullOrEmpty(lastNameFa))
                            professor = _context.Professors.FirstOrDefault(x =>
                                x.FirstNameFa == firstNameFa && x.LastNameFa == lastNameFa);

                        // اگر باز هم پیدا نشد → جستجو با نام انگلیسی
                        if (professor == null && !string.IsNullOrEmpty(firstNameEn) && !string.IsNullOrEmpty(lastNameEn))
                            professor = _context.Professors.FirstOrDefault(x =>
                                x.FirstNameEn == firstNameEn && x.LastNameEn == lastNameEn);
                    }

                    // --- ساخت یا بازیابی CoAuthor فقط اگر پروفسور نبود
                    CoAuthor? author = null;

                    if (professor == null)
                    {
                        var existingAuthor = _context.ArticleCoAuthors.FirstOrDefault(a =>
                            (!string.IsNullOrEmpty(identifier) && a.Identifier == identifier) ||
                            (!string.IsNullOrEmpty(firstNameFa) && !string.IsNullOrEmpty(lastNameFa) &&
                             a.FirstNameFa == firstNameFa && a.LastNameFa == lastNameFa) ||
                            (!string.IsNullOrEmpty(firstNameEn) && !string.IsNullOrEmpty(lastNameEn) &&
                             a.FirstNameEn == firstNameEn && a.LastNameEn == lastNameEn));

                        author = existingAuthor ?? new CoAuthor
                        {
                            FirstNameFa = firstNameFa,
                            LastNameFa = lastNameFa,
                            FirstNameEn = firstNameEn,
                            LastNameEn = lastNameEn,
                            AffiliationFa = affiliationFa,
                            AffiliationEn = affiliationEn,
                            Identifier = identifier,
                            LastUpdate = DateTime.UtcNow
                        };

                        if (existingAuthor == null)
                        {
                            _context.ArticleCoAuthors.Add(author);
                            await _context.SaveChangesAsync();
                        }
                    }

                    // --- بررسی عدم وجود نویسنده در ArticleAuthors قبل از افزودن
                    ArticleAuthor? existingArticleAuthor = null;

                    existingArticleAuthor = _context.ArticleAuthors
                    .FirstOrDefault(aa => aa.ArticleId == articleId &&
                                          ((aa.ProfessorId.HasValue && professor != null && aa.ProfessorId == professor.Id) ||
                                           (author != null && aa.CoAuthorId.HasValue && aa.CoAuthorId == author.Id)));

                    if (existingArticleAuthor == null)
                    {
                        var articleAuthor = new ArticleAuthor
                        {
                            ArticleId = articleId,
                            Order = index + 1,
                            CoAuthorId = professor == null ? author?.Id : null,
                            ProfessorId = professor?.Id,
                            LastUpdate = DateTime.UtcNow
                        };
                        _context.ArticleAuthors.Add(articleAuthor);
                    }
                }

                // --- اضافه کردن کلیدواژه‌ها
                var keywords = xmlDoc.Descendants("keyword_fa").Concat(xmlDoc.Descendants("keyword")).ToList();
                foreach (var keywordNode in keywords)
                {
                    var keywordparams = keywordNode.Value.Split('،', ',');
                    foreach (var param in keywordparams)
                    {
                        if (!string.IsNullOrEmpty(param))
                        {
                            var existingKeyword = _context.ArticleKeywords
                                .FirstOrDefault(k => k.ArticleId == articleId && k.Keyword == param);

                            if (existingKeyword == null)
                            {
                                var keyword = new ArticleKeyword
                                {
                                    ArticleId = articleId,
                                    Keyword = param,
                                    IsPersian = param.ContainsPersianCharacters() ?? false,
                                    LastUpdate = DateTime.Now,
                                    IsAuthorKeyword = false,
                                };
                                _context.Add(keyword);
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract authors and keywords from XML for article ID: {ArticleId}", articleId);
            }
        }

        /// <summary>
        /// Extracts authors from XML document with corresponding author detection
        /// </summary>
        /// <param name="doc">Primary XML document</param>
        /// <param name="docEn">English XML document (optional)</param>
        /// <param name="articleId">Article identifier</param>
        /// <param name="corresponding">Corresponding author name</param>
        /// <param name="correspondingEmail">Corresponding author email</param>
        private async Task ExtractAuthorsAsync(XDocument doc, XDocument? docEn, int articleId, string corresponding, string correspondingEmail)
        {
            try
            {
                var authorCount = doc.Descendants("Author").Count();
                var correspondingWords = (corresponding ?? "")
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim().ToLower())
                    .ToList();
                var authors = new List<ArticleAuthor>();

                for (int i = 0; i < authorCount; i++)
                {
                    string firstNameFa = GetTagValue("FirstName", i, doc) ?? "";
                    string lastNameFa = GetTagValue("LastName", i, doc) ?? "";
                    string firstNameEn = GetTagValue("FirstName", i, docEn) ?? "";
                    string lastNameEn = GetTagValue("LastName", i, docEn) ?? "";
                    string fullNameEn = (firstNameEn + lastNameEn + firstNameFa + lastNameFa).Replace(" ", "").ToLower();
                    var affiliationFa = GetTagValue("Affiliation", i, doc);
                    var affiliationEn = GetTagValue("Affiliation", i, docEn);

                    var author = new CoAuthor
                    {
                        Identifier = GetTagValue("Identifier", i, docEn),
                        LastUpdate = DateTime.UtcNow
                    };

                  
                    if (affiliationEn.ContainsPersianCharacters() ?? true)
                    {
                        author.AffiliationFa = affiliationEn;
                        author.AffiliationEn = affiliationFa;
                    }
                    else
                    {
                        author.AffiliationFa = affiliationFa;
                        author.AffiliationEn = affiliationEn;
                    }

                    if (lastNameEn.ContainsPersianCharacters() ?? true)
                    {
                        author.LastNameFa = lastNameEn;
                        author.LastNameEn = lastNameFa;

                        author.FirstNameFa = firstNameEn;
                        author.FirstNameEn = firstNameFa;
                    }
                    else
                    {
                        author.LastNameFa = lastNameFa;
                        author.LastNameEn = lastNameEn;

                        author.FirstNameFa = firstNameFa;
                        author.FirstNameEn = firstNameEn;
                    }
                    bool isCorresponding = authorCount == 1 || (!string.IsNullOrWhiteSpace(corresponding) &&
                                          correspondingWords.All(word => fullNameEn.Contains(word.ToLower().Trim())));

                    if (isCorresponding)
                        author.Email = correspondingEmail;

                    Professor? professor = null;
                    if (!author.Identifier.IsNullOrEmpty())
                        professor = await _context.Professors.FirstOrDefaultAsync(x => x.OrcidId == author.Identifier);
                    if (professor == null && (author.AffiliationFa?.Contains("دانشگاه اصفهان") == true) ||
                        (author.AffiliationEn?.Contains("University of Isfahan") == true))
                    {
                        if (professor == null && !string.IsNullOrEmpty(author.FirstNameFa) && !string.IsNullOrEmpty(author.LastNameFa))
                            professor = await _context.Professors.FirstOrDefaultAsync(x =>
                                x.FirstNameFa == author.FirstNameFa && x.LastNameFa == author.LastNameFa);

                        if (professor == null && !string.IsNullOrEmpty(author.FirstNameEn) && !string.IsNullOrEmpty(author.LastNameEn))
                            professor = await _context.Professors.FirstOrDefaultAsync(x =>
                                x.FirstNameEn == author.FirstNameEn && x.LastNameEn == author.LastNameEn);
                    }

                    var query = _context.ArticleCoAuthors.AsQueryable();

                    if (!string.IsNullOrWhiteSpace(author.Identifier))
                    {
                        query = query.Where(x => x.Identifier == author.Identifier);
                    }
                    else
                    {
                        var nameCondition = query.Where(x =>
                            (!string.IsNullOrWhiteSpace(author.LastNameEn) && !string.IsNullOrWhiteSpace(author.FirstNameEn) &&
                             x.LastNameEn == author.LastNameEn && x.FirstNameEn == author.FirstNameEn)
                            ||
                            (!string.IsNullOrWhiteSpace(author.LastNameEn) && !string.IsNullOrWhiteSpace(author.FirstNameFa) &&
                             x.LastNameEn == author.LastNameEn && x.FirstNameFa == author.FirstNameFa)
                        );

                        if (!string.IsNullOrWhiteSpace(author.AffiliationEn) || !string.IsNullOrWhiteSpace(author.AffiliationFa))
                        {
                            nameCondition = nameCondition.Where(x =>
                                (string.IsNullOrWhiteSpace(author.AffiliationEn) || x.AffiliationEn == author.AffiliationEn) &&
                                (string.IsNullOrWhiteSpace(author.AffiliationFa) || x.AffiliationFa == author.AffiliationFa)
                            );
                        }

                        query = nameCondition;
                    }

                    var matchedAuthor = query.FirstOrDefault();
                    if (matchedAuthor != null)
                        author = matchedAuthor;

                    var existingArticleAuthor = _context.ArticleAuthors
                        .FirstOrDefault(aa => aa.ArticleId == articleId && aa.CoAuthorId == author.Id);

                    if (existingArticleAuthor == null)
                    {
                        var articleAuthor = new ArticleAuthor
                        {
                            ArticleId = articleId,
                            Order = i + 1,
                            CoAuthor = author,
                            LastUpdate = DateTime.UtcNow,
                            IsCorrespondingAuthor = isCorresponding,
                            ProfessorId = professor?.Id
                        };
                        authors.Add(articleAuthor);
                    }
                }

                if (!authors.Any(x => x.IsCorrespondingAuthor == true) && !string.IsNullOrWhiteSpace(corresponding))
                {
                    foreach (var author in authors)
                    {
                        var full = (author.CoAuthor.FirstNameFa + author.CoAuthor.LastNameFa + author.CoAuthor.FirstNameEn + author.CoAuthor.LastNameEn).Replace(" ", "").ToLower();
                        if (correspondingWords.Any(word => full.Contains(word.ToLower().Trim())))
                        {
                            author.CoAuthor.Email = correspondingEmail;
                            author.IsCorrespondingAuthor = true;
                            break;
                        }
                    }
                }

                _context.ArticleAuthors.AddRange(authors);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract authors from XML for article ID: {ArticleId}", articleId);
            }
        }

        /// <summary>
        /// Extracts keywords from XML document
        /// </summary>
        /// <param name="doc">XML document containing keyword data</param>
        /// <param name="articleId">Article identifier to associate keywords with</param>
        /// <returns>True if extraction was successful, false otherwise</returns>
        private async Task<bool> ExtractKeywordsAsync(XDocument? doc, int articleId)
        {
            if (doc == null || articleId == 0)
                return false;

            try
            {
                var nodes = doc.Descendants("Object").ToList();
                foreach (var node in nodes)
                {
                    string param = node.Descendants("Param").FirstOrDefault()?.Value ?? "";
                    if (!string.IsNullOrEmpty(param))
                    {
                        var existingKeyword = _context.ArticleKeywords
                            .FirstOrDefault(k => k.ArticleId == articleId && k.Keyword == param);

                        if (existingKeyword == null)
                        {
                            var keyword = new ArticleKeyword
                            {
                                ArticleId = articleId,
                                Keyword = param,
                                IsPersian = param.ContainsPersianCharacters() ?? false,
                                IsAuthorKeyword = false,
                                LastUpdate = DateTime.Now,
                            };
                            _context.Add(keyword);
                        }
                    }
                }
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract keywords from XML for article ID: {ArticleId}", articleId);
                return false;
            }
        }

        /// <summary>
        /// Retrieves the value of a specific XML tag with optional attributes filtering
        /// </summary>
        /// <param name="tagName">Name of the XML tag</param>
        /// <param name="selectNumber">Index of the element if multiple exist</param>
        /// <param name="document">XML document to search in</param>
        /// <param name="attributes">Optional attributes to filter by</param>
        /// <returns>Tag value or empty string if not found</returns>
        private readonly Dictionary<string, string> _tagValueCache = new();

        private string GetTagValue(string tagName, int selectNumber = 0, XDocument? document = null, Dictionary<string, string>? attributes = null)
        {
            try
            {
                document ??= xmlDoc;
                if (document == null)
                    return "";

                var elements = document.Descendants()
                    .Where(e => string.Equals(e.Name.LocalName, tagName, StringComparison.OrdinalIgnoreCase));

                if (attributes != null && attributes.Any())
                {
                    elements = elements.Where(e => attributes.All(attr =>
                        e.Attributes().Any(a =>
                            string.Equals(a.Name.LocalName, attr.Key, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(a.Value, attr.Value, StringComparison.OrdinalIgnoreCase))));
                }

                var result = elements.ElementAtOrDefault(selectNumber)?.Value.Trim() ?? "";
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get value for tag: {TagName}", tagName);
                return "";
            }
        }

        /// <summary>
        /// Retrieves the value of a specific XML tag from an XML element
        /// </summary>
        /// <param name="tagName">Name of the XML tag</param>
        /// <param name="document">XML element to search in</param>
        /// <param name="selectNumber">Index of the element if multiple exist</param>
        /// <returns>Tag value or empty string if not found</returns>
        private string GetTagValue(string tagName, XElement? document, int selectNumber = 0)
        {
            try
            {
                return document?.Descendants(tagName).ElementAtOrDefault(selectNumber)?.Value.Trim() ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get value for tag: {TagName} from document", tagName);
                return "";
            }
        }

        /// <summary>
        /// Retrieves content from a URL, handling both web pages and file downloads
        /// </summary>
        /// <param name="url">URL to retrieve content from</param>
        /// <returns>Content as string</returns>
        /// <exception cref="Exception">Thrown when content retrieval fails</exception>
        private async Task<string> GetContentOfUrlAsync(string url)
        {
            try
            {
                try
                {
                    _webScraper.OpenUrl(url);
                    string? pageSource = _webScraper.Driver?.PageSource;
                    if (pageSource != null && pageSource.TrimStart().StartsWith("<?xml") || pageSource!.Contains("<article") || pageSource.Contains("<Article") || pageSource.Contains("<root"))
                    {
                        return pageSource;
                    }
                    _logger.LogWarning("Page source doesn't appear to be XML content, attempting direct download for URL: {Url}", url);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "WebScraper failed for URL: {Url}, attempting direct download", url);
                }
                var uri = new Uri(url);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Referrer = new Uri($"{uri.Scheme}://{uri.Host}/");

                using var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync();

                // Handle compressed content efficiently
                if (response.Content.Headers.ContentEncoding?.Contains("gzip") == true ||
                    response.Content.Headers.ContentEncoding?.Contains("deflate") == true)
                {
                    using var decompressedStream = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress);
                    using var reader = new StreamReader(decompressedStream, Encoding.UTF8);
                    var content = await reader.ReadToEndAsync();
                    _logger.LogInformation("Successfully downloaded decompressed content from URL: {Url}", url);
                    return content;
                }
                else
                {
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    var content = await reader.ReadToEndAsync();
                    _logger.LogInformation("Successfully downloaded content from URL: {Url}", url);
                    return content;
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HttpRequestException occurred while fetching content from {Url}", url);
                throw new Exception($"Failed to fetch XML from {url}: {httpEx.Message}", httpEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General exception occurred while fetching content from {Url}", url);
                throw new Exception($"Failed to fetch content from {url}: {ex.Message}", ex);
            }
        }
        
        public void Dispose()
        {
            // HttpClient is static and shared, don't dispose it here
            // It will be disposed when the application shuts down
            GC.SuppressFinalize(this);
        }
    }
}
