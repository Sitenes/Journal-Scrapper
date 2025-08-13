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
    public class CrawlXml
    {
        private XDocument? xmlDoc;
        private readonly DynamicDbContext _context;
        private readonly ILogger<CrawlXml> _logger;

        public CrawlXml(DynamicDbContext context, ILogger<CrawlXml> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool ProcessFromUrl(string xmlUrl, int journalId)
        {
            if (string.IsNullOrWhiteSpace(xmlUrl))
            {
                _logger.LogError("Invalid XML URL provided");
                return false;
            }

            try
            {
                // Download XML content from URL
                string xmlContent = GetContentOfUrl(xmlUrl);
                if (string.IsNullOrWhiteSpace(xmlContent))
                {
                    _logger.LogError("Failed to download XML content from URL: {XmlUrl}", xmlUrl);
                    return false;
                }

                xmlDoc = XDocument.Parse(xmlContent);
                bool hasPublisherName = xmlDoc?.Descendants("PublisherName").Any() == true;

                Article? articleInfo = null;
                bool isNewArticle = false;

                // Extract identifiers from XML
                var doi = GetTagValue("ELocationID", attributes: new Dictionary<string, string> { { "EIdType", "doi" } });
                var iscId = GetTagValue("ELocationID", attributes: new Dictionary<string, string> { { "EIdType", "pii" } });
                var fullTextUrlIsc = GetTagValue("ArchiveCopySource");
                // Try to find existing article by various methods
                articleInfo = FindExistingArticle(doi, iscId, xmlUrl, fullTextUrlIsc);

                if (articleInfo == null)
                {
                    // If not found, create new article
                    if (hasPublisherName)
                    {
                        articleInfo = ExtractArticleInfo(xmlDoc, journalId, xmlUrl);
                    }
                    else
                    {
                        var documentArticle = xmlDoc?.Root?.Descendants("article")?.ElementAtOrDefault(0);
                        articleInfo = ExtractSimpleArticleInfo(documentArticle, journalId, xmlUrl);
                    }

                    if (articleInfo == null)
                    {
                        _logger.LogError("Failed to extract article info from XML");
                        return false;
                    }

                    isNewArticle = true;
                    _context.Articles.Add(articleInfo);
                }
                else
                {
                    // Update existing article with missing fields
                    UpdateArticleInfo(articleInfo, xmlDoc, hasPublisherName);
                }

                _context.SaveChanges();

                // Extract authors and keywords
                if (hasPublisherName)
                {
                    ExtractAuthors(xmlDoc, null, articleInfo.Id, string.Empty, string.Empty);
                    ExtractKeywords(xmlDoc, articleInfo.Id);
                }
                else
                {
                    ExtractSimpleAuthorsAndKeywords(xmlDoc, articleInfo.Id);
                }

                _logger.LogInformation("Article processed successfully from URL: {XmlUrl}. Title: {Title}, New: {IsNew}",
                    xmlUrl, articleInfo.TitleEn ?? articleInfo.TitleFa, isNewArticle);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process XML from URL: {XmlUrl}", xmlUrl);
                return false;
            }
        }
        private Article? FindExistingArticle(string doi, string iscId, string xmlUrl,string fullTextUrlIsc)
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
                        StringTool.GetDomainFromUrl(x.PageUrlIsc) == xmlDomain
                    ||  !string.IsNullOrWhiteSpace(fullTextUrlIsc) &&
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

            // Detailed metadata lookup
            //var issn = GetTagValue("Issn");
            //var volume = ParseInt(GetTagValue("Volume"));
            //var issue = GetTagValue("Issue");
            //var pageStart = ParseInt(GetTagValue("FirstPage"));
            //var pageEnd = ParseInt(GetTagValue("LastPage"));
            //var year = ParseInt(GetTagValue("Year"));
            //var month = ParseInt(GetTagValue("Month"));
            //var day = ParseInt(GetTagValue("Day"));

            //if (!string.IsNullOrWhiteSpace(issn))
            //{
            //    candidate = _context.Articles
            //        .FirstOrDefault(x =>
            //            x.Journal != null && x.Journal.ISSN == issn
            //         && x.Volume == volume
            //         && x.Issue == issue
            //         && x.PageStart == pageStart
            //         && x.PageEnd == pageEnd
            //         && x.PublicationYear == year
            //         && x.PublicationMonth == month
            //         && x.PublicationDay == day
            //        );
            //}

            return candidate;
        }

        private int? ParseInt(string? input) => int.TryParse(input, out var value) ? value : null;
        private void UpdateArticleInfo(Article article, XDocument xmlDoc, bool hasPublisherName)
        {
            try
            {
                bool updated = false;

                if (hasPublisherName)
                {
                    // Update Persian fields if empty
                    if (string.IsNullOrEmpty(article.TitleFa))
                    {
                        article.TitleFa = GetTagValue("ArticleTitle");
                        updated = true;
                    }

                    if (string.IsNullOrEmpty(article.AbstractFa))
                    {
                        article.AbstractFa = GetTagValue("Abstract");
                        updated = true;
                    }

                    // Update English fields if empty
                    if (string.IsNullOrEmpty(article.TitleEn))
                    {
                        article.TitleEn = GetTagValue("VernacularTitle");
                        updated = true;
                    }

                    if (string.IsNullOrEmpty(article.AbstractEn))
                    {
                        article.AbstractEn = GetTagValue("OtherAbstract");
                        updated = true;
                    }

                    // Update other fields if empty
                    if (article.Volume == null)
                    {
                        if (int.TryParse(GetTagValue("Volume"), out int vol))
                        {
                            article.Volume = vol;
                            updated = true;
                        }
                    }

                    if (string.IsNullOrEmpty(article.IssueEn))
                    {
                        article.IssueEn = GetTagValue("Issue");
                        updated = true;
                    }
                }
                else
                {
                    var documentArticle = xmlDoc?.Root?.Descendants("article")?.ElementAtOrDefault(0);

                    if (string.IsNullOrEmpty(article.TitleFa))
                    {
                        article.TitleFa = GetTagValue("title_fa", documentArticle);
                        updated = true;
                    }

                    if (string.IsNullOrEmpty(article.TitleEn))
                    {
                        article.TitleEn = GetTagValue("title", documentArticle);
                        updated = true;
                    }

                    if (string.IsNullOrEmpty(article.AbstractFa))
                    {
                        article.AbstractFa = GetTagValue("abstract_fa", documentArticle);
                        updated = true;
                    }

                    if (string.IsNullOrEmpty(article.AbstractEn))
                    {
                        article.AbstractEn = GetTagValue("abstract", documentArticle);
                        updated = true;
                    }
                }

                if (updated)
                {
                    article.LastUpdate = DateTime.Now;
                    _logger.LogInformation("Updated article fields for article ID: {ArticleId}", article.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update article info for article ID: {ArticleId}", article.Id);
            }
        }

        private Article? ExtractArticleInfo(XDocument xmlDoc, int journalId, string xmlUrl)
        {
            try
            {
                var articleInfo = new Article
                {
                    Volume = int.TryParse(GetTagValue("Volume"), out int vol) ? vol : null,
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
                    Volume = int.TryParse(GetTagValue("volume", documentArticle), out var v) ? v : null,
                    IssueEn = GetTagValue("number", documentArticle) ?? "",
                    Doi = GetTagValue("journal_id_doi", documentArticle) ?? "",
                    IscArticleId = GetTagValue("journal_id_pii", documentArticle) ?? "",
                    Type = GetTagValue("publish_type", documentArticle) ?? "",
                    PageStart = int.TryParse(GetTagValue("start_page", documentArticle), out var start) ? start : null,
                    PageEnd = int.TryParse(GetTagValue("end_page", documentArticle), out var end) ? end : null,
                    FullTextUrlIsc = GetTagValue("web_url", documentArticle) ?? "",
                    JournalId = journalId,
                    PageUrlIsc = xmlUrl,
                    IsIsc = true,
                    LastUpdate = DateTime.Now,
                };

                // Process titles
                var titleFa = GetTagValue("title_fa", documentArticle) ?? "";
                var titleEn = GetTagValue("title", documentArticle) ?? "";

                if (titleFa.ContainsPersianCharacters() == true)
                {
                    articleInfo.TitleFa = titleFa;
                    articleInfo.TitleEn = titleEn;
                }
                else if (titleEn.ContainsPersianCharacters() == true)
                {
                    articleInfo.TitleFa = titleEn;
                    articleInfo.TitleEn = titleFa;
                }
                else
                {
                    // Default assignment if explicit fields exist
                    articleInfo.TitleFa = GetTagValue("title_fa", documentArticle) ?? "";
                    articleInfo.TitleEn = GetTagValue("title", documentArticle) ?? "";
                }

                // Process abstracts
                var abstractFa = GetTagValue("abstract_fa", documentArticle) ?? "";
                var abstractEn = GetTagValue("abstract", documentArticle) ?? "";

                if (abstractFa.ContainsPersianCharacters() == true)
                {
                    articleInfo.AbstractFa = abstractFa;
                    articleInfo.AbstractEn = abstractEn;
                }
                else if (abstractEn.ContainsPersianCharacters() == true)
                {
                    articleInfo.AbstractFa = abstractEn;
                    articleInfo.AbstractEn = abstractFa;
                }
                else
                {
                    // Default assignment if explicit fields exist
                    articleInfo.AbstractFa = GetTagValue("abstract_fa", documentArticle) ?? "";
                    articleInfo.AbstractEn = GetTagValue("abstract", documentArticle) ?? "";
                }

                var pubDateElem = xmlDoc?.Descendants("pubdate")
                    .FirstOrDefault(x => x.Element("type")?.Value.ToLower() == "gregorian");
                if (pubDateElem != null)
                {
                    articleInfo.PublicationYear = int.TryParse(pubDateElem.Element("Year")?.Value?.Trim(), out var y) ? y : null;
                    articleInfo.PublicationMonth = int.TryParse(pubDateElem.Element("Month")?.Value?.Trim(), out var m) ? m : null;
                    articleInfo.PublicationDay = int.TryParse(pubDateElem.Element("Day")?.Value?.Trim(), out var d) ? d : null;
                }

                return articleInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract simple article info from XML");
                return null;
            }
        }

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

                    var existingAuthor = _context.ArticleCoAuthors.FirstOrDefault(a =>
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
                        _context.SaveChanges();
                    }

                    var existingArticleAuthor = _context.ArticleAuthors
                        .FirstOrDefault(aa => aa.ArticleId == articleId && aa.CoAuthorId == author.Id);

                    if (existingArticleAuthor == null)
                    {
                        var articleAuthor = new ArticleAuthor
                        {
                            ArticleId = articleId,
                            CoAuthorId = author.Id,
                            Order = index + 1,
                            LastUpdate = DateTime.UtcNow
                        };
                        _context.ArticleAuthors.Add(articleAuthor);
                    }
                }

                var keywords = xmlDoc.Descendants("keyword_fa").Concat(xmlDoc.Descendants("keyword")).ToList();
                foreach (var keywordNode in keywords)
                {
                    string param = keywordNode.Descendants("Param").FirstOrDefault()?.Value ?? "";
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

                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract authors and keywords from XML for article ID: {ArticleId}", articleId);
            }
        }

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

        private string GetContentOfUrl(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Proxy = null;
                request.AllowAutoRedirect = false;
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/114.0.0.0 Safari/537.36";

                using HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                if (IsRedirect(response.StatusCode))
                {
                    string redirectUrl = response.Headers["Location"];
                    if (!string.IsNullOrEmpty(redirectUrl))
                    {
                        if (!redirectUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            Uri baseUri = new Uri(url);
                            Uri newUri = new Uri(baseUri, redirectUrl);
                            redirectUrl = newUri.ToString();
                        }
                        return GetContentOfUrl(redirectUrl);
                    }
                }

                string contentType = response.ContentType?.ToLower() ?? "";

                using Stream stream = response.GetResponseStream();

                if (contentType.Contains("text") || contentType.Contains("json") || contentType.Contains("xml"))
                {
                    using StreamReader reader = new StreamReader(stream!, Encoding.UTF8);
                    return reader.ReadToEnd();
                }
                else
                {
                    using MemoryStream ms = new MemoryStream();
                    stream!.CopyTo(ms);
                    byte[] data = ms.ToArray();
                    string base64 = Convert.ToBase64String(data);
                    return $"[Binary Data, Base64 encoded]: {base64.Substring(0, Math.Min(200, base64.Length))}...";
                }
            }
            catch (WebException ex)
            {
                _logger.LogError(ex, "Failed to get content from URL: {Url}", url);

                using var errorResponse = ex.Response;
                using var stream = errorResponse?.GetResponseStream();
                using var reader = new StreamReader(stream ?? Stream.Null, Encoding.UTF8);
                string errorText = reader.ReadToEnd();
                throw new Exception($"Failed to get content: {ex.Message}\n{errorText}", ex);
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