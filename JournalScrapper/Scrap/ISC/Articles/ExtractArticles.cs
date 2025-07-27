using DataLayer;
using JournalScrappers;
using JournalScrappers.Scrap.ISC.Articles;
using OpenQA.Selenium;
using System.Net;
using System.Text;

class ExtractArticles
{
    private readonly DynamicDbContext _context;

    public ExtractArticles(DynamicDbContext context)
    {
        this._context = context;
    }
    public void ScrapArticles()
    {
        ExtractXml extractXml = new ExtractXml(_context);
        var journals = _context.Journals.Where(x=>x.URL != "" && x.IsIsc == true).ToList()/*.Reverse<Journal>()*/;
        foreach (var journal in journals)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(journal.URL)
                    || _context.Articles.Any(x => x.JournalId == journal.Id) // comment if you want get all again
                    )
                    continue;

                WebScraper.GetPageContent(journal.URL);

                var plusXpath = By.XPath("//*[not(self::a or self::button) and (contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'plus') or contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'angle-down') or contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'pull-right'))]");
                var plusElements = WebScraper.driver.FindElements(plusXpath);
                var journalUrl = WebScraper.driver.Url;
                foreach (var plusElement in plusElements)
                {
                    IJavaScriptExecutor jsExecutor = (IJavaScriptExecutor)WebScraper.driver;
                    jsExecutor.ExecuteScript("arguments[0].click();", plusElement);
                    if (!WebScraper.driver.Url.Contains(journalUrl))
                    {
                        WebScraper.driver.Navigate().Back();
                        plusElements = WebScraper.driver.FindElements(plusXpath);
                    }
                }
                Thread.Sleep(500);

                var issues = WebScraper.driver.FindElements(By.XPath("//a[contains(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'issue')]"))?.Select(x => x.GetAttribute("href")).ToList();

                foreach (var issue in issues)
                {
                    WebScraper.GetPageContent(issue);
                    var articles = WebScraper.driver.FindElements(By.XPath("//a[contains(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'article') and not(ancestor::footer)]"))
                    ?.Select(x => x.GetAttribute("href"))
                    .Distinct()
                    .Where(x => !x.EndsWith(".pdf") && !x.Contains("linkedin", StringComparison.CurrentCultureIgnoreCase))
                    .ToList();
                    foreach (var article in articles)
                    {
                        try
                        {
                            extractXml.ExtractXML(article, journal.Id);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            WebScraper.WriteFailedCsv($"ExtractXML Failed -> article:{article}", e);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                WebScraper.WriteFailedCsv($"Journal Failed -> id:{journal.Id},link:{journal.URL}", e);
            }
        }
    }


}