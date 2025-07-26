using DataLayer;
using Entities.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;


namespace JournalScrappers.Scrap.ISC.Journals;
public class JournalScrapper
{
    private readonly IConfiguration _configuration;
    private readonly DynamicDbContext _dbContext;
    private readonly WebDriver _webDriver;

    public JournalScrapper(IConfiguration configuration, DynamicDbContext dbContext)
    {
        _configuration = configuration;
        _dbContext = dbContext;

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
            try
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
                    Thread.Sleep(3000);
                    _webDriver.WaitUntilTextDisplayed(By.ClassName("odd"));
                    //var journals = table.FindElementsSafe(By.TagName("tr"));
                    var journals = _webDriver.FindElements(By.ClassName("odd")).ToList();

                    journals.AddRange(_webDriver.FindElements(By.ClassName("even")));
                    foreach (var journal in journals)
                    {

                        var name = journal.FindElementSafe(By.XPath("./td[3]//a"));
                        var nameText = name.GetElementValueSafe();
                        var yearPublished = journal.FindElementSafe(By.XPath("./td[4]")).GetElementValueSafe();
                        var journalDetail = await _dbContext.JournalDetailsIsc.Include(x => x.JournalYear.Journal).FirstOrDefaultAsync(x => (x.JournalYear.Journal.Title_Fa == nameText || x.JournalYear.Journal.Title_EN == nameText) && x.JournalYear.Year.ToString() == yearPublished);
                        Journal? journalItem = await _dbContext.Journals.FirstOrDefaultAsync(x => x.Title_Fa == nameText || x.Title_EN == nameText);

                        if (journalItem == null || (journalItem.ISSN.IsNullOrEmpty() && journalItem.EISSN.IsNullOrEmpty() && journalItem.IsIsi) || !journalItem.IsIsi)
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
                            name = journal.FindElementSafe(By.XPath("./td[3]//a"));
                            name.Click();
                            await ScrapDetails(journal, languageName);
                        }

                        if (journalDetail == null)
                        {
                            var year = new JournalYear
                            {
                                Year = int.Parse(yearPublished),
                                JournalId = journalItem?.Id ?? 0
                            };
                            journalDetail = new JournalDetailIsc
                            {
                                JournalYear = year,
                                ImpactFactor = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[5]")).GetElementValueSafe(),
                                ImpactFactorWithoutSelfCitation = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[6]")).GetElementValueSafe(),
                                HIndex = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[7]")).GetElementValueSafe(),
                                ImmediateImpactFactor = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[8]")).GetElementValueSafe(),
                                CumulativeCitations = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[9]")).GetElementValueSafe(),
                                JournalStatus = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[10]/a"))?.GetAttribute("aria-label") ?? "",
                            };

                            await _dbContext.JournalDetailsIsc.AddAsync(journalDetail);
                            await _dbContext.SaveChangesAsync();

                        }
                        else
                        {
                            journalDetail.ImpactFactor = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[5]")).GetElementValueSafe();
                            journalDetail.ImpactFactorWithoutSelfCitation = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[6]")).GetElementValueSafe();
                            journalDetail.HIndex = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[7]")).GetElementValueSafe();
                            journalDetail.ImmediateImpactFactor = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[8]")).GetElementValueSafe();
                            journalDetail.CumulativeCitations = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[9]")).GetElementValueSafe();
                            journalDetail.JournalStatus = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[10]/a"))?.GetAttribute("aria-label") ?? "";
                            _dbContext.Update(journalDetail);
                        }

                        Console.WriteLine($"** number: {count++} {journalItem.Title_Fa}");
                    }
                    //Thread.Sleep(1000);
                    var nextPageButton = _webDriver.FindElementSafe(By.Id("grdJournals_next"));
                    nextPageButton.Click();
                }
            }
            catch (Exception)
            {

                Thread.Sleep(5000);
            }
        }
        await _dbContext.SaveChangesAsync();
        _webDriver.Quit();
    }

    private async Task ScrapDetails(IWebElement journalElement,string languageName)
    {
        _webDriver.SwitchTo().Window(_webDriver.WindowHandles[1]);
        ((IJavaScriptExecutor)_webDriver).ExecuteScript("document.body.style.zoom='50%';");

        var journal = await ScrapInformation();

        var statusButton = _webDriver.FindElementSafe(By.XPath("//*[@id=\"pills-status-tab\"]"));
        statusButton.Click();
        JournalYear? journalyear = null;
        JournalDetailIsc? year = null;
        foreach (var tr in _webDriver.FindElementsSafe(By.XPath("//*[@id=\"ContentPlaceHolder1_grdStatus\"]/tbody/tr")))
        {
            var cells = tr.FindElementsSafe(By.TagName("td")).Select(_webDriver.ScrollToElement).ToList();
            string subjectName = "", categoryName = "", QLevel = "", averageImpactFactorMacroLevelTopic = "", averageImpactFactorMidLevelTopic = "", YearPublished = "";
            //var spans = cells.Where(x => x.FindElementsSafe(By.TagName("span")).Count > 0)
            //    .Select(x => x.FindElementSafe(By.TagName("span"))).ToArray();

            var newYear = await _dbContext.JournalDetailsIsc.Include(x => x.JournalYear.Journal).FirstOrDefaultAsync(x => (x.JournalYear.Journal.Title_Fa == journal.Title_Fa || x.JournalYear.Journal.Title_EN == journal.Title_EN) && x.JournalYear.Year.ToString() == YearPublished);
            if (cells.Count > 6)
            {
                YearPublished = cells.FirstOrDefault().GetElementValueSafe();
                journalyear = _dbContext.JournalYears.FirstOrDefault(x => x.Year.ToString() == YearPublished && x.JournalId == journal.Id);
                if (journalyear == null)
                {
                    journalyear = new JournalYear { Year = int.Parse(YearPublished), JournalId = journal.Id, };
                    _dbContext.JournalYears.Add(journalyear);
                    _dbContext.SaveChanges();
                }
                if (newYear == null)
                {
                    year = new JournalDetailIsc
                    {
                        ImpactFactor = cells[1].GetElementValueSafe(),
                        SelfCitationFactor = cells[2].GetElementValueSafe(),
                        ImpactFactorWithoutSelfCitation = cells[3].GetElementValueSafe(),
                        JournalYearId = journalyear.Id,
                    };

                    await _dbContext.JournalDetailsIsc.AddAsync(year);
                }
                else
                {
                    year = newYear;
                    if (year.SelfCitationFactor.IsNullOrEmpty())
                    {
                        year.SelfCitationFactor = cells[2].GetElementValueSafe();
                        _dbContext.JournalDetailsIsc.Update(year);
                    }
                }

                await _dbContext.SaveChangesAsync();
                QLevel = cells[4].GetElementValueSafe();
                subjectName = cells[5].GetElementValueSafe();
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
                subjectArea = new JournalSubjectArea { Name = subjectName };
                _dbContext.ScopusSubjectAreas.Add(subjectArea);
                _dbContext.SaveChanges();
            }
            var category = _dbContext.ScopusJournalCategories.FirstOrDefault(x => x.Name == categoryName);
            if (category == null)
            {
                category = new JournalCategory { Name = categoryName, SubjectAreaId = subjectArea.Id };
                _dbContext.ScopusJournalCategories.Add(category);
                _dbContext.SaveChanges();
            }

            var newQuality = new JournalQurtile
            {
                JournalCategoryId = category.Id,
                QLevel = QLevel.ToInt() ?? 0,
                JournalYearId = journalyear.Id,
                AverageImpactFactorMacroLevelTopic = averageImpactFactorMacroLevelTopic,
                AverageImpactFactorMidLevelTopic = averageImpactFactorMidLevelTopic
            };
            if (!await _dbContext.JournalQurtiles.AnyAsync(x => x.JournalCategoryId == newQuality.JournalCategoryId && x.JournalYearId == newQuality.JournalYearId))
                await _dbContext.AddAsync(newQuality);

            await _dbContext.SaveChangesAsync();
        }
        await _dbContext.SaveChangesAsync();


        _webDriver.Close();
        _webDriver.SwitchTo().Window(_webDriver.WindowHandles[0]);
    }

    private async Task<Journal> ScrapInformation()
    {
        _webDriver.WaitUntilElementDisplayed(By.XPath("//*[@id=\"pills-biblio-tab\"]"));
        var informationButton = _webDriver.FindElementSafe(By.XPath("//*[@id=\"pills-biblio-tab\"]"));
        informationButton.Click();

        _webDriver.WaitUntilTextDisplayed(By.XPath("//*[@id=\"tdTitle\"]"));
        Thread.Sleep(1000);
        var journalTitle = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdTitle\"]")).GetElementValueSafe();

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

        var journal = await _dbContext.Journals.FirstOrDefaultAsync(x => (x.Title_Fa == journalTitle || x.Title_EN == journalTitle) && x.IsIsc);
        var journalScopus = await _dbContext.Journals.FirstOrDefaultAsync(x => (x.ISSN == issn || x.EISSN == eissn) && !x.IsIsc);

        if(journalScopus != null)
        {
            journal.Country = country;
            journal.Publisher = publisher;
            journal.MicroLevelIssue = subject1;
            journal.MidLevelIssue = subject2;
            journal.MacroLevelIssue = subject3;
            journal.Address = address;
            journal.URL = url;
            journal.Email = email;
            journal.LastUpdate = lastUpdate;

            await _dbContext.SaveChangesAsync();
        }

        if (journal == null)
        {
            journal = new Journal
            {
                ISSN = issn,
                EISSN = eissn,
                Country = country,
                Publisher = publisher,
                MicroLevelIssue = subject1,
                MidLevelIssue = subject2,
                MacroLevelIssue = subject3,
                Address = address,
                URL = url,
                Email = email,
                //Language = "فارسی",
                LastUpdate = lastUpdate
            };

            if (title.ContainsPersianCharacters() ?? true)
                journal.Title_Fa = title;
            else
                journal.Title_EN = title;

            await _dbContext.Journals.AddAsync(journal);
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            if(journal.ISSN.IsNullOrEmpty() &&
            journal.EISSN.IsNullOrEmpty()
            )
            journal.ISSN = issn;
            journal.EISSN = eissn;
            journal.Country = country;
            journal.Publisher = publisher;
            journal.MicroLevelIssue = subject1;
            journal.MidLevelIssue = subject2;
            journal.MacroLevelIssue = subject3;
            journal.Address = address;
            journal.URL = url;
            journal.Email = email;
            journal.LastUpdate = lastUpdate;
        }

        await _dbContext.SaveChangesAsync();

        return journal;
    }



}