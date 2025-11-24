using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using DataLayer;
using Entities.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OpenQA.Selenium;

namespace JournalScrappers.Scrap.ISC.Articles
{
    public class ExtractXml
    {
        private XDocument? xmlDoc;
        private readonly DynamicDbContext _context;
        private readonly ILogger<ExtractXml> _logger;

        public ExtractXml(DynamicDbContext context, ILogger<ExtractXml> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Helper method to get descendants with case-insensitive tag name matching
        /// </summary>
        private IEnumerable<XElement> GetDescendantsCaseInsensitive(XContainer? container, string tagName)
        {
            if (container == null)
                return Enumerable.Empty<XElement>();

            return container.Descendants()
                .Where(e => string.Equals(e.Name.LocalName, tagName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Helper method to get a single descendant with case-insensitive tag name matching
        /// </summary>
        private XElement? GetDescendantCaseInsensitive(XContainer? container, string tagName)
        {
            return GetDescendantsCaseInsensitive(container, tagName).FirstOrDefault();
        }

        public async Task<bool> ExtractXMLAsync(string xmlLink, int journalId)
        {
            if (string.IsNullOrWhiteSpace(xmlLink) || journalId == 0)
            {
                _logger.LogError("ورودی نامعتبر: لینک XML خالی است یا شناسه ژورنال صفر است");
                return false;
            }

            string articleXMLLinkFa = xmlLink + (xmlLink.Contains("?") ? "&lang=fa" : "?lang=fa");
            string articleXMLLinkEn = xmlLink + (xmlLink.Contains("?") ? "&lang=en" : "?lang=en");

            try
            {
                xmlDoc = XDocument.Parse(await GetContentOfUrlAsync(articleXMLLinkFa));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "تجزیه XML فارسی برای {ArticleXMLLinkFa} ناموفق بود، تلاش برای لینک اصلی: {XmlLink}", articleXMLLinkFa, xmlLink);
                try
                {
                    xmlDoc = XDocument.Parse(await GetContentOfUrlAsync(xmlLink));
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "تجزیه XML اصلی برای {XmlLink} ناموفق بود", xmlLink);
                    //return false;
                }
            }

            var xmlDocFa = xmlDoc;
            bool hasArticleSet = xmlDocFa?.Root?.Name.LocalName.Equals("ArticleSet", StringComparison.OrdinalIgnoreCase) == true;
            bool hasPublisherName = GetDescendantsCaseInsensitive(xmlDocFa, "PublisherName").Any();

            if (hasArticleSet)
            {
                var articles = GetDescendantsCaseInsensitive(xmlDocFa?.Root, "article");
                if (!articles.Any())
                {
                    _logger.LogError("هیچ مقاله‌ای در ArticleSet یافت نشد: {XmlLink}", xmlLink);
                    //return false;
                }

                foreach (var articleElement in articles)
                {
                    if (!await ProcessSingleArticleAsync(articleElement, journalId, xmlLink, articleXMLLinkEn, hasPublisherName))
                    {
                        _logger.LogWarning("پردازش یکی از مقالات در ArticleSet ناموفق بود: {XmlLink}", xmlLink);
                        continue;
                    }
                }
            }
            else
            {
                if (!await ProcessSingleArticleAsync(GetDescendantCaseInsensitive(xmlDocFa?.Root, "article"), journalId, xmlLink, articleXMLLinkEn, hasPublisherName))
                {
                    return false;
                }
            }

            return true;
        }

        private async Task<bool> ProcessSingleArticleAsync(XElement? articleElement, int journalId, string xmlLink, string articleXMLLinkEn, bool hasPublisherName)
        {
            if (articleElement == null)
            {
                _logger.LogError("عنصر مقاله در XML یافت نشد: {XmlLink}", xmlLink);
                return false;
            }

            if (hasPublisherName)
            {
                var xmlDocFa = articleElement.Document;
                var articleInfo = await ExtractArticleInfoAsync(xmlDocFa, articleXMLLinkEn, journalId, xmlLink);
                if (articleInfo == null)
                    return false;

                if (_context.Articles.Any(x =>
                    (x.TitleEn == articleInfo.TitleEn || x.TitleFa == articleInfo.TitleFa) || x.Doi == articleInfo.Doi))
                {
                    _logger.LogInformation("مقاله از قبل وجود دارد: {TitleEn}, {TitleFa}", articleInfo.TitleEn, articleInfo.TitleFa);
                    return true;
                }

                _context.Articles.Add(articleInfo);
                await _context.SaveChangesAsync();

                _logger.LogInformation("مقاله استخراج شد: عنوان: {TitleEn}, DOI: {Doi}, شناسه ژورنال: {JournalId}, لینک: {XmlLink}",
                    articleInfo.TitleEn, articleInfo.Doi, journalId, xmlLink);

                await ExtractAuthorsAsync(xmlDocFa, xmlDoc, articleInfo.Id, "", "");
                await ExtractKeywordsAsync(xmlDocFa, articleInfo.Id);
                await ExtractKeywordsAsync(xmlDoc, articleInfo.Id);
            }
            else
            {
                var articleInfo = ExtractSimpleArticleInfo(articleElement, journalId, xmlLink);
                if (articleInfo == null)
                    return false;

                if (_context.Articles.Any(x =>
                    (x.TitleEn == articleInfo.TitleEn || x.TitleFa == articleInfo.TitleFa) || x.Doi == articleInfo.Doi))
                {
                    _logger.LogInformation("مقاله از قبل وجود دارد: {TitleEn}, {TitleFa}", articleInfo.TitleEn, articleInfo.TitleFa);
                    return true;
                }

                _context.Articles.Add(articleInfo);
                await _context.SaveChangesAsync();

                _logger.LogInformation("مقاله استخراج شد: عنوان: {TitleEn}, DOI: {Doi}, شناسه ژورنال: {JournalId}, لینک: {XmlLink}",
                    articleInfo.TitleEn, articleInfo.Doi, journalId, xmlLink);

                await ExtractSimpleAuthorsAndKeywordsAsync(xmlDoc, articleInfo.Id);
            }

            return true;
        }

        private async Task<Article?> ExtractArticleInfoAsync(XDocument xmlDocFa, string articleXMLLinkEn, int journalId, string xmlLink)
        {
            var articleInfo = new Article
            {
                VolumeEn = GetTagValue("Volume"),
                IssueFa = GetTagValue("Issue").ContainsPersianCharacters() ?? false ? GetTagValue("Issue") : null,
                IssueEn = GetTagValue("Issue"),
                TitleFa = GetTagValue("ArticleTitle"),
                TitleEn = GetTagValue("VernacularTitle"),
                PageStart = int.TryParse(GetTagValue("FirstPage"), out int firstPage) ? firstPage : null,
                PageEnd = int.TryParse(GetTagValue("LastPage"), out int lastPage) ? lastPage : null,
                Type = GetTagValue("PublicationType"),
                AbstractFa = GetTagValue("Abstract"),
                AbstractEn = GetTagValue("OtherAbstract"),
                IscArticleId = GetTagValue("ELocationID", attributes: new Dictionary<string, string> { { "EIdType", "pii" } }),
                Doi = GetTagValue("ELocationID", attributes: new Dictionary<string, string> { { "EIdType", "doi" } }),
                JournalId = journalId,
                PageUrlIsc = xmlLink,
                IsIsc = true,
                LastUpdate = DateTime.Now,
                FullTextUrlIsc = GetTagValue("ArchiveCopySource"),
                OriginalLanguage = GetTagValue("Language"),
                SourceType = "ISC",
            };

            var pubDateElem = GetDescendantCaseInsensitive(xmlDocFa, "PubDate");
            if (pubDateElem != null)
            {
                try
                {
                    articleInfo.PublicationYear = int.TryParse(pubDateElem.Element("Year")?.Value?.Trim(), out var y) ? y : null;
                    articleInfo.PublicationMonth = int.TryParse(pubDateElem.Element("Month")?.Value?.Trim(), out var m) ? m : null;
                    articleInfo.PublicationDay = int.TryParse(pubDateElem.Element("Day")?.Value?.Trim(), out var d) ? d : null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "تجزیه تاریخ انتشار برای مقاله ناموفق بود: {XmlLink}", xmlLink);
                }
            }

            string abstractFa = GetTagValue("Abstract") ?? "";
            string otherAbstractFa = GetTagValue("OtherAbstract") ?? "";
            string titleFa = GetTagValue("ArticleTitle") ?? "";
            string vernacularTitleFa = GetTagValue("VernacularTitle") ?? "";

            try
            {
                xmlDoc = XDocument.Parse(await GetContentOfUrlAsync(articleXMLLinkEn));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "تجزیه XML انگلیسی برای {ArticleXMLLinkEn} ناموفق بود", articleXMLLinkEn);
                xmlDoc = null;
            }

            string abstractEn = GetTagValue("Abstract") ?? "";
            string otherAbstractEn = GetTagValue("OtherAbstract") ?? "";
            string titleEn = GetTagValue("ArticleTitle") ?? "";
            string vernacularTitleEn = GetTagValue("VernacularTitle") ?? "";

            (articleInfo.AbstractFa, articleInfo.AbstractEn) = FindEnAndFa(abstractFa, otherAbstractFa, abstractEn, otherAbstractEn);
            (articleInfo.TitleFa, articleInfo.TitleEn) = FindEnAndFa(titleFa, vernacularTitleFa, titleEn, vernacularTitleEn);

            if (!articleInfo.TitleEn.IsNullOrEmpty())
                articleInfo.ArticleIdentifier = articleInfo.TitleEn.ToIdentifierText();
            else
                articleInfo.ArticleIdentifier = articleInfo.TitleFa.ToIdentifierText();

            return articleInfo;
        }

