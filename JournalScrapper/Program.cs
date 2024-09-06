using JournalScrapper;
using OpenQA.Selenium;

AppDbContext _context = new AppDbContext();
ExtractXml extractXml = new ExtractXml();
var journals = _context.Journals.ToList();
foreach (var journal in journals)
{
    try
    {
        throw new Exception("test");

        if (string.IsNullOrWhiteSpace(journal.URL))
            continue;

        WebScraper.GetPageContent(journal.URL);
        var plusElements = WebScraper.driver.FindElements(By.XPath("//i[contains(translate(@class, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'plus')]"));

        foreach (var plusElement in plusElements)
        {
            IJavaScriptExecutor jsExecutor = (IJavaScriptExecutor)WebScraper.driver;
            jsExecutor.ExecuteScript("arguments[0].click();", plusElement);
        }
        System.Threading.Thread.Sleep(1000);  // صبر کردن برای بارگذاری تغییرات

        var issues = WebScraper.driver.FindElements(By.XPath("//a[contains(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'issue')]"))?.Select(x => x.GetAttribute("href")).ToList();
        foreach (var issue in issues)
        {
            WebScraper.GetPageContent(issue);
            var articles = WebScraper.driver.FindElements(By.XPath("//a[contains(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'article')]"))?.Select(x => x.GetAttribute("href")).Distinct().ToList();
            foreach (var article in articles)
            {
                try
                {
                    extractXml.ExtractXML(article, journal.Journal_id);
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
        WebScraper.WriteFailedCsv($"Journal Failed -> id:{journal.Journal_id},link:{journal.URL}", e);
    }
}