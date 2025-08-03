using DataLayer;
using Entities.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Serilog;
using Log = Serilog.Log;

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
        options.AddArgument("--headless=new");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1920,1080");
        options.AddAdditionalOption("useAutomationExtension", false);
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");

        _webDriver = new ChromeDriver(options);
        //_webDriver.Manage().Window.Maximize();
        ((IJavaScriptExecutor)_webDriver).ExecuteScript("document.body.style.zoom='50%';");
    }

    public async Task Scrap()
    {
        try
        {
            _webDriver.NavigateWithScrollAndZoom("https://jcr.isc.ac");
            Thread.Sleep(3000);
            _webDriver.WaitUntilElementDisplayed(By.XPath("//tr[@role=\"row\"]/td[5]"));

            var yearDropdown = _webDriver.FindElementSafe(By.Id("dlyears1"));
            new SelectElement(yearDropdown).SelectByValue("1380");
            Thread.Sleep(1000);

            var radioIds = new[] { "rdlangFa", "rdlangEn", "rdlangAr" };
            var count = 1;

            foreach (var languageId in radioIds)
            {
                try
                {
                    var radioButton = _webDriver.FindElement(By.Id(languageId));
                    if (!radioButton.Selected) radioButton.Click();
                    Log.Information("زبان انتخاب شده: {LanguageId}", languageId);

                    Thread.Sleep(1000);
                    _webDriver.FindElementSafe(By.XPath("//button[contains(text(), 'جستجو')]"))?.Click();
                    ScrollToBottom();
                    Thread.Sleep(2000);

                    _webDriver.FindElementSafe(By.Id("thYear"))?.Click();
                    ScrollToBottom();

                    var lastPageText = _webDriver.FindElementSafe(By.XPath("//a[@data-dt-idx=\"6\"]"))?.Text;
                    if (!int.TryParse(lastPageText, out var pageCount))
                    {
                        Log.Warning("صفحه بندی یافت نشد برای زبان {LanguageId}", languageId);
                        continue;
                    }

                    for (var i = 0; i < pageCount; i++)
                    {
                        try
                        {
                            Thread.Sleep(3000);
                            _webDriver.WaitUntilTextDisplayed(By.ClassName("odd"));
                            var journals = _webDriver.FindElements(By.ClassName("odd"))
                                .Concat(_webDriver.FindElements(By.ClassName("even"))).ToList();

                            foreach (var journal in journals)
                            {
                                try
                                {
                                    Thread.Sleep(1000);
                                    var name = journal.FindElementSafe(By.XPath("./td[3]//a"));
                                    var nameText = name.GetElementValueSafe();
                                    var yearPublished = journal.FindElementSafe(By.XPath("./td[4]"))?.GetElementValueSafe();

                                    var journalItem = await _dbContext.Journals
                                       .FirstOrDefaultAsync(x => x.Title_Fa == nameText || x.Title_EN == nameText);

                                    if (journalItem == null ||
                                        (journalItem.ISSN.IsNullOrEmpty() && journalItem.EISSN.IsNullOrEmpty() &&
                                         journalItem.IsScopus) || !journalItem.IsIsc)
                                    {
                                        name.Click();
                                        var languageName = GetLanguageName(languageId);
                                        journalItem = await ScrapDetails(journal, languageName);
                                    }

                                    JournalDetailIsc? journalDetail = null;
                                    if (journalItem != null)
                                        journalDetail = await _dbContext.JournalDetailsIsc
                                            .Include(x => x.JournalYear.Journal)
                                            .FirstOrDefaultAsync(x =>
                                                (x.JournalYear.JournalId == journalItem.Id &&
                                                x.JournalYear.Year.ToString() == yearPublished));

                                    if (journalDetail == null)
                                    {
                                        var year = new JournalYear
                                        {
                                            Year = int.Parse(yearPublished),
                                            JournalId = journalItem.Id
                                        };

                                        journalDetail = new JournalDetailIsc
                                        {
                                            JournalYear = year,
                                            ImpactFactor = GetCellValue(5),
                                            ImpactFactorWithoutSelfCitation = GetCellValue(6),
                                            HIndex = GetCellValue(7),
                                            ImmediateImpactFactor = GetCellValue(8),
                                            CumulativeCitations = GetCellValue(9),
                                            JournalStatus = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[10]/a"))?.GetAttribute("aria-label") ?? ""
                                        };

                                        await _dbContext.JournalDetailsIsc.AddAsync(journalDetail);
                                    }
                                    else
                                    {
                                        journalDetail.ImpactFactor = GetCellValue(5);
                                        journalDetail.ImpactFactorWithoutSelfCitation = GetCellValue(6);
                                        journalDetail.HIndex = GetCellValue(7);
                                        journalDetail.ImmediateImpactFactor = GetCellValue(8);
                                        journalDetail.CumulativeCitations = GetCellValue(9);
                                        journalDetail.JournalStatus = _webDriver.FindElementSafe(By.XPath("//tr[@role=\"row\"]/td[10]/a"))?.GetAttribute("aria-label") ?? "";
                                        _dbContext.Update(journalDetail);
                                    }

                                    Log.Information("بررسی کامل ژورنال شماره {Count}: {Title}", count++, journalItem.Title_Fa);
                                }
                                catch (Exception e)
                                {
                                    CloseExtraTabs();
                                    Log.Error(e, $"خطا هنگام پردازش ژورنال Page :{i}, Journal:{journal.FindElementSafe(By.XPath("./td[3]//a"))}");
                                    Thread.Sleep(5000);
                                }
                            }
                            _webDriver.FindElementSafe(By.Id("grdJournals_next"))?.Click();
                        }
                        catch (Exception e)
                        {
                            Log.Error(e, "خطا در پردازش صفحه شماره {PageIndex}", i);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "خطا هنگام پردازش زبان {LanguageId}", languageId);
                    Thread.Sleep(5000);
                }
            }

            await _dbContext.SaveChangesAsync();
            Log.Information("ذخیره تغییرات در دیتابیس با موفقیت انجام شد.");
        }
        catch (Exception e)
        {
            Log.Fatal(e, "خطای بحرانی در فرآیند Scraping رخ داد");
        }
        finally
        {
            _webDriver.Quit();
            Log.Information("مرورگر بسته شد و فرآیند Scraping پایان یافت.");
        }
    }

    private string GetLanguageName(string languageId) => languageId switch
    {
        "rdlangFa" => "فارسی",
        "rdlangEn" => "انگلیسی",
        "rdlangAr" => "عربی",
        _ => string.Empty
    };

    private string GetCellValue(int index)
    {
        return _webDriver.FindElementSafe(By.XPath($"//tr[@role='row']/td[{index}]"))?.GetElementValueSafe();
    }

    private void ScrollToBottom()
    {
        ((IJavaScriptExecutor)_webDriver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
    }

    private void CloseExtraTabs()
    {
        while (_webDriver.WindowHandles.Count > 1 || _webDriver.CurrentWindowHandle != _webDriver.WindowHandles[0])
        {
            if (_webDriver.CurrentWindowHandle != _webDriver.WindowHandles[0])
            {
                var currentHandle = _webDriver.CurrentWindowHandle;
                _webDriver.Close();
                var remainingHandles = _webDriver.WindowHandles;
                if (remainingHandles.Count > 0)
                {
                    _webDriver.SwitchTo().Window(remainingHandles[0]);
                    Thread.Sleep(100);
                }
                else break;
            }
        }
        _webDriver.SwitchTo().Window(_webDriver.WindowHandles[0]);
    }

    // پیاده سازی تابع ScrapDetails هم می‌تواند مشابه لاگ‌گذاری شود
    private async Task<Journal> ScrapDetails(IWebElement journalElement, string language)
    {
        try
        {
            _webDriver.SwitchTo().Window(_webDriver.WindowHandles[1]);
            ((IJavaScriptExecutor)_webDriver).ExecuteScript("document.body.style.zoom='50%';");

            var journal = await ScrapInformation(language);

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

                if (cells.Count > 6)
                {
                    YearPublished = cells.FirstOrDefault().GetElementValueSafe();
                    journalyear = _dbContext.JournalYears.FirstOrDefault(x => x.Year.ToString() == YearPublished && x.JournalId == journal.Id);
                    JournalDetailIsc? newYear = null;
                    if (journalyear != null)
                        newYear = await _dbContext.JournalDetailsIsc.Include(x => x.JournalYear).FirstOrDefaultAsync(x => x.JournalYearId == (journalyear.Id));

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

                var subjectArea = _dbContext.JournalSubjectAreas.FirstOrDefault(x => x.Name == subjectName);
                if (subjectArea == null)
                {
                    subjectArea = new JournalSubjectArea { Name = subjectName };
                    _dbContext.JournalSubjectAreas.Add(subjectArea);
                    _dbContext.SaveChanges();
                }
                var category = _dbContext.JournalCategories.FirstOrDefault(x => x.Name == categoryName);
                if (category == null)
                {
                    category = new JournalCategory { Name = categoryName, SubjectAreaId = subjectArea.Id };
                    _dbContext.JournalCategories.Add(category);
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


            Log.Information("جزئیات سال ژورنال با موفقیت بررسی شد: {JournalTitle}", journal.Title_Fa ?? journal.Title_EN);
            return journal;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "خطا در استخراج جزئیات ژورنال");
            throw;
        }
        finally
        {
            _webDriver.Close();
            _webDriver.SwitchTo().Window(_webDriver.WindowHandles[0]);
        }
    }

    private async Task<Journal> ScrapInformation(string lang)
    {
        try
        {
            const string xpath = "//*[@id=\"pills-biblio-tab\"]";

            _webDriver.WaitUntilElementDisplayed(By.XPath(xpath));
            Thread.Sleep(1000);

            var informationButton = _webDriver.FindElementSafe(By.XPath(xpath));

            if (informationButton == null)
            {
                Log.Warning("دکمه اطلاعات یافت نشد. رفرش صفحه و تلاش مجدد.");
                _webDriver.Navigate().Refresh();
                Thread.Sleep(3000);
                _webDriver.WaitUntilElementDisplayed(By.XPath(xpath));
                informationButton = _webDriver.FindElementSafe(By.XPath(xpath));
            }

            if (informationButton == null)
            {
                Log.Error("پس از رفرش هم دکمه اطلاعات یافت نشد.");
                throw new Exception("دکمه اطلاعات یافت نشد.");
            }

            informationButton.Click();

            _webDriver.WaitUntilTextDisplayed(By.XPath("//*[@id=\"tdTitle\"]"));
            Thread.Sleep(1000);
            var journalTitle = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdTitle\"]")).GetElementValueSafe();

            // خواندن سایر فیلدها
            var title = journalTitle;
            var issn = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdISSN\"]"))?.GetElementValueSafe().Replace("-", "");
            var eissn = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdEISSN\"]"))?.GetElementValueSafe().Replace("-", "");
            var country = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdCountry\"]"))?.GetElementValueSafe();
            var publisher = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdPublisher\"]"))?.GetElementValueSafe();
            var subject1 = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdSubject1\"]"))?.GetElementValueSafe();
            var subject2 = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdSubject2\"]"))?.GetElementValueSafe();
            var subject3 = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdSubject3\"]"))?.GetElementValueSafe();
            var address = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdPublisherAddress\"]"))?.GetElementValueSafe();
            var url = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdWebsite\"]"))?.GetElementValueSafe();
            var email = _webDriver.FindElementSafe(By.XPath("//*[@id=\"tdEmail\"]"))?.GetElementValueSafe();
            var lastUpdate = DateTime.Now;

            var journal = await _dbContext.Journals.FirstOrDefaultAsync(x => (x.Title_Fa == journalTitle || x.Title_EN == journalTitle) && x.IsIsc);

            string normalizedIssn = string.IsNullOrWhiteSpace(issn) || issn == "-" ? null : issn.Trim();
            string normalizedEissn = string.IsNullOrWhiteSpace(eissn) || eissn == "-" ? null : eissn.Trim();

            // اگر هر دو خالی یا برابر هستند، تطابقی انجام نشود
            Journal? journalScopus = null;
            if (!string.IsNullOrEmpty(normalizedIssn))
            {
                journalScopus = await _dbContext.Journals
                    .FirstOrDefaultAsync(x => (x.ISSN == normalizedIssn || x.ISSN == normalizedIssn) && !x.IsIsc);
            }
            if (journalScopus == null && !string.IsNullOrEmpty(normalizedEissn))
            {
                journalScopus = await _dbContext.Journals.FirstOrDefaultAsync(x =>
                   !x.IsIsc &&
                   (
                       x.ISSN == normalizedEissn || x.EISSN == normalizedEissn
                   ));
            }
            if (journalScopus != null)
            {
                journalScopus.Country = country;
                journalScopus.Publisher = publisher;
                journalScopus.MicroLevelIssue = subject1;
                journalScopus.MidLevelIssue = subject2;
                journalScopus.MacroLevelIssue = subject3;
                journalScopus.Address = address;
                journalScopus.URL = url;
                journalScopus.Email = email;
                journalScopus.IsIsc = true;
                journalScopus.LastUpdate = DateTime.Now;
                journalScopus.Language = journalScopus.Language == null ? lang:journalScopus.Language?.Contains(lang) ?? true ? journalScopus.Language : journalScopus.Language + "," + lang;
                if (title.ContainsPersianCharacters() ?? true)
                    journalScopus.Title_Fa = title;
                else
                    journalScopus.Title_EN = title;

                await _dbContext.SaveChangesAsync();
                Log.Information("بروزرسانی اطلاعات ژورنال Scopus موفق: {Title}", journalScopus.Title_Fa ?? journalScopus.Title_EN);
                return journalScopus;
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
                    IsIsc = true,
                    URL = url,
                    Email = email,
                    LastUpdate = lastUpdate,
                    Language = lang
                };

                if (title.ContainsPersianCharacters() ?? true)
                    journal.Title_Fa = title;
                else
                    journal.Title_EN = title;

                await _dbContext.Journals.AddAsync(journal);
                await _dbContext.SaveChangesAsync();
                Log.Information("ایجاد ژورنال جدید با عنوان: {Title}", title);
            }
            else
            {
                if (journal.ISSN.IsNullOrEmpty() && journal.EISSN.IsNullOrEmpty())
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
                journal.IsIsc = true;
                journal.Language = journal.Language.Contains(lang) ? journal.Language : journal.Language + "," + lang;

                await _dbContext.SaveChangesAsync();
                Log.Information("بروزرسانی ژورنال موجود با عنوان: {Title}", title);
            }

            return journal;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "خطا در استخراج اطلاعات ژورنال");
            throw;
        }
    }
}