        private Article? ExtractSimpleArticleInfo(XElement? documentArticle, int journalId, string xmlLink)
        {
            if (documentArticle == null)
            {
                _logger.LogError("عنصر مقاله در XML یافت نشد: {XmlLink}", xmlLink);
                return null;
            }

            var articleInfo = new Article
            {
                VolumeEn = GetTagValue("volume", documentArticle),
                IssueFa = GetTagValue("number", documentArticle) ?? "",
                Doi = GetTagValue("journal_id_doi", documentArticle) ?? "",
                IscArticleId = GetTagValue("journal_id_pii", documentArticle) ?? "",
                Type = GetTagValue("publish_type", documentArticle) ?? "",
                TitleFa = GetTagValue("title_fa", documentArticle) ?? "",
                TitleEn = GetTagValue("title", documentArticle) ?? "",
                PageStart = int.TryParse(GetTagValue("start_page", documentArticle), out var start) ? start : null,
                PageEnd = int.TryParse(GetTagValue("end_page", documentArticle), out var end) ? end : null,
                AbstractFa = GetTagValue("abstract_fa", documentArticle) ?? "",
                AbstractEn = GetTagValue("abstract", documentArticle) ?? "",
                FullTextUrlIsc = GetTagValue("web_url", documentArticle) ?? "",
                JournalId = journalId,
                PageUrlIsc = xmlLink,
                IsIsc = true,
                LastUpdate = DateTime.Now,
            };

            var pubDateElem = GetDescendantsCaseInsensitive(xmlDoc, "pubdate")
                .FirstOrDefault(x => GetDescendantCaseInsensitive(x, "type")?.Value.ToLower() == "gregorian");
            if (pubDateElem != null)
            {
                try
                {
                    articleInfo.PublicationYear = int.TryParse(pubDateElem.Element("Year")?.Value?.Trim(), out var y) ? y : null;
                    articleInfo.PublicationMonth = int.TryParse(pubDateElem.Element("Month")?.Value?.Trim(), out var m) ? m : null;
                    articleInfo.PublicationDay = int.TryParse(pubDateElem.Element("Day")?.Value?.Trim(), out var d) ? d : null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "تجزیه تاریخ انتشار برای مقاله ناموفق بود: {XmlLink}", xmlLink);
                }
            }

