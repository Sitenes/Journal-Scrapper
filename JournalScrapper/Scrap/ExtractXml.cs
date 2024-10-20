using JournalScrapper.Entity;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using static Azure.Core.HttpHeader;

namespace JournalScrapper
{
    internal class ExtractXml
    {
        XDocument? xmlDoc = null;
        AppDbContext _context = new AppDbContext();
        public bool ExtractXML(string pageLink, int journalId)
        {
            if (string.IsNullOrWhiteSpace(pageLink) || journalId == 0)
                return false;
            if (_context.Articles.Any(x => x.Url == pageLink))
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

            var hasPublisherName = WebScraper.driver.FindElements(By.XPath("//*[contains(text(), 'PublisherName')]")).Count > 0;
            if (hasPublisherName)
            {
                Article articleInfo = new Article
                {
                    PublisherName_FA = GetTagValue("PublisherName"),
                    JournalTitle_FA = GetTagValue("JournalTitle"),
                    JournalTitle_EN = GetTagValue("JournalTitle"),
                    Issn = GetTagValue("Issn"),
                    Volume = GetTagValue("Volume"),
                    Issue = GetTagValue("Issue"),

                    Title_FA = GetTagValue("ArticleTitle"),
                    Title_EN = GetTagValue("VernacularTitle"),

                    FirstPage = GetTagValue("FirstPage"),
                    LastPage = GetTagValue("LastPage"),

                    PublicationType = GetTagValue("PublicationType"),
                    Abstract_FA = GetTagValue("Abstract"),
                    Abstract_EN = GetTagValue("OtherAbstract"),

                    pii = GetTagValue("ELocationID"),
                    doi = GetTagValue("ELocationID", 1),

                    ArchiveCopySource = GetTagValue("ArchiveCopySource"),

                    JournalId = journalId,
                    CorrespondingAuthorEmail = correspondingEmail,
                    CorrespondingAuthorName = correspondingName,
                    Url = pageLink
                };
                var pubDateElem = xmlDocFa.Descendants("pubdate")
    .ElementAt(0);
                if (pubDateElem != null)
                {
                    int year = int.Parse(pubDateElem.Element("year").Value);
                    int month = int.Parse(pubDateElem.Element("month").Value);
                    int day = int.Parse(pubDateElem.Element("day").Value);
                    articleInfo.PubDate = new DateTime(year, month, day).ToString("yyyy-MM-dd");
                }

                var pubDateResElem = xmlDocFa.Descendants("pubdate")
.ElementAt(1);
                if (pubDateElem != null)
                {
                    int year = int.Parse(pubDateResElem.Element("year").Value);
                    int month = int.Parse(pubDateResElem.Element("month").Value);
                    int day = int.Parse(pubDateResElem.Element("day").Value);
                    articleInfo.PubDateReceived = new DateTime(year, month, day).ToString("yyyy-MM-dd");
                }

                var Abstract_FA = GetTagValue("Abstract");
                var OtherAbstract_FA = GetTagValue("OtherAbstract");
                var Title_FA = GetTagValue("ArticleTitle");
                var VernacularTitle_FA = GetTagValue("VernacularTitle");
                try
                {
                    xmlDoc = XDocument.Parse(GetContentOfUrl(articleXMLLinkEn));
                }
                catch (Exception)
                {
                }
                var xmlDocEn = xmlDoc;
                articleInfo.PublisherName_EN = GetTagValue("PublisherName");
                (articleInfo.PublisherName_FA, articleInfo.PublisherName_EN) = FindEnAndFa(articleInfo.PublisherName_EN, articleInfo.PublisherName_FA);

                articleInfo.JournalTitle_FA = GetTagValue("JournalTitle");
                (articleInfo.JournalTitle_FA, articleInfo.JournalTitle_EN) = FindEnAndFa(articleInfo.JournalTitle_FA, articleInfo.JournalTitle_EN);

                var Abstract_EN = GetTagValue("Abstract");
                var OtherAbstract_EN = GetTagValue("OtherAbstract");
                var Title_EN = GetTagValue("ArticleTitle");
                var VernacularTitle_EN = GetTagValue("VernacularTitle");

                (articleInfo.Abstract_FA, articleInfo.Abstract_EN) = FindEnAndFa(Abstract_FA, OtherAbstract_FA, Abstract_EN, OtherAbstract_EN);
                (articleInfo.Title_FA, articleInfo.Title_EN) = FindEnAndFa(Title_FA, VernacularTitle_FA, Title_EN, VernacularTitle_EN);
                var pdfTitle = string.IsNullOrWhiteSpace(articleInfo.Title_EN) ? articleInfo.Title_FA : articleInfo.Title_EN;
                var firstAuthor = GetTagValue("FirstName") + " " + GetTagValue("LastName");
                if (_context.Articles.Any(x => x.Title_EN == articleInfo.Title_EN && x.Title_FA == articleInfo.Title_FA))
                    return true;

                //articleInfo.PDFFilePath = GetArticlePDFFile(pdfTitle, firstAuthor, articleInfo.ArchiveCopySource);

                _context.Articles.Add(articleInfo);
                _context.SaveChanges();
                ExtractAuthors(xmlDocFa, xmlDocEn, articleInfo.Id);
                ExtractKeywords(xmlDocFa, articleInfo.Id);
                ExtractKeywords(xmlDocEn, articleInfo.Id);
            }
            else
            {
                var documentArticle = xmlDocFa.Root.Descendants("article").ElementAt(0);
                var articleInfo = new Article
                {
                    JournalTitle_FA = GetTagValue("title_fa"),
                    JournalTitle_EN = GetTagValue("title"),
                    Issn = GetTagValue("journal_id_issn"),
                    Volume = GetTagValue("volume"),
                    Issue = GetTagValue("number"),
                    doi = GetTagValue("journal_id_doi"),
                    pii = GetTagValue("journal_id_pii"),
                    PublicationType = GetTagValue("publish_type"),
                    Title_FA = GetTagValue("title_fa", documentArticle),
                    Title_EN = GetTagValue("title",  documentArticle),
                    FirstPage = GetTagValue("start_page",  documentArticle),
                    LastPage = GetTagValue("end_page",  documentArticle),
                    Abstract_FA = GetTagValue("abstract_fa",  documentArticle),
                    Abstract_EN = GetTagValue("abstract",  documentArticle),
                    ArchiveCopySource = GetTagValue("web_url"),
                    CorrespondingAuthorEmail = correspondingEmail,
                    CorrespondingAuthorName = correspondingName,
                    //JournalTitle_FA = GetTagValue("title_fa")
                };

                // Extract PubDate Gregorian
                var pubDateElem = xmlDocFa.Descendants("pubdate")
                    .FirstOrDefault(x => x.Element("type")?.Value == "gregorian");
                if (pubDateElem != null)
                {
                    int year = int.Parse(pubDateElem.Element("year").Value);
                    int month = int.Parse(pubDateElem.Element("month").Value);
                    int day = int.Parse(pubDateElem.Element("day").Value);
                    articleInfo.PubDate = new DateTime(year, month, day).ToString("yyyy-MM-dd");
                }
                _context.Articles.Add(articleInfo);
                _context.SaveChanges();

                // Process authors
                var authorElements = xmlDocFa.Descendants("author");
                foreach (var authorElement in authorElements)
                {
                    Author author = new Author
                    {
                        FirstName_FA = GetTagValue("first_name_fa",0, authorElement.Document),
                        LastName_FA = GetTagValue("last_name_fa", 0 ,authorElement.Document),
                        FirstName_EN = GetTagValue("first_name", 0 ,authorElement.Document),
                        LastName_EN = GetTagValue("last_name", 0 ,authorElement.Document),
                        Affiliation_FA = GetTagValue("affiliation_fa", 0 ,authorElement.Document),
                        Affiliation_EN = GetTagValue("affiliation", 0 ,authorElement.Document),
                        Identifier = GetTagValue("orcid", 0 ,authorElement.Document)
                    };

                    _context.Authors.Add(author);
                    //_context.SaveChanges();
                }

                // Process keywords
                var keywords = xmlDocFa.Descendants("keyword_fa").ToList();
                keywords.AddRange(xmlDocFa.Descendants("keyword").ToList());
                
                for (int i = 0; i < keywords.Count; i++)
                {
                    var nodeFa = keywords[i];

                    Keyword keyword = new Keyword();
                    //if (nodeFa.Attribute("Type")?.Value == "keyword")
                    //{
                    var paramFa = nodeFa.Descendants("Param").FirstOrDefault()?.Value;
                    if (!string.IsNullOrEmpty(paramFa))
                    {
                        keyword.ArticleId = articleInfo.Id;
                        keyword.Value = paramFa;

                        keyword.IsPersian = ContainsPersianCharacters(paramFa) ?? false;
                    }
                    //}

                    _context.Add(keyword);
                }
                _context.SaveChanges();
                // Save article to database
                }
            return true;
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
                title = System.Text.RegularExpressions.Regex.Replace(title, invalidCharsPattern, "");
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
                Author author = new Author
                {
                    FirstName_FA = GetTagValue("FirstName", j, docFa),
                    LastName_FA = GetTagValue("LastName", j, docFa),
                    FirstName_EN = GetTagValue("FirstName", j, docEn),
                    LastName_EN = GetTagValue("LastName", j, docEn),
                    Affiliation_FA = GetTagValue("Affiliation", j, docFa),
                    Affiliation_EN = GetTagValue("Affiliation", j, docEn),
                    Identifier = GetTagValue("Identifier", j, docEn),
                    ArticleId = articleId,
                    Order = j + 1
                };

                _context.Authors.Add(author);
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

                    Keyword keyword = new Keyword();
                    //if (nodeFa.Attribute("Type")?.Value == "keyword")
                    //{
                    var paramFa = nodeFa.Descendants("Param").FirstOrDefault()?.Value;
                    if (!string.IsNullOrEmpty(paramFa))
                    {
                        keyword.ArticleId = articleId;
                        keyword.Value = paramFa;

                        keyword.IsPersian = ContainsPersianCharacters(paramFa) ?? false;
                    }
                    //}

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
                bool? containsPersian = ContainsPersianCharacters(abstractText);

                if (containsPersian == true)
                    Abstract_FA = abstractText;
                else if (containsPersian == false)
                    Abstract_EN = abstractText;
            }

