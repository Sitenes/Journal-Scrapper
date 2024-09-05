using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using static JournalScrapper.Entity;

namespace JournalScrapper
{
    internal class ExtractXml
    {
        public bool ExtractXML(string pageLink,int journalId)
        {
            var driver = WebScraper.driver;
            if (string.IsNullOrWhiteSpace(pageLink) || journalId == 0)
                return false;


            string articleXMLLink = FindXMLLink();
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


            WebScraper.GetPageContent(articleXMLLinkFa);


            var hasPublisherName = driver.FindElements(By.XPath("//*[contains(text(), 'PublisherName')]")).Count > 0;

            Article articleInfo = new Article
            {
                PublisherNameFA = hasPublisherName ? GetTagValue("//PublisherName", driver) : GetTagValue("//title_fa", driver),
                Issn = hasPublisherName ? GetTagValue("//Issn", driver) : GetTagValue("//journal_id_issn", driver),
                Volume = hasPublisherName ? GetTagValue("//Volume", driver) : GetTagValue("//volume", driver),
                Issue = hasPublisherName ? GetTagValue("//Issue", driver) : GetTagValue("//number", driver),
                doi = hasPublisherName ? GetTagValue("//doi", driver) : GetTagValue("//journal_id_doi", driver),
                pii = hasPublisherName ? GetTagValue("//pii", driver) : GetTagValue("//journal_id_pii", driver),
                PublicationType = hasPublisherName ? GetTagValue("//PublicationType", driver) : GetTagValue("//publish_type", driver),
                ArchiveCopySource = hasPublisherName ? GetTagValue("//ArchiveCopySource", driver) : GetTagValue("//web_url", driver),
                JournalTitleFA = hasPublisherName ? GetTagValue("//JournalTitle", driver) : GetTagValue("//title_fa", driver),
                Title_FA = hasPublisherName ? GetTagValue("//ArticleTitle", driver) : GetTagValue("//title_fa", driver),
                FirstPage = hasPublisherName ? GetTagValue("//FirstPage", driver) : GetTagValue("//start_page", driver),
                LastPage = hasPublisherName ? GetTagValue("//LastPage", driver) : GetTagValue("//end_page", driver),
                PubDate = GetTagValue("//PubDate", driver),
                PubDateReceived = GetTagValue("//PubDateReceived", driver),
                PDFFilePath = GetTagValue("//PDFFilePath", driver),
                JournalId = journalId,
            };
            var Abstract_FA = hasPublisherName ? GetTagValue("//Abstract", driver) : GetTagValue("//abstract", driver);
            var OtherAbstract_FA = hasPublisherName ? GetTagValue("//OtherAbstract", driver) : GetTagValue("//abstract_fa", driver);

            WebScraper.GetPageContent(articleXMLLinkEn);

            //SaveArticleInfoAsync(articleInfo);
            //ProcessKeywordsAndAuthorsAsync(driver, articleInfo);
            return true;
        }
        public static bool ContainsPersianCharacters(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;
            string persianPattern = @"[\u0600-\u06FF\uFB8A\uFB8B\uFB8C\uFB8D\uFB8E\uFB8F\uFB90-\uFBFF]";

            var hasPersian = Regex.IsMatch(input, persianPattern);
            return hasPersian;
        }
        private string GetTagValue(string xpath, IWebDriver driver)
        {
            try
            {
                var element = driver.FindElement(By.XPath(xpath));
                return element != null ? element.Text.Trim() : string.Empty;
            }
            catch (NoSuchElementException)
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
                var link =  aElement?.GetAttribute("href");
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
