using DataLayer;
using Entities.Models.Entities;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using JournalScrappers;
using Microsoft.IdentityModel.Tokens;

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

        public bool ExtractXML(string pageLink, int journalId)
        {
            if (string.IsNullOrWhiteSpace(pageLink) || journalId == 0)
            {
                _logger.LogError("ورودی نامعتبر: لینک صفحه خالی است یا شناسه ژورنال صفر است");
                return false;
            }

            if (_context.Articles.Any(x => x.IscArticleId == pageLink && x.JournalId == journalId))
            {
                _logger.LogInformation("مقاله از قبل در پایگاه داده وجود دارد: {PageLink}", pageLink);
                return true;
            }

            WebScraper.GetPageContent(pageLink);
            string articleXMLLink = FindXMLLink();
            (string correspondingName, string correspondingEmail) = FindCorrespondingAuthor();

            if (string.IsNullOrWhiteSpace(articleXMLLink))
            {
                _logger.LogError("یافتن لینک XML برای مقاله ناموفق بود: {PageLink}", pageLink);
                return false;
            }

            string articleXMLLinkFa = articleXMLLink + (articleXMLLink.Contains("?") ? "&lang=fa" : "?lang=fa");
            string articleXMLLinkEn = articleXMLLink + (articleXMLLink.Contains("?") ? "&lang=en" : "?lang=en");

            try
            {
                xmlDoc = XDocument.Parse(GetContentOfUrl(articleXMLLinkFa));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "تجزیه XML فارسی برای {ArticleXMLLinkFa} ناموفق بود، تلاش برای لینک اصلی: {ArticleXMLLink}", articleXMLLinkFa, articleXMLLink);
                xmlDoc = XDocument.Parse(GetContentOfUrl(articleXMLLink));
            }

            var xmlDocFa = xmlDoc;
            bool hasPublisherName = xmlDocFa?.Descendants("PublisherName").Any() == true;

            if (hasPublisherName)
            {
                var articleInfo = ExtractArticleInfo(xmlDocFa, articleXMLLinkEn, journalId, pageLink);
                if (articleInfo == null)
                    return false;

                if (_context.Articles.Any(x =>
                (x.TitleEn == articleInfo.TitleEn || x.TitleFa == articleInfo.TitleFa) || x.Doi == articleInfo.Doi))
                {
                    _logger.LogInformation("مقاله از قبل وجود دارد: {TitleEn}, {TitleFa}", articleInfo.TitleEn, articleInfo.TitleFa);
                    return true;
                }

                _context.Articles.Add(articleInfo);
                _context.SaveChanges();

                _logger.LogInformation("مقاله استخراج شد: عنوان: {TitleEn}, DOI: {Doi}, شناسه ژورنال: {JournalId}, لینک: {PageLink}",
                    articleInfo.TitleEn, articleInfo.Doi, journalId, pageLink);

                ExtractAuthors(xmlDocFa, xmlDoc, articleInfo.Id, correspondingName, correspondingEmail);
                ExtractKeywords(xmlDocFa, articleInfo.Id);
                ExtractKeywords(xmlDoc, articleInfo.Id);
            }
            else
            {
                var documentArticle = xmlDocFa?.Root?.Descendants("article")?.ElementAtOrDefault(0);
                var articleInfo = ExtractSimpleArticleInfo(documentArticle, journalId, pageLink);
                if (articleInfo == null)
                    return false;

                if (_context.Articles.Any(x =>
              (x.TitleEn == articleInfo.TitleEn || x.TitleFa == articleInfo.TitleFa) || x.Doi == articleInfo.Doi))
                {
                    _logger.LogInformation("مقاله از قبل وجود دارد: {TitleEn}, {TitleFa}", articleInfo.TitleEn, articleInfo.TitleFa);
                    return true;
                }

                _context.Articles.Add(articleInfo);
                _context.SaveChanges();

                _logger.LogInformation("مقاله استخراج شد: عنوان: {TitleEn}, DOI: {Doi}, شناسه ژورنال: {JournalId}, لینک: {PageLink}",
                    articleInfo.TitleEn, articleInfo.Doi, journalId, pageLink);

                ExtractSimpleAuthorsAndKeywords(xmlDocFa, articleInfo.Id);
            }

            return true;
        }

        private Article? ExtractArticleInfo(XDocument xmlDocFa, string articleXMLLinkEn, int journalId, string pageLink)
        {
            var articleInfo = new Article
            {
                Volume = int.TryParse(GetTagValue("Volume"), out int vol) ? vol : null,
                Issue = GetTagValue("Issue"),
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
                PageUrlIsc = pageLink,
                IsIsc = true,
                LastUpdate = DateTime.Now,
                FullTextUrlIsc = GetTagValue("ArchiveCopySource"),
                OriginalLanguage = GetTagValue("Language"),
                SourceType = "ISC",

            };

            var pubDateElem = xmlDocFa.Descendants("PubDate").FirstOrDefault();
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
                    _logger.LogError(ex, "تجزیه تاریخ انتشار برای مقاله ناموفق بود: {PageLink}", pageLink);
                }
            }

            string abstractFa = GetTagValue("Abstract") ?? "";
            string otherAbstractFa = GetTagValue("OtherAbstract") ?? "";
            string titleFa = GetTagValue("ArticleTitle") ?? "";
            string vernacularTitleFa = GetTagValue("VernacularTitle") ?? "";

            try
            {
                xmlDoc = XDocument.Parse(GetContentOfUrl(articleXMLLinkEn));
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

        private Article? ExtractSimpleArticleInfo(XElement? documentArticle, int journalId, string pageLink)
        {
            if (documentArticle == null)
            {
                _logger.LogError("عنصر مقاله در XML یافت نشد: {PageLink}", pageLink);
                return null;
            }

            var articleInfo = new Article
            {
                Volume = int.TryParse(GetTagValue("volume", documentArticle), out var v) ? v : null,
                Issue = GetTagValue("number", documentArticle) ?? "",
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
                PageUrlIsc = pageLink,
                IsIsc = true,
                LastUpdate = DateTime.Now,
            };

            var pubDateElem = xmlDoc?.Descendants("pubdate")
                .FirstOrDefault(x => x.Element("type")?.Value.ToLower() == "gregorian");
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
                    _logger.LogError(ex, "تجزیه تاریخ انتشار برای مقاله ناموفق بود: {PageLink}", pageLink);
                }
            }

            return articleInfo;
        }

        private void ExtractSimpleAuthorsAndKeywords(XDocument xmlDocFa, int articleId)
        {
            var authorElements = xmlDocFa.Descendants("author").ToList();
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

                var articleAuthor = new ArticleAuthor
                {
                    ArticleId = articleId,
                    CoAuthorId = author.Id,
                    Order = index + 1,
                    LastUpdate = DateTime.UtcNow
                };

                _context.ArticleAuthors.Add(articleAuthor);
            }

            var keywords = xmlDocFa.Descendants("keyword_fa").Concat(xmlDocFa.Descendants("keyword")).ToList();
            foreach (var keywordNode in keywords)
            {
                string param = keywordNode.Descendants("Param").FirstOrDefault()?.Value ?? "";
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

            _context.SaveChanges();
        }

        public static string GetArticlePDFFile(string articleTitle, string mainAuthor, string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return "";

            try
            {
                string outputPath = Path.Combine(WebScraper.FindDirectoryInParents(), "PDF");
                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);

                string sanitizedTitle = Regex.Replace(articleTitle.Replace(" ", "_"), "[<>:\"/\\\\|?*\\x00-\\x1F]", "");
                if (sanitizedTitle.Length > 40)
                    sanitizedTitle = sanitizedTitle.Substring(0, 40);

                string fileName = $"{sanitizedTitle}({mainAuthor}).pdf";
                string downloadPath = Path.Combine(outputPath, fileName);

                if (!File.Exists(downloadPath))
                    DownloadFile(url, downloadPath);

                return fileName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطا در دانلود PDF: {ex.Message}");
                return "";
            }
        }

        public static void DownloadFile(string fileUrl, string savePath)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(fileUrl);
                request.UserAgent = "Mozilla/5.0";
                using var response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine($"دانلود PDF ناموفق بود: کد خطای HTTP {response.StatusCode}");
                    return;
                }

                using var responseStream = response.GetResponseStream();
                using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write);
                responseStream?.CopyTo(fileStream);
            }
            catch (WebException ex)
            {
                Console.WriteLine($"دانلود PDF ناموفق بود: {ex.Message}");
            }
        }

        private void ExtractAuthors(XDocument docFa, XDocument? docEn, int articleId, string corresponding, string correspondingEmail)
        {
            var authorCount = docFa.Descendants("Author").Count();
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
                    var full = (author.CoAuthor.FirstNameFa + author.CoAuthor.LastNameFa + author.CoAuthor.FirstNameEn + author.CoAuthor.LastNameEn).Replace(" ","").ToLower();
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
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "استخراج کلمات کلیدی برای مقاله با شناسه {ArticleId} ناموفق بود", articleId);
                return false;
            }
        }

        private (string Name, string Email) FindCorrespondingAuthor()
        {
            try
            {
                var emailElement = WebScraper.driver.FindElement(By.XPath("//a[contains(@href, 'mailto:')]"));
                var parentLi = emailElement.FindElement(By.XPath("./ancestor::li"));
                var nameElement = parentLi.FindElement(By.XPath(".//a[not(contains(@href, 'mailto:'))]"));

                string name = nameElement.Text.Trim();
                string email = emailElement.GetAttribute("href").Replace("mailto:", "").Trim();

                if (string.IsNullOrWhiteSpace(email))
                    throw new Exception("ایمیل نویسنده مسئول یافت نشد");

                _logger.LogInformation("نویسنده مسئول: {Name}, ایمیل: {Email}", name, email);
                return (name, email);
            }
            catch (Exception)
            {
                try
                {
                    var author = WebScraper.driver.FindElement(By.XPath("//sup[contains(text(),'1')]/preceding::a[contains(text(), ' ')][1]"));
                    string name = author.Text.Trim();
                    var emailElement = WebScraper.driver.FindElement(By.XPath("//div[@class='yw_text_small abstractsmall']//span[.//text()[contains(., '@')]]"));
                    string email = emailElement.Text.Trim();

                    if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(email))
                    {
                        _logger.LogInformation("نویسنده مسئول: {Name}, ایمیل: {Email}", name, email);
                        return (name, email);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "یافتن نویسنده مسئول برای {Url} ناموفق بود", WebScraper.driver.Url);
                }
                return ("", "");
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
                return document?.Descendants(tagName).ElementAtOrDefault(selectNumber)?.Value.Trim() ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در دریافت مقدار تگ {TagName} از سند", tagName);
                return "";
            }
        }

        private string FindXMLLink()
        {
            try
            {
                var aElement = WebScraper.driver.FindElement(
                    By.XPath("//a[contains(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'article') and " +
                             "contains(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'xml')]"));
                var link = aElement.GetAttribute("href")
                    .Replace("&lang=en", "").Replace("lang=en", "")
                    .Replace("&lang=fa", "").Replace("lang=fa", "");
                return link;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در یافتن لینک XML");
                return "";
            }
        }

        private string GetContentOfUrl(string url)
        {
            var request = WebRequest.Create(new Uri(url)) as HttpWebRequest;
            request!.Proxy = null;
            request.AllowAutoRedirect = true;
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/114.0.0.0 Safari/537.36";

            try
            {
                using var response = request.GetResponse();
                using var receiveStream = response.GetResponseStream();
                using var readStream = new StreamReader(receiveStream!, Encoding.UTF8);
                return readStream.ReadToEnd();
            }
            catch (WebException ex)
            {
                _logger.LogError(ex, "خطا در دریافت محتوا از {Url}", url);
                using var errorResponse = ex.Response;
                using var stream = errorResponse?.GetResponseStream();
                using var reader = new StreamReader(stream ?? Stream.Null, Encoding.UTF8);
                string errorText = reader.ReadToEnd();
                throw new Exception($"خطا در دریافت محتوا: {ex.Message}\n{errorText}", ex);
            }
        }
    }
}