            return articleInfo;
        }

        private async Task ExtractSimpleAuthorsAndKeywordsAsync(XDocument xmlDocFa, int articleId)
        {
            var authorElements = GetDescendantsCaseInsensitive(xmlDocFa, "author").ToList();
            foreach (var (authorElement, index) in authorElements.Select((elem, i) => (elem, i)))
            {
                string firstNameFa = GetTagValue("first_name_fa", 0, authorElement.Document) ?? "";
                string lastNameFa = GetTagValue("last_name_fa", 0, authorElement.Document) ?? "";
                string firstNameEn = GetTagValue("first_name", 0, authorElement.Document) ?? "";
                string lastNameEn = GetTagValue("last_name", 0, authorElement.Document) ?? "";
                string affiliationFa = GetTagValue("affiliation_fa", 0, authorElement.Document) ?? "";
                string affiliationEn = GetTagValue("affiliation", 0, authorElement.Document) ?? "";
                string identifier = GetTagValue("orcid", 0, authorElement.Document) ?? "";

                var existingAuthor = await _context.ArticleCoAuthors.FirstOrDefaultAsync(a =>
                    (!string.IsNullOrEmpty(identifier) && a.Identifier == identifier) ||
                    (!string.IsNullOrEmpty(firstNameFa) && !string.IsNullOrEmpty(lastNameFa) &&
                     a.FirstNameFa == firstNameFa && a.LastNameFa == lastNameFa) ||
                    (!string.IsNullOrEmpty(firstNameEn) && !string.IsNullOrEmpty(lastNameEn) &&
                     a.FirstNameEn == firstNameEn && a.LastNameEn == lastNameEn));

                var author = existingAuthor ?? new CoAuthor
                {
                    FirstNameFa = firstNameFa,
                    LastNameFa = lastNameFa,
                    FirstNameEn = firstNameEn,
                    LastNameEn = lastNameEn,
                    AffiliationFa = affiliationFa,
                    AffiliationEn = affiliationEn,
                    Identifier = identifier
                };

                if (existingAuthor == null)
                {
                    _context.ArticleCoAuthors.Add(author);
                    await _context.SaveChangesAsync();
                }

                var articleAuthor = new ArticleAuthor
                {
                    ArticleId = articleId,
                    CoAuthorId = author.Id,
                    Order = index + 1,
                    LastUpdate = DateTime.UtcNow
                };

                _context.ArticleAuthors.Add(articleAuthor);
            }

            var keywords = GetDescendantsCaseInsensitive(xmlDocFa, "keyword_fa")
                .Concat(GetDescendantsCaseInsensitive(xmlDocFa, "keyword")).ToList();
            foreach (var keywordNode in keywords)
            {
                string param = GetDescendantCaseInsensitive(keywordNode, "Param")?.Value ?? "";
                if (!string.IsNullOrEmpty(param))
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

            await _context.SaveChangesAsync();
        }

        private async Task ExtractAuthorsAsync(XDocument docFa, XDocument? docEn, int articleId, string corresponding, string correspondingEmail)
        {
            var authorCount = GetDescendantsCaseInsensitive(docFa, "Author").Count();
            var correspondingWords = (corresponding ?? "")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim().ToLower())
                .ToList();
            var authors = new List<ArticleAuthor>();
            for (int i = 0; i < authorCount; i++)
            {
                string firstNameFa = GetTagValue("FirstName", i, docFa) ?? "";
                string lastNameFa = GetTagValue("LastName", i, docFa) ?? "";
                string firstNameEn = GetTagValue("FirstName", i, docEn) ?? "";
                string lastNameEn = GetTagValue("LastName", i, docEn) ?? "";
                string fullNameEn = (firstNameEn + lastNameEn + firstNameFa + lastNameFa).Replace(" ", "").ToLower();

                var author = new CoAuthor
                {
                    FirstNameFa = firstNameFa,
                    LastNameFa = lastNameFa,
                    FirstNameEn = firstNameEn,
                    LastNameEn = lastNameEn,
                    AffiliationFa = GetTagValue("Affiliation", i, docFa),
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
                        professor = await _context.Professors.FirstOrDefaultAsync(x =>
                            (x.FirstNameFa == author.FirstNameFa && x.LastNameFa == author.LastNameFa) ||
                            (x.FirstNameEn == author.FirstNameEn && x.LastNameEn == author.LastNameEn));

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

                var matchedAuthor = await query.FirstOrDefaultAsync();
                if (matchedAuthor != null)
                    author = matchedAuthor;

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
            if (!authors.Any(x => x.IsCorrespondingAuthor == true) && !string.IsNullOrWhiteSpace(corresponding))
            {
                foreach (var author in authors)
                {
                    var coAuthor = author.CoAuthor;
                    if (coAuthor != null)
                    {
                        var full = (coAuthor.FirstNameFa + coAuthor.LastNameFa + coAuthor.FirstNameEn + coAuthor.LastNameEn).Replace(" ", "").ToLower();
                        if (correspondingWords.Any(word => full.Contains(word.ToLower().Trim())))
                        {
                            coAuthor.Email = correspondingEmail;
                            author.IsCorrespondingAuthor = true;
                            break;
                        }
                    }
                }
            }

            _context.ArticleAuthors.AddRange(authors);
            await _context.SaveChangesAsync();
        }

        private async Task<bool> ExtractKeywordsAsync(XDocument? doc, int articleId)
        {
            if (doc == null)
                return false;

            try
            {
                var nodes = GetDescendantsCaseInsensitive(doc, "Object").ToList();
                foreach (var node in nodes)
                {
                    string param = GetDescendantCaseInsensitive(node, "Param")?.Value ?? "";
                    if (!string.IsNullOrEmpty(param))
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
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "استخراج کلمات کلیدی برای مقاله با شناسه {ArticleId} ناموفق بود", articleId);
                return false;
            }
        }

        public static (string Fa, string En) FindEnAndFa(params string[] abstracts)
        {
            string abstractFa = "";
            string abstractEn = "";

            foreach (var text in abstracts)
            {
                if (text.ContainsPersianCharacters() == true)
                    abstractFa = text;
                else if (text.ContainsPersianCharacters() == false)
                    abstractEn = text;
            }

            return (abstractFa, abstractEn);
        }

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
                _logger.LogError(ex, "خطا در دریافت مقدار تگ {TagName}", tagName);
                return "";
            }
        }

