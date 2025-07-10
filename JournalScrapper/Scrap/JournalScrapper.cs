using System.Threading.Channels;
using System.Xml.Linq;
using CSV2Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using static Azure.Core.HttpHeader;

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

        var options = new ChromeOptions();
        //options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36");
        _webDriver = new ChromeDriver(options);
        _webDriver.Manage().Window.Maximize();
        ((IJavaScriptExecutor)_webDriver).ExecuteScript("document.body.style.zoom='50%';");
    }

    public async Task Scrap()
    {
        _webDriver.NavigateWithScrollAndZoom(_configuration["ArticleUrl"]);
        var webWait = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(20));
        webWait.Until(driver =>
            !driver.FindElementSafe(By.Id("grdJournals_processing")).Displayed);

        Thread.Sleep(200);

        var yearDropdown = _webDriver.FindElementSafe(By.Id("dlyears1"));
        var selectElement = new SelectElement(yearDropdown);
        selectElement.SelectByValue("1380");

        var searchButton = _webDriver.FindElementSafe(By.XPath("//button[contains(text(), 'جستجو')]"));
        searchButton.Click();
        ((IJavaScriptExecutor)_webDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
        var lastPageNumber = _webDriver.FindElementSafe(By.XPath("//a[@data-dt-idx=\"6\"]")).Text;
        var pageCount = Convert.ToInt32(lastPageNumber);
        var count = 1;
        for (var i = 0; i < pageCount; i++)
        {
            var table = _webDriver.FindElementSafe(By.TagName("tbody"));
            var journals = table.FindElementsSafe(By.TagName("tr"));
            foreach (var journal in journals)
            {
                Thread.Sleep(100);
                var name = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[3]/a"));

                var yearPublished = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[4]")).GetElementValueSafe();
                var year = await _dbContext.Years.Include(x => x.Journal).FirstOrDefaultAsync(x => x.Journal.Title_Fa == name.GetElementValueSafe() && x.YearPublished == yearPublished);
                if (year == null)
                {
                    year = new Year
                    {
                        YearPublished = yearPublished,
                        ImpactFactor = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[5]")).GetElementValueSafe(),
                        ImpactFactorWithoutSelfCitation = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[6]")).GetElementValueSafe(),
                        HIndex = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[7]")).GetElementValueSafe(),
                        ImmediateImpactFactor = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[8]")).GetElementValueSafe(),
                        CumulativeCitations = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[9]")).GetElementValueSafe(),
                        JournalStatus = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[10]/a"))?.GetAttribute("aria-label") ?? "",
                    };

                    name.Click();
                    await ScrapDetails(journal, year);

                }
                else
                {
                    year.ImpactFactor = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[5]")).GetElementValueSafe();
                    year.ImpactFactorWithoutSelfCitation = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[6]")).GetElementValueSafe();
                    year.HIndex = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[7]")).GetElementValueSafe();
                    year.ImmediateImpactFactor = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[8]")).GetElementValueSafe();
                    year.CumulativeCitations = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[9]")).GetElementValueSafe();
                    year.JournalStatus = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[10]/a")).GetAttribute("aria-label") ?? "";
                    _dbContext.Update(year);
                    await _dbContext.SaveChangesAsync();
                }


                Console.WriteLine($"** number: {count++}");
            }

            var nextPageButton = _webDriver.FindElementSafe(By.Id("grdJournals_next"));
            nextPageButton.Click();
            var webDriverWait = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(20));
            webDriverWait.Until(driver =>
                !driver.FindElementSafe(By.Id("grdJournals_processing")).Displayed);
        }

        _webDriver.Quit();
    }

    private async Task ScrapDetails(IWebElement journalElement, Year? year)
    {
        _webDriver.SwitchTo().Window(_webDriver.WindowHandles[1]);

        var journal = await ScrapInformation();

        var statusButton = _webDriver.FindElementSafe(By.XPath("//*[@id=\"pills-status-tab\"]"));
        statusButton.Click();

        foreach (var tr in _webDriver.FindElementsSafe(By.XPath("//*[@id=\"ContentPlaceHolder1_grdStatus\"]/tbody/tr")))
        {
            var cells = tr.FindElementsSafe(By.TagName("td")).Select(_webDriver.ScrollToElement).ToList();

            var YearPublished = cells.FirstOrDefault().GetElementValueSafe();
            if (year == null)
                year = await _dbContext.Years.Include(x => x.Journal).FirstOrDefaultAsync(x => x.Journal.Title_Fa == journal.Title_Fa && x.YearPublished == YearPublished);

            if (YearPublished != null)
                if (year == null)
                {
                    year = new Year
                    {
                        YearPublished = YearPublished,
                        ImpactFactor = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[5]")).GetElementValueSafe(),
                        ImpactFactorWithoutSelfCitation = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[6]")).GetElementValueSafe(),
                        HIndex = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[7]")).GetElementValueSafe(),
                        ImmediateImpactFactor = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[8]")).GetElementValueSafe(),
                        CumulativeCitations = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[9]")).GetElementValueSafe(),
                        JournalStatus = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[10]/a"))?.GetAttribute("aria-label") ?? "",
                        JournalId = journal.Id
                    };

                    await _dbContext.Years.AddAsync(year);

                }
                else
                {
                    year.YearPublished = cells.FirstOrDefault().GetElementValueSafe();
                    year.ImpactFactor = cells[1].GetElementValueSafe();
                    year.SelfCitationFactor = cells[2].GetElementValueSafe();
                    year.ImpactFactorWithoutSelfCitation = cells[3].GetElementValueSafe();
                    year.JournalId = journal.Id;
                    _dbContext.Years.Update(year);
                }


            if (!await _dbContext.Years.AnyAsync(x => x.Id == year.Id))
                await _dbContext.SaveChangesAsync();

            var spans = cells.Where(x => x.FindElementsSafe(By.TagName("span")).Count > 0)
                .Select(x => x.FindElementSafe(By.TagName("span"))).ToArray();

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
            if (!await _dbContext.Qualities.AnyAsync(x => x.JournalCategoryId == newQuality.JournalCategoryId && x.YearId == newQuality.YearId))
                await _dbContext.AddAsync(newQuality);
            year = null;
        }

        await _dbContext.SaveChangesAsync();
        _webDriver.Close();
        _webDriver.SwitchTo().Window(_webDriver.WindowHandles[0]);
    }

    private async Task<Journal> ScrapInformation()
    {
        var informationButton = _webDriver.FindElementSafe(By.XPath("//*[@id=\"pills-biblio-tab\"]"));
        informationButton.Click();

        var journal = await _dbContext.Journals.FirstOrDefaultAsync(x => x.Title_Fa == _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdTitle\"]")).GetElementValueSafe());
        if (journal == null)
        {
            journal = new Journal
            {
                Title_Fa = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdTitle\"]")).GetElementValueSafe(),
                ISSN = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdISSN\"]")).GetElementValueSafe(),
                EISSN = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdEISSN\"]")).GetElementValueSafe(),
                Country = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdCountry\"]")).GetElementValueSafe(),
                Publisher = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdPublisher\"]")).GetElementValueSafe(),
                MicroLevelIssue = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdSubject1\"]")).GetElementValueSafe(),
                IntermediateLevelIssue = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdSubject2\"]")).GetElementValueSafe(),
                MacroLevelIssue = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdSubject3\"]")).GetElementValueSafe(),
                Address = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdPublisherAddress\"]")).GetElementValueSafe(),
                URL = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdWebsite\"]")).GetElementValueSafe(),
                Email = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdEmail\"]")).GetElementValueSafe(),
                Language = "فارسی"
            };
            await _dbContext.Journals.AddAsync(journal);
            await _dbContext.SaveChangesAsync();
        }

        return journal;
    }

   
   
}