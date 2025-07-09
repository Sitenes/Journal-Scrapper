using System.Threading.Channels;
using System.Xml.Linq;
using CSV2Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace JournalScrapper.Scrap;

public class JournalScrapper
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _dbContext;
    private readonly WebDriver _webDriver;

    public JournalScrapper(IConfiguration configuration)
    {
        _configuration = configuration;
        _dbContext = new AppDbContext();
        _webDriver = new ChromeDriver();
    }

    public async Task Scrap()
    {
        await _webDriver.Navigate().GoToUrlAsync(_configuration["ArticleUrl"]);
        var webWait = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(20));
        webWait.Until(driver =>
            !driver.FindElement(By.Id("grdJournals_processing")).Displayed);


        // پیدا کردن المنت select
        var yearDropdown = _webDriver.FindElement(By.Id("dlyears1"));

        // ساخت SelectElement
        var selectElement = new SelectElement(yearDropdown);

        // انتخاب مقدار 1380
        selectElement.SelectByValue("1380");

        var searchButton = _webDriver.FindElement(By.XPath("//button[contains(text(), 'جستجو')]"));
        searchButton.Click();

        var lastPageNumber = _webDriver.FindElement(By.XPath("//a[@data-dt-idx=\"6\"]")).Text;
        var pageCount = Convert.ToInt32(lastPageNumber);
        var count = 1;
        for (var i = 0; i < pageCount; i++)
        {
            var table = _webDriver.FindElement(By.TagName("tbody"));
            var journals = table.FindElements(By.TagName("tr"));
            foreach (var journal in journals)
            {
                var name = journal.FindElement(By.CssSelector(" td:nth-child(3) > a"));

                var year = new Year
                {
                    YearPublished = _webDriver.FindElement(By.XPath("//tr[@role=\"row\"]/td[4]")).GetElementValueSafe(),
                    ImpactFactor = _webDriver.FindElement(By.XPath("//tr[@role=\"row\"]/td[5]")).GetElementValueSafe(),
                    ImpactFactorWithoutSelfCitation = _webDriver.FindElement(By.XPath("//tr[@role=\"row\"]/td[6]")).GetElementValueSafe(),
                    HIndex = _webDriver.FindElement(By.XPath("//tr[@role=\"row\"]/td[7]")).GetElementValueSafe(),
                    ImmediateImpactFactor = _webDriver.FindElement(By.XPath("//tr[@role=\"row\"]/td[8]")).GetElementValueSafe(),
                    CumulativeCitations = _webDriver.FindElement(By.XPath("//tr[@role=\"row\"]/td[9]")).GetElementValueSafe(),
                    JournalStatus = _webDriver.FindElement(By.XPath("//tr[@role=\"row\"]/td[10]/a")).GetAttribute("aria-label") ?? "",

                };

                await ScrapDetails(journal,year);

                Console.WriteLine($"** number: {count++}");
            }

            var nextPageButton = _webDriver.FindElement(By.Id("grdJournals_next"));
            nextPageButton.Click();
            var webDriverWait = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(20));
            webDriverWait.Until(driver =>
                !driver.FindElement(By.Id("grdJournals_processing")).Displayed);
        }

        _webDriver.Quit();
    }

    private async Task ScrapDetails(IWebElement journalElement, Year year)
    {
        var detailButton =
            journalElement.FindElement(By.CssSelector("td:nth-child(3) > a"));
        detailButton.Click();
        _webDriver.SwitchTo().Window(_webDriver.WindowHandles[1]);

        var journal = await ScrapInformation();

        var statusButton = _webDriver.FindElement(By.XPath("//*[@id=\"pills-status-tab\"]"));
        statusButton.Click();

        foreach (var tr in _webDriver.FindElements(By.XPath("//*[@id=\"ContentPlaceHolder1_grdStatus\"]/tbody/tr")))
        {

            var cells = tr.FindElements(By.TagName("td"));
            year.YearPublished = cells.FirstOrDefault().GetElementValueSafe();
            year.ImpactFactor = cells[1].GetElementValueSafe();
            year.SelfCitationFactor = cells[2].GetElementValueSafe();
            year.ImpactFactorWithoutSelfCitation = cells[3].GetElementValueSafe();
            year.JournalId = journal.Id;
            if (!await _dbContext.Years.AnyAsync(x => x.YearPublished == year.YearPublished && x.JournalId == year.JournalId))
                await _dbContext.Years.AddAsync(year);
            await _dbContext.SaveChangesAsync();

            var spans = cells.Where(x => x.FindElements(By.TagName("span")).Count > 0)
                .Select(x => x.FindElement(By.TagName("span"))).ToArray();

            var subjectName = spans[1].GetElementValueSafe();

            var subjectArea = _dbContext.ScopusSubjectAreas.FirstOrDefault(x => x.Name == subjectName);
            if (subjectArea == null)
            {
                subjectArea = new ScopusSubjectArea { Name = subjectName };
                _dbContext.ScopusSubjectAreas.Add(subjectArea);
                _dbContext.SaveChanges();
            }
            var categoryName = spans[3].GetElementValueSafe();
            var category = _dbContext.ScopusJournalCategories.FirstOrDefault(x => x.Name == categoryName);
            if (category == null)
            {
                category = new ScopusJournalCategory { Name = categoryName, SubjectAreaId = subjectArea.Id };
                _dbContext.ScopusJournalCategories.Add(category);
                _dbContext.SaveChanges();
            }
            var newQuality = new Qurtile
            {
                Year = year,
                JournalCategoryId = category.Id,
                QLevel = spans[0].Text.ToInt() ?? 0,
            };
            await _dbContext.AddAsync(newQuality);
        }

        await _dbContext.SaveChangesAsync();
        _webDriver.Close();
        _webDriver.SwitchTo().Window(_webDriver.WindowHandles[0]);
    }

    private async Task<Journal> ScrapInformation()
    {
        var informationButton = _webDriver.FindElement(By.XPath("//*[@id=\"pills-biblio-tab\"]"));

        informationButton.Click();

        var journal = await _dbContext.Journals.FirstOrDefaultAsync(x => x.Title_Fa == _webDriver.FindElement(By.XPath("//*[@id=\"tdTitle\"]")).GetElementValueSafe());
        if (journal == null)
        {
            journal = new Journal
            {
                Title_Fa = _webDriver.FindElement(By.XPath("//*[@id=\"tdTitle\"]")).GetElementValueSafe(),
                ISSN = _webDriver.FindElement(By.XPath("//*[@id=\"tdISSN\"]")).GetElementValueSafe(),
                EISSN = _webDriver.FindElement(By.XPath("//*[@id=\"tdEISSN\"]")).GetElementValueSafe(),
                Country = _webDriver.FindElement(By.XPath("//*[@id=\"tdCountry\"]")).GetElementValueSafe(),
                Publisher = _webDriver.FindElement(By.XPath("//*[@id=\"tdPublisher\"]")).GetElementValueSafe(),
                MicroLevelIssue = _webDriver.FindElement(By.XPath("//*[@id=\"tdSubject1\"]")).GetElementValueSafe(),
                IntermediateLevelIssue = _webDriver.FindElement(By.XPath("//*[@id=\"tdSubject2\"]")).GetElementValueSafe(),
                MacroLevelIssue = _webDriver.FindElement(By.XPath("//*[@id=\"tdSubject3\"]")).GetElementValueSafe(),
                Address = _webDriver.FindElement(By.XPath("//*[@id=\"tdPublisherAddress\"]")).GetElementValueSafe(),
                URL = _webDriver.FindElement(By.XPath("//*[@id=\"tdWebsite\"]")).GetElementValueSafe(),
                Email = _webDriver.FindElement(By.XPath("//*[@id=\"tdEmail\"]")).GetElementValueSafe(),
            };
            await _dbContext.Journals.AddAsync(journal);
            await _dbContext.SaveChangesAsync();
        }

        return journal;
    }

}