            return (Abstract_FA, Abstract_EN);
        }
        private string GetELocationID(string eIdType)
        {
            var eLocationID = xmlDoc?.Descendants("ELocationID")
                .FirstOrDefault(e => e.Attribute("EIdType")?.Value.Equals(eIdType, StringComparison.OrdinalIgnoreCase) == true);

            return eLocationID?.Value ?? string.Empty;
        }
        public static bool? ContainsPersianCharacters(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            string persianPattern = @"[\u0600-\u06FF\uFB8A\uFB8B\uFB8C\uFB8D\uFB8E\uFB8F\uFB90-\uFBFF]";
            string englishPattern = @"[a-zA-Z]";

            int persianCount = Regex.Matches(input, persianPattern).Count;
            int englishCount = Regex.Matches(input, englishPattern).Count;

            // اگر تعداد کاراکترهای فارسی بیشتر باشد true بازگردانده می‌شود و در غیر این صورت false
            return persianCount > englishCount;
        }
        private string GetTagValue(string tagName, int selectNumber = 0, XDocument? document = null)
        {
            try
            {
                if (document == null)
                    document = xmlDoc;
                var element = document?.Descendants(tagName).ElementAtOrDefault(selectNumber);
                var text = element != null ? element.Value.Trim() : string.Empty;
                return text;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        private string GetTagValue(string tagName,XElement document,int selectNumber = 0)
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
            var response = request.GetResponse();
            Stream receiveStream = response.GetResponseStream();

            StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
            var content = readStream.ReadToEnd();
            return content;
        }
    }

}
