using DataLayer;
using Entities.Models.Entities;
using Microsoft.IdentityModel.Tokens;
using OpenQA.Selenium;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace JournalScrappers.Scrap.ISC.Articles
{
    public class ExtractXml
    {
        XDocument? xmlDoc = null;
        private readonly DynamicDbContext _context;

        public ExtractXml(DynamicDbContext context)
        {
            _context = context;
        }
        public bool ExtractXML(string pageLink, int journalId)
        {
            if (string.IsNullOrWhiteSpace(pageLink) || journalId == 0)
                return false;
            if (_context.Articles.Any(x => x.IscArticleId == pageLink))
                return true;
            WebScraper.GetPageContent(pageLink);

            string articleXMLLink = FindXMLLink();
            (var correspondingName, var correspondingEmail) = FindCorrespondingAuthor();
            if (string.IsNullOrWhiteSpace(articleXMLLink))
                return false;

            var articleXMLLinkFa = articleXMLLink;
            if (articleXMLLinkFa.Contains("?"))
                articleXMLLinkFa = articleXMLLinkFa + "&lang=fa";
            else
                articleXMLLinkFa = articleXMLLinkFa + "?lang=fa";

            var articleXMLLinkEn = articleXMLLink;
            if (articleXMLLinkEn.Contains("?"))
                articleXMLLinkEn = articleXMLLinkEn + "&lang=en";
            else
                articleXMLLinkEn = articleXMLLinkEn + "?lang=en";

            try
            {
                xmlDoc = XDocument.Parse(GetContentOfUrl(articleXMLLinkFa));
            }
            catch (Exception)
            {
                xmlDoc = XDocument.Parse(GetContentOfUrl(articleXMLLink));
            }
            var xmlDocFa = xmlDoc;

            var hasPublisherName = xmlDocFa.Descendants("PublisherName").Any();
            if (hasPublisherName)
            {
                //var PublisherName_FA = GetTagValue("PublisherName");
                //var JournalTitle_FA = GetTagValue("JournalTitle");
                //var JournalTitle_EN = GetTagValue("JournalTitle");
                //var Issn = GetTagValue("Issn").Replace("-", "");
                //Journal? journal = _context.Journals.FirstOrDefault(x => x.Id == journalId);
                //if (journal == null)
                //{
                //if (!JournalTitle_FA.IsNullOrEmpty())
                //{
                //journal = _context.Journals.FirstOrDefault(x => x.ISSN == Issn || x.EISSN == Issn);
                //journal = _context.Journals.FirstOrDefault(x => (x.Title_Fa == JournalTitle_FA || x.Title_EN == JournalTitle_FA));
                //if (journal == null)
                //{
                //    journal = new Journal
                //    {
                //        Title_Fa = JournalTitle_FA,
                //        ISSN = Issn,
                //        Publisher = PublisherName_FA,

                //    };
                //}
                //}

                //}
                Article articleInfo = new Article
                {
                    Volume = int.Parse(GetTagValue("Volume")),
                    Issue = GetTagValue("Issue"),

                    TitleFa = GetTagValue("ArticleTitle"),
                    TitleEn = GetTagValue("VernacularTitle"),

                    PageStart = int.Parse(GetTagValue("FirstPage")),
                    PageEnd = int.Parse(GetTagValue("LastPage")),

                    Type = GetTagValue("PublicationType"),
                    AbstractFa = GetTagValue("Abstract"),
                    AbstractEn = GetTagValue("OtherAbstract"),

                    // مقدار ELocationID با EIdType = pii
                    IscArticleId = GetTagValue("ELocationID", attributes: new Dictionary<string, string> { { "EIdType", "pii" } }),

                    // مقدار ELocationID با EIdType = doi
                    Doi = GetTagValue("ELocationID", attributes: new Dictionary<string, string> { { "EIdType", "doi" } }),

                    FullTextUrlIsc = GetTagValue("ArchiveCopySource"),

                    JournalId = journalId,
                    //CorrespondingAuthorEmail = correspondingEmail,
                    //CorrespondingAuthorName = correspondingName,
                    PageUrlIsc = pageLink
                };
                var pubDateElem = xmlDocFa.Descendants("PubDate").FirstOrDefault();
                if (pubDateElem != null)
                {
                    try
                    {
                        int? year = int.TryParse(pubDateElem.Element("Year")?.Value?.Trim(), out var y) ? y : null;
                        int? month = int.TryParse(pubDateElem.Element("Month")?.Value?.Trim(), out var m) ? m : null;
                        int? day = int.TryParse(pubDateElem.Element("Day")?.Value?.Trim(), out var d) ? d : null;

                        articleInfo.PublicationYear = year;
                        articleInfo.PublicationMonth = month;
                        articleInfo.PublicationDay = day;
                    }
                    catch { }
                }

                //var pubDateResElem = xmlDocFa.Descendants("PubDate").Skip(1).FirstOrDefault();
                //if (pubDateResElem != null)
                //{
                //    int year = int.TryParse(pubDateResElem.Element("Year")?.Value?.Trim(), out var y) ? y : 1;
                //    int month = int.TryParse(pubDateResElem.Element("Month")?.Value?.Trim(), out var m) ? m : 1;
                //    int day = int.TryParse(pubDateResElem.Element("Day")?.Value?.Trim(), out var d) ? d : 1;
                //    try
                //    {
                //        articleInfo.PubDateReceived = new DateTime(year, month, day).ToString("yyyy-MM-dd");
                //    }
                //    catch
                //    {
                //        articleInfo.PubDateReceived = string.Empty;
                //    }
                //}
                //else
                //{
                //    articleInfo.PubDateReceived = string.Empty;
                //}

                var Abstract_FA = GetTagValue("Abstract") ?? "";
                var OtherAbstract_FA = GetTagValue("OtherAbstract") ?? "";
                var Title_FA = GetTagValue("ArticleTitle") ?? "";
                var VernacularTitle_FA = GetTagValue("VernacularTitle") ?? "";

                try
                {
                    xmlDoc = XDocument.Parse(GetContentOfUrl(articleXMLLinkEn));
                }
                catch
                {
                    xmlDoc = null;
                }

                var xmlDocEn = xmlDoc;

                //articleInfo.PublisherName_EN = GetTagValue("PublisherName") ?? "";
                //articleInfo.PublisherName_FA = articleInfo.PublisherName_FA ?? "";
                //(articleInfo.PublisherName_FA, articleInfo.PublisherName_EN) = FindEnAndFa(articleInfo.PublisherName_EN, articleInfo.PublisherName_FA);

                //articleInfo.JournalTitle_FA = GetTagValue("JournalTitle") ?? "";
                //articleInfo.JournalTitle_EN = articleInfo.JournalTitle_EN ?? "";
                //(articleInfo.JournalTitle_FA, articleInfo.JournalTitle_EN) = FindEnAndFa(articleInfo.JournalTitle_FA, articleInfo.JournalTitle_EN);

                var Abstract_EN = GetTagValue("Abstract") ?? "";
                var OtherAbstract_EN = GetTagValue("OtherAbstract") ?? "";
                var Title_EN = GetTagValue("ArticleTitle") ?? "";
                var VernacularTitle_EN = GetTagValue("VernacularTitle") ?? "";

                (articleInfo.AbstractFa, articleInfo.AbstractEn) = FindEnAndFa(Abstract_FA, OtherAbstract_FA, Abstract_EN, OtherAbstract_EN);
                (articleInfo.TitleFa, articleInfo.TitleEn) = FindEnAndFa(Title_FA, VernacularTitle_FA, Title_EN, VernacularTitle_EN);

                var pdfTitle = string.IsNullOrWhiteSpace(articleInfo.TitleEn) ? articleInfo.TitleFa : articleInfo.TitleEn;

                var firstName = GetTagValue("FirstName") ?? "";
                var lastName = GetTagValue("LastName") ?? "";
                var firstAuthor = $"{firstName} {lastName}".Trim();

                if (_context.Articles.Any(x => x.TitleEn == articleInfo.TitleEn && x.TitleFa == articleInfo.TitleFa))
                    return true;

                _context.Articles.Add(articleInfo);
                _context.SaveChanges();

                ExtractAuthors(xmlDocFa, xmlDocEn, articleInfo.Id);
                ExtractKeywords(xmlDocFa, articleInfo.Id);
                ExtractKeywords(xmlDocEn, articleInfo.Id);
            }
            else
            {
                var documentArticle = xmlDocFa.Root?.Descendants("article")?.ElementAtOrDefault(0);
                var articleInfo = new Article
                {
                    //JournalTitle_FA = GetTagValue("title_fa") ?? "",
                    //JournalTitle_EN = GetTagValue("title") ?? "",
                    //Issn = GetTagValue("journal_id_issn") ?? "",
                    Volume = int.TryParse(GetTagValue("volume"), out var v) ? v : null,
                    Issue = GetTagValue("number") ?? "",
                    Doi = GetTagValue("journal_id_doi") ?? "",
                    IscArticleId = GetTagValue("journal_id_pii") ?? "",
                    Type = GetTagValue("publish_type") ?? "",
                    TitleFa = GetTagValue("title_fa", documentArticle) ?? "",
                    TitleEn = GetTagValue("title", documentArticle) ?? "",
                    PageStart = int.TryParse(GetTagValue("start_page", documentArticle), out var start) ? start : null,
                    PageEnd = int.TryParse(GetTagValue("end_page", documentArticle), out var end) ? end : null,
                    AbstractFa = GetTagValue("abstract_fa", documentArticle) ?? "",
                    AbstractEn = GetTagValue("abstract", documentArticle) ?? "",
                    FullTextUrlIsc = GetTagValue("web_url") ?? "",
                    //CorrespondingAuthorEmail = correspondingEmail ?? "",
                    //CorrespondingAuthorName = correspondingName ?? ""
                };

                var pubDateElem = xmlDocFa.Descendants("pubdate")
                    .FirstOrDefault(x => (x.Element("type")?.Value ?? "").ToLower() == "gregorian");
                if (pubDateElem != null)
                {
                    try
                    {
                        int? year = int.TryParse(pubDateElem.Element("Year")?.Value?.Trim(), out var y) ? y : null;
                        int? month = int.TryParse(pubDateElem.Element("Month")?.Value?.Trim(), out var m) ? m : null;
                        int? day = int.TryParse(pubDateElem.Element("Day")?.Value?.Trim(), out var d) ? d : null;

                        articleInfo.PublicationYear = year;
                        articleInfo.PublicationMonth = month;
                        articleInfo.PublicationDay = day;
                    }
                    catch { }
                }

                _context.Articles.Add(articleInfo);
                _context.SaveChanges();

                var authorElements = xmlDocFa.Descendants("author");
                for (int i = 0; i < authorElements.Count(); i++)
                {
                    var authorElement = authorElements.ElementAt(i);
                    string firstNameFa = GetTagValue("first_name_fa", 0, authorElement.Document) ?? "";
                    string lastNameFa = GetTagValue("last_name_fa", 0, authorElement.Document) ?? "";
                    string firstNameEn = GetTagValue("first_name", 0, authorElement.Document) ?? "";
                    string lastNameEn = GetTagValue("last_name", 0, authorElement.Document) ?? "";
                    string affiliationFa = GetTagValue("affiliation_fa", 0, authorElement.Document) ?? "";
                    string affiliationEn = GetTagValue("affiliation", 0, authorElement.Document) ?? "";
                    string identifier = GetTagValue("orcid", 0, authorElement.Document) ?? "";

                    var existingAuthor = _context.CoAuthors.FirstOrDefault(a =>
                        (!string.IsNullOrEmpty(identifier) && a.Identifier == identifier) ||
                        (!string.IsNullOrEmpty(firstNameFa) && !string.IsNullOrEmpty(lastNameFa) &&
                         a.FirstNameFa == firstNameFa && a.LastNameFa == lastNameFa) ||
                        (!string.IsNullOrEmpty(firstNameEn) && !string.IsNullOrEmpty(lastNameEn) &&
                         a.FirstNameEn == firstNameEn && a.LastNameEn == lastNameEn)
                    );

                    // اگر نبود، بساز
                    CoAuthor author = existingAuthor ?? new CoAuthor
                    {
                        FirstNameFa = firstNameFa,
                        LastNameFa = lastNameFa,
                        FirstNameEn = firstNameEn,
                        LastNameEn = lastNameEn,
                        AffiliationFa = affiliationFa,
                        AffiliationEn = affiliationEn,
                        Identifier = identifier
                    };

                    // اگر جدید بود، به دیتابیس اضافه کن
                    if (existingAuthor == null)
                    {
                        _context.CoAuthors.Add(author);
                        _context.SaveChanges();
                    }

                    ArticleAuthor articleAuthor = new ArticleAuthor
                    {
                        ArticleId = articleInfo.Id,
                        CoAuthorId = author.Id,
                        Order = i + 1, // شروع از ۱
                        LastUpdate = DateTime.Now
                    };

                    _context.ArticleAuthors.Add(articleAuthor);
                }
                _context.SaveChanges();


                var keywords = xmlDocFa.Descendants("keyword_fa").ToList();
                keywords.AddRange(xmlDocFa.Descendants("keyword").ToList());

                for (int i = 0; i < keywords.Count; i++)
                {
                    var nodeFa = keywords[i];
                    ArticleKeyword keyword = new ArticleKeyword();
                    var paramFa = nodeFa.Descendants("Param").FirstOrDefault()?.Value ?? "";
                    if (!string.IsNullOrEmpty(paramFa))
                    {
                        keyword.ArticleId = articleInfo.Id;
                        keyword.Keyword = paramFa;
                        keyword.IsPersian = paramFa.ContainsPersianCharacters() ?? false;
                        _context.Add(keyword);
                    }
                }

                _context.SaveChanges();

            }
            return true;
        }

        private string GetElementValueSafe(XElement? element)
        {
            return element?.Value?.Trim() ?? string.Empty;
        }
        public static string GetArticlePDFFile(string articleTitle, string mainAuthor, string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                    return "";
                string outputArticlePDF = WebScraper.FindDirectoryInParents();
                outputArticlePDF = Path.Combine(outputArticlePDF, "PDF");
                if (!Directory.Exists(outputArticlePDF))
                    Directory.CreateDirectory(outputArticlePDF);

                // حذف کاراکترهای نامعتبر از عنوان مقاله و تنظیم آن
                string title = articleTitle.Replace(" ", "_");
                string invalidCharsPattern = "[<>:\"/\\\\|?*\\x00-\\x1F]";
                title = Regex.Replace(title, invalidCharsPattern, "");
                if (title.Length > 40)
                    title = title.Substring(0, 40);
                string fileName = $"{title}({mainAuthor}).pdf";
                string downloadPath = Path.Combine(outputArticlePDF, fileName);

                if (!File.Exists(downloadPath))
                {
                    DownloadFile(url, downloadPath);
                    //Console.WriteLine($"PDF downloaded successfully to {downloadPath}");
                }

                return fileName;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return string.Empty;
        }

        public static void DownloadFile(string fileUrl, string savePath)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fileUrl);
                request.UserAgent = "Mozilla/5.0"; // تنظیم User-Agent برای شبیه سازی درخواست مرورگر
                //request.Headers.Add("Accept-Language", "en-US,en;q=0.5");

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (Stream responseStream = response.GetResponseStream())
                        {
                            using (FileStream fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                            {
                                responseStream.CopyTo(fileStream);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to download PDF: HTTP error code {response.StatusCode}");
                    }
                }
            }
            catch (WebException e)
            {
                Console.WriteLine("Failed to download PDF: ", e);
            }
        }

        void ExtractAuthors(XDocument docFa, XDocument docEn, int articleId)
        {
            var authorCout = docFa.Descendants("Author").Count();

            for (int j = 0; j < authorCout; j++)
            {
                CoAuthor author = new CoAuthor
                {
                    FirstNameFa = GetTagValue("FirstName", j, docFa),
                    LastNameFa = GetTagValue("LastName", j, docFa),
                    FirstNameEn = GetTagValue("FirstName", j, docEn),
                    LastNameEn = GetTagValue("LastName", j, docEn),
                    AffiliationFa = GetTagValue("Affiliation", j, docFa),
                    AffiliationEn = GetTagValue("Affiliation", j, docEn),
                    Identifier = GetTagValue("Identifier", j, docEn),

                };

                ArticleAuthor articleAuthor = new ArticleAuthor
                {
                    ArticleId = articleId,
                    Order = j + 1,
                    CoAuthor = author,

                };

                _context.ArticleAuthors.Add(articleAuthor);
                _context.SaveChanges();
            }

        }
        bool ExtractKeywords(XDocument doc, int articleId)
        {
            try
            {
                var nodeListFa = doc.Descendants("Object").ToList();

                for (int i = 0; i < nodeListFa.Count; i++)
                {
                    var nodeFa = nodeListFa[i];

                    ArticleKeyword keyword = new ArticleKeyword();
                    //if (nodeFa.Attribute("Type")?.Value == "keyword")
                    //{
                    var paramFa = nodeFa.Descendants("Param").FirstOrDefault()?.Value;
                    if (!string.IsNullOrEmpty(paramFa))
                    {
                        keyword.ArticleId = articleId;
                        keyword.Keyword = paramFa;
                        
                        keyword.IsPersian = paramFa.ContainsPersianCharacters() ?? false;
                    }
                    _context.Add(keyword);
                }
                _context.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return false;

        }

        (string Name, string Email) FindCorrespondingAuthor()
        {
            string name = "";
            string email = "";
            try
            {
                //WebDriverWait wait = new WebDriverWait(WebScraper.driver, TimeSpan.FromSeconds(5));
                //wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.XPath("//a[contains(@href, 'mailto:')]")));


                IWebElement emailElement = WebScraper.driver.FindElement(By.XPath("//a[contains(@href, 'mailto:')]"));

                IWebElement parentLi = emailElement.FindElement(By.XPath("./ancestor::li"));

                IWebElement nameElement = parentLi.FindElement(By.XPath(".//a[not(contains(@href, 'mailto:'))]"));

                name = nameElement.Text.Trim();
                email = emailElement.GetAttribute("href").Replace("mailto:", "").Trim();

                if (string.IsNullOrWhiteSpace(email))
                    throw new Exception();
                return (name, email);
            }
            catch (Exception e)
            {
                try
                {
                    var author = WebScraper.driver.FindElement(By.XPath("//sup[contains(text(),'1')]/preceding::a[contains(text(), ' ')][1]"));
                    var newName = author.Text.Trim();
                    if (!string.IsNullOrWhiteSpace(newName))
                        name = newName;

                    var emailElement = WebScraper.driver.FindElement(By.XPath("//div[@class='yw_text_small abstractsmall']//span[.//text()[contains(., '@')]]"));
                    var newEmail = emailElement.Text.Trim();
                    if (!string.IsNullOrWhiteSpace(newEmail))
                        email = newEmail;


                    Console.WriteLine("Author: " + name);
                    Console.WriteLine("Email: " + email);
                    return (name, email);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("mailto didn't found for: " + WebScraper.driver.Url);
                }
                return ("", "");
            }

        }
        public static (string Fa, string En) FindEnAndFa(params string[] abstracts)
        {
            string Abstract_FA = "";
            string Abstract_EN = "";

            foreach (var abstractText in abstracts)
            {
                bool? containsPersian = abstractText.ContainsPersianCharacters();

                if (containsPersian == true)
                    Abstract_FA = abstractText;
                else if (containsPersian == false)
                    Abstract_EN = abstractText;
            }

            return (Abstract_FA, Abstract_EN);
        }
        //private string GetELocationID(string eIdType)
        //{
        //    var eLocationID = xmlDoc?.Descendants("ELocationID")
        //        .FirstOrDefault(e => e.Attribute("EIdType")?.Value.Equals(eIdType, StringComparison.OrdinalIgnoreCase) == true);

        //    return eLocationID?.Value ?? string.Empty;
        //}

        private string GetTagValue(string tagName, int selectNumber = 0, XDocument? document = null, Dictionary<string, string>? attributes = null)
        {
            try
            {
                if (document == null)
                    document = xmlDoc;

                var elements = document!.Descendants()
                    .Where(e => string.Equals(e.Name.LocalName, tagName, StringComparison.OrdinalIgnoreCase));

                if (attributes != null && attributes.Any())
                {
                    elements = elements.Where(e =>
                        attributes.All(attr =>
                            e.Attributes().Any(a =>
                                string.Equals(a.Name.LocalName, attr.Key, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(a.Value, attr.Value, StringComparison.OrdinalIgnoreCase)
                            )
                        ));
                }

                var element = elements.ElementAtOrDefault(selectNumber);
                return element != null ? element.Value.Trim() : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string GetTagValue(string tagName, XElement document, int selectNumber = 0)
        {
            try
            {
                var element = document?.Descendants(tagName).ElementAtOrDefault(selectNumber);
                var text = element != null ? element.Value.Trim() : string.Empty;
                return text;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        private string FindXMLLink()
        {
            try
            {
                var aElement = WebScraper.driver.FindElement(
                    By.XPath("//a[contains(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'article')" +
                    " and contains(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'xml')]"));
                var link = aElement?.GetAttribute("href");
                link = link.Replace("&lang=en", "").Replace("lang=en", "").Replace("&lang=fa", "").Replace("lang=fa", "");
                return link;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error finding XML link: " + e.Message);
                return string.Empty;
            }
        }
        private string GetContentOfUrl(string url)
        {
            HttpWebRequest request = WebRequest.Create(new Uri(url)) as HttpWebRequest;
            request.Proxy = null;
            request.AllowAutoRedirect = true;
            request.ContentType = "application/x-www-form-urlencoded";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/114.0.0.0 Safari/537.36";

            try
            {
                using (var response = request.GetResponse())
                using (var receiveStream = response.GetResponseStream())
                using (var readStream = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    var content = readStream.ReadToEnd();
                    return content;
                }
            }
            catch (WebException ex)
            {
                using (var errorResponse = ex.Response)
                using (var stream = errorResponse?.GetResponseStream())
                using (var reader = new StreamReader(stream ?? Stream.Null))
                {
                    string errorText = reader.ReadToEnd();
                    // می‌توانید اینجا خطا را لاگ کنید
                    throw new Exception($"خطا در دریافت محتوا: {ex.Message}\n{errorText}", ex);
                }
            }
        }

    }

}
