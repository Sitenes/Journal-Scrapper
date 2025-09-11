using DataLayer;
using Entities.Models.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.IdentityModel.Tokens;

namespace JournalScrappers.Scrap.ISC.Articles
{
    /// <summary>
    /// Provides functionality to crawl and extract article information from XML sources
    /// </summary>
    public class CrawlXml
    {
        private XDocument? xmlDoc;
        private readonly DynamicDbContext _context;
        private readonly ILogger<CrawlXml> _logger;
        private readonly WebScraper _webScraper;

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
        /// Processes XML content from a given URL and extracts article information
        /// </summary>
        /// <param name="xmlUrl">The URL containing XML data</param>
        /// <param name="journalId">The journal identifier to associate articles with</param>
        /// <returns>True if processing was successful, false otherwise</returns>
        public bool ProcessFromUrl(string xmlUrl, int journalId)
        {
            if (string.IsNullOrWhiteSpace(xmlUrl))
            {
                _logger.LogError("Invalid XML URL provided");
                return false;
            }

            try
            {
                // دانلود محتوا از URL
                string xmlContent = GetContentOfUrl(xmlUrl);
                if (string.IsNullOrWhiteSpace(xmlContent))
                {
                    _logger.LogError("Failed to download XML content from URL: {XmlUrl}", xmlUrl);
                    return false;
                }

                xmlDoc = XDocument.Parse(xmlContent);
                bool hasPublisherName = xmlDoc?.Descendants("PublisherName").Any() == true;

                // گرفتن لیست همه مقالات
                var articleElements = xmlDoc?
                    .Root?
                    .Descendants()
                    .Where(x => x.Name.LocalName.ToLower() == "article")
                    .ToList();
                if (articleElements == null || articleElements.Count == 0)
                {
                    _logger.LogWarning("No articles found in XML from URL: {XmlUrl}", xmlUrl);
                    return false;
                }

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
                        var oldArticleInfo = FindExistingArticle(articleInfo.Doi, articleInfo.IscArticleId, xmlUrl, articleInfo.FullTextUrlIsc);

                        if (oldArticleInfo != null)
                        {
                            UpdateArticleInfo(oldArticleInfo, articleInfo);
                        }
                        else
                        {
                            isNewArticle = true;
                            _context.Articles.Add(articleInfo);
                        }

                        _context.SaveChanges();
                        if (oldArticleInfo != null)
                            articleInfo = oldArticleInfo;
                        // استخراج نویسنده‌ها و کلیدواژه‌ها
                        if (hasPublisherName)
                        {
                            ExtractAuthors(new XDocument(articleElement), null, articleInfo.Id, string.Empty, string.Empty);
                            ExtractKeywords(new XDocument(articleElement), articleInfo.Id);
                        }
                        else
                        {
                            ExtractSimpleAuthorsAndKeywords(new XDocument(articleElement), articleInfo.Id);
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
        private Article? FindExistingArticle(string? doi, string? iscId, string? xmlUrl, string? fullTextUrlIsc)
        {
            var xmlDomain = StringTool.GetDomainFromUrl(xmlUrl);

            // Primary identifiers
            var candidate = _context.Articles
                .FirstOrDefault(x =>
                    !string.IsNullOrWhiteSpace(doi) &&
                        (x.Doi == doi)
                    || !string.IsNullOrWhiteSpace(iscId) &&
                        x.IscArticleId == iscId &&
                        !string.IsNullOrWhiteSpace(x.PageUrlIsc) &&
                        x.PageUrlIsc.Contains(xmlDomain)
                    || !string.IsNullOrWhiteSpace(fullTextUrlIsc) &&
                        x.FullTextUrlIsc == fullTextUrlIsc
                );

            if (candidate is not null)
                return candidate;

            // Title-based fallback
            var titleFa = GetTagValue("VernacularTitle") ?? GetTagValue("title_fa");
            var titleEn = GetTagValue("ArticleTitle") ?? GetTagValue("title_en");

            if (!string.IsNullOrWhiteSpace(titleFa) || !string.IsNullOrWhiteSpace(titleEn))
            {
                candidate = _context.Articles
                    .FirstOrDefault(x =>
                        !string.IsNullOrWhiteSpace(titleFa) && x.TitleFa == titleFa
                     || !string.IsNullOrWhiteSpace(titleEn) && x.TitleEn == titleEn
                    );
                if (candidate is not null)
                    return candidate;
            }

            return candidate;
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
        private Article? ExtractArticleInfo(XDocument xmlDoc, int journalId, string xmlUrl)
        {
            try
            {
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
        private void ExtractSimpleAuthorsAndKeywords(XDocument xmlDoc, int articleId)
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
                            _context.SaveChanges();
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

                _context.SaveChanges();
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
        private void ExtractAuthors(XDocument doc, XDocument? docEn, int articleId, string corresponding, string correspondingEmail)
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

                    var author = new CoAuthor
                    {
                        FirstNameFa = firstNameFa,
                        LastNameFa = lastNameFa,
                        FirstNameEn = firstNameEn,
                        LastNameEn = lastNameEn,
                        AffiliationFa = GetTagValue("Affiliation", i, doc),
                        AffiliationEn = GetTagValue("Affiliation", i, docEn),
                        Identifier = GetTagValue("Identifier", i, docEn),
                        LastUpdate = DateTime.UtcNow
                    };

                    bool isCorresponding = authorCount == 1 || (!string.IsNullOrWhiteSpace(corresponding) &&
                                          correspondingWords.All(word => fullNameEn.Contains(word.ToLower().Trim())));

                    if (isCorresponding)
                        author.Email = correspondingEmail;

                    Professor? professor = null;
                    if ((author.AffiliationFa?.Contains("دانشگاه اصفهان") == true) ||
                        (author.AffiliationEn?.Contains("University of Isfahan") == true))
                    {
                        if (!string.IsNullOrEmpty(author.Identifier))
                            professor = _context.Professors.FirstOrDefault(x =>
                                (x.FirstNameFa == author.FirstNameFa && x.LastNameFa == author.LastNameFa) ||
                                (x.FirstNameEn == author.FirstNameEn && x.LastNameEn == author.LastNameEn));

                        if (professor == null && !string.IsNullOrEmpty(author.FirstNameFa) && !string.IsNullOrEmpty(author.LastNameFa))
                            professor = _context.Professors.FirstOrDefault(x =>
                                x.FirstNameFa == author.FirstNameFa && x.LastNameFa == author.LastNameFa);

                        if (professor == null && !string.IsNullOrEmpty(author.FirstNameEn) && !string.IsNullOrEmpty(author.LastNameEn))
                            professor = _context.Professors.FirstOrDefault(x =>
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
                _context.SaveChanges();
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
        private bool ExtractKeywords(XDocument? doc, int articleId)
        {
            if (doc == null)
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
                _context.SaveChanges();
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

                return elements.ElementAtOrDefault(selectNumber)?.Value.Trim() ?? "";
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
        private string GetContentOfUrl(string url)
        {
            try
            {
                // First attempt: Try using WebScraper for web pages
                try
                {
                    _webScraper.GetPageContent(url);
                    string pageSource = _webScraper.Driver.PageSource;

                    // Check if the content looks like XML
                    if (pageSource.TrimStart().StartsWith("<?xml") || pageSource.Contains("<article") || pageSource.Contains("<Article") || pageSource.Contains("<root"))
                    {
                        return pageSource;
                    }

                    // If it doesn't look like XML content, it might be a file download page
                    _logger.LogWarning("Page source doesn't appear to be XML content, attempting direct download for URL: {Url}", url);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "WebScraper failed for URL: {Url}, attempting direct download", url);
                }


                using (var webClient = new WebClient())
                {
                    // Set headers to mimic a real browser
                    webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36");
                    webClient.Headers.Add("Accept", "application/xml,text/xml,application/xhtml+xml,text/html;q=0.9,*/*;q=0.8");
                    webClient.Headers.Add("Accept-Language", "en-US,en;q=0.9,fa;q=0.8");
                    webClient.Headers.Add("Cache-Control", "no-cache");

                    var uri = new Uri(url);
                    webClient.Headers.Add("Referer", $"{uri.Scheme}://{uri.Host}/");

                    // Download content as bytes first
                    byte[] data = webClient.DownloadData(url);

                    // Check if the content is gzipped
                    if (data.Length >= 2 && data[0] == 0x1f && data[1] == 0x8b)
                    {
                        // Content is gzipped, decompress it
                        using (var compressedStream = new MemoryStream(data))
                        using (var gzipStream = new System.IO.Compression.GZipStream(compressedStream, System.IO.Compression.CompressionMode.Decompress))
                        using (var decompressedStream = new MemoryStream())
                        {
                            gzipStream.CopyTo(decompressedStream);
                            byte[] decompressedData = decompressedStream.ToArray();

                            // Try to detect encoding from BOM or use UTF-8 as default
                            string content = DetectEncodingAndDecode(decompressedData);

                            _logger.LogInformation("Successfully downloaded and decompressed gzipped content from URL: {Url}", url);
                            return content;
                        }
                    }
                    else
                    {
                        // Content is not gzipped, decode directly
                        string content = DetectEncodingAndDecode(data);

                        _logger.LogInformation("Successfully downloaded content directly from URL: {Url}", url);
                        return content;
                    }
                }
            }
            catch (WebException webEx)
            {
                string errorDetails = "";
                if (webEx.Response != null)
                {
                    using var errorResponse = webEx.Response;
                    using var stream = errorResponse.GetResponseStream();
                    if (stream != null)
                    {
                        using var reader = new StreamReader(stream, Encoding.UTF8);
                        errorDetails = reader.ReadToEnd();
                    }
                }

                _logger.LogError(webEx, "WebException occurred while fetching content from {Url}. Status: {Status}. Response: {Response}",
                    url, webEx.Status, errorDetails);
                throw new Exception($"Failed to fetch XML from {url}: {webEx.Message}\nServer Response: {errorDetails}", webEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General exception occurred while fetching content from {Url}", url);
                throw new Exception($"Failed to fetch content from {url}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Detects encoding from byte array and decodes to string
        /// </summary>
        /// <param name="data">Byte array to decode</param>
        /// <returns>Decoded string</returns>
        private string DetectEncodingAndDecode(byte[] data)
        {
            // Check for BOM (Byte Order Mark)
            if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
            {
                // UTF-8 BOM
                return Encoding.UTF8.GetString(data, 3, data.Length - 3);
            }
            else if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xFE)
            {
                // UTF-16 LE BOM
                return Encoding.Unicode.GetString(data, 2, data.Length - 2);
            }
            else if (data.Length >= 2 && data[0] == 0xFE && data[1] == 0xFF)
            {
                // UTF-16 BE BOM
                return Encoding.BigEndianUnicode.GetString(data, 2, data.Length - 2);
            }
            else if (data.Length >= 4 && data[0] == 0xFF && data[1] == 0xFE && data[2] == 0x00 && data[3] == 0x00)
            {
                // UTF-32 LE BOM
                return Encoding.UTF32.GetString(data, 4, data.Length - 4);
            }
            else if (data.Length >= 4 && data[0] == 0x00 && data[1] == 0x00 && data[2] == 0xFE && data[3] == 0xFF)
            {
                // UTF-32 BE BOM
                return Encoding.GetEncoding("UTF-32BE").GetString(data, 4, data.Length - 4);
            }
            else
            {
                // No BOM detected, try UTF-8 first
                try
                {
                    return Encoding.UTF8.GetString(data);
                }
                catch (DecoderFallbackException)
                {
                    // If UTF-8 fails, try Windows-1252 (common fallback)
                    return Encoding.GetEncoding("windows-1252").GetString(data);
                }
            }
        }
        /// <summary>
        /// Checks if an HTTP status code indicates a redirect
        /// </summary>
        /// <param name="code">HTTP status code to check</param>
        /// <returns>True if the status code indicates a redirect, false otherwise</returns>
        private bool IsRedirect(HttpStatusCode code)
        {
            return code == HttpStatusCode.MovedPermanently // 301
                || code == HttpStatusCode.Found           // 302
                || code == HttpStatusCode.TemporaryRedirect // 307
                || code == HttpStatusCode.PermanentRedirect; // 308
        }
    }
}
