using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using static Azure.Core.HttpHeader;
using static JournalScrapper.Entity;

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


            xmlDoc = XDocument.Parse(WebScraper.GetPageContent(articleXMLLinkFa));
            var xmlDocFa = xmlDoc;

            var hasPublisherName = WebScraper.driver.FindElements(By.XPath("//*[contains(text(), 'PublisherName')]")).Count > 0;

            Article articleInfo = new Article
            {
                PublisherName_FA = GetTagValue(hasPublisherName ? "PublisherName" : "title_fa"),
                PublisherName_EN = GetTagValue(hasPublisherName ? "PublisherName" : "title"),
                JournalTitleFA = GetTagValue(hasPublisherName ? "JournalTitle" : "title_fa"),
                Issn = GetTagValue(hasPublisherName ? "Issn" : "journal_id_issn"),
                Volume = GetTagValue(hasPublisherName ? "Volume" : "volume"),
                Issue = GetTagValue(hasPublisherName ? "Issue" : "number"),

                PubDate = GetTagValue(hasPublisherName ? "PubDate" : "pubdate"),
                PubDateReceived = hasPublisherName ? GetTagValue("PubDate", 1) : GetTagValue("received"),

                Title_FA = GetTagValue(hasPublisherName ? "ArticleTitle" : "title_fa"),
                Title_EN = GetTagValue(hasPublisherName ? "VernacularTitle" : "title"),

                FirstPage = GetTagValue(hasPublisherName ? "FirstPage" : "start_page"),
                LastPage = GetTagValue(hasPublisherName ? "LastPage" : "end_page"),

                PublicationType = GetTagValue(hasPublisherName ? "PublicationType" : "publish_type"),
                Abstract_FA = GetTagValue(hasPublisherName ? "Abstract" : "abstract"),
                Abstract_EN = GetTagValue(hasPublisherName ? "OtherAbstract" : "abstract_fa"),

                pii = GetTagValue(hasPublisherName ? "ELocationID" : "journal_id_pii"),
                doi = hasPublisherName ? GetTagValue("ELocationID", 1) : GetTagValue("journal_id_doi"),

                ArchiveCopySource = GetTagValue(hasPublisherName ? "ArchiveCopySource" : "web_url"),

                JournalId = journalId,
                CorrespondingAuthorEmail = correspondingEmail,
                CorrespondingAuthorName = correspondingName,
                Url = pageLink
            };

            var Abstract_FA = GetTagValue("Abstract");
            var OtherAbstract_FA = GetTagValue("OtherAbstract");
            var Title_FA = GetTagValue("ArticleTitle");
            var VernacularTitle_FA = GetTagValue("VernacularTitle");

            xmlDoc = XDocument.Parse(WebScraper.GetPageContent(articleXMLLinkEn));
            var xmlDocEn = xmlDoc;
            var PublisherName_EN = GetTagValue("PublisherName");
            articleInfo.PublisherName_EN = ContainsPersianCharacters(PublisherName_EN) ?? true ? "" : PublisherName_EN;

            var Abstract_EN = GetTagValue("Abstract");
            var OtherAbstract_EN = GetTagValue("OtherAbstract");
            var Title_EN = GetTagValue("ArticleTitle");
            var VernacularTitle_EN = GetTagValue("VernacularTitle");

            (articleInfo.Abstract_FA, articleInfo.Abstract_EN) = FindEnAndFa(Abstract_FA, OtherAbstract_FA, Abstract_EN, OtherAbstract_EN);
            (articleInfo.Title_FA, articleInfo.Title_EN) = FindEnAndFa(Title_FA, VernacularTitle_FA, Title_EN, VernacularTitle_EN);
            var pdfTitle = string.IsNullOrWhiteSpace(articleInfo.Title_EN) ? articleInfo.Title_FA : articleInfo.Title_EN;
            var firstAuthor = GetTagValue("FirstName") + " " + GetTagValue("LastName");
            if (_context.Articles.Any(x => x.Title_EN == articleInfo.Title_EN || x.Title_FA == articleInfo.Title_FA))
                return true;

            articleInfo.PDFFilePath = GetArticlePDFFile(pdfTitle, firstAuthor, articleInfo.ArchiveCopySource);

            _context.Articles.Add(articleInfo);
            _context.SaveChanges();
            ExtractAuthors(xmlDocFa, xmlDocEn, articleInfo.Id);
            ExtractKeywords(xmlDocFa, articleInfo.Id);
            ExtractKeywords(xmlDocEn, articleInfo.Id);
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
                    Console.WriteLine($"PDF downloaded successfully to {downloadPath}");
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
                    FirstNameFA = GetTagValue("FirstName", j, docFa),
                    LastNameFA = GetTagValue("LastName", j, docFa),
                    FirstNameEN = GetTagValue("FirstName", j, docEn),
                    LastNameEN = GetTagValue("LastName", j, docEn),
                    AffiliationFA = GetTagValue("Affiliation", j, docFa),
                    AffiliationEN = GetTagValue("Affiliation", j, docEn),
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
                WebDriverWait wait = new WebDriverWait(WebScraper.driver, TimeSpan.FromSeconds(5));
                wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.XPath("//a[contains(@href, 'mailto:')]")));


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
        (string Fa, string En) FindEnAndFa(string abstractFA, string otherAbstractFA, string abstractEN, string otherAbstractEN)
        {
            var Abstract_FA = "";
            var Abstract_EN = "";
            if (ContainsPersianCharacters(abstractFA) == true)
                Abstract_FA = abstractFA;
            else if (ContainsPersianCharacters(abstractFA) == false)
                Abstract_EN = abstractFA;

            // بررسی OtherAbstract_FA
            if (ContainsPersianCharacters(otherAbstractFA) == true)
                Abstract_FA = otherAbstractFA;
            else if (ContainsPersianCharacters(otherAbstractFA) == false)
                Abstract_EN = otherAbstractFA;

            // بررسی Abstract_EN
            if (ContainsPersianCharacters(abstractEN) == true)
                Abstract_FA = abstractEN;
            else if (ContainsPersianCharacters(abstractEN) == false)
                Abstract_EN = abstractEN;

            // بررسی OtherAbstract_EN
            if (ContainsPersianCharacters(otherAbstractEN) == true)
                Abstract_FA = otherAbstractEN;
            else if (ContainsPersianCharacters(otherAbstractEN) == false)
                Abstract_EN = otherAbstractEN;
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

            var hasPersian = Regex.IsMatch(input, persianPattern);
            return hasPersian;
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
    }
}
