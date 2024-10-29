using System.Threading.Channels;
using CSV2Sql.Models;
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
        var lastPageNumber = _webDriver.FindElement(By.XPath("//*[@id=\"grdJournals_paginate\"]/span/a[6]")).Text;
        var pageCount = Convert.ToInt32(lastPageNumber);
        var count = 1;

        for (var i =0; i < pageCount; i++)
        {
            var table = _webDriver.FindElement(By.TagName("tbody"));
            var journals = table.FindElements(By.TagName("tr"));
            foreach (var journal in journals)
            {
                var name = journal.FindElement(By.CssSelector(" td:nth-child(3) > a"));
                await ScrapDetails(journal);

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

    private async Task ScrapDetails(IWebElement journalElement)
    {
        var detailButton =
            journalElement.FindElement(By.CssSelector("td:last-child > a"));
        detailButton.Click();
        _webDriver.SwitchTo().Window(_webDriver.WindowHandles[1]);

        var journal = await ScrapInformation();

        var statusButton = _webDriver.FindElement(By.XPath("//*[@id=\"aStatus\"]"));
        statusButton.Click();

        Year year = null;
        foreach (var tr in _webDriver.FindElements(By.XPath("//*[@id=\"ContentPlaceHolder1_grdStatus\"]/tbody/tr")))
        {
            var cells = tr.FindElements(By.TagName("td"));
            if (cells.First().GetAttribute("class").Split(" ").Contains("biggerFont"))
            {
                year = new Year
                {
                    YearPublished = cells.First().Text,
                    ImpactFactor = cells[1].Text,
                    Journal = journal
                };
                await _dbContext.AddAsync(year);
            }

            var spans = cells.Where(x => x.FindElements(By.TagName("span")).Count > 0)
                .Select(x => x.FindElement(By.TagName("span"))).ToArray();
            var newQuality = new Quality
            {
                Year = year,
                Q = spans[0].Text,
                Name = spans[1].Text
            };
            await _dbContext.AddAsync(newQuality);
        }

        await _dbContext.SaveChangesAsync();
        _webDriver.Close();
        _webDriver.SwitchTo().Window(_webDriver.WindowHandles[0]);
    }

    private async Task<ISCJournal> ScrapInformation()
    {
        var informationButton = _webDriver.FindElement(By.XPath("//*[@id=\"aBiblio\"]"));

        informationButton.Click();
        var journal = new ISCJournal
        {
            Title = _webDriver.FindElement(By.XPath("//*[@id=\"tdTitle\"]")).Text,
            ISSN = _webDriver.FindElement(By.XPath("//*[@id=\"tdISSN\"]")).Text,
            EISSN = _webDriver.FindElement(By.XPath("//*[@id=\"tdEISSN\"]")).Text,
            Country = _webDriver.FindElement(By.XPath("//*[@id=\"tdCountry\"]")).Text,
            Publisher = _webDriver.FindElement(By.XPath("//*[@id=\"tdPublisher\"]")).Text,
        };

        await _dbContext.ISCJournals.AddAsync(journal);
        await _dbContext.SaveChangesAsync();
        return journal;
    }
}