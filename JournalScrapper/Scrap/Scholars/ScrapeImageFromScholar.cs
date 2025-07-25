using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using DataLayer;

namespace JournalScrappers.Scrap.Scholar
{
    public class ScrapeImageFromScholar
    {
        private readonly WebDriver _webDriver;
        private readonly DynamicDbContext _context;

        public ScrapeImageFromScholar(DynamicDbContext context)
        {

            var options = new ChromeOptions();
            //options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36");
            _webDriver = new ChromeDriver(options);
            _webDriver.Manage().Window.Maximize();
            ((IJavaScriptExecutor)_webDriver).ExecuteScript("document.body.style.zoom='50%';");
            this._context = context;
        }
        public void ScrapAllProfileImages()
        {
            var professors = _context.Professors.ToList()/*.Reverse<Journal>()*/;
            foreach (var professor in professors)
            {
                try
                {
                    professor.ImageUrl = "";
                    if (string.IsNullOrWhiteSpace(professor.GoogleScholarID))
                        continue;
                    var url = "https://scholar.googleusercontent.com/citations?view_op=medium_photo&citpid=5&user=" + professor.GoogleScholarID;

                    var path = ScrapeCoverOfJournal(url);
                    professor.ImageUrl = path;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    WebScraper.WriteFailedCsv($"Journal Failed -> id:{professor.Id}", e);
                }
            }
            _context.SaveChanges();
        }

        public string ScrapeCoverOfJournal(string url)
        {
            //_webDriver.NavigateWithScrollAndZoom(url);
            //Thread.Sleep(2000);
            try
            {
                // پیدا کردن اولین img با alt برابر با title صفحه
                //var imageElement = _webDriver.FindElement(By.Id("gsc_prf_pup-img"));

                //if (imageElement == null)
                //{
                //    Console.WriteLine($"Error : {url}");
                //    return "";
                //}

                //string imageUrl = imageElement.GetAttribute("src") ?? "";
                //if (string.IsNullOrWhiteSpace(imageUrl))
                //    return "";

                //// اگر آدرس نسبی بود تبدیل به آدرس کامل کن
                //if (!imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                //{
                //    Uri baseUri = new Uri(url);
                //    Uri fullUri = new Uri(baseUri, imageUrl);
                //    imageUrl = fullUri.ToString();
                //}

                var guidFileName = $"{Guid.NewGuid()}.jpg";
                var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "covers");
                Directory.CreateDirectory(wwwrootPath);
                var localPath = Path.Combine(wwwrootPath, guidFileName);

                using (WebClient client = new WebClient())
                {
                    client.Proxy = null; // جلوگیری از استفاده از پراکسی لوکال
                    client.DownloadFile(url, localPath);
                }


                return $"upload/professor/profileimage/{guidFileName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting image from: {url}");
                WebScraper.WriteFailedCsv($"Image scrape failed -> {url}", ex);
            }
            return "";
        }
    }
}