        private string GetTagValue(string tagName, XElement? document, int selectNumber = 0)
        {
            try
            {
                return GetDescendantsCaseInsensitive(document, tagName).ElementAtOrDefault(selectNumber)?.Value.Trim() ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در دریافت مقدار تگ {TagName} از سند", tagName);
                return "";
            }
        }

        /// <summary>
        /// Retrieves content from a URL, handling both web pages and file downloads with proper encoding detection
        /// </summary>
        /// <param name="url">URL to retrieve content from</param>
        /// <returns>Content as string</returns>
        /// <exception cref="Exception">Thrown when content retrieval fails</exception>
        private async Task<string> GetContentOfUrlAsync(string url)
        {
            try
            {
                // Skip WebScraper as it's causing JavaScript errors
                // Go directly to HTTP client approach for XML files
                using var httpClient = new HttpClient();
                {
                    // Set headers to mimic a real browser
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36");
                    httpClient.DefaultRequestHeaders.Add("Accept", "application/xml,text/xml,application/xhtml+xml,text/html;q=0.9,*/*;q=0.8");
                    httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9,fa;q=0.8");
                    httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");

                    var uri = new Uri(url);
                    httpClient.DefaultRequestHeaders.Add("Referer", $"{uri.Scheme}://{uri.Host}/");

                    // Download content as bytes first
                    byte[] data = await httpClient.GetByteArrayAsync(url);

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

        private bool IsRedirect(HttpStatusCode code)
        {
            return code == HttpStatusCode.MovedPermanently // 301
                || code == HttpStatusCode.Found           // 302
                || code == HttpStatusCode.TemporaryRedirect // 307
                || code == HttpStatusCode.PermanentRedirect; // 308
        }

    }
}
