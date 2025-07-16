using System.Threading.Channels;
using System.Xml.Linq;
using CSV2Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OpenQA.Selenium;
using OpenQA.Selenium.BiDi.Log;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using static Azure.Core.HttpHeader;
using static JournalScrapper.Entity.ISCMySql;

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

        _webDriver.WaitUntilElementDisplayed(By.XPath("//tr[@role=\"row\"]/td[5]"));
        var yearDropdown = _webDriver.FindElementSafe(By.Id("dlyears1"));
        var selectElement = new SelectElement(yearDropdown);
        Thread.Sleep(1000);
        selectElement.SelectByValue("1380");

        var radioIds = new List<string> { "rdlangFa", "rdlangEn", "rdlangAr" };

        foreach (var languageId in radioIds)
        {
            var radioButton = _webDriver.FindElement(By.Id(languageId));

            if (!radioButton.Selected)
                radioButton.Click();

            Console.WriteLine($"انتخاب شد: {languageId}");

            Thread.Sleep(1000);

            var searchButton = _webDriver.FindElementSafe(By.XPath("//button[contains(text(), 'جستجو')]"));
            searchButton.Click();
            ((IJavaScriptExecutor)_webDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
            Thread.Sleep(2000);
            var yearsortButton = _webDriver.FindElementSafe(By.Id("thYear"));
            yearsortButton.Click();
            ((IJavaScriptExecutor)_webDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");

            var lastPageNumber = _webDriver.FindElementSafe(By.XPath("//a[@data-dt-idx=\"6\"]")).Text;
            var pageCount = Convert.ToInt32(lastPageNumber);
            var count = 1;
            for (var i = 0; i < pageCount; i++)
            {
                _webDriver.WaitUntilTextDisplayed(By.XPath("//tr[@role=\"row\"]/td[5]"));
                Thread.Sleep(2000);
                //var journals = table.FindElementsSafe(By.TagName("tr"));
                var journals = _webDriver.FindElements(By.ClassName("odd")).ToList();
                //Thread.Sleep(500);
                journals.AddRange(_webDriver.FindElements(By.ClassName("even")));
                foreach (var journal in journals)
                {
                    var name = journal.FindElementSafe(By.XPath("./td[3]//a"));
                    var nameText = name.GetElementValueSafe();
                    var yearPublished = journal.FindElementSafe(By.XPath("./td[4]")).GetElementValueSafe();
                    var year = await _dbContext.Years.Include(x => x.Journal).FirstOrDefaultAsync(x => x.Journal.Title_Fa == name.GetElementValueSafe() && x.YearPublished == yearPublished);
                    CSV2Sql.Models.Journal? journalItem = await _dbContext.Journals.FirstOrDefaultAsync(x => x.Title_Fa == nameText);

                    if (journalItem == null)
                    {
                        string languageName = "";
                        switch (languageId)
                        {
                            case "rdlangFa":
                                languageName = "فارسی";
                                break;

                            case "rdlangEn":

                                languageName = "انگلیسی";
                                break;

                            case "rdlangAr":

                                languageName = "عربی";
                                break;

                            default:
                                break;
                        }
                        journalItem = new CSV2Sql.Models.Journal
                        {
                            Title_Fa = nameText,
                            Language = languageName
                        };
                        await _dbContext.Journals.AddAsync(journalItem);
                        await _dbContext.SaveChangesAsync();

                        name = journal.FindElementSafe(By.XPath("./td[3]//a"));
                        name.Click();
                        await ScrapDetails(journal);
                    }

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
                            JournalId = journalItem?.Id ?? 0
                        };
                        year.JournalId = journalItem!.Id;
                        await _dbContext.Years.AddAsync(year);
                        await _dbContext.SaveChangesAsync();

                    }
                    else
                    {
                        year.ImpactFactor = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[5]")).GetElementValueSafe();
                        year.ImpactFactorWithoutSelfCitation = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[6]")).GetElementValueSafe();
                        year.HIndex = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[7]")).GetElementValueSafe();
                        year.ImmediateImpactFactor = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[8]")).GetElementValueSafe();
                        year.CumulativeCitations = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[9]")).GetElementValueSafe();
                        year.JournalStatus = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[10]/a"))?.GetAttribute("aria-label") ?? "";
                        year.JournalId = journalItem.Id;
                        _dbContext.Update(year);
                    }
                    if (journalItem.ISSN.IsNullOrEmpty())
                    {
                        name = journal.FindElementSafe(By.XPath("./td[3]//a"));
                        name.Click();
                        await ScrapDetails(journal);
                    }
                    Console.WriteLine($"** number: {count++} {journalItem.Title_Fa}");
                }
                //Thread.Sleep(1000);
                var nextPageButton = _webDriver.FindElementSafe(By.Id("grdJournals_next"));
                nextPageButton.Click();
            }
        }
        await _dbContext.SaveChangesAsync();
        _webDriver.Quit();
    }

    private async Task ScrapDetails(IWebElement journalElement)
    {
        _webDriver.SwitchTo().Window(_webDriver.WindowHandles[1]);

        var journal = await ScrapInformation();

        var statusButton = _webDriver.FindElementSafe(By.XPath("//*[@id=\"pills-status-tab\"]"));
        statusButton.Click();
        Year? year = null;
        foreach (var tr in _webDriver.FindElementsSafe(By.XPath("//*[@id=\"ContentPlaceHolder1_grdStatus\"]/tbody/tr")))
        {
            var cells = tr.FindElementsSafe(By.TagName("td")).Select(_webDriver.ScrollToElement).ToList();
            var YearPublished = cells.FirstOrDefault().GetElementValueSafe();

            string subjectName = "", categoryName = "", QLevel = "", averageImpactFactorMacroLevelTopic = "", averageImpactFactorMidLevelTopic = "";
            //var spans = cells.Where(x => x.FindElementsSafe(By.TagName("span")).Count > 0)
            //    .Select(x => x.FindElementSafe(By.TagName("span"))).ToArray();

            year = await _dbContext.Years.Include(x => x.Journal).FirstOrDefaultAsync(x => x.Journal.Title_Fa == journal.Title_Fa && x.YearPublished == YearPublished) ?? year;
            if (cells.Count > 6)
            {
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
                    year.SelfCitationFactor = cells[2].GetElementValueSafe();
                    _dbContext.Years.Update(year);
                }

                await _dbContext.SaveChangesAsync();
                QLevel = cells[4].GetElementValueSafe();
                categoryName = cells[5].GetElementValueSafe();
                averageImpactFactorMacroLevelTopic = cells[6].GetElementValueSafe();
                categoryName = cells[7].GetElementValueSafe();
                averageImpactFactorMidLevelTopic = cells[8].GetElementValueSafe();
            }
            else
            {
                QLevel = cells[0].GetElementValueSafe();
                subjectName = cells[1].GetElementValueSafe();
                averageImpactFactorMacroLevelTopic = cells[2].GetElementValueSafe();
                categoryName = cells[3].GetElementValueSafe();
                averageImpactFactorMidLevelTopic = cells[4].GetElementValueSafe();
            }

            var subjectArea = _dbContext.ScopusSubjectAreas.FirstOrDefault(x => x.Name == subjectName);
            if (subjectArea == null)
            {
                subjectArea = new ScopusSubjectArea { Name = subjectName };
                _dbContext.ScopusSubjectAreas.Add(subjectArea);
                _dbContext.SaveChanges();
            }
            var category = _dbContext.ScopusJournalCategories.FirstOrDefault(x => x.Name == categoryName);
            if (category == null)
            {
                category = new ScopusJournalCategory { Name = categoryName, SubjectAreaId = subjectArea.Id };
                _dbContext.ScopusJournalCategories.Add(category);
                _dbContext.SaveChanges();
            }
            var newQuality = new Qurtile
            {
                JournalCategoryId = category.Id,
                QLevel = QLevel.ToInt() ?? 0,
                YearId = year.Id
            };
            if (!await _dbContext.Qualities.AnyAsync(x => x.JournalCategoryId == newQuality.JournalCategoryId && x.YearId == newQuality.YearId))
                await _dbContext.AddAsync(newQuality);
        }
        await _dbContext.SaveChangesAsync();


        _webDriver.Close();
        _webDriver.SwitchTo().Window(_webDriver.WindowHandles[0]);
    }

    private async Task<CSV2Sql.Models.Journal> ScrapInformation()
    {
        _webDriver.WaitUntilElementDisplayed(By.XPath("//*[@id=\"pills-biblio-tab\"]"));
        var informationButton = _webDriver.FindElementSafe(By.XPath("//*[@id=\"pills-biblio-tab\"]"));
        informationButton.Click();

        _webDriver.WaitUntilTextDisplayed(By.XPath("//*[@id=\"tdTitle\"]"));
        Thread.Sleep(1000);
        var journal = await _dbContext.Journals.FirstOrDefaultAsync(x => x.Title_Fa == _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdTitle\"]")).GetElementValueSafe());
        var title = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdTitle\"]")).GetElementValueSafe();
        var issn = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdISSN\"]")).GetElementValueSafe();
        var eissn = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdEISSN\"]")).GetElementValueSafe();
        var country = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdCountry\"]")).GetElementValueSafe();
        var publisher = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdPublisher\"]")).GetElementValueSafe();
        var subject1 = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdSubject1\"]")).GetElementValueSafe();
        var subject2 = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdSubject2\"]")).GetElementValueSafe();
        var subject3 = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdSubject3\"]")).GetElementValueSafe();
        var address = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdPublisherAddress\"]")).GetElementValueSafe();
        var url = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdWebsite\"]")).GetElementValueSafe();
        var email = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdEmail\"]")).GetElementValueSafe();
        var lastUpdate = DateTime.Now;
        if (journal == null)
        {
            journal = new CSV2Sql.Models.Journal
            {
                Title_Fa = title,
                ISSN = issn,
                EISSN = eissn,
                Country = country,
                Publisher = publisher,
                MicroLevelIssue = subject1,
                IntermediateLevelIssue = subject2,
                MacroLevelIssue = subject3,
                Address = address,
                URL = url,
                Email = email,
                Language = "فارسی",
                LastUpdate = lastUpdate
            };
            await _dbContext.Journals.AddAsync(journal);
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            journal.ISSN = issn;
            journal.EISSN = eissn;
            journal.Country = country;
            journal.Publisher = publisher;
            journal.MicroLevelIssue = subject1;
            journal.IntermediateLevelIssue = subject2;
            journal.MacroLevelIssue = subject3;
            journal.Address = address;
            journal.URL = url;
            journal.Email = email;
            journal.Language = "فارسی";
            journal.LastUpdate = lastUpdate;
        }

        await _dbContext.SaveChangesAsync();

        return journal;
    }



}