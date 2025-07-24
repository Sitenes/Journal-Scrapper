//using System.Net;
//using System.Threading.Channels;
//using System.Xml.Linq;
//using CSV2Sql.Models;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using Microsoft.IdentityModel.Tokens;
//using OpenQA.Selenium;
//using OpenQA.Selenium.BiDi.Log;
//using OpenQA.Selenium.Chrome;
//using OpenQA.Selenium.Support.UI;
//using SeleniumExtras.WaitHelpers;
//using static Azure.Core.HttpHeader;
//using static JournalScrapper.Entity.ISCMySql;

//namespace JournalScrapper.Scrap.ISC.Journal;

//public class JournalCoverScrapper
//{
//    private readonly AppDbContext _context;
//    private readonly WebDriver _webDriver;

//    public JournalCoverScrapper()
//    {
//        _context = new AppDbContext();

//        var options = new ChromeOptions();
//        //options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36");
//        _webDriver = new ChromeDriver(options);
//        _webDriver.Manage().Window.Maximize();
//        ((IJavaScriptExecutor)_webDriver).ExecuteScript("document.body.style.zoom='50%';");
//    }
//    public void ScrapAllJournalCovers()
//    {
//        var journals = _context.Journals.ToList()/*.Reverse<Journal>()*/;
//        foreach (var journal in journals)
//        {
//            try
//            {
//                if (string.IsNullOrWhiteSpace(journal.URL)
//                    || _context.Articles.Any(x => x.JournalId == journal.Id) // comment if you want get all again
//                    )
//                    continue;
//                ScrapeCoverOfJournal(journal.URL, journal.Title_Fa ?? journal.Title_EN);
//            }
//            catch (Exception e)
//            {
//                Console.WriteLine(e);
//                WebScraper.WriteFailedCsv($"Journal Failed -> id:{journal.Id},link:{journal.URL}", e);
//            }
//        }
//    }

//    public void ScrapeCoverOfJournal(string url, string? title)
//    {
//        _webDriver.NavigateWithScrollAndZoom(url);
//        Thread.Sleep(5000);
//        title = title?.Trim();

//        if (string.IsNullOrWhiteSpace(title))
//            return;

//        try
//        {
//            // پیدا کردن اولین img با alt برابر با title صفحه
//            var imageElement = _webDriver.FindElements(By.TagName("img"))
//                .FirstOrDefault(img =>
//                {
//                    var alt = img.GetAttribute("alt")?.Trim();
//                    return !string.IsNullOrEmpty(alt) && alt.Contains("Main Image", StringComparison.OrdinalIgnoreCase);
//                });
//            if (imageElement == null)
//            {
//                imageElement = _webDriver.FindElements(By.TagName("img"))
//               .FirstOrDefault(img =>
//               {
//                   var alt = img.GetAttribute("alt")?.Trim();
//                   return !string.IsNullOrEmpty(alt) && alt.Contains(title, StringComparison.OrdinalIgnoreCase);
//               });
//            }
//            if (imageElement == null)
//            {
//                Console.WriteLine($"Error : {title}|{url}");
//                return;
//            }

//            string imageUrl = imageElement.GetAttribute("src") ?? "";
//            if (string.IsNullOrWhiteSpace(imageUrl))
//                return;

//            // اگر آدرس نسبی بود تبدیل به آدرس کامل کن
//            if (!imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
//            {
//                Uri baseUri = new Uri(url);
//                Uri fullUri = new Uri(baseUri, imageUrl);
//                imageUrl = fullUri.ToString();
//            }

//            var guidFileName = $"{Guid.NewGuid()}.jpg";
//            var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "covers");
//            Directory.CreateDirectory(wwwrootPath);
//            var localPath = Path.Combine(wwwrootPath, guidFileName);

//            using (WebClient client = new WebClient())
//            {
//                client.Proxy = null; // جلوگیری از استفاده از پراکسی لوکال
//                client.DownloadFile(imageUrl, localPath);
//            }


//            var journal = _context.Journals.FirstOrDefault(j => j.URL == url);
//            if (journal != null)
//            {
//                journal.CoverImagePath = $"/covers/{guidFileName}";
//                _context.SaveChanges();
//            }
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Error extracting image from: {url}");
//            WebScraper.WriteFailedCsv($"Image scrape failed -> {url}", ex);
//        }
//    }
//}