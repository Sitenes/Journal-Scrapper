using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;
using DataLayer;
using Entities.Models.Entities;
using JournalScrappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;


namespace ResearchScraper
{


    #region Models

    public class CSVModel
    {
        public class ScimagojrCategory
        {
            public string Code { get; set; }
            public string Description { get; set; }
        }
    }
    #endregion


    #region Google Scholar Scraper
    public class GoogleScholarScraper
    {
        private readonly WebScraper _scraper;
        private readonly DynamicDbContext _dbContext;

        public GoogleScholarScraper(DynamicDbContext dbContext, WebScraper webScraper)
        {
            _scraper = webScraper;
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task ExecuteAsync(Professor professor = default)
        {
            try
            {
                await _scraper.OpenUrlAsync($"https://scholar.google.com/citations?hl=en&user={professor.GoogleScholarID}", ".gsc_a_at");
                await Task.Delay(1000);
                await ScrapeProfileAsync(professor);
                var articleUrls = await ScrapeArticleUrlsAsync();
                await UpdateExistingArticlesAsync(professor, articleUrls);
                await ScrapeArticlesAsync(professor, articleUrls);

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _scraper.Log($"Error in ExecuteAsync: {ex.Message}", LogLevel.Error, "GoogleScholarScraper");
            }
            finally
            {
                _scraper.CloseBrowser();
            }
        }

        private async Task ScrapeProfileAsync(Professor professor)
        {
            professor.ScholarProfiles ??= new List<ScholarProfile>();
            var citation = new ScholarProfile();

            if ((_scraper.GetElementText("#gsc_rsb_st > tbody > tr:nth-child(1) > td.gsc_rsb_sc1 > a")).Contains("Citations", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse(_scraper.GetElementText("#gsc_rsb_st > tbody > tr:nth-child(1) > td:nth-child(2)"), out int citationAll);
                int.TryParse(_scraper.GetElementText("#gsc_rsb_st > tbody > tr:nth-child(1) > td:nth-child(3)"), out int citation2020);
                citation.CitationSince2020 = citation2020;
                citation.CitationAll = citationAll;
            }

            if ((_scraper.GetElementText("#gsc_rsb_st > tbody > tr:nth-child(2) > td.gsc_rsb_sc1 > a")).Contains("h-index", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse(_scraper.GetElementText("#gsc_rsb_st > tbody > tr:nth-child(2) > td:nth-child(2)"), out int hIndexAll);
                int.TryParse(_scraper.GetElementText("#gsc_rsb_st > tbody > tr:nth-child(2) > td:nth-child(3)"), out int hIndex2020);
                citation.HIndexAll = hIndexAll;
                citation.HIndexSince2020 = hIndex2020;
            }

            if ((_scraper.GetElementText("#gsc_rsb_st > tbody > tr:nth-child(3) > td.gsc_rsb_sc1 > a")).Contains("i10-index", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse(_scraper.GetElementText("#gsc_rsb_st > tbody > tr:nth-child(3) > td:nth-child(2)"), out int i10IndexAll);
                int.TryParse(_scraper.GetElementText("#gsc_rsb_st > tbody > tr:nth-child(3) > td:nth-child(3)"), out int i10Index2020);
                citation.I10IndexAll = i10IndexAll;
                citation.I10IndexSince2020 = i10Index2020;
            }

            await _scraper.ClickElementAsync("#gsc_prf_ion_btn");
            var otherName = _scraper.GetElementText("#gs_prf_ion_txt");
            if (!string.IsNullOrEmpty(otherName))
                citation.OtherName = otherName;

            citation.LastUpdate = DateTime.Now;
            professor.ScholarProfiles.Add(citation);

            await _dbContext.SaveChangesAsync();
        }

        private async Task<Dictionary<string, (int Citations, string Title, string Journal, string Year)>> ScrapeArticleUrlsAsync()
        {
            var articleUrls = new Dictionary<string, (int, string, string, string)>();
            var maxRetries = 3;
            var retryDelay = TimeSpan.FromSeconds(2);
            var processedArticles = new HashSet<string>();

            while (true)
            {
                try
                {
                    var currentPageArticles = await ProcessCurrentPageArticlesAsync();
                    foreach (var article in currentPageArticles)
                    {
                        if (!processedArticles.Contains(article.Key))
                        {
                            articleUrls.Add(article.Key, article.Value);
                            processedArticles.Add(article.Key);
                        }
                    }

                    var moreButton = _scraper.FindOne("#gsc_bpf_more");
                    if (moreButton == null || moreButton.GetAttribute("disabled") == "true")
                        break;

                    await _scraper.ClickElementAsync("#gsc_bpf_more");
                    await Task.Delay(1500);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (maxRetries-- <= 0) throw;
                    await Task.Delay(retryDelay);
                    retryDelay *= 2;
                }
            }

            return articleUrls;
        }

        private async Task<Dictionary<string, (int Citations, string Title, string Journal, string Year)>> ProcessCurrentPageArticlesAsync()
        {
            var pageArticles = new Dictionary<string, (int, string, string, string)>();
            var hrefs = _scraper.GetElementsText(".gsc_a_at", "href");
            var titles = _scraper.GetElementsText(".gsc_a_at");
            var citations = _scraper.GetElementsText(".gsc_a_ac.gs_ibl");
            var count = titles.Count;

            var journalTasks = new string[count];
            var yearTasks = new string[count];

            for (int i = 0; i < count; i++)
            {
                int currentIndex = i;
                journalTasks[i] = Regex.Split(_scraper.GetElementTextByXPath($"//*[@id=\"gsc_a_b\"]/tr[{currentIndex + 1}]/td[1]/div[2]"), @"\)|\(|\d+").FirstOrDefault() ?? "";


                yearTasks[i] = _scraper.GetElementText($"#gsc_a_b > tr:nth-child({currentIndex + 1}) > td.gsc_a_y > span");
            }


            for (int i = 0; i < count; i++)
            {
                if (i >= hrefs.Count) continue;
                var citationValue = i < citations.Count && int.TryParse(citations[i], out var cit) ? cit : 0;
                pageArticles.TryAdd(hrefs[i], (citationValue, titles[i], journalTasks[i], yearTasks[i]));
            }

            return pageArticles;
        }

        private async Task ScrapeArticlesAsync(Professor professor, Dictionary<string, (int Citations, string Title, string Journal, string Year)> articleUrls)
        {
            foreach (var url in articleUrls)
            {

                try
                {
                    await _scraper.OpenUrlAsync(url.Key, ".gsc_a_at");
                    var article = new Article
                    {
                        TitleEn = _scraper.GetElementText("#gsc_oci_title") ?? url.Value.Title,
                        ScholarCitations = new List<ScholarArticleCitation> { new ScholarArticleCitation { ScholarCitation = url.Value.Citations, LastUpdate = DateTime.Now } }
                    };

                    var tableRows = _scraper.FindMany("#gsc_oci_table > div");
                    var fieldValues = new Dictionary<string, string>();

                    foreach (var row in tableRows)
                    {
                        try
                        {
                            var label = (_scraper.GetElementText(row, "div.gsc_oci_field")).Trim().ToLower();
                            var value = (_scraper.GetElementText(row, "div.gsc_oci_value")).Trim();
                            if (!string.IsNullOrEmpty(label) && !string.IsNullOrEmpty(value))
                                fieldValues[label] = value;
                        }
                        catch (Exception ex)
                        {
                            _scraper.Log($"Error reading table row: {ex.Message}", LogLevel.Warning, "ScrapeArticles");
                        }
                    }

                    foreach (var kvp in fieldValues)
                    {
                        switch (kvp.Key)
                        {
                            case "publication date":
                                article.Publication = kvp.Value;
                                article.PublicationYear = string.IsNullOrEmpty(article.Publication) ? 0 : ExtractYear(article.Publication);
                                break;
                            case "journal":
                                var journal = await _dbContext.Journals.FirstOrDefaultAsync(x => x.Title_EN == kvp.Value);
                                article.Journal = journal ?? new Journal { Title_EN = kvp.Value };
                                break;
                            case "volume": int.TryParse(kvp.Value, out int volume); article.Volume = volume; break;
                            case "issue": article.IssueEn = kvp.Value; break;
                            case "pages":
                                try
                                {
                                    var pages = kvp.Value.Split("-");
                                    article.PageStart = int.Parse(pages[0]);
                                    if (pages.Length == 2) article.PageEnd = int.Parse(pages[1]);
                                }
                                catch { }
                                break;
                            case "publisher":
                                if (article.Journal != null) article.Journal.Publisher = kvp.Value;
                                break;
                            case "description": article.Description = kvp.Value; break;
                        }
                    }

                    article.LastUpdate = DateTime.Now;
                    var extraDescription = _scraper.GetElementText("#gsc_oci_descr > div > div:nth-child(2)");
                    if (!string.IsNullOrEmpty(extraDescription))
                        article.Description = $"{article.Description} {extraDescription}".Trim();

                    if (string.IsNullOrEmpty(article.TitleEn) || string.IsNullOrEmpty(article.TitleFa))
                    {
                        professor.ArticleAuthors.Add(new ArticleAuthor { Professor = professor, Article = article });
                    }

                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _scraper.Log($"Error scraping article {url.Key}: {ex.Message}", LogLevel.Error, "ScrapeArticles");
                }
            }
        }

        private async Task UpdateExistingArticlesAsync(Professor professor, Dictionary<string, (int Citations, string Title, string Journal, string Year)> articleUrls)
        {

            foreach (var articleUrl in articleUrls.ToList())
            {
                var articles = await _dbContext.Articles
                    .Include(x => x.ArticleAuthors)
                    .Where(x => (EF.Functions.FreeText(x.TitleEn, articleUrl.Value.Title) || EF.Functions.FreeText(x.TitleFa, articleUrl.Value.Title)) && x.ArticleAuthors.Any(xx => xx.ProfessorId == professor.Id))
                    .ToListAsync();

                Article article = null;
                if (articles.Any())
                {
                    var index = _scraper.AreStringsSimilar(articles.Select(x => x.TitleEn).ToList(), articleUrl.Value.Title);
                    if (index.HasValue) article = articles[index.Value];
                }

                if (article != null)
                {
                    article.ScholarCitations.Add(new ScholarArticleCitation { ScholarCitation = articleUrl.Value.Citations, LastUpdate = DateTime.Now });
                    if (!professor.ArticleAuthors.Any(x => x.Article.TitleEn == articleUrl.Value.Title || x.Article.TitleFa == articleUrl.Value.Title))
                    {
                        professor.ArticleAuthors.Add(new ArticleAuthor { Professor = professor, Article = article });
                    }
                    articleUrls.Remove(articleUrl.Key);
                }
            }
            await _dbContext.SaveChangesAsync();
        }

        private static int ExtractYear(string input)
        {
            Match match = Regex.Match(input, @"\b\d{4}\b");
            return match.Success ? int.Parse(match.Value) : throw new ArgumentException("سال معتبر یافت نشد.");
        }
    }
    #endregion

    #region Scopus Scraper
    public class ScopusScraper
    {
        private readonly WebScraper _scraper;
        private readonly ILogger<ScopusScraper> _logger;
        private readonly DynamicDbContext _dbContext;
        private readonly string _scopusProfileUrlBase = "https://www.scopus.com/authid/detail.uri?authorId=";

        public ScopusScraper(DynamicDbContext dbContext, WebScraper webScraper, ILogger<ScopusScraper> logger)
        {
            _scraper = webScraper;
            this._logger = logger;
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }
        public async Task ScrapeAllProfessors()
        {
            List<Professor> profiles = _dbContext.Professors.Where(x => x.ScopusID != null && x.ScopusID != "").OrderByDescending(x => x.Id).ToList();

            foreach (Professor profile in profiles)
            {
                await ExecuteAsync(profile);
            }
        }

        public async Task ExecuteAsync(Professor professor)
        {
            //if cant get user info ignor it
            try
            {
                //open scopus page of user
                await _scraper.OpenUrlAsync($"{_scopusProfileUrlBase}{professor.ScopusID}", "#scopus-author-profile-page-control-microui__general-information-content > h1");

                //get all article url from page of user
                var articleUrls = await ScrapeArticleUrlsAsync();

                ////if user have article or page is not correct ignor it
                if (articleUrls.Count != 0)
                {

                    ////remove existing article
                    await UpdateExistingArticlesAsync(professor, articleUrls);

                    if (articleUrls.Count != 0)
                    {
                        ////scrape user parameter
                        //await ScrapeProfileAsync(professor);

                        //scrape articles
                        await ScrapeArticlesAsync(professor, articleUrls);
                    }
                }
            }
            catch (Exception ex)
            {
                _scraper.Log($"Error scraping profile {professor.ScopusID}: {ex.Message}", LogLevel.Error, "Execute", ex);
            }
        }

        public static Article ExtractArticleData(string data, Article article)
        {
            var result = article;

            // استخراج Volume و Issue
            var volumeIssueMatch = Regex.Match(data, @"(\d+)\((\d+)\)");
            if (volumeIssueMatch.Success)
            {
                result.Volume = int.Parse(volumeIssueMatch.Groups[1].Value);
                result.IssueEn = data;
            }
            else
            {
                // Assuming static Log access or instance; adjust if needed
                // For static method, might need to handle logging differently, but per instructions, use the provided Log
                // Skipping detailed static log for now as per structure no change
            }

            // استخراج PageCount
            // استخراج PageCount (اولین عدد بعد از "pp.")
            var pagesMatch = Regex.Match(data, @"pp\.\s*(\d+)–(\d+)");
            if (pagesMatch.Success)
            {
                result.PageStart = int.Parse(pagesMatch.Groups[1].Value); // عدد 44
                result.PageEnd = int.Parse(pagesMatch.Groups[2].Value); // عدد 58
            }
            else
            {
                // Similar to above
            }

            return result;
        }

        public async Task SummeryExecuteAsync()
        {
            await _scraper.OpenUrlAsync("https://www-scopus-com.api2.semantak.com/results/results.uri?st1=University+of+Isfahan&st2=&s=AFFIL%28University+of+Isfahan%29&limit=200&origin=searchbasic&sort=plf-f&src=s&sot=b&sdt=b&sessionSearchId=f679a74080287b0f9bce47fe968ad461",
            "#container > micro-ui > document-search-results-page > div.micro-ui-namespace.DocumentSearchResultsPage-module__S9XTT > section:nth-child(1) > div:nth-child(3) > div > div > div:nth-child(1) > h2");
            Thread.Sleep(30000);

            int i = 0;
            while (true)
            {
                try
                {
                    var title = _scraper.GetElementText($"#container > micro-ui > document-search-results-page > div.micro-ui-namespace.DocumentSearchResultsPage-module__S9XTT > section:nth-child(2) > div > div.Col-module__hwM1N.PageLayout-module__j0MIQ > div > div:nth-child(3) > div > div.document-results-list-layout > div:nth-child(2) > table > tbody > tr:nth-child({i}) > td.TableItems-module__UF1E0 > div > div > h3 > a > span > span");
                    if (title != "")
                    {
                        var journalLable = _scraper.GetElementText($"#container > micro-ui > document-search-results-page > div.micro-ui-namespace.DocumentSearchResultsPage-module__S9XTT > section:nth-child(2) > div > div.Col-module__hwM1N.PageLayout-module__j0MIQ > div > div:nth-child(3) > div > div.document-results-list-layout > div:nth-child(2) > table > tbody > tr:nth-child({i}) > td.TableItems-module__zJIIe > div > div > a > span > span");
                        var journal = _dbContext.Journals.FirstOrDefault(x => x.Title_EN == journalLable);

                        var year = _scraper.GetElementText($"#container > micro-ui > document-search-results-page > div.micro-ui-namespace.DocumentSearchResultsPage-module__S9XTT > section:nth-child(2) > div > div.Col-module__hwM1N.PageLayout-module__j0MIQ > div > div:nth-child(3) > div > div.document-results-list-layout > div:nth-child(2) > table > tbody > tr:nth-child({i}) > td.TableItems-module__472S1 > div > span");
                        var article = new Article()
                        {
                            TitleEn = title,
                            Journal = journal,
                            Publication = year,
                            PublicationYear = int.Parse(year),
                        };

                        var more = _scraper.GetElementText($"#container > micro-ui > document-search-results-page > div.micro-ui-namespace.DocumentSearchResultsPage-module__S9XTT > section:nth-child(2) > div > div.Col-module__hwM1N.PageLayout-module__j0MIQ > div > div:nth-child(3) > div > div.document-results-list-layout > div:nth-child(2) > table > tbody > tr:nth-child({i}) > td.TableItems-module__zJIIe > div > div > span");
                        ExtractArticleData(more, article);

                        int ii = 0;
                        while (true)
                        {
                            var author = _scraper.FindOne($"#container > micro-ui > document-search-results-page > div.micro-ui-namespace.DocumentSearchResultsPage-module__S9XTT > section:nth-child(2) > div > div.Col-module__hwM1N.PageLayout-module__j0MIQ > div > div:nth-child(3) > div > div.document-results-list-layout > div:nth-child(2) > table > tbody > tr:nth-child({i}) > td.TableItems-module__lmTQ0 > div > div > span:nth-child({ii}) > button");
                            if (author != null)
                            {
                                author.Click();
                                Thread.Sleep(300);

                                var link = _scraper.FindOne($"#container > micro-ui > document-search-results-page > div.micro-ui-namespace.DocumentSearchResultsPage-module__S9XTT > section:nth-child(2) > div > div.Col-module__hwM1N.PageLayout-module__j0MIQ > div > div:nth-child(3) > div > div.document-results-list-layout > div:nth-child(2) > table > tbody > tr:nth-child({i}) > td.TableItems-module__lmTQ0 > div > div > span:nth-child({ii}) > div > div > div > div > div > div > div:nth-child(1) > div:nth-child(2) > div > a");
                                if (link != null)
                                {
                                    Match match = Regex.Match(link.GetAttribute("href"), @"authorId=(\d+)");
                                    var scopusId = match.Value.Split("=").LastOrDefault();

                                    if (await _dbContext.Professors.AnyAsync(x => x.ScopusID == scopusId))
                                    {
                                        var prof = await _dbContext.Professors.FirstOrDefaultAsync(x => x.ScopusID == scopusId);
                                        var authorArticle = new ArticleAuthor { Professor = prof, Article = article, Order = 0, LastUpdate = DateTime.Now };
                                        prof.ArticleAuthors.Add(authorArticle);
                                    }
                                    else
                                    {
                                        CoAuthor? coAuthor;
                                        if (await _dbContext.ArticleCoAuthors.AnyAsync(x => x.ScopusId == scopusId))
                                        {
                                            coAuthor = await _dbContext.ArticleCoAuthors.FirstOrDefaultAsync(x => x.ScopusId == scopusId);
                                        }
                                        else
                                        {
                                            var morAff = _scraper.GetElementText($"#container > micro-ui > document-search-results-page > div.micro-ui-namespace.DocumentSearchResultsPage-module__S9XTT > section:nth-child(2) > div > div.Col-module__hwM1N.PageLayout-module__j0MIQ > div > div:nth-child(3) > div > div.document-results-list-layout > div:nth-child(2) > table > tbody > tr:nth-child({i}) > td.TableItems-module__lmTQ0 > div > div > span:nth-child({ii}) > div > div > div > div > div > div > div:nth-child(1) > div.Stack-module__tT3r4.Stack-module___CTfk > div > span > a > span.Typography-module__lVnit.Typography-module__Nfgvc.Button-module__Imdmt");
                                            coAuthor = new CoAuthor
                                            {
                                                Name = _scraper.GetElementText($"#container > micro-ui > document-search-results-page > div.micro-ui-namespace.DocumentSearchResultsPage-module__S9XTT > section:nth-child(2) > div > div.Col-module__hwM1N.PageLayout-module__j0MIQ > div > div:nth-child(3) > div > div.document-results-list-layout > div:nth-child(2) > table > tbody > tr:nth-child({i}) > td.TableItems-module__lmTQ0 > div > div > span:nth-child({ii}) > div > div > div > div > div > div > div:nth-child(1) > div.Stack-module__tT3r4.Stack-module___CTfk > h1"),
                                                University = _scraper.GetElementText($"#doc-details-page-container > article > div:nth-child(2) > div.Col-module__hwM1N.DocumentDetailsPage-module__mKrYL > section > div.Stack-module__tT3r4.Stack-module___CTfk > div:nth-child(2) > div > ul > li:nth-child({ii}) > div > div > div > div > div > div > div:nth-child(1) > div.Stack-module__tT3r4.Stack-module___CTfk > div > span > a > span.Typography-module__lVnit.Typography-module__Nfgvc.Button-module__Imdmt"),
                                                City = string.IsNullOrEmpty(morAff) ? "" : (morAff.Split(",").FirstOrDefault() == "" ? morAff.Split(",")[1] : ""),
                                                Country = string.IsNullOrEmpty(morAff) ? "" : morAff.Split(",").LastOrDefault(),
                                                ScopusId = scopusId,
                                                LastUpdate = DateTime.Now
                                            };
                                        }

                                        var authorArticle = new ArticleAuthor { CoAuthor = coAuthor, Article = article, Order = 0, LastUpdate = DateTime.Now };
                                        article.ArticleAuthors.Add(authorArticle);

                                    }
                                    ii++;
                                }
                                else
                                {
                                    ii++;
                                }
                            }
                            else
                            {
                                if (ii < 10)
                                {
                                    ii++;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            await _scraper.ClickElementAsync($"#container > micro-ui > document-search-results-page > div.micro-ui-namespace.DocumentSearchResultsPage-module__S9XTT > section:nth-child(2) > div > div.Col-module__hwM1N.PageLayout-module__j0MIQ > div > div:nth-child(3) > div > div.document-results-list-layout > div:nth-child(2) > table > tbody > tr:nth-child({i}) > td.TableItems-module__lmTQ0 > div > div > span:nth-child({ii - 1}) > div > div > div > div > header > div > button");
                        }

                        await _dbContext.Articles.AddAsync(article);
                        try
                        {
                            await _dbContext.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            _scraper.Log($"Error saving article with title '{article.TitleEn}' during SummeryExecuteAsync: {ex.Message}", LogLevel.Error, "SummeryExecuteAsync_SaveChanges", ex);
                        }
                        i++;
                    }
                    else
                    {
                        var next = _scraper.FindOne("#container > micro-ui > document-search-results-page > div.micro-ui-namespace.DocumentSearchResultsPage-module__S9XTT > section:nth-child(2) > div > div.Col-module__hwM1N.PageLayout-module__j0MIQ > div > div:nth-child(3) > div > div.document-results-list-layout > div:nth-child(3) > div > nav > ul > li.page-item > button");
                        if (next.GetAttribute("disabled") == "true")
                        {
                            break;
                        }
                        else
                        {
                            if (i > 205)
                            {
                                next.Click();
                                i = 0;
                                Thread.Sleep(60000);
                            }
                            else
                            {
                                i++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _scraper.Log($"Error in SummeryExecuteAsync loop at index {i}: {ex.Message}", LogLevel.Error, "SummeryExecuteAsync_Loop", ex);
                    i++;
                }
            }
        }

        public class ProfessorCsvModel
        {
            public string Authors { get; set; }
            [Name("Author full names")]
            public string AuthorFullNames { get; set; }
            [Name("Author(s) ID")]
            public string AuthorsID { get; set; }
            public string Title { get; set; }
            public string Year { get; set; }
            [Name("Source title")]
            public string SourceTitle { get; set; }
            public string Volume { get; set; }
            public string Issue { get; set; }
            [Name("Art. No.")]
            public string ArtNo { get; set; }
            [Name("Page start")]
            public string PageStart { get; set; }
            [Name("Page end")]
            public string PageEnd { get; set; }
            [Name("Page count")]
            public string PageCount { get; set; }
            [Name("Cited by")]
            public string CitedBy { get; set; }
            public string DOI { get; set; }
            public string Link { get; set; }
            public string Affiliations { get; set; }
            [Name("Authors with affiliations")]
            public string AuthorsWithAffiliations { get; set; }
            public string Abstract { get; set; }
            [Name("Author Keywords")]
            public string AuthorKeywords { get; set; }
            [Name("Index Keywords")]
            public string IndexKeywords { get; set; }
            [Name("Molecular Sequence Numbers")]
            public string MolecularSequenceNumbers { get; set; }
            [Name("Chemicals/CAS")]
            public string ChemicalsCAS { get; set; }
            public string Tradenames { get; set; }
            public string Manufacturers { get; set; }
            [Name("Funding Details")]
            public string FundingDetails { get; set; }
            [Name("Funding Texts")]
            public string FundingTexts { get; set; }
            public string References { get; set; }
            [Name("Correspondence Address")]
            public string CorrespondenceAddress { get; set; }
            public string Editors { get; set; }
            public string Publisher { get; set; }
            public string Sponsors { get; set; }
            [Name("Conference name")]
            public string ConferenceName { get; set; }
            [Name("Conference date")]
            public string ConferenceDate { get; set; }
            [Name("Conference location")]
            public string ConferenceLocation { get; set; }
            [Name("Conference code")]
            public string ConferenceCode { get; set; }
            public string ISSN { get; set; }
            public string ISBN { get; set; }
            public string CODEN { get; set; }
            [Name("PubMed ID")]
            public string PubMedID { get; set; }
            [Name("Language of Original Document")]
            public string LanguageOfOriginalDocument { get; set; }
            [Name("Abbreviated Source Title")]
            public string AbbreviatedSourceTitle { get; set; }
            [Name("Document Type")]
            public string DocumentType { get; set; }
            [Name("Publication Stage")]
            public string PublicationStage { get; set; }
            [Name("Open Access")]
            public string OpenAccess { get; set; }
            public string Source { get; set; }
            public string EID { get; set; }
        }

        public async Task SummeryExecuteAsyncFrommExelScopus()
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                HasHeaderRecord = true
            };

            using (var reader = new StreamReader(@"D:\article\scopus\main.csv"))
            using (var csv = new CsvReader(reader, config))
            {
                var records = csv.GetRecords<ProfessorCsvModel>().ToList();

                var professors = await _dbContext.Professors.ToListAsync();
                var Article = await _dbContext.Articles.ToListAsync();
                var journals = await _dbContext.Journals.ToListAsync();
                var coAuthors = await _dbContext.ArticleCoAuthors.ToListAsync();
                var Citations = await _dbContext.ScopusArticleCitations.Include(x => x.Article).ToListAsync();

                var articleToAdds = new List<Article>();
                var journalToAdds = new List<Journal>();
                var coAuthorToAdds = new List<CoAuthor>();
                var citationToAdds = new List<ScopusArticleCitation>();
                var fundingSponsorToAdds = new List<FundingSponsor>();
                foreach (var record in records)
                {
                    try
                    {
                        var journal = journals.FirstOrDefault(x => x.ISSN == record.ISSN);

                        if (journal == null)
                        {
                            journal = new Journal
                            {
                                Title_EN = record.SourceTitle,
                                Publisher = record.Publisher,
                                ISSN = record.ISSN
                            };
                            journalToAdds.Add(journal);
                            journals.Add(journal);
                        }

                        var profesors = record.AuthorsID.Split(";").ToList();
                        var ProfsorList = new List<Professor>();
                        var coAuthorList = new List<CoAuthor>();

                        for (int i = 0; i < profesors.Count; i++)
                        {
                            var p = profesors[i].Replace(" ", "");
                            var professor = professors.FirstOrDefault(x => x.ScopusID == p);
                            if (professor != null)
                            {
                                ProfsorList.Add(professor);
                            }
                            else
                            {
                                var coAuthor = coAuthors.FirstOrDefault(x => x.ScopusId == p);
                                if (coAuthor == null)
                                {
                                    coAuthor = new CoAuthor
                                    {
                                        Name = record.Authors.Split(";")[i],
                                        ScopusId = profesors[i].Replace(" ", ""),
                                        LastUpdate = DateTime.Now
                                    };

                                    coAuthorToAdds.Add(coAuthor);
                                    coAuthorList.Add(coAuthor);
                                    coAuthors.Add(coAuthor);
                                }
                                else
                                {
                                    coAuthorList.Add(coAuthor);
                                }
                            }
                        }

                        var AuthoArticleList = new List<ArticleAuthor>();

                        foreach (var prof in ProfsorList)
                        {
                            var authorArticle = new ArticleAuthor
                            {
                                Professor = prof,
                                Order = 0,
                                LastUpdate = DateTime.Now
                            };
                            AuthoArticleList.Add(authorArticle);
                        }

                        foreach (var prof in coAuthorList)
                        {
                            var authorArticle = new ArticleAuthor
                            {
                                CoAuthor = prof,
                                Order = 0,
                                LastUpdate = DateTime.Now
                            };
                            AuthoArticleList.Add(authorArticle);
                        }

                        var keywords = new List<ArticleKeyword>();
                        var authorKeywords = string.IsNullOrEmpty(record.AuthorKeywords) ? null : record.AuthorKeywords.Split(";").ToList();
                        var indexKeywords = string.IsNullOrEmpty(record.IndexKeywords) ? null : record.IndexKeywords.Split(";").ToList();
                        if (authorKeywords != null)
                        {
                            foreach (var keyword in authorKeywords)
                            {
                                if (!string.IsNullOrEmpty(keyword))
                                {
                                    keywords.Add(new ArticleKeyword { Keyword = keyword, LastUpdate = DateTime.Now, IsAuthorKeyword = true });
                                }
                            }
                        }

                        if (indexKeywords != null)
                        {
                            foreach (var keyword in indexKeywords)
                            {
                                if (!string.IsNullOrEmpty(keyword))
                                {
                                    keywords.Add(new ArticleKeyword { Keyword = keyword, LastUpdate = DateTime.Now, IsAuthorKeyword = false });
                                }
                            }
                        }

                        var article = new Article
                        {
                            TitleEn = record.Title,
                            Doi = record.DOI,
                            AbstractEn = record.Abstract,
                            Publication = record.Year,
                            PublicationYear = int.Parse(record.Year),
                            Volume = string.IsNullOrEmpty(record.Volume) ? null : int.TryParse(ExtractNumber(record.Volume),out int volume) ? null : volume,
                            IssueEn = record.Issue,
                            PageStart = string.IsNullOrEmpty(record.PageStart) ? null : int.Parse(record.PageStart),
                            PageEnd = string.IsNullOrEmpty(record.PageEnd) ? null : int.Parse(record.PageEnd),
                            Type = record.DocumentType,
                            SourceType = record.Source,
                            OpenAccess = record.OpenAccess,
                            OriginalLanguage = record.LanguageOfOriginalDocument,
                            LastUpdate = DateTime.Now,
                            Keywords = keywords,
                            Journal = journal,
                            ArticleAuthors = AuthoArticleList,
                            IsScopus = true,
                        };

                        if (!Article.Any(x => x.TitleEn == article.TitleEn && x.PublicationYear == article.PublicationYear && x.Volume == article.Volume && x.IssueEn == article.IssueEn && x.PageStart == article.PageStart && x.PageEnd == article.PageEnd))
                        {
                            articleToAdds.Add(article);
                            Article.Add(article);
                        }

                        var citation = Citations.FirstOrDefault(x => x.Article.TitleEn == record.Title && x.Article.PublicationYear == int.Parse(record.Year) && x.Article.Volume == (string.IsNullOrEmpty(record.Volume) ? null : int.Parse(record.Volume)) && x.Article.IssueEn == record.Issue && x.Article.PageStart == (string.IsNullOrEmpty(record.PageStart) ? null : int.Parse(record.PageStart)) && x.Article.PageEnd == (string.IsNullOrEmpty(record.PageEnd) ? null : int.Parse(record.PageEnd)));
                        if (citation == null)
                        {
                            citation = new ScopusArticleCitation { Article = article, ScopusCitation = int.Parse(record.CitedBy), LastUpdate = DateTime.Now };
                            citationToAdds.Add(citation);
                            Citations.Add(citation);
                        }
                        var citations = new List<ScopusArticleCitation>();
                        citations.Add(citation);
                    }
                    catch (Exception ex)
                    {
                        _scraper.Log($"Error processing CSV record with Title '{record.Title}' and Year '{record.Year}' in SummeryExecuteAsyncFrommExelScopus: {ex.Message}", LogLevel.Error, "SummeryExecuteAsyncFrommExelScopus_RecordProcessing", ex);
                    }
                }
                await _dbContext.Journals.AddRangeAsync(journalToAdds);
                await _dbContext.ArticleCoAuthors.AddRangeAsync(coAuthorToAdds);
                await _dbContext.FundingSponsors.AddRangeAsync(fundingSponsorToAdds);
                await _dbContext.Articles.AddRangeAsync(articleToAdds);
                await _dbContext.ScopusArticleCitations.AddRangeAsync(citationToAdds);

                try
                {
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _scraper.Log($"Error saving changes in SummeryExecuteAsyncFrommExelScopus: {ex.Message}", LogLevel.Error, "SummeryExecuteAsyncFrommExelScopus_SaveChanges", ex);
                }
            }
        }

        public class ProfessorCsvModel2
        {
            [Name("Publication Type")]
            public string PublicationType { get; set; }

            [Name("Authors")]
            public string Authors { get; set; }

            [Name("Book Authors")]
            public string BookAuthors { get; set; }

            [Name("Book Editors")]
            public string BookEditors { get; set; }

            [Name("Book Group Authors")]
            public string BookGroupAuthors { get; set; }

            [Name("Author Full Names")]
            public string AuthorFullNames { get; set; }

            [Name("Book Author Full Names")]
            public string BookAuthorFullNames { get; set; }

            [Name("Group Authors")]
            public string GroupAuthors { get; set; }

            [Name("Article Title")]
            public string ArticleTitle { get; set; }

            [Name("Source Title")]
            public string SourceTitle { get; set; }

            [Name("Book Series Title")]
            public string BookSeriesTitle { get; set; }

            [Name("Book Series Subtitle")]
            public string BookSeriesSubtitle { get; set; }

            [Name("Language")]
            public string Language { get; set; }

            [Name("Document Type")]
            public string DocumentType { get; set; }

            [Name("Conference Title")]
            public string ConferenceTitle { get; set; }

            [Name("Conference Date")]
            public string ConferenceDate { get; set; }

            [Name("Conference Location")]
            public string ConferenceLocation { get; set; }

            [Name("Conference Sponsor")]
            public string ConferenceSponsor { get; set; }

            [Name("Conference Host")]
            public string ConferenceHost { get; set; }

            [Name("Author Keywords")]
            public string AuthorKeywords { get; set; }

            [Name("Keywords Plus")]
            public string KeywordsPlus { get; set; }

            [Name("Abstract")]
            public string Abstract { get; set; }

            [Name("Addresses")]
            public string Addresses { get; set; }

            [Name("Affiliations")]
            public string Affiliations { get; set; }

            [Name("Reprint Addresses")]
            public string ReprintAddresses { get; set; }

            [Name("Email Addresses")]
            public string EmailAddresses { get; set; }

            [Name("Researcher Ids")]
            public string ResearcherIds { get; set; }

            [Name("ORCIDs")]
            public string ORCIDs { get; set; }

            [Name("Funding Orgs")]
            public string FundingOrgs { get; set; }

            [Name("Funding Name Preferred")]
            public string FundingNamePreferred { get; set; }

            [Name("Funding Text")]
            public string FundingText { get; set; }

            [Name("Cited References")]
            public string CitedReferences { get; set; }

            [Name("Cited Reference Count")]
            public string CitedReferenceCount { get; set; }

            [Name("Times Cited, WoS Core")]
            public string TimesCitedWoSCore { get; set; }

            [Name("Times Cited, All Databases")]
            public string TimesCitedAllDatabases { get; set; }

            [Name("180 Day Usage Count")]
            public string UsageCount180Day { get; set; }

            [Name("Since 2013 Usage Count")]
            public string UsageCountSince2013 { get; set; }

            [Name("Publisher")]
            public string Publisher { get; set; }

            [Name("Publisher City")]
            public string PublisherCity { get; set; }

            [Name("Publisher Address")]
            public string PublisherAddress { get; set; }

            [Name("ISSN")]
            public string ISSN { get; set; }

            [Name("eISSN")]
            public string EISSN { get; set; }

            [Name("ISBN")]
            public string ISBN { get; set; }

            [Name("Journal Abbreviation")]
            public string JournalAbbreviation { get; set; }

            [Name("Journal ISO Abbreviation")]
            public string JournalISOAbbreviation { get; set; }

            [Name("Publication Date")]
            public string PublicationDate { get; set; }

            [Name("Publication Year")]
            public string PublicationYear { get; set; }

            [Name("Volume")]
            public string Volume { get; set; }

            [Name("Issue")]
            public string Issue { get; set; }

            [Name("Part Number")]
            public string PartNumber { get; set; }

            [Name("Supplement")]
            public string Supplement { get; set; }

            [Name("Special Issue")]
            public string SpecialIssue { get; set; }

            [Name("Meeting Abstract")]
            public string MeetingAbstract { get; set; }

            [Name("Start Page")]
            public string StartPage { get; set; }

            [Name("End Page")]
            public string EndPage { get; set; }

            [Name("Article Number")]
            public string ArticleNumber { get; set; }

            [Name("DOI")]
            public string DOI { get; set; }

            [Name("DOI Link")]
            public string DOILink { get; set; }

            [Name("Book DOI")]
            public string BookDOI { get; set; }

            [Name("Early Access Date")]
            public string EarlyAccessDate { get; set; }

            [Name("Number of Pages")]
            public string NumberOfPages { get; set; }

            [Name("WoS Categories")]
            public string WoSCategories { get; set; }

            [Name("Web of Science Index")]
            public string WebOfScienceIndex { get; set; }

            [Name("Research Areas")]
            public string ResearchAreas { get; set; }

            [Name("IDS Number")]
            public string IDSNumber { get; set; }

            [Name("Pubmed Id")]
            public string PubmedId { get; set; }

            [Name("Open Access Designations")]
            public string OpenAccessDesignations { get; set; }

            [Name("Highly Cited Status")]
            public string HighlyCitedStatus { get; set; }

            [Name("Hot Paper Status")]
            public string HotPaperStatus { get; set; }

            [Name("Date of Export")]
            public string DateOfExport { get; set; }

            [Name("UT (Unique WOS ID)")]
            public string UniqueWOSID { get; set; }

            [Name("Web of Science Record")]
            public string WebOfScienceRecord { get; set; }
        }

        public async Task SummeryExecuteAsyncFrommExelWos()
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null,
                IgnoreBlankLines = true,
                HeaderValidated = null
            };


            using (var reader = new StreamReader(@"D:\article\wos\main.csv"))
            using (var csv = new CsvReader(reader, config))
            {
                var records = csv.GetRecords<ProfessorCsvModel2>().ToList();

                var professors = await _dbContext.Professors.ToListAsync();
                var articles = await _dbContext.Articles.Include(x => x.ArticleAuthors)
                .ThenInclude(x => x.Professor).ToListAsync();
                var journals = await _dbContext.Journals.ToListAsync();
                var coAuthors = await _dbContext.ArticleCoAuthors.ToListAsync();
                var Citations = await _dbContext.ScopusArticleCitations.Include(x => x.Article).ToListAsync();

                var articleToAdds = new List<Article>();
                var journalToAdds = new List<Journal>();
                var coAuthorToAdds = new List<CoAuthor>();
                var citationToAdds = new List<ScopusArticleCitation>();
                var fundingSponsorToAdds = new List<FundingSponsor>();
                int ixx = 0;
                foreach (var record in records)
                {
                    try
                    {
                        var article = new Article
                        {
                            TitleEn = record.ArticleTitle,
                            Doi = record.DOI,
                            AbstractEn = record.Abstract,
                            Publication = record.PublicationYear,
                            PublicationYear = int.Parse(record.PublicationYear),
                            Volume = string.IsNullOrEmpty(record.Volume) ? null : int.Parse(record.Volume),
                            IssueEn = record.Issue,
                            PageStart = string.IsNullOrEmpty(record.StartPage) ? null : int.Parse(record.StartPage),
                            PageEnd = string.IsNullOrEmpty(record.EndPage) ? null : int.Parse(record.EndPage),
                            Type = record.DocumentType,
                            SourceType = record.PublicationType == "j" ? "Journal" : record.PublicationType == "c" ? "Conference" : record.PublicationType == "b" ? "Book" : "Other",
                            OpenAccess = record.OpenAccessDesignations,
                            OriginalLanguage = record.Language,
                            LastUpdate = DateTime.Now,
                        };

                        if (!articles.Any(x => Regex.Replace(x.TitleEn ?? "", @"[^a-zA-Z]", "").ToLower() == Regex.Replace(article.TitleEn ?? "", @"[^a-zA-Z]", "").ToLower() &&
                             x.PublicationYear == article.PublicationYear && x.Volume == article.Volume
                             && x.PageStart == article.PageStart && x.PageEnd == article.PageEnd))
                        {
                            article.IsWos = true;
                            articleToAdds.Add(article);
                            articles.Add(article);

                            var journal = journals.FirstOrDefault(x => x.ISSN == record.ISSN.Replace(" ", ""));

                            if (journal == null)
                            {
                                journal = new Journal
                                {
                                    Title_EN = record.SourceTitle,
                                    Publisher = record.Publisher,
                                    ISSN = record.ISSN
                                };
                                journalToAdds.Add(journal);
                                journals.Add(journal);
                                article.Journal = journal;
                            }


                            var profesors = record.ResearcherIds.Split(";")
                            .Select(x => x.Split("/").LastOrDefault()).ToList();

                            var ProfsorList = new List<Professor>();
                            var coAuthorList = new List<CoAuthor>();

                            for (int i = 0; i < profesors.Count; i++)
                            {
                                var p = profesors[i].Replace(" ", "");
                                var professor = professors.FirstOrDefault(x => x.WebOfScienceID == p);
                                if (professor != null)
                                {
                                    ProfsorList.Add(professor);
                                }
                                else
                                {
                                    var coAuthor = coAuthors.FirstOrDefault(x => x.WebOfScienceID == p);
                                    if (coAuthor == null)
                                    {
                                        coAuthor = new CoAuthor
                                        {
                                            Name = record.AuthorFullNames.Split(";")[i],
                                            WebOfScienceID = profesors[i],
                                            LastUpdate = DateTime.Now
                                        };
                                        coAuthorToAdds.Add(coAuthor);
                                        coAuthorList.Add(coAuthor);
                                        coAuthors.Add(coAuthor);
                                    }
                                    else
                                    {
                                        coAuthorList.Add(coAuthor);
                                    }
                                }
                            }

                            var AuthoArticleList = new List<ArticleAuthor>();

                            foreach (var prof in ProfsorList)
                            {
                                var authorArticle = new ArticleAuthor
                                {
                                    Professor = prof,
                                    Order = 0,
                                    LastUpdate = DateTime.Now
                                };
                                AuthoArticleList.Add(authorArticle);
                            }

                            foreach (var prof in coAuthorList)
                            {
                                var authorArticle = new ArticleAuthor
                                {
                                    CoAuthor = prof,
                                    Order = 0,
                                    LastUpdate = DateTime.Now
                                };
                                AuthoArticleList.Add(authorArticle);
                            }

                            article.ArticleAuthors.AddRange(AuthoArticleList);

                            var keywords = new List<ArticleKeyword>();
                            var authorKeywords = string.IsNullOrEmpty(record.AuthorKeywords) ? null : record.AuthorKeywords.Split(";").ToList();
                            var indexKeywords = string.IsNullOrEmpty(record.KeywordsPlus) ? null : record.KeywordsPlus.Split(";").ToList();
                            if (authorKeywords != null)
                            {
                                foreach (var keyword in authorKeywords)
                                {
                                    if (!string.IsNullOrEmpty(keyword))
                                    {
                                        keywords.Add(new ArticleKeyword { Keyword = keyword, LastUpdate = DateTime.Now, IsAuthorKeyword = true });
                                    }
                                }
                            }

                            if (indexKeywords != null)
                            {
                                foreach (var keyword in indexKeywords)
                                {
                                    if (!string.IsNullOrEmpty(keyword))
                                    {
                                        keywords.Add(new ArticleKeyword { Keyword = keyword, LastUpdate = DateTime.Now, IsAuthorKeyword = false });
                                    }
                                }
                            }
                            article.Keywords.AddRange(keywords);
                        }
                        else
                        {
                            ixx++;
                            article = articles.FirstOrDefault(x => Regex.Replace(x.TitleEn ?? "", @"[^a-zA-Z]", "").ToLower() == Regex.Replace(article.TitleEn ?? "", @"[^a-zA-Z]", "").ToLower() &&
                            x.PublicationYear == article.PublicationYear && x.Volume == article.Volume &&
                            x.PageStart == article.PageStart && x.PageEnd == article.PageEnd);

                            var articleAuthors = article.ArticleAuthors.ToList();

                            var profesors = record.ResearcherIds.Split(";")
                             .Select(x => x.Split("/").LastOrDefault()).ToList();

                            var ProfsorList = new List<Professor>();
                            var coAuthorList = new List<CoAuthor>();

                            for (int i = 0; i < profesors.Count; i++)
                            {
                                var p = profesors[i].Replace(" ", "");
                                var professor = professors.FirstOrDefault(x => x.WebOfScienceID == p);
                                if (professor == null)
                                {
                                    var coAuthor = new CoAuthor
                                    {
                                        Name = record.AuthorFullNames.Split(";")[i],
                                        WebOfScienceID = profesors[i],
                                        LastUpdate = DateTime.Now
                                    };
                                    coAuthorList.Add(coAuthor);
                                }
                            }

                            foreach (var auth in coAuthorList)
                            {
                                var prof = articleAuthors.FirstOrDefault();
                                double i = 0;
                                foreach (var authId in articleAuthors)
                                {
                                    if (authId.Professor != null)
                                    {
                                        var ii = CalculateNameSimilarity(authId.Professor?.LastNameEn + " " + authId.Professor?.FirstNameEn, auth.Name);

                                        if (ii > i)
                                        {
                                            prof = authId;
                                            i = ii;
                                        }
                                    }
                                }
                                if (prof != null)
                                    if (prof.Professor != null)
                                        if (CalculateNameSimilarity(prof.Professor.FirstNameEn + " " + prof.Professor.LastNameEn, auth.Name) > 0.7)
                                            prof.Professor.WebOfScienceID = auth.WebOfScienceID;
                            }

                            article.IsWos = true;
                        }

                    }
                    catch (Exception ex)
                    {
                        _scraper.Log($"Error processing CSV record with ArticleTitle '{record.ArticleTitle}' and PublicationYear '{record.PublicationYear}' in SummeryExecuteAsyncFrommExelWos: {ex.Message}", LogLevel.Error, "SummeryExecuteAsyncFrommExelWos_RecordProcessing", ex);
                    }
                }

                await _dbContext.Journals.AddRangeAsync(journalToAdds);
                await _dbContext.ArticleCoAuthors.AddRangeAsync(coAuthorToAdds);
                await _dbContext.FundingSponsors.AddRangeAsync(fundingSponsorToAdds);
                await _dbContext.Articles.AddRangeAsync(articleToAdds);
                await _dbContext.ScopusArticleCitations.AddRangeAsync(citationToAdds);

                try
                {
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _scraper.Log($"Error saving changes in SummeryExecuteAsyncFrommExelWos: {ex.Message}", LogLevel.Error, "SummeryExecuteAsyncFrommExelWos_SaveChanges", ex);
                }
            }
        }

        public class ProfessorCsvModel3
        {

            [Name("Id")]
            public string Id { get; set; }

            [Name("Affi")]
            public string Affi { get; set; }

            [Name("Name")]
            public string Name { get; set; }

            [Name("Email")]
            public string Email { get; set; }

            [Name("Citation")]
            public string Citation { get; set; }

            [Name("ImagePath")]
            public string ImagePath { get; set; }
        }
        public async Task SummeryExecuteAsyncFrommExelImg()
        {
            string sourceFolder = @"D:\img\img"; // پوشه منبع عکس‌ها
            string destinationFolder = @"D:\img\img2"; // پوشه مقصد برای کپی عکس‌ها

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null,
                IgnoreBlankLines = true,
                HeaderValidated = null
            };


            using (var reader = new StreamReader(@"D:\img\item.csv"))
            using (var csv = new CsvReader(reader, config))
            {
                var records = csv.GetRecords<ProfessorCsvModel3>().ToList();
                var peofesors = _dbContext.Professors.ToList();

                foreach (var record in records)
                {
                    var prof = peofesors.FirstOrDefault(x => x.GoogleScholarID == record.Id);
                    if (prof != null)
                    {
                        if (!record.ImagePath.IsNullOrEmpty())
                        {
                            var img = record.ImagePath.Split(@"\").LastOrDefault();
                            var sourceImagePath = Path.Combine(sourceFolder, img);
                            if (File.Exists(sourceImagePath))
                            {
                                try
                                {
                                    var fileExtension = Path.GetExtension(record.ImagePath);
                                    var newFileName = $"{prof.GoogleScholarID}{fileExtension}";
                                    var destinationPath = Path.Combine(destinationFolder, newFileName);

                                    File.Copy(sourceImagePath, destinationPath, true);

                                    prof.ImageUrl = @"upload\professor\profileimage\" + newFileName;
                                }
                                catch (Exception ex)
                                {
                                    _scraper.Log($"Error copying image for professor with GoogleScholarID '{prof.GoogleScholarID}' and ImagePath '{record.ImagePath}' in SummeryExecuteAsyncFrommExelImg: {ex.Message}", LogLevel.Error, "SummeryExecuteAsyncFrommExelImg_ImageCopy", ex);
                                }
                            }
                            else
                            {
                                _scraper.Log($"Image file not found for professor with GoogleScholarID '{prof.GoogleScholarID}' at path '{sourceImagePath}'", LogLevel.Warning, "SummeryExecuteAsyncFrommExelImg_FileNotFound");
                            }
                        }
                    }
                }
                try
                {
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _scraper.Log($"Error saving changes in SummeryExecuteAsyncFrommExelImg: {ex.Message}", LogLevel.Error, "SummeryExecuteAsyncFrommExelImg_SaveChanges", ex);
                }
            }

        }

        private double CalculateNameSimilarity(string name1, string name2)
        {
            // نرمال‌سازی نام‌ها
            name1 = name1.Replace(" ", "").ToLower();
            name2 = name2.Replace(" ", "").ToLower();

            // تعداد کاراکترهای مشترک را محاسبه می‌کنیم
            var commonChars = name1.Intersect(name2).Count();

            // طول رشته بلندتر را پیدا می‌کنیم
            var maxLength = Math.Max(name1.Length, name2.Length);

            // محاسبه درصد شباهت
            var similarity = (double)commonChars / maxLength;

            return similarity;
        }

        private async Task ScrapeProfileAsync(Professor professor)
        {
            //orcid
            professor.OrcidId = (_scraper.GetElementsText("#AuthorHeader__orcid-tooltip-link", "href"))[0];

            //more parameter
            var citation = new ScopusProfile();
            if ((_scraper.GetElementText("#scopus-author-profile-page-control-microui__general-information-content > section > ul > li:nth-child(1) > div > div > div > div > div:nth-child(2) > span > p")).Contains("Citations", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse(_scraper.GetElementText("#scopus-author-profile-page-control-microui__general-information-content > section > ul > li:nth-child(1) > div > div > div > div > div:nth-child(1) > span"), out int citations);
                citation.CitationCounts = citations;
            }

            if ((_scraper.GetElementText("#scopus-author-profile-page-control-microui__general-information-content > section > ul > li:nth-child(2) > div > div > div > div > div:nth-child(2) > span > p")).Contains("Documents", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse(_scraper.GetElementText("#scopus-author-profile-page-control-microui__general-information-content > section > ul > li:nth-child(2) > div > div > div > div > div:nth-child(1) > span"), out int documents);
                citation.Documents = documents;
            }

            //right meteric of user
            await ScrapeAuthorMetricsAsync(citation);


            if (!await _dbContext.ScopusProfiles.AnyAsync(x => x.Documents == citation.Documents && x.CitationCounts == citation.CitationCounts && x.CoAuthorScore == citation.CoAuthorScore && x.SingleAuthorScore == citation.SingleAuthorScore && x.LastAuthorScore == citation.LastAuthorScore && x.FirstAuthorScore == citation.FirstAuthorScore))
            {
                citation.Lastupdate = DateTime.UtcNow;
                professor.ScopusProfiles.Add(citation);
            }
            else
            {
                var scopProfile = await _dbContext.ScopusProfiles.FirstOrDefaultAsync(x => x.Documents == citation.Documents && x.CitationCounts == citation.CitationCounts && x.CoAuthorScore == citation.CoAuthorScore && x.SingleAuthorScore == citation.SingleAuthorScore && x.LastAuthorScore == citation.LastAuthorScore && x.FirstAuthorScore == citation.FirstAuthorScore);
                scopProfile.Lastupdate = DateTime.Now;
            }

            ExtractCitations(professor);

            if (!professor.ScopusHIndexes.Any())
            {
                var metric = new List<ScopusHIndex>();
                await ScrapeAdditionalMetricsAsync(metric, professor);
                professor.ScopusHIndexes.AddRange(metric);
            }

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _scraper.Log($"Error saving profile for professor with ScopusID '{professor.ScopusID}' in ScrapeProfileAsync: {ex.Message}", LogLevel.Error, "ScrapeProfileAsync_SaveChanges", ex);
            }
        }

        public Dictionary<int, int> ExtractCitations(Professor professor)
        {
            var citations = new Dictionary<int, int>();
            try
            {
                var elements = _scraper.GetElementsText("g.highcharts-markers.highcharts-series-1.highcharts-line-series.highcharts-tracker path.highcharts-point", "aria-label");
                foreach (var element in elements)
                {
                    var match = Regex.Match(element, @"(\d{4}),\s*(\d+)\.\s*Citations");
                    if (match.Success)
                    {
                        int year = int.Parse(match.Groups[1].Value);
                        int citationCount = int.Parse(match.Groups[2].Value);
                        citations[year] = citationCount;
                        professor.ProfileCitationByYears.Add(new ProfileCitationByYear { Year = year, CitationCount = citationCount, Professor = professor, LastUpdate = DateTime.Now });
                    }
                    else
                    {
                        _scraper.Log($"Invalid aria-label format: {element}", LogLevel.Warning, "ExtractCitations");
                    }
                }
                _scraper.Log($"Extracted {citations.Count} citation data points", LogLevel.Information, "ExtractCitations");
            }
            catch (Exception ex)
            {
                _scraper.Log($"Error extracting citations: {ex.Message}", LogLevel.Error, "ExtractCitations", ex);
            }
            return citations;
        }

        private async Task ScrapeAuthorMetricsAsync(ScopusProfile citation)
        {
            var authorTypes = new[] { "First author", "Last author", "Co-author", "Single author" };
            foreach (var type in authorTypes)
            {

                var buttonSelector = $"#page-{type.Replace(" ", "\\ ")}-button";
                await _scraper.ClickElementAsync(buttonSelector);

                int.TryParse((_scraper.GetElementText($"{buttonSelector} > span.Typography-module__lVnit.Typography-module__Nfgvc.Button-module__Imdmt > div > div > div > span:nth-child(2)")).Split("%")[0], out int price);
                int.TryParse(_scraper.GetElementText($"#page-{type.Replace(" ", "\\ ")} > div > section > div > div:nth-child(1) > div > div > div > div:nth-child(1) > span"), out int articleCount);
                int.TryParse(_scraper.GetElementText($"#page-{type.Replace(" ", "\\ ")} > div > section > div > div:nth-child(2) > div > div > div > div:nth-child(1) > span"), out int avgCitations);
                double.TryParse(_scraper.GetElementText($"#page-{type.Replace(" ", "\\ ")} > div > section > div > div:nth-child(3) > div > div > div > div:nth-child(1) > span"), out double fwci);

                switch (type)
                {
                    case "First author":
                        citation.FirstAuthorScore = price;
                        citation.FirstAuthorArticleCount = articleCount;
                        citation.FirstAuthorAverageCitations = avgCitations;
                        citation.FirstAuthorFwci = fwci;
                        if (citation.FirstAuthorArticleCount == 0)
                        {
                            await _scraper.ClickElementAsync(buttonSelector);
                            int.TryParse((_scraper.GetElementText($"{buttonSelector} > span.Typography-module__lVnit.Typography-module__Nfgvc.Button-module__Imdmt > div > div > div > span:nth-child(2)")).Split("%")[0], out price);
                            int.TryParse(_scraper.GetElementText($"#page-{type.Replace(" ", "\\ ")} > div > section > div > div:nth-child(1) > div > div > div > div:nth-child(1) > span"), out articleCount);
                            int.TryParse(_scraper.GetElementText($"#page-{type.Replace(" ", "\\ ")} > div > section > div > div:nth-child(2) > div > div > div > div:nth-child(1) > span"), out avgCitations);
                            double.TryParse(_scraper.GetElementText($"#page-{type.Replace(" ", "\\ ")} > div > section > div > div:nth-child(3) > div > div > div > div:nth-child(1) > span"), out fwci);
                            citation.FirstAuthorScore = price;
                            citation.FirstAuthorArticleCount = articleCount;
                            citation.FirstAuthorAverageCitations = avgCitations;
                            citation.FirstAuthorFwci = fwci;
                        }
                        break;
                    case "Last author":
                        citation.LastAuthorScore = price;
                        citation.LastAuthorArticleCount = articleCount;
                        citation.LastAuthorAverageCitations = avgCitations;
                        citation.LastAuthorFwci = fwci;
                        if (citation.LastAuthorArticleCount == 0)
                        {
                            await _scraper.ClickElementAsync(buttonSelector);
                            int.TryParse((_scraper.GetElementText($"{buttonSelector} > span.Typography-module__lVnit.Typography-module__Nfgvc.Button-module__Imdmt > div > div > div > span:nth-child(2)")).Split("%")[0], out price);
                            int.TryParse(_scraper.GetElementText($"#page-{type.Replace(" ", "\\ ")} > div > section > div > div:nth-child(1) > div > div > div > div:nth-child(1) > span"), out articleCount);
                            int.TryParse(_scraper.GetElementText($"#page-{type.Replace(" ", "\\ ")} > div > section > div > div:nth-child(2) > div > div > div > div:nth-child(1) > span"), out avgCitations);
                            double.TryParse(_scraper.GetElementText($"#page-{type.Replace(" ", "\\ ")} > div > section > div > div:nth-child(3) > div > div > div > div:nth-child(1) > span"), out fwci);
                            citation.LastAuthorScore = price;
                            citation.LastAuthorArticleCount = articleCount;
                            citation.LastAuthorAverageCitations = avgCitations;
                            citation.LastAuthorFwci = fwci;
                        }
                        break;
                    case "Co-author":
                        citation.CoAuthorScore = price;
                        citation.CoAuthorArticleCount = articleCount;
                        citation.CoAuthorAverageCitations = avgCitations;
                        citation.CoAuthorFwci = fwci;
                        if (citation.CoAuthorArticleCount == 0)
                        {
                            await _scraper.ClickElementAsync(buttonSelector);
                            int.TryParse((_scraper.GetElementText($"{buttonSelector} > span.Typography-module__lVnit.Typography-module__Nfgvc.Button-module__Imdmt > div > div > div > span:nth-child(2)")).Split("%")[0], out price);
                            int.TryParse(_scraper.GetElementText($"#page-{type.Replace(" ", "\\ ")} > div > section > div > div:nth-child(1) > div > div > div > div:nth-child(1) > span"), out articleCount);
                            int.TryParse(_scraper.GetElementText($"#page-{type.Replace(" ", "\\ ")} > div > section > div > div:nth-child(2) > div > div > div > div:nth-child(1) > span"), out avgCitations);
                            double.TryParse(_scraper.GetElementText($"#page-{type.Replace(" ", "\\ ")} > div > section > div > div:nth-child(3) > div > div > div > div:nth-child(1) > span"), out fwci);
                            citation.CoAuthorScore = price;
                            citation.CoAuthorArticleCount = articleCount;
                            citation.CoAuthorAverageCitations = avgCitations;
                            citation.CoAuthorFwci = fwci;
                        }
                        break;
                    case "Single author":
                        citation.SingleAuthorScore = price;
                        citation.SingleAuthorArticleCount = articleCount;
                        citation.SingleAuthorAverageCitations = avgCitations;
                        citation.SingleAuthorFwci = fwci;
                        if (citation.SingleAuthorArticleCount == 0)
                        {
                            await _scraper.ClickElementAsync(buttonSelector);
                            int.TryParse((_scraper.GetElementText($"{buttonSelector} > span.Typography-module__lVnit.Typography-module__Nfgvc.Button-module__Imdmt > div > div > div > span:nth-child(2)")).Split("%")[0], out price);
                            int.TryParse(_scraper.GetElementText($"#page-{type.Replace(" ", "\\ ")} > div > section > div > div:nth-child(1) > div > div > div > div:nth-child(1) > span"), out articleCount);
                            int.TryParse(_scraper.GetElementText($"#page-{type.Replace(" ", "\\ ")} > div > section > div > div:nth-child(2) > div > div > div > div:nth-child(1) > span"), out avgCitations);
                            double.TryParse(_scraper.GetElementText($"#page-{type.Replace(" ", "\\ ")} > div > section > div > div:nth-child(3) > div > div > div > div:nth-child(1) > span"), out fwci);
                            citation.SingleAuthorScore = price;
                            citation.SingleAuthorArticleCount = articleCount;
                            citation.SingleAuthorAverageCitations = avgCitations;
                            citation.SingleAuthorFwci = fwci;
                        }
                        break;
                }
            }
        }

        private async Task ScrapeAdditionalMetricsAsync(List<ScopusHIndex> hIndexes, Professor author)
        {
            try
            {
                await _scraper.ClickElementAsync("#AuthorProfile_ViewHGraph");
                for (int year = 2009; year <= 2025; year++)
                {

                    await _scraper.ClickElementAsync("#toYr1-button");
                    await _scraper.ClickElementAsync($"#ui-id-{year - 2008}");
                    await _scraper.ClickElementAsync("#updateGraphButton_submit1");
                    await Task.Delay(2000);

                    int.TryParse(_scraper.GetElementText("#analyzeSourceTitle > span.pull-right.fontXXLarge"), out int hIndex);
                    hIndexes.Add(new ScopusHIndex { ProfessorId = author.Id, Year = year, HIndex = hIndex, LastUpdate = DateTime.Now });
                }
            }
            catch (Exception ex)
            {
                _scraper.Log($"Error scraping additional metrics for professor with ID '{author.Id}': {ex.Message}", LogLevel.Error, "ScrapeAdditionalMetricsAsync", ex);
            }
        }

        private async Task<Dictionary<string, (int Citations, string Title, string Journal, string Year)>> ScrapeArticleUrlsAsync()
        {
            //output result
            var articleUrls = new Dictionary<string, (int, string, string, string)>();
            while (true)
            {
                try
                {

                    //await Task.Delay(1000);

                    //TODO : Remove 
                    // تنظیم تعداد نمایش به 200
                    var displaySelect = _scraper.FindElementWithRetry(By.XPath("//select[contains(., '10 results')]"));
                    if (displaySelect != null)
                    {
                        var selectElement = new SelectElement(displaySelect);
                        selectElement.SelectByValue("200");
                        _scraper.FindElementWithRetry(By.XPath("//li[@data-testid='results-list-item']"));
                    }
                    //href and title and citation and journal and year
                    var hrefs = _scraper.GetElementsText(".Button-module__f8gtt.Button-module__rphhF.Button-module__VBKvn.Button-module__mf1kR.Button-module__hK_LA.Button-module__qDdAl.Button-module__rTQlw", "href");
                    var titles = _scraper.GetElementsText(".Button-module__f8gtt.Button-module__rphhF.Button-module__VBKvn.Button-module__mf1kR.Button-module__hK_LA.Button-module__qDdAl.Button-module__rTQlw");

                    var citationPath = $"//li[@data-testid='results-list-item']//div[@data-testid='count-label-and-value']//span[@data-testid='unclickable-count' or @data-testid='clickable-count']\r\n";
                    var citations = _scraper.GetElementsTextByXPath(citationPath);

                    var journalPath = $"//li[@data-testid='results-list-item']//span[a and contains(@class, 'Typography')]/a//span/span";
                    var journal = _scraper.GetElementsTextByXPath(journalPath);

                    var yearPath = $"//li[@data-testid='results-list-item']//span[a and contains(@class, 'Typography')]/span";
                    var yearText = _scraper.GetElementsTextByXPath(yearPath);
                    var year = yearText.Select(x => x?.Split(',')[1].Trim() ?? "0").ToList();

                    if (hrefs.Count == 0)
                    {
                        var notFound = _scraper.GetElementText("#warningMsgContainer > span:nth-child(2)");
                        if (!string.IsNullOrEmpty(notFound)) break;
                        continue;
                    }
                    for (int i = 0; i < Math.Min(hrefs.Count, titles.Count); i++)
                    {
                        articleUrls.TryAdd(hrefs[i], (int.Parse(string.IsNullOrEmpty(citations[i]) ? "0" : citations[i]), titles[i], journal[i], year[i]));
                    }

                    var nextButton = _scraper.FindOne("#documents-panel > div > div > div:nth-child(1) > div.Stack-module__tT3r4.Stack-module__Y4rmW.Paginator-module__ecV__.Paginator-module__CqVPc > nav > ul > li.page-item > button");
                    if (nextButton?.GetAttribute("disabled") == "true" || nextButton == null) break;

                    await _scraper.ClickElementAsync("#documents-panel > div > div > div:nth-child(1) > div.Stack-module__tT3r4.Stack-module__Y4rmW.Paginator-module__ecV__.Paginator-module__CqVPc > nav > ul > li.page-item > button");
                }
                catch (Exception ex)
                {
                    _scraper.Log($"Error in ScrapeArticleUrlsAsync loop: {ex.Message}", LogLevel.Error, "ScrapeArticleUrlsAsync_Loop", ex);
                    break;
                }
            }
            return articleUrls;
        }

        private string ChangeDate(string dateStr)
        {
            if (dateStr == "NULL") return "";
            DateTime date;
            string[] formats = { "d MMMM yyyy", "MMMM yyyy", "yyyy" };
            return DateTime.TryParseExact(dateStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date) ? date.ToString("yyyy/MM/dd") : "";
        }

        private static int ExtractYear(string input)
        {
            Match match = Regex.Match(input, @"\b\d{4}\b");
            return match.Success ? int.Parse(match.Value) : throw new ArgumentException("سال معتبر یافت نشد.");
        }

        // using Microsoft.EntityFrameworkCore; // حتما در بالای فایل وارد کن

        private async Task ScrapeArticlesAsync(Professor professor, Dictionary<string, (int Citations, string Title, string Journal, string Year)> articleUrls)
        {
            foreach (var url in articleUrls)
            {

                try
                {
                    await _scraper.OpenUrlAsync(url.Key);

                    var scraped = await BuildScrapedArticleAsync(url.Key, url.Value);


                    var existing = await FindExistingArticleAsync(scraped);

                    // ---------- 3. اگر موجود بود => آپدیت هوشمند ----------
                    if (existing != null)
                    {
                        var updated = await UpdateArticleFromScrapedAsync(existing, scraped);
                    }
                    else
                    {
                        // ---------- 4. اگر موجود نبود => افزودن به DB با بررسی وابستگی ها ----------
                        if (scraped.Journal != null)
                        {
                            var j = await GetOrCreateJournalAsync(scraped.Journal);
                            scraped.Journal = null;
                            scraped.JournalId = j.Id;
                        }

                        // برای هر CoAuthor / ArticleAuthor: سعی می‌کنیم پروفسور یا CoAuthor موجود را لینک کنیم یا رکورد جدید بسازیم
                        await AttachAuthorsForNewArticleAsync(scraped);

                        // اگر سیته‌ای از citations/funding/keywords هست، به همان شکل در scraped باقی است (بعداً هنگام AddCascade اضافه می‌شوند)
                        scraped.LastUpdate = DateTime.Now;

                        await _dbContext.Articles.AddAsync(scraped);
                        await _dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _scraper.Log($"Error scraping article {url.Key}: {ex.Message}\n{ex.StackTrace}", LogLevel.Error, "ScrapeArticles", ex);
                    try { await _dbContext.SaveChangesAsync(); }
                    catch (Exception saveEx)
                    {
                        _scraper.Log($"Error saving changes after article scrape failure for {url.Key}: {saveEx.Message}", LogLevel.Error, "ScrapeArticles_SaveAfterFailure", saveEx);
                    }
                }
            }
        }

        /// <summary>
        /// این تابع فقط وظیفه دارد که از صفحه مقاله محتوا را خوانده و یک Article (غیر متصل به DbContext) بسازد.
        /// آن بخش از کدی که الان داری برای خواندن مقادیر از صفحه را در این تابع بگذار (تقریبا همان کد فعلی).
        /// </summary>
        private async Task<Article> BuildScrapedArticleAsync(string articleUrl, (int Citations, string Title, string Journal, string Year) meta)
        {

            var title = string.Join("", _scraper.GetElementsTextByXPath("//h2[@data-testid=\"publication-titles\"]"));
            var article = new Article
            {
                TitleEn = title.IsNullOrEmpty() ? meta.Title : title,
                ScopusArticleId = articleUrl.Split("/").LastOrDefault(),
                IsScopus = true,
            };

            article.ArticleIdentifier = _scraper.ToIdentifierText(article.TitleEn);

            // journal name
            var journalName = _scraper.GetElementText("#source-preview-flyout > span");

            #region First Tab

            article.AbstractEn = _scraper.GetElementText("#document-details-abstract > div.Stack_stack__xdqq_.Stack_verticalSpacer__ejXNp > p");
            article.LastUpdate = DateTime.Now;

            #region Second Tab Impact

            // Click on the Impact tab if not already selected
            try
            {
                var impactButton = _scraper.FindElementWithRetry(By.Id("impact"), 2, 6);
                if (impactButton!.GetAttribute("aria-selected") != "true")
                {
                    impactButton.Click();
                    // Wait for the content to load (adjust wait time as needed)
                    _scraper.Wait(By.Id("publication-plumx-metrics"), 6);
                }
            }
            catch (NoSuchElementException)
            {
                _scraper.Log($"Impact button not found. URL : {articleUrl}", LogLevel.Error);
            }

            // Extract FWCI and Citation details from the top section
            var citationsDiv = _scraper.FindOne(By.CssSelector("[data-testid='citations-in-scopus']"));
            var citationCountStr = meta.Citations;

            var percentileStr = citationsDiv?.FindElement(By.CssSelector(".info-field_metaValueText__YnbWS")).Text;
            string percentileDigits = new string(percentileStr?.Where(char.IsDigit).ToArray());
            int scopusPercentileCitation = int.Parse(percentileDigits);

            var fwciDiv = _scraper.FindOne(By.CssSelector("[data-testid='fwci-in-scopus']"));
            var fwciStr = fwciDiv?.FindElement(By.CssSelector("[data-testid='unclickable-count']")).Text;
            double fwciValue = double.Parse(fwciStr ?? "0");

            // Scroll to the bottom to ensure PlumX section is loaded (optional, but helpful)
            ((IJavaScriptExecutor)_scraper.Driver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
            Thread.Sleep(200); // Wait for potential lazy loading

            // Extract PlumX metrics from the bottom section
            int? readers = null;
            int? mentions = null;
            int? patentFamilyCitations = null;
            int? policyCitations = null;
            int? citationIndexes = null;

            var plumSection = _scraper.FindOne(By.Id("publication-plumx-metrics"));
            var metricCards = plumSection?.FindElements(By.CssSelector(".Metrics_card__xAQPz"));
            if (metricCards != null)
                foreach (var card in metricCards)
                {
                    var cardDivs = card.FindElements(By.TagName("div"));
                    if (cardDivs.Count >= 2)
                    {
                        string label = cardDivs[0].Text.Trim();
                        string valueStr = cardDivs[1].Text.Trim();
                        int value;
                        if (int.TryParse(valueStr, out value))
                        {
                            switch (label)
                            {
                                case "Readers":
                                    readers = value;
                                    break;
                                case "Mentions":
                                    mentions = value;
                                    break;
                                case "Patent Family Citations": // Assuming possible label
                                    patentFamilyCitations = value;
                                    break;
                                case "Policy Citations": // Assuming possible label
                                    policyCitations = value;
                                    break;
                                case "Citation Indexes":
                                    citationIndexes = value;
                                    break;
                                    // Add more cases if other labels are expected based on PlumX categories
                            }
                        }
                    }
                }

            // Now create and add the citation object, overriding ScopusCitation if needed, or using meta.Citations
            var cit = new ScopusArticleCitation
            {
                ScopusCitation = citationCountStr, // Or meta.Citations if preferred
                Fwci = fwciValue,
                ScopusPercentileCitation = scopusPercentileCitation,
                Readers = readers,
                Mentions = mentions,
                PatentFamilyCitations = patentFamilyCitations,
                PolicyCitations = policyCitations,
                CitationIndexes = citationIndexes,
                // References could be extracted from tab if needed, e.g., from References tab text "References (52)"
                LastUpdate = DateTime.Now
            };
            article.ScopusCitations.Add(cit);

            #endregion



            try
            {

                var authorWord = _scraper.FindOne("#document-details-author-keywords > div > h3");
                if (authorWord != null)
                {
                    //authorWord.Click();
                    //int i = 1;
                    //while (true)
                    //{
                    //	var values = await _scraper.GetElementTextAsync($"#doc-details-page-container > article > div:nth-child(4) > div:nth-child(2) > section > div.DocumentDetailsMain-module__hueFY > div.Stack-module__tT3r4.Stack-module___CTfk > div > span:nth-child({i}) > span:nth-child(1) > span");
                    //	if (string.IsNullOrEmpty(values)) break;
                    //	article.Keywords.Add(new ArticleKeyword { Article = article, IsAuthorKeyword = true, Keyword = values, LastUpdate = DateTime.Now });
                    //	i++;
                    //}

                    var keyWords = (_scraper.GetElementText("#document-details-author-keywords > p > span")).Split(";").ToList();
                    foreach (var word in keyWords)
                    {
                        article.Keywords.Add(new ArticleKeyword { Article = article, IsAuthorKeyword = true, Keyword = word, LastUpdate = DateTime.Now });
                    }
                }

            }
            catch (Exception ex)
            {
                _scraper.Log($"Error Getting Keywords Url : {articleUrl}", LogLevel.Error, "BuildScrapedArticleAsync_AuthorPopup", ex);

            }
            try
            {

                var indexWord = _scraper.FindOne("#document-details-indexed-keywords > div.DocumentDetailsSections_header__vgDsD > h3");
                if (indexWord != null)
                {
                    // گرفتن همه spanهای داخل dl > dd > p
                    var spans = _scraper.GetElementsText("#document-details-indexed-keywords > div.Stack_stack__xdqq_.Stack_verticalSpacer__ejXNp > dl > dd > p > span");

                    var allKeywords = new List<string>();

                    foreach (var text in spans)
                    {
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            // جدا کردن با ; و حذف فاصله اضافی
                            var kws = text.Split(';')
                                          .Select(k => k.Trim())
                                          .Where(k => !string.IsNullOrEmpty(k));
                            allKeywords.AddRange(kws);
                        }
                    }

                    // حذف تکراری‌ها اگر لازم بود
                    allKeywords = allKeywords.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                    foreach (var word in allKeywords)
                    {
                        article.Keywords.Add(new ArticleKeyword { Article = article, IsAuthorKeyword = false, Keyword = word, LastUpdate = DateTime.Now });
                    }
                }
            }
            catch (Exception ex)
            {
                _scraper.Log($"Error Getting Keywords Url : {articleUrl}", LogLevel.Error, "BuildScrapedArticleAsync_AuthorPopup", ex);

            }
            #endregion

            var byFulltext = By.XPath("//button[contains(normalize-space(.), 'Full text')]");
            var fullButton = _scraper.FindElementWithRetry(byFulltext);
            if (fullButton != null)
            {
                _scraper.Wait(byFulltext);
                fullButton.Click();
                var publisher = By.XPath("//a[contains(normalize-space(.), 'View at Publisher')]");
                _scraper.Wait(publisher);
                article.FullTextUrlScopus = _scraper.FindOne(publisher)?.GetAttribute("href");
            }
            #region Inforamtion Detailed Modal


            try
            {
                var informationButton = _scraper.FindElementWithRetry(By.XPath("//button[contains(normalize-space(.), 'Show all information')]"), 2, 7);
                informationButton?.Click();
                if (_scraper.FindElementWithRetry(By.XPath("//div[@role='dialog' and .//h2[contains(text(), 'Detailed information')]]"), 1, 7) == null)
                {
                    informationButton = _scraper.FindElementWithRetry(By.XPath("//button[contains(normalize-space(.), 'Show all information')]"), 1, 10);
                    informationButton?.Click();
                    _scraper.Wait(By.XPath("//div[@role='dialog' and .//h2[contains(text(), 'Detailed information')]]"), 7);
                }

                int i = 1;
                while (true)
                {
                    string? value = null;
                    string? lable = null;
                    try
                    {
                        value = null;
                        lable = null;
                        lable = _scraper.GetElementTextByXPath($"(//section[@data-testid='detailed-information-bibliographic-information']//dt)[{i}]");
                        value = _scraper.GetElementTextByXPath($"(//section[@data-testid='detailed-information-bibliographic-information']//dd)[{i}]") ?? "";

                        if (lable == "")
                        {
                            break;
                        }
                        switch (lable)
                        {
                            case "Pages":
                                var pages = value.Split("-");
                                article.PageStart = int.TryParse(pages.FirstOrDefault(), out int pageStart) ? pageStart : null;
                                article.PageEnd = int.TryParse(pages.LastOrDefault(), out int pageEnd) ? pageEnd : null;
                                break;

                            case "Issue":
                                article.IssueEn = value;
                                break;

                            case "Volume":
                                article.Volume = int.TryParse(ExtractNumber(value), out int volume) ? null : volume;
                                break;

                            case "Publication year":
                                article.PublicationYear = int.Parse(value);
                                break;

                            case "Publisher":
                                if (article.Journal != null)
                                    article.Journal.Publisher = value;
                                break;

                            case "ISSN":
                                Journal? exiJournal = null;
                                if (!value.IsNullOrEmpty())
                                    exiJournal = await _dbContext.Journals
                                        .FirstOrDefaultAsync(x => x.ISSN == value || x.EISSN == value);

                                if (exiJournal == null && !journalName.IsNullOrEmpty())
                                {
                                    exiJournal = await _dbContext.Journals
                                        .FirstOrDefaultAsync(x => x.Title_EN == journalName);
                                }

                                article.Journal = exiJournal ?? new Journal { Title_EN = journalName, ISSN = value };
                                break;

                            //case "PubMed ID":
                            //	article.me
                            //	break;

                            case "Publication date":
                                article.Publication = value;
                                break;

                            case "Original language":
                                article.OriginalLanguage = value;
                                break;

                            case "DOI":
                                article.Doi = value;
                                break;

                            case "Document type":
                                article.Type = value;
                                break;

                            case "Source type":
                                article.SourceType = value;
                                break;


                            case "Open access":
                                article.OpenAccess = value;
                                break;
                        }

                        i++;
                    }
                    catch (Exception e)
                    {
                        _scraper.Log($"Error extracting bibliographic information at index {i} with label '{lable}' and value '{value}': {e.Message}", LogLevel.Warning, "BuildScrapedArticleAsync_BibliographicLoop", e);
                    }
                }
            }
            catch (Exception ex)
            {
                _scraper.Log($"Error opening or processing detailed information dialog in BuildScrapedArticleAsync: {ex.Message}", LogLevel.Error, "BuildScrapedArticleAsync_DetailedInfo", ex);
            }

            var affiliationItems = _scraper.FindMany("section[data-testid='detailed-information-affiliations'] ul.DetailedInformationFlyout_list__76Ipn > li");
            var affiliationsDict = new Dictionary<string, string>();

            bool hasSup = false;
            foreach (var affItem in affiliationItems)
            {
                var sup = _scraper.FindOneWithin(affItem, "sup");
                var affText = affItem.Text?.Trim() ?? "";

                if (sup != null && !string.IsNullOrEmpty(affText))
                {
                    var key = sup.Text.Trim();
                    var value = affText.Substring(key.Length).Trim();
                    affiliationsDict[key] = value;
                    hasSup = true;
                }
            }

            // اگر هیچ sup وجود نداشت ولی affiliationItems داشت، کل افیلیشن رو به همه نویسندگان بده
            if (!hasSup && affiliationItems.Count > 0)
            {
                // همه افیلیشن‌ها رو ترکیب کن یا اولین رو بگیر
                var defaultAffiliation = string.Join(" | ", affiliationItems.Select(a => a.Text?.Trim() ?? ""));
                affiliationsDict["ALL"] = defaultAffiliation;
            }

            var authorItems = _scraper.FindMany("section[data-testid='detailed-information-authors'] ul.DetailedInformationFlyout_list__76Ipn > li");
            int order = 1;
            foreach (var authorItem in authorItems)
            {
                var nameEl = _scraper.FindOneWithin(authorItem, "button");
                string name = nameEl?.Text.Trim() ?? "";

                var affCodesEls = _scraper.FindManyWithin(authorItem, "sup");
                var affCodes = affCodesEls.Select(a => a.Text?.Trim().Replace(",", "") ?? "").ToList();
                string affiliationFull = string.Join("; ", affCodes.Select(c => affiliationsDict.ContainsKey(c) ? affiliationsDict[c] : c));

                string email = "";
                var emailLink = _scraper.FindOneWithin(authorItem, "a[href^='mailto:']");
                if (emailLink != null)
                    email = emailLink.GetAttribute("href")?.Replace("mailto:", "").Trim() ?? "";

                // گرفتن Scopus ID از popup (اگر موجود)
                string scopusId = "";
                var authorButton = _scraper.FindOneWithin(authorItem, "button");
                if (authorButton != null)
                {
                    try
                    {


                        authorButton.Click();

                        var fullProfileLink = _scraper.Wait(By.CssSelector("a[href*='authid/detail.uri']"));
                        if (fullProfileLink != null)
                        {
                            var href = fullProfileLink.GetAttribute("href");
                            var match = System.Text.RegularExpressions.Regex.Match(href ?? "", @"\d+");
                            if (match.Success) scopusId = match.Value;
                        }
                        else
                        {
                            _scraper.Log($"Full profile link not found for author '{name}' in popup", LogLevel.Warning, "BuildScrapedArticleAsync_AuthorPopup");
                        }

                        // بستن پنجره
                        var closeBtn = _scraper.FindMany("button[data-testid='flyout-close-button']");
                        if (closeBtn != null && closeBtn.Any())
                            ((IJavaScriptExecutor)_scraper.Driver).ExecuteScript("arguments[0].click();", closeBtn.Last());
                    }
                    catch (Exception ex)
                    {
                        _scraper.Log($"Full profile link not found for author '{name}' in popup", LogLevel.Warning, "BuildScrapedArticleAsync_AuthorPopup", ex);

                    }
                }

                string lastName = "";
                string firstName = "";
                if (!string.IsNullOrEmpty(name))
                {
                    var nameParts = name.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (nameParts.Length >= 2)
                    {
                        lastName = nameParts[0].Trim();
                        firstName = nameParts[1].Trim();
                    }
                    else
                    {
                        lastName = name.Trim();
                    }
                }

                var aa = new ArticleAuthor
                {
                    Order = order,
                    LastUpdate = DateTime.Now,
                    IsCorrespondingAuthor = !string.IsNullOrEmpty(email),
                };

                aa.CoAuthor = new CoAuthor
                {
                    FirstNameEn = firstName,
                    LastNameEn = lastName,
                    Email = email,
                    ScopusId = scopusId,
                    AffiliationEn = affiliationFull ?? "",
                    LastUpdate = DateTime.Now
                };

                article.ArticleAuthors.Add(aa);
                order++;
            }
            #endregion

            return article;
        }


        /// <summary>
        /// تلاش می‌کند مقالهٔ مشابه را در DB پیدا کند با ترتیب اولویت:
        /// 1) DOI
        /// 2) ScopusArticleId / WosArticleId / IscArticleId
        /// 3) ArticleIdentifier (نرمالایز عنوان)
        /// 4) عنوان fuzzy + سال + بررسی نام ژورنال (کلمات جزئی)
        /// </summary>
        private async Task<Article?> FindExistingArticleAsync(Article scraped)
        {
            var q = _dbContext.Articles.Include(a => a.Journal).AsQueryable();

            if (!string.IsNullOrWhiteSpace(scraped.Doi))
            {
                var byDoi = await q.FirstOrDefaultAsync(a => a.Doi == scraped.Doi);
                if (byDoi != null) return byDoi;
            }

            if (!string.IsNullOrWhiteSpace(scraped.ScopusArticleId))
            {
                var byScopus = await q.FirstOrDefaultAsync(a => a.ScopusArticleId == scraped.ScopusArticleId);
                if (byScopus != null) return byScopus;
            }

            if (!string.IsNullOrWhiteSpace(scraped.ArticleIdentifier))
            {
                var byIdent = await q.FirstOrDefaultAsync(a => a.ArticleIdentifier == scraped.ArticleIdentifier);
                if (byIdent != null) return byIdent;
            }

            var journalWords = (scraped.Journal?.Title_EN ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(w => w.Trim().ToLower()).Where(w => w.Length > 2).ToArray();



            if (!string.IsNullOrWhiteSpace(scraped.TitleEn) && scraped.PublicationYear.HasValue && scraped.PublicationYear != 0)
            {
                var byFreetext = await q.FirstOrDefaultAsync(a => !string.IsNullOrEmpty(a.TitleEn) && a.TitleEn.Trim().ToLower() == scraped.TitleEn.Trim().ToLower() && a.PublicationYear == scraped.PublicationYear);
                if (byFreetext != null) return byFreetext;
            }
            return null;
        }

        /// <summary>
        /// آپدیت هوشمندِ رکورد موجود با مقادیر scraped — فقط فیلدهایی که در existing خالی هستند را پر می‌کند.
        /// همچنین ادغام لیست‌ها (keywords, topics, funding, citations, authors) را انجام می‌دهد.
        /// بازمی‌گرداند true اگر به‌روزرسانی‌ای انجام شده باشد.
        /// </summary>
        // Now create and add the citation object, overriding ScopusCitation if needed, or using meta.Citations
        public async Task<bool> UpdateArticleFromScrapedAsync(Article existing, Article scraped)
        {
            bool changed = false;

            // ساده: رشته‌ها و مقادیر nullable را فقط در صورت خالی بودن existing پر کن
            if (string.IsNullOrWhiteSpace(existing.Doi) && !string.IsNullOrWhiteSpace(scraped.Doi))
            { existing.Doi = scraped.Doi; changed = true; }

            if (string.IsNullOrWhiteSpace(existing.TitleEn) && !string.IsNullOrWhiteSpace(scraped.TitleEn))
            { existing.TitleEn = scraped.TitleEn; changed = true; }

            if (existing.Volume == null && scraped.Volume != null)
            { existing.Volume = scraped.Volume; changed = true; }

            if (existing.PageStart == null && scraped.PageStart != null)
            { existing.PageStart = scraped.PageStart; changed = true; }

            if (existing.PageEnd == null && scraped.PageEnd != null)
            { existing.PageEnd = scraped.PageEnd; changed = true; }

            if (existing.PublicationYear == null && scraped.PublicationYear != null)
            { existing.PublicationYear = scraped.PublicationYear; changed = true; }

            if (string.IsNullOrWhiteSpace(existing.AbstractEn) && !string.IsNullOrWhiteSpace(scraped.AbstractEn))
            { existing.AbstractEn = scraped.AbstractEn; changed = true; }

            if (string.IsNullOrWhiteSpace(existing.FullTextUrlScopus) && !string.IsNullOrWhiteSpace(scraped.FullTextUrlScopus))
            { existing.FullTextUrlScopus = scraped.FullTextUrlScopus; changed = true; }

            // Journal: اگر existing.JournalId خالیه و scraped یک Journal داره، تلاش کن Journal را پیدا یا ایجاد کنی و لینک بزنی
            if ((existing.JournalId == null || existing.JournalId == 0) && scraped.Journal != null)
            {
                var j = await GetOrCreateJournalAsync(scraped.Journal);
                existing.JournalId = j.Id;
                changed = true;
            }

            // Replace ScopusCitations: delete previous and add new scraped ones
            if (scraped.ScopusCitations.Any())
            {
                _dbContext.ScopusArticleCitations.RemoveRange(existing.ScopusCitations);
                existing.ScopusCitations.Clear();
                foreach (var sc in scraped.ScopusCitations)
                {
                    var newCit = new ScopusArticleCitation
                    {
                        ScopusCitation = sc.ScopusCitation,
                        Fwci = sc.Fwci,
                        ScopusPercentileCitation = sc.ScopusPercentileCitation,
                        Readers = sc.Readers,
                        Mentions = sc.Mentions,
                        PatentFamilyCitations = sc.PatentFamilyCitations,
                        PolicyCitations = sc.PolicyCitations,
                        CitationIndexes = sc.CitationIndexes,
                        LastUpdate = DateTime.Now
                    };
                    existing.ScopusCitations.Add(newCit);
                }
                changed = true;
            }

            // Merge keywords
            var keywords = new List<ArticleKeyword>();
            foreach (var kw in scraped.Keywords)
            {
                if (!existing.Keywords.Any(e => e.Keyword != null && kw.Keyword != null &&
                    string.Equals(e.Keyword.Trim(), kw.Keyword.Trim(), StringComparison.OrdinalIgnoreCase) && e.IsAuthorKeyword == kw.IsAuthorKeyword))
                {

                    keywords.Add(new ArticleKeyword { Keyword = kw.Keyword?.Trim(), IsAuthorKeyword = kw.IsAuthorKeyword, LastUpdate = DateTime.Now });
                    changed = true;
                }
            }
            existing.Keywords.AddRange(keywords);

            // Merge topics
            foreach (var tp in scraped.Topics)
            {
                if (!existing.Topics.Any(e => e.Topic != null && tp.Topic != null &&
                    string.Equals(e.Topic.Trim(), tp.Topic.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    existing.Topics.Add(new ArticleTopic { Topic = tp.Topic?.Trim(), LastUpdate = DateTime.Now });
                    changed = true;
                }
            }

            // Merge funding sponsors (بررسی بر اساس FundingLink یا OrganName)
            foreach (var fs in scraped.FundingSponsors)
            {
                if (!existing.FundingSponsors.Any(e => (!string.IsNullOrEmpty(e.FundingLink) && e.FundingLink == fs.FundingLink)
                    || (!string.IsNullOrEmpty(e.OrganName) && e.OrganName == fs.OrganName)))
                {
                    existing.FundingSponsors.Add(new FundingSponsor
                    {
                        Acronym = fs.Acronym,
                        FundingLink = fs.FundingLink,
                        FundingNumber = fs.FundingNumber,
                        OrganName = fs.OrganName,
                        FundingText = fs.FundingText,
                        LastUpdate = DateTime.Now
                    });
                    changed = true;
                }
            }

            // Merge authors: اگر نویسنده‌ای جدید در scraped هست که در existing نیست، اضافه کن.
            // برای هر ArticleAuthor در scraped: سعی کن Professor با ScopusId یا CoAuthor با ScopusId را پیدا کنی و لینک کن؛
            // سپس بررسی کن که ArticleAuthor متناظر وجود ندارد (بر اساس Order یا نام+email)
            _dbContext.ArticleAuthors.RemoveRange(existing.ArticleAuthors);
            existing.ArticleAuthors = new List<ArticleAuthor>();
            var authors = scraped.ArticleAuthors.Where(x => !x.CoAuthor?.ScopusId.IsNullOrEmpty() ?? false).ToList();
            foreach (var scrapedAA in authors)
            {
                // استخراج اطلاعات احتمالی coauthor موقت
                var scopusId = scrapedAA.CoAuthor?.ScopusId;
                var email = scrapedAA.CoAuthor?.Email?.Trim();
                var first = scrapedAA.CoAuthor?.FirstNameEn?.Trim();
                var last = scrapedAA.CoAuthor?.LastNameEn?.Trim();

                // تلاش برای پیدا کردن Professor اول
                ArticleAuthor? already = null;

                if (!string.IsNullOrEmpty(scopusId))
                {
                    var prof = await _dbContext.Professors.FirstOrDefaultAsync(p => p.ScopusID == scopusId);
                    if (prof != null)
                    {
                        already = existing.ArticleAuthors.FirstOrDefault(a => a.ProfessorId == prof.Id);
                        if (already == null)
                        {
                            var newAA = new ArticleAuthor
                            {
                                Professor = prof,
                                Article = existing,
                                Order = scrapedAA.Order,
                                LastUpdate = DateTime.Now,
                                IsCorrespondingAuthor = scrapedAA.IsCorrespondingAuthor
                            };
                            existing.ArticleAuthors.Add(newAA);
                            changed = true;
                            continue;
                        }
                    }
                }

                // اگر پروفسور پیدا نشد، تلاش برای مطابقت با CoAuthor
                CoAuthor co = null;
                if (!string.IsNullOrEmpty(scopusId))
                {
                    co = await _dbContext.ArticleCoAuthors.FirstOrDefaultAsync(c => c.ScopusId == scopusId);
                }
                if (co == null && (!string.IsNullOrEmpty(first) || !string.IsNullOrEmpty(last) || !string.IsNullOrEmpty(email)))
                {
                    co = await _dbContext.ArticleCoAuthors.FirstOrDefaultAsync(c =>
                        (!string.IsNullOrEmpty(c.Email) && c.Email == email) ||
                        (c.FirstNameEn == first && c.LastNameEn == last));
                }

                if (co == null)
                {
                    // ایجاد CoAuthor جدید (و اضافه کردن)
                    co = new CoAuthor
                    {
                        FirstNameEn = first,
                        LastNameEn = last,
                        Email = email,
                        ScopusId = scopusId,
                        AffiliationEn = scrapedAA.CoAuthor?.AffiliationEn,
                        LastUpdate = DateTime.Now
                    };
                    _dbContext.ArticleCoAuthors.Add(co);
                    await _dbContext.SaveChangesAsync(); // تا id بگیرد
                    changed = true;
                }

                // اکنون بررسی کن که آیا ArticleAuthor لینک شده به این co وجود دارد
                if (!existing.ArticleAuthors.Any(a => a.CoAuthorId == co.Id))
                {
                    existing.ArticleAuthors.Add(new ArticleAuthor
                    {
                        CoAuthor = co,
                        Article = existing,
                        Order = scrapedAA.Order,
                        LastUpdate = DateTime.Now,
                        IsCorrespondingAuthor = scrapedAA.IsCorrespondingAuthor
                    });
                    changed = true;
                }
            }

            if (changed)
            {
                existing.LastUpdate = DateTime.Now;
                try
                {
                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _scraper.Log($"Error saving updated article with ID '{existing.Id}' in UpdateArticleFromScrapedAsync: {ex.Message}", LogLevel.Error, "UpdateArticleFromScrapedAsync_SaveChanges", ex);
                }
            }

            return changed;
        }
        /// <summary>
        /// اگر Journal قبلا وجود داشت آن را برمی‌گرداند، وگرنه ایجاد می‌کند و برمی‌گرداند.
        /// تطبیق بر اساس ISSN/EISSN و سپس عنوان.
        /// </summary>
        private async Task<Journal> GetOrCreateJournalAsync(Journal scrapedJournal)
        {
            if (!string.IsNullOrWhiteSpace(scrapedJournal.ISSN))
            {
                var ex = await _dbContext.Journals.FirstOrDefaultAsync(j => j.ISSN == scrapedJournal.ISSN || j.EISSN == scrapedJournal.ISSN);
                if (ex != null) return ex;
            }

            if (!string.IsNullOrWhiteSpace(scrapedJournal.Title_EN))
            {
                var ex2 = await _dbContext.Journals.FirstOrDefaultAsync(j => j.Title_EN == scrapedJournal.Title_EN);
                if (ex2 != null) return ex2;
            }

            // اگر پیدا نشد، ایجاد کن
            var jnew = new Journal
            {
                Title_EN = scrapedJournal.Title_EN,
                ISSN = scrapedJournal.ISSN,
                Publisher = scrapedJournal.Publisher,
                LastUpdate = DateTime.Now
            };

            _dbContext.Journals.Add(jnew);
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _scraper.Log($"Error saving new journal with Title_EN '{jnew.Title_EN}' and ISSN '{jnew.ISSN}' in GetOrCreateJournalAsync: {ex.Message}", LogLevel.Error, "GetOrCreateJournalAsync_SaveChanges", ex);
            }
            return jnew;
        }

        /// <summary>
        /// هنگام افزودن مقالهٔ جدید: برای هر ArticleAuthor که داخل scraped ساخته شده،
        /// اگر Professor با ScopusId وجود دارد آن را لینک می‌کند،
        /// در غیر اینصورت CoAuthor موجود را پیدا یا ایجاد کرده و لینک می‌کند.
        /// </summary>
        private async Task AttachAuthorsForNewArticleAsync(Article scraped)
        {
            var newArticleAuthors = new List<ArticleAuthor>();

            foreach (var aa in scraped.ArticleAuthors.ToList()) // از روی کپی لیست iterate کن
            {
                var scopusId = aa.CoAuthor?.ScopusId;
                var email = aa.CoAuthor?.Email?.Trim();
                string first = aa.CoAuthor?.FirstNameEn?.Trim() ?? "";
                string last = aa.CoAuthor?.LastNameEn?.Trim() ?? "";

                // تلاش برای پیدا کردن Professor
                Professor? prof = null;
                if (!string.IsNullOrEmpty(scopusId))
                    prof = await _dbContext.Professors.FirstOrDefaultAsync(p => p.ScopusID == scopusId);

                CoAuthor? co = null;

                if (prof != null)
                {
                    // اگر پروفسور آدرس ایمیل نداره و scraped ایمیل داره، آپدیت کن
                    if (string.IsNullOrEmpty(prof.PersonalEmail) && !string.IsNullOrEmpty(email))
                    {
                        prof.PersonalEmail = email;
                        _dbContext.Professors.Update(prof);
                    }

                    newArticleAuthors.Add(new ArticleAuthor
                    {
                        Professor = prof,
                        Order = aa.Order,
                        LastUpdate = DateTime.Now,
                        IsCorrespondingAuthor = aa.IsCorrespondingAuthor
                    });
                }
                else
                {
                    // اگر پروفسور نیست، جستجو/ایجاد CoAuthor
                    if (!string.IsNullOrEmpty(scopusId))
                        co = await _dbContext.ArticleCoAuthors.FirstOrDefaultAsync(c => c.ScopusId == scopusId);

                    if (co == null && (!string.IsNullOrEmpty(email) || (!string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(last))))
                    {
                        co = await _dbContext.ArticleCoAuthors.FirstOrDefaultAsync(c =>
                            (!string.IsNullOrEmpty(c.Email) && c.Email == email) ||
                            (c.FirstNameEn == first && c.LastNameEn == last));
                    }

                    if (co == null)
                    {
                        co = aa.CoAuthor;
                        _dbContext.ArticleCoAuthors.Add(co);

                    }
                    else
                    {
                        // اگر ایمیل جدید هست و قبلاً نداشته، پر کن
                        if (string.IsNullOrEmpty(co.Email) && !string.IsNullOrEmpty(email))
                            co.Email = email;
                        if (string.IsNullOrEmpty(co.ScopusId) && !string.IsNullOrEmpty(scopusId))
                            co.ScopusId = scopusId;
                        if (string.IsNullOrEmpty(co.LastNameEn))
                            co.LastNameEn = last;
                        if (string.IsNullOrEmpty(co.FirstNameEn))
                            co.FirstNameEn = first;
                        _dbContext.ArticleCoAuthors.Update(co);
                    }

                    newArticleAuthors.Add(new ArticleAuthor
                    {
                        CoAuthor = co,
                        Order = aa.Order,
                        LastUpdate = DateTime.Now,
                        IsCorrespondingAuthor = aa.IsCorrespondingAuthor
                    });
                }

                // اضافه کردن افیلیشن برای هر شخص
                // فرض بر این است که CoAuthor دارای پراپرتی‌های AffiliationEn و AffiliationFa است
                if (aa.CoAuthor != null)
                {
                    if (!string.IsNullOrEmpty(aa.CoAuthor.AffiliationEn))
                    {
                        var affEn = new ArticleProfessorAffiliation
                        {
                            Affiliation = aa.CoAuthor.AffiliationEn.Trim(),
                            IsPersian = false,
                            LastUpdate = DateTime.Now,
                            Articles = scraped,
                            WorkflowUserId = null
                        };
                        if (prof != null)
                        {
                            affEn.professorId = prof.Id;
                            affEn.Professor = prof;
                            affEn.CoAuthorId = 0; // تنظیم به 0 چون nullable نیست اما در دیتابیس ممکن است اجازه دهد
                        }
                        else
                        {
                            affEn.CoAuthorId = co.Id;
                            affEn.CoAuthor = co;
                            affEn.professorId = 0; // تنظیم به 0 چون nullable نیست اما در دیتابیس ممکن است اجازه دهد
                        }
                        _dbContext.ArticleProfessorAffiliations.Add(affEn);
                    }

                    if (!string.IsNullOrEmpty(aa.CoAuthor.AffiliationFa))
                    {
                        var affFa = new ArticleProfessorAffiliation
                        {
                            Affiliation = aa.CoAuthor.AffiliationFa.Trim(),
                            IsPersian = true,
                            LastUpdate = DateTime.Now,
                            Articles = scraped,
                            WorkflowUserId = null
                        };
                        if (prof != null)
                        {
                            affFa.professorId = prof.Id;
                            affFa.Professor = prof;
                            affFa.CoAuthorId = 0;
                        }
                        else
                        {
                            affFa.CoAuthorId = co.Id;
                            affFa.CoAuthor = co;
                            affFa.professorId = 0;
                        }
                        _dbContext.ArticleProfessorAffiliations.Add(affFa);
                    }
                }
            }

            // جایگزین کردن لیست ArticleAuthors در scraped با لیست آماده برای persist
            scraped.ArticleAuthors = newArticleAuthors;
            try
            {
                await _dbContext.SaveChangesAsync(); // تا id بگیرد
            }
            catch (Exception ex)
            {
                _scraper.Log($"Error saving authors attachments in AttachAuthorsForNewArticleAsync: {ex.Message}", LogLevel.Error, "AttachAuthorsForNewArticleAsync_SaveChanges", ex);
            }
        }

        #region comment
        //try
        //{
        //	var volume = await _scraper.GetElementTextAsync("#doc-details-page-container > article > div:nth-child(1) > div > div > div > span:nth-child(2)");
        //	if (!string.IsNullOrEmpty(volume) && volume.Contains("Volume"))
        //	{
        //		article.Volume = int.Parse(volume.Split("Volume")[1].Split(",")[0]);
        //		if (volume.Contains("Issue")) article.Issue = volume.Split("Issue")[1].Split(",")[0];
        //		if (volume.Contains("Page"))
        //		{
        //			var pages = volume.Split("Pages")[1].Split(",")[0].Split("-");
        //			article.PageStart = int.Parse(pages[0]);
        //			article.PageEnd = int.Parse(pages[1]);
        //		}
        //		article.Publication = ChangeDate(await _scraper.GetElementTextAsync("#doc-details-page-container > article > div:nth-child(1) > div > div > div > span:nth-child(3)"));
        //		article.PublicationYear = string.IsNullOrEmpty(article.Publication) ? 0 : ExtractYear(article.Publication);
        //	}
        //	else
        //	{
        //		volume = await _scraper.GetElementTextAsync("#doc-details-page-container > article > div:nth-child(1) > div > div > div > span:nth-child(3)");
        //		if (!string.IsNullOrEmpty(volume) && volume.Contains("Volume")) article.Volume = int.Parse(volume.Split("Volume")[1].Split(",")[0]);
        //		if (volume.Contains("Issue")) article.Issue = volume.Split("Issue")[1].Split(",")[0];
        //		if (volume.Contains("Page"))
        //		{
        //			var pages = volume.Split("Pages")[1].Split(",")[0].Split("-");
        //			article.PageStart = int.Parse(pages[0]);
        //			article.PageEnd = int.Parse(pages[1]);
        //		}
        //		article.OpenAccess = await _scraper.GetElementTextAsync("#doc-details-page-container > article > div:nth-child(1) > div > div > div > em");
        //		article.Publication = ChangeDate(await _scraper.GetElementTextAsync("#doc-details-page-container > article > div:nth-child(1) > div > div > div > span:nth-child(4)"));
        //		article.PublicationYear = string.IsNullOrEmpty(article.Publication) ? 0 : ExtractYear(article.Publication);
        //	}
        //}
        //catch { }

        //var sourceRows = await _scraper.FindManyAsync("#source-info-aside > div > div > div > dl");
        //var additionalRows = await _scraper.FindManyAsync("#show-additional-source-info > dl");
        //var fieldValues = new Dictionary<string, string>();

        //foreach (var row in sourceRows.Concat(additionalRows))
        //{
        //	try
        //	{
        //		var label = (await _scraper.GetElementTextAsync(row, "dt")).Trim().ToLower();
        //		var value = (await _scraper.GetElementTextAsync(row, "dd")).Trim();
        //		if (!string.IsNullOrEmpty(label) && !string.IsNullOrEmpty(value))
        //			fieldValues[label] = value;
        //	}
        //	catch (Exception ex)
        //	{
        //		_scraper.LogAsync($"Error reading source row: {ex.Message}", LogLevel.Warning, "ScrapeArticles");
        //	}
        //}

        //foreach (var kvp in fieldValues)
        //{
        //	switch (kvp.Key)
        //	{
        //		case "document type": article.Type = kvp.Value.Contains("•") ? kvp.Value.Split("•")[0] : kvp.Value; break;
        //		case "source type": article.SourceType = kvp.Value; break;
        //		case "issn":
        //			var exJournal = await _dbContext.Journals.FirstOrDefaultAsync(x => x.Title == journalName && x.ISSN == kvp.Value.Replace("-", ""));
        //			article.Journal = exJournal ?? new Journal { Title = journalName, ISSN = kvp.Value.Replace("-", "") };
        //			break;
        //		case "doi": article.Doi = kvp.Value; break;
        //		case "publisher": if (article.Journal != null) article.Journal.Publisher = kvp.Value; break;
        //		case "original language": article.OriginalLanguage = kvp.Value; break;
        //	}
        //}
        #endregion

        private string ExtractNumber(string url)
        {
            Match match = Regex.Match(url, @"\d+");
            return match.Success ? match.Value : "";
        }

        public static bool IsStringMostlyContained(string string1, string string2)
        {
            string1 = string1.Trim().ToLower().Split(";").FirstOrDefault().Split(",").FirstOrDefault();
            string2 = string2.Trim().ToLower().Split(",").FirstOrDefault();
            if (string1.Length == 0 || string2.Length == 0) return false;
            int matchingCharacters = string1.Count(c => string2.Contains(c));
            double matchPercentage = (double)matchingCharacters / string1.Length * 100;
            return matchPercentage >= 90;
        }

        private async Task UpdateExistingArticlesAsync(Professor professor, Dictionary<string, (int Citations, string Title, string Journal, string Year)> articleUrls)
        {

            foreach (var articleUrl in articleUrls.ToList())
            {

                //is article in db
                var article = await _dbContext.Articles.FirstOrDefaultAsync(x => x.ScopusArticleId == ExtractNumber(articleUrl.Key));

                //if cant find them with scopus key (اگر مقاله قبلا در اسکوپوس بوده باشد)
                if (article == null)
                {
                    var journalWords = articleUrl.Value.Journal.Split(' ');

                    //find with more article info
                    if (journalWords != null && journalWords.Any())
                        article = await _dbContext.Articles.Include(x => x.ArticleAuthors)
                            .FirstOrDefaultAsync(x => x.PublicationYear.ToString() == articleUrl.Value.Year
                            && (x.TitleEn != null && x.TitleEn.Trim().ToLower() == articleUrl.Value.Title.Trim().ToLower())
                            && journalWords.All(word => x.Journal!= null && x.Journal.Title_EN != null && x.Journal.Title_EN.ToLower().Contains(word.ToLower())));
                }

                if (article != null)
                {

                    //keyword(remove)      author(remove)        topic(remove)          journal(find)
                    {
                        var removeArticle = _dbContext.Articles
                        .Include(x => x.Keywords)
                        .Include(x => x.Topics)
                        .Include(x => x.ArticleAuthors)
                        .Include(x => x.ScopusCitations)
                        .FirstOrDefault(x => x.Id == article.Id);

                        _dbContext.ScopusArticleCitations.RemoveRange(removeArticle.ScopusCitations);
                        if ((!article.IsIsc ?? true))
                        {
                            _dbContext.ArticleAuthors.RemoveRange(removeArticle.ArticleAuthors);
                            _dbContext.ArticleKeywords.RemoveRange(removeArticle.Keywords);
                            _dbContext.Articles.Remove(removeArticle);
                        }
                    }

                    //--------------------------
                    // var citation = new ScopusArticleCitation { ScopusCitation = articleUrl.Value.Citations, LastUpdate = DateTime.Now };

                    // //اگر سایتیشن تغییر نکرده باشد
                    // if (article.ScopusCitations.Any(x => x.ScopusCitation == citation.ScopusCitation))
                    // {
                    // 	var cit = article.ScopusCitations.First(x => x.ScopusCitation == citation.ScopusCitation);
                    // 	cit.LastUpdate = DateTime.Now;
                    // }
                    // else
                    // {
                    // 	article.ScopusCitations.Add(citation);
                    // }

                    // //اگر این فرد در مقاله حضور داشته باشد
                    // if (!article.ArticleAuthors.Any(x => x.ProfessorId == professor.Id))
                    // {
                    // 	professor.ArticleAuthors.Add(new ArticleAuthor { Professor = professor, Article = article, LastUpdate = DateTime.Now });
                    // }

                    // articleUrls.Remove(articleUrl.Key);
                    //----------------------------
                }
            }
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _scraper.Log($"Error saving changes in UpdateExistingArticlesAsync for professor with ScopusID '{professor.ScopusID}': {ex.Message}", LogLevel.Error, "UpdateExistingArticlesAsync_SaveChanges", ex);
            }
        }
    }
    #endregion

    #region WOS Scraper
    public class WOSScraper
    {
        private readonly WebScraper _scraper;
        private readonly DynamicDbContext _dbContext;
        private string _wosProfileUrlBase = "https://www-webofscience-com.access.semantak.com/wos/author/record/";

        public WOSScraper(DynamicDbContext dbContext, WebScraper webScraper)
        {
            _scraper = webScraper;
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task ExecuteAsync(Professor professor = default)
        {
            try
            {
                await _scraper.OpenUrlAsync($"{_wosProfileUrlBase}{professor.WebOfScienceID}", "body > app-wos > main > div > div > div.holder.new-wos-style > div > div > div.held > app-input-route > app-author-page > div > div > div.author-details-section > app-author-record-header > div > div > div.author-data-column > mat-card-title > h1");
                await Task.Delay(1000);

                var articleUrls = await ScrapeArticleUrlsAsync();
                await ScrapeProfileAsync(professor);
                await UpdateExistingArticlesAsync(professor, articleUrls);
                await ScrapeArticlesAsync(professor, articleUrls);
            }
            catch (Exception ex)
            {
                _scraper.Log($"Error scraping profile {professor.ScopusID}: {ex.Message}", LogLevel.Error, "Execute");
            }
        }

        private static string ExtractLastNumber(string input)
        {
            Match match = Regex.Match(input, @"\d+$");
            return match.Success ? match.Value : string.Empty;
        }

        private async Task<Dictionary<string, (int Citations, string Title, string Journal, string Year)>> ScrapeArticleUrlsAsync()
        {
            var articleUrls = new Dictionary<string, (int, string, string, string)>();
            while (true)
            {
                try
                {
                    //await Task.Delay(1000);
                    _scraper.FindElementWithRetry(By.CssSelector(".title.title-link.font-size-18.ng-star-inserted"));
                    var hrefs = _scraper.GetElementsText(".title.title-link.font-size-18.ng-star-inserted", "href");
                    var titles = _scraper.GetElementsText(".title.title-link.font-size-18.ng-star-inserted");
                    var citations = new List<string>();
                    var journal = new List<string>();
                    var year = new List<string>();

                    if (hrefs.Count == 0) continue;

                    for (int i = 0; i < titles.Count; i++)
                    {
                        citations.Add(_scraper.GetElementText($"#mat-tab-content-0-0 > div > app-publications-tab > div > app-publications-placeholder > app-author-document-summary > div > div > div > div > div:nth-child(2) > app-records-list > app-record:nth-child({i + 1}) > div > div > div.stats-container > div > div.stats-section-section > div.no-bottom-border.citations.ng-star-inserted > a"));
                        journal.Add(_scraper.GetElementText($"#mat-tab-content-0-0 > div > app-publications-tab > div > app-publications-placeholder > app-author-document-summary > div > div > div > div > div:nth-child(2) > app-records-list > app-record:nth-child({i + 1}) > div > div > div.data-section > div:nth-child(3) > div.jcr-and-pub-info-section > app-jcr-sidenav > mat-sidenav-container > mat-sidenav-content > span > a > span"));
                        year.Add(_scraper.GetElementText($"#mat-tab-content-0-0 > div > app-publications-tab > div > app-publications-placeholder > app-author-document-summary > div > div > div > div > div:nth-child(2) > app-records-list > app-record:nth-child({i + 1}) > div > div > div.data-section > div:nth-child(3) > div.jcr-and-pub-info-section > span.value.ng-star-inserted"));
                    }

                    for (int i = 0; i < Math.Min(hrefs.Count, titles.Count); i++)
                    {
                        articleUrls.TryAdd(hrefs[i], (int.Parse(string.IsNullOrEmpty(citations[i]) ? "0" : citations[i]), titles[i], journal[i], year[i]));
                    }

                    var nextButton = _scraper.FindOne("#mat-tab-content-0-0 > div > app-publications-tab > div > app-publications-placeholder > app-author-document-summary > div > div > div > div > div:nth-child(2) > app-page-controls:nth-child(5) > div > form > div > button:nth-child(4)");
                    if (nextButton?.GetAttribute("disabled") == "true" || nextButton == null) break;

                    await _scraper.ClickElementAsync("#mat-tab-content-0-0 > div > app-publications-tab > div > app-publications-placeholder > app-author-document-summary > div > div > div > div > div:nth-child(2) > app-page-controls:nth-child(5) > div > form > div > button:nth-child(4)");
                }
                catch { break; }
            }
            return articleUrls;
        }

        private async Task UpdateExistingArticlesAsync(Professor professor, Dictionary<string, (int Citations, string Title, string Journal, string Year)> articleUrls)
        {

            foreach (var articleUrl in articleUrls.ToList())
            {
                var article = await _dbContext.Articles.FirstOrDefaultAsync(x => x.WosArticleId == ExtractLastNumber(articleUrl.Key));
                if (article == null)
                {
                    var journalWords = articleUrl.Value.Journal.Split(' ');
                    article = await _dbContext.Articles.Include(x => x.ArticleAuthors)
                        .Where(x => x.Publication == articleUrl.Value.Year
                        && (EF.Functions.FreeText(x.TitleEn, articleUrl.Value.Title) || EF.Functions.FreeText(x.TitleFa, articleUrl.Value.Title))
                        && journalWords.All(word => x.Journal.Title_EN.ToLower().Contains(word.ToLower())))
                        .FirstOrDefaultAsync();
                }

                if (article != null)
                {
                    var citation = new ScopusArticleCitation { ScopusCitation = articleUrl.Value.Citations, LastUpdate = DateTime.Now };
                    article.WosArticleId = articleUrl.Key;
                    if (article.ScopusCitations.Any(x => x.ScopusCitation == citation.ScopusCitation))
                    {
                        var cit = article.ScopusCitations.First(x => x.ScopusCitation == citation.ScopusCitation);
                        cit.LastUpdate = DateTime.Now;
                    }
                    else
                    {
                        article.ScopusCitations.Add(citation);
                    }

                    if (!article.ArticleAuthors.Any(x => x.ProfessorId == professor.Id))
                    {
                        professor.ArticleAuthors.Add(new ArticleAuthor { Professor = professor, Article = article, LastUpdate = DateTime.Now });
                    }
                    articleUrls.Remove(articleUrl.Key);
                }
            }
            await _dbContext.SaveChangesAsync();
        }

        private async Task ScrapeProfileAsync(Professor professor)
        {
            await Task.Delay(new Random().Next(1000, 2000));
            var wosProf = new WOSProfile();

            var summaryValues = _scraper.GetElementsText(".summary-count");
            var summaryLabels = _scraper.GetElementsText(".summary-label");

            for (int i = 0; i < summaryValues.Count; i++)
            {
                switch (summaryLabels[i])
                {
                    case "Total documents": wosProf.TotalDocuments = int.Parse(summaryValues[i]); break;
                    case "Publications indexed in Web of Science": wosProf.PublicationsIndexedInWebOfScience = int.Parse(summaryValues[i]); break;
                    case "Web of Science Core Collection publications": wosProf.WebOfScienceCoreCollectionPublications = int.Parse(summaryValues[i]); break;
                    case "Preprints": wosProf.Preprints = int.Parse(summaryValues[i]); break;
                    case "Dissertations or Theses": wosProf.DissertationsOrTheses = int.Parse(summaryValues[i]); break;
                    case "Non-indexed publications": wosProf.NonIndexedPublications = int.Parse(summaryValues[i]); break;
                    case "Verified peer reviews": wosProf.VerifiedPeerReviews = int.Parse(summaryValues[i]); break;
                    case "Verified editor records": wosProf.VerifiedEditorRecords = int.Parse(summaryValues[i]); break;
                    case "Awarded grants": wosProf.AwardedGrants = int.Parse(summaryValues[i]); break;
                }
            }

            summaryValues = _scraper.GetElementsText(".wat-author-metric");
            summaryLabels = _scraper.GetElementsText(".wat-author-metric-descriptor");

            for (int i = 0; i < summaryLabels.Count; i++)
            {
                switch (summaryLabels[i])
                {
                    case "Citing Policy Documents": wosProf.CitingPolicyDocuments = int.Parse(summaryValues[i]); break;
                    case "Sum of Times Cited by Policy": wosProf.SumOfTimesCitedByPolicy = int.Parse(summaryValues[i]); break;
                    case "Citing Patents": wosProf.CitingPatents = int.Parse(summaryValues[i]); break;
                    case "Sum of Times Cited by Patents": wosProf.SumOfTimesCitedByPatents = int.Parse(summaryValues[i]); break;
                    case "Citing Articles": wosProf.CitingArticles = int.Parse(summaryValues[i]); break;
                    case "Sum of Times Cited": wosProf.SumOfTimesCited = int.Parse(summaryValues[i]); break;
                    case "Publications": wosProf.Publications = int.Parse(summaryValues[i]); break;
                    case "H-Index": wosProf.HIndex = int.Parse(summaryValues[i]); break;
                }
            }

            wosProf.Lastupdate = DateTime.Now;
            wosProf.Professor = professor;
            _dbContext.WOSProfiles.Add(wosProf);
            await _dbContext.SaveChangesAsync();
        }

        private async Task ScrapeArticlesAsync(Professor professor, Dictionary<string, (int Citations, string Title, string Journal, string Year)> articleUrls)
        {
            foreach (var url in articleUrls)
            {

                try
                {
                    await _scraper.OpenUrlAsync(url.Key, "#FullRTa-fullRecordtitle-0");
                    var article = new Article();
                    var title = _scraper.GetElementText("#FullRTa-fullRecordtitle-0");
                    if (!string.IsNullOrEmpty(title))
                    {
                        article.TitleEn = title;
                        var doi = (_scraper.GetElementText("#FullRTa-DOI")).Split("/").FirstOrDefault();
                        if (!string.IsNullOrEmpty(doi)) article.Doi = doi;

                        var publication = _scraper.GetElementText("#FullRTa-indexedDate");
                        if (!string.IsNullOrEmpty(publication))
                        {
                            article.Publication = publication;
                            article.PublicationYear = ExtractYear(publication);
                        }

                        var type = _scraper.GetElementText("#FullRTa-doctype-0");
                        if (!string.IsNullOrEmpty(type)) article.Type = type.Split(";").FirstOrDefault();

                        var abstracts = _scraper.GetElementText("#FullRTa-abstract-basic > p");
                        if (!string.IsNullOrEmpty(abstracts)) article.AbstractEn = abstracts;

                        int index = 0;
                        while (true)
                        {
                            var keyWordValue = _scraper.GetElementText($"#FRkeywordsTa-authorKeywordLink-{index} > span");
                            if (string.IsNullOrEmpty(keyWordValue))
                            {
                                if (index < 3) { index++; continue; }
                                break;
                            }
                            article.Keywords.Add(new ArticleKeyword { Article = article, IsAuthorKeyword = true, Keyword = keyWordValue, LastUpdate = DateTime.Now });
                            index++;
                        }

                        index = 0;
                        while (true)
                        {
                            var keyWordValue = _scraper.GetElementText($"#FRkeywordsTa-keyWordsPlusLink-{index} > span");
                            if (string.IsNullOrEmpty(keyWordValue))
                            {
                                if (index < 3) { index++; continue; }
                                break;
                            }
                            article.Keywords.Add(new ArticleKeyword { Article = article, IsAuthorKeyword = false, Keyword = keyWordValue, LastUpdate = DateTime.Now });
                            index++;
                        }

                        var foundingSponser = _scraper.FindOne("#FundingTa-fundAck");
                        if (foundingSponser != null)
                        {
                            foundingSponser.Click();
                            await Task.Delay(200);
                            int i = 0;
                            while (true)
                            {
                                var newSponser = new FundingSponsor
                                {
                                    FundingText = _scraper.GetElementText("#snMainArticle > app-full-record-funding > div:nth-child(1) > div.value.ng-star-inserted > p"),
                                    LastUpdate = DateTime.Now,
                                    OrganName = _scraper.GetElementText($"#FundingTa-fundingShowHide-{i}-agencyName")
                                };
                                if (string.IsNullOrEmpty(newSponser.OrganName)) break;
                                newSponser.Acronym = _scraper.GetElementText($"#FundingTa-fundingGrants{i}");
                                article.FundingSponsors.Add(newSponser);
                                i++;
                            }

                            var moreDetail = _scraper.FindOne("#HiddenSecTa-showMoreDataButton");
                            if (moreDetail != null)
                            {
                                moreDetail.Click();
                                await Task.Delay(200);
                                article.OriginalLanguage = _scraper.GetElementText("#HiddenSecTa-language-0");
                                var journalISSN = _scraper.GetElementText("#HiddenSecTa-ISSN");
                                if (!string.IsNullOrEmpty(journalISSN))
                                {
                                    article.Journal = await _dbContext.Journals.AnyAsync(x => x.ISSN == journalISSN)
                                        ? await _dbContext.Journals.FirstOrDefaultAsync(x => x.ISSN == journalISSN)
                                        : new Journal { ISSN = journalISSN, EISSN = _scraper.GetElementText("#HiddenSecTa-EISSN") };
                                }
                            }

                            index = 0;
                            var authors = new List<string>();
                            while (true)
                            {
                                var linke = _scraper.GetElementsText($"#SumAuthTa-DisplayName-author-en-{index}", "href");
                                if (!linke.Any())
                                {
                                    if (index < 3) { index++; continue; }
                                    break;
                                }
                                authors.Add(linke[0]);
                                index++;
                            }

                            foreach (var author in authors)
                            {
                                await _scraper.OpenUrlAsync(author, ".title.title-link.font-size-18.ng-star-inserted");
                                await Task.Delay(1000);
                                var authorId = _scraper.GetElementText("body > app-wos > main > div > div > div.holder.new-wos-style > div > div > div.held > app-input-route > app-author-page > div > div > div.author-details-section > app-author-record-header > div > app-author-details > div > div > div > span:nth-child(3)");

                                if (await _dbContext.Professors.AnyAsync(x => x.WebOfScienceID == authorId))
                                {
                                    var prof = await _dbContext.Professors.FirstOrDefaultAsync(x => x.WebOfScienceID == authorId);
                                    article.ArticleAuthors.Add(new ArticleAuthor { LastUpdate = DateTime.Now, Article = article, Professor = prof });
                                }
                                else
                                {
                                    CoAuthor coAuthor;
                                    if (await _dbContext.ArticleCoAuthors.AnyAsync(x => x.WebOfScienceID == authorId))
                                    {
                                        coAuthor = await _dbContext.ArticleCoAuthors.FirstOrDefaultAsync(x => x.WebOfScienceID == authorId);
                                    }
                                    else
                                    {
                                        coAuthor = new CoAuthor
                                        {
                                            WebOfScienceID = authorId,
                                            LastUpdate = DateTime.Now,
                                            Name = _scraper.GetElementText("body > app-wos > main > div > div > div.holder.new-wos-style > div > div > div.held > app-input-route > app-author-page > div > div > div.author-details-section > app-author-record-header > div > div > div.author-data-column > mat-card-title > h1"),
                                            University = _scraper.GetElementText("body > app-wos > main > div > div > div.holder.new-wos-style > div > div > div.held > app-input-route > app-author-page > div > div > div.author-details-section > app-author-record-header > div > div > div.author-data-column > mat-card-content > div > span > span")
                                        };
                                    }
                                    article.ArticleAuthors.Add(new ArticleAuthor { LastUpdate = DateTime.Now, Article = article, CoAuthor = coAuthor });
                                }
                            }
                        }

                        article.LastUpdate = DateTime.Now;
                        _dbContext.Articles.Add(article);
                        await _dbContext.SaveChangesAsync();
                    }
                }
                catch { }
            }
        }

        private static int ExtractYear(string input)
        {
            Match match = Regex.Match(input, @"\b\d{4}\b");
            return match.Success ? int.Parse(match.Value) : throw new ArgumentException("سال معتبر یافت نشد.");
        }

        public async Task<bool> ClickScopusLinkAsync()
        {
            try
            {
                await _scraper.OpenUrlAsync("https://login.access.semantak.com/menu", "#catdiv > p > b");
                var logoLinks = _scraper.FindMany("div#dbs a.logo");
                var scopusLink = logoLinks?.FirstOrDefault(link => link.GetAttribute("onclick")?.Contains("open_wos.php") == true || link.GetAttribute("title").Contains("Web")) ?? throw new InvalidOperationException("Scopus link not found");

                scopusLink.Click();
                await Task.Delay(3000);

                var windowHandles = _scraper.Driver.WindowHandles;
                if (windowHandles.Count > 1)
                {
                    await _scraper.SwitchTabAsync(windowHandles.Count - 1);
                }
                else
                {
                    throw new InvalidOperationException("New tab for Scopus not opened");
                }
                return true;
            }
            catch (Exception ex)
            {
                _scraper.Log($"Error clicking Scopus link: {ex.Message}", LogLevel.Error, "ClickScopusLink");
                return false;
            }
        }
    }
    #endregion

    #region Journal Scrape.
    public class JournalScopusScraper
    {
        private readonly DynamicDbContext _dbContext;

        public JournalScopusScraper(DynamicDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<List<GroupedJournalData>> ExecuteScopusAsync()
        {
            const int batchSize = 1000;
            try
            {
                var folderPath = @"D:\journals";
                var allRecords = new ConcurrentBag<Dictionary<string, object>>();
                var badDataRecords = new ConcurrentBag<string>();

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ";",
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    Quote = '"',
                    Escape = '\\',
                    BadDataFound = context => { }
                };

                // خواندن موازی فایل‌ها
                await Parallel.ForEachAsync(
                    Directory.GetFiles(folderPath, "*.csv"),
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    async (filePath, token) =>
                    {
                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        if (!int.TryParse(fileName.Split("scimagojr ").LastOrDefault()?.Trim(), out int year))
                            return;

                        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                        using var bufferedStream = new BufferedStream(fs, 8192);
                        using var reader = new StreamReader(bufferedStream);
                        using var csv = new CsvReader(reader, config);

                        csv.Context.TypeConverterCache.AddConverter<List<string>>(new StringListConverter());
                        csv.Context.TypeConverterCache.AddConverter<Dictionary<string, string>>(new CoverageConverter());
                        csv.Context.TypeConverterCache.AddConverter<List<Dictionary<string, string>>>(new CategoryListConverter());

                        await csv.ReadAsync();
                        csv.ReadHeader();
                        var headers = csv.HeaderRecord;

                        while (await csv.ReadAsync())
                        {
                            var record = new Dictionary<string, object>();
                            foreach (var header in headers)
                            {
                                try
                                {
                                    record[header] = header switch
                                    {
                                        "Categories" => csv.GetField<List<Dictionary<string, string>>>(header),
                                        "Coverage" => csv.GetField<Dictionary<string, string>>(header),
                                        "Areas" => csv.GetField<List<string>>(header),
                                        _ => csv.GetField(header)
                                    };
                                }
                                catch (CsvHelper.TypeConversion.TypeConverterException ex)
                                {
                                    badDataRecords.Add($"Type conversion error in file {fileName}, row {csv.Parser.Row}, column {header}: {ex.Message}");
                                    record[header] = null;
                                }
                            }
                            record["Year"] = year;
                            allRecords.Add(record);
                        }
                    });

                // Pre-load categories for better performance
                var categoryDict = await _dbContext.JournalCategories
                    .ToDictionaryAsync(x => x.Name.Trim());

                var groupedRecords = allRecords
                    .AsParallel()
                    .GroupBy(r => r["Sourceid"]?.ToString())
                    .Select(g => new GroupedJournalData
                    {
                        Sourceid = g.Key,
                        Title = g.First().GetValueOrDefault("Title")?.ToString(),
                        Type = g.First().GetValueOrDefault("Type")?.ToString(),
                        ISSN = g.First().GetValueOrDefault("ISSN")?.ToString(),
                        Publisher = g.First().GetValueOrDefault("Publisher")?.ToString(),
                        Country = g.First().GetValueOrDefault("Country")?.ToString(),
                        Region = g.First().GetValueOrDefault("Region")?.ToString(),
                        CoverageStartYear = g.First().GetValueOrDefault("CoverageStartYear")?.ToString(),
                        CoverageEndYear = g.First().GetValueOrDefault("CoverageEndYear")?.ToString(),
                        YearlyData = g.ToDictionary(
                            r => (int)r["Year"],
                            r => r.Where(kvp => kvp.Key != "Sourceid" && kvp.Key != "Title" &&
                                              kvp.Key != "Type" && kvp.Key != "ISSN")
                                  .ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
                    })
                    .ToList();

                var journalsToAdd = new List<Journal>();
                var journalYearsToAdd = new List<JournalYear>();
                var detailsToAdd = new List<JournalDetailScopus>();
                var qurtilesToAdd = new List<JournalQurtile>();
                var relationsToAdd = new List<JournalCategory>();

                foreach (var record in groupedRecords)
                {
                    try
                    {
                        var journal = new Journal
                        {
                            Sourceid = record.Sourceid,
                            Title_EN = record.Title,
                            Type = record.Type,
                            ISSN = record.ISSN,
                            Publisher = record.Publisher,
                            Country = record.Country,
                            Region = record.Region,
                            CoverageStartYear = record.CoverageStartYear,
                            CoverageEndYear = record.CoverageEndYear
                        };

                        journalsToAdd.Add(journal);

                        foreach (var yearData in record.YearlyData)
                        {
                            var yearDict = yearData.Value;
                            var year = new JournalYear
                            {
                                Journal = journal,
                                Year = yearData.Key,
                            };
                            journalYearsToAdd.Add(year);
                            try
                            {

                                var newDetail = new JournalDetailScopus
                                {
                                    SJR = yearDict.GetValueOrDefault("SJR") != null &&
                                          !string.IsNullOrWhiteSpace(yearDict["SJR"]?.ToString()) &&
                                          double.TryParse(yearDict["SJR"].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double sjr) ?
                                          sjr : null,

                                    SJRBestQuartile = yearDict.GetValueOrDefault("SJR Best Quartile")?.ToString(),

                                    HIndex = yearDict.GetValueOrDefault("H index") != null &&
                                             !string.IsNullOrWhiteSpace(yearDict["H index"]?.ToString()) &&
                                             int.TryParse(yearDict["H index"].ToString(), out int hIndex) ?
                                             hIndex : null,

                                    TotalRefs = yearDict.GetValueOrDefault("Total Refs.") != null &&
                                                !string.IsNullOrWhiteSpace(yearDict["Total Refs."]?.ToString()) &&
                                                int.TryParse(yearDict["Total Refs."].ToString(), out int totalRefs) ?
                                                totalRefs : null,

                                    RefPerDoc = yearDict.GetValueOrDefault("Ref. / Doc.") != null &&
                                                !string.IsNullOrWhiteSpace(yearDict["Ref. / Doc."]?.ToString()) &&
                                                double.TryParse(yearDict["Ref. / Doc."].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double refPerDoc) ?
                                                refPerDoc : null,

                                    PercentFemale = yearDict.GetValueOrDefault("%Female") != null &&
                                                    yearDict["%Female"]?.ToString() != "-" &&
                                                    !string.IsNullOrWhiteSpace(yearDict["%Female"]?.ToString()) &&
                                                    double.TryParse(yearDict["%Female"].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double percentFemale) ?
                                                    percentFemale : null,

                                    Overton = yearDict.GetValueOrDefault("Overton")?.ToString(),
                                    SDG = yearDict.GetValueOrDefault("SDG")?.ToString(),
                                    JournalYear = year
                                };
                                detailsToAdd.Add(newDetail);
                            }
                            catch (Exception ex)
                            {
                                // Optionally log the error
                                Console.WriteLine($"Error processing journal detail for year {yearData.Key}: {ex.Message}");
                            }

                            if (yearDict.GetValueOrDefault("Categories") is List<Dictionary<string, string>> categories)
                            {
                                foreach (var element in categories)
                                {
                                    var categoryName = element.GetValueOrDefault("Name")?.Trim();
                                    if (categoryDict.TryGetValue(categoryName, out var category))
                                    {
                                        if (int.TryParse(element.GetValueOrDefault("Quartile")?.Split("Q").LastOrDefault(),
                                            out int qLevel))
                                        {
                                            qurtilesToAdd.Add(new JournalQurtile
                                            {
                                                QLevel = qLevel,
                                                JournalYear = year,
                                                JournalCategory = category,
                                            });
                                        }
                                    }
                                }
                            }
                        }

                        // Batch insert when reaching batch size
                        if (journalsToAdd.Count >= batchSize)
                        {
                            await BatchInsertData(journalsToAdd, detailsToAdd, qurtilesToAdd, journalYearsToAdd);
                            journalsToAdd.Clear();
                            detailsToAdd.Clear();
                            qurtilesToAdd.Clear();
                            relationsToAdd.Clear();
                            journalYearsToAdd.Clear();
                        }

                    }
                    catch (Exception ex) { Console.WriteLine("F"); }
                }

                // Insert remaining records
                if (journalsToAdd.Any())
                {
                    await BatchInsertData(journalsToAdd, detailsToAdd, qurtilesToAdd, journalYearsToAdd);
                }

                return groupedRecords;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<GroupedJournalData>();
            }
        }

        private async Task BatchInsertData(
            List<Journal> journals,
            List<JournalDetailScopus> details,
            List<JournalQurtile> qurtiles,
            List<JournalYear> relations)
        {
            await _dbContext.Journals.AddRangeAsync(journals);
            await _dbContext.JournalDetailsScopus.AddRangeAsync(details);
            await _dbContext.JournalQurtiles.AddRangeAsync(qurtiles);
            await _dbContext.JournalYears.AddRangeAsync(relations);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<CSVModel.ScimagojrCategory>> ReadCategoryAndSubjectAreaFromExcel()
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "file", "journal", "SciClasCode.csv");
                var records = new List<CSVModel.ScimagojrCategory>();
                var badDataRecords = new List<string>();

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ",",
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    Quote = '"',
                    Escape = '\\',
                    BadDataFound = context =>
                    {
                        badDataRecords.Add($"Bad data in file {filePath}: {context.RawRecord}");
                    }
                };

                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, config))
                {
                    await csv.ReadAsync();
                    csv.ReadHeader();

                    while (await csv.ReadAsync())
                    {
                        try
                        {
                            var record = new CSVModel.ScimagojrCategory
                            {
                                Code = csv.GetField<string?>("Code") ?? "",
                                Description = csv.GetField<string?>("Description") ?? ""
                            };
                            records.Add(record);
                        }
                        catch (Exception ex)
                        {
                            badDataRecords.Add($"Error in file {filePath}, row {csv.Parser.Row}: {ex.Message}");
                        }
                    }
                }
                int subjectId = 0;
                var subject = new JournalSubjectArea();

                for (int i = 0; i < records.Count; i++)
                {
                    int sourceId = int.Parse(records[i].Code.IsNullOrEmpty() ? "0" : records[i].Code);

                    if (sourceId == 0)
                    {
                        subject = new JournalSubjectArea
                        {
                            Name = records[i].Description,
                            SourceId = int.Parse(records[i + 1].Code)
                        };
                        await _dbContext.JournalSubjectAreas.AddAsync(subject);
                        await _dbContext.SaveChangesAsync();
                        subjectId = sourceId;
                    }
                    else
                    {
                        if (_dbContext.JournalSubjectAreas.Any(x => x.SourceId == sourceId))
                            continue;

                        var category = new JournalCategory
                        {
                            Name = records[i].Description,
                            SourceId = sourceId,
                            SubjectArea = subject
                        };

                        await _dbContext.JournalCategories.AddAsync(category);
                        await _dbContext.SaveChangesAsync();
                    }

                }
                // Optionally, you can log or handle badDataRecords here
                return records;
            }
            catch (Exception ex)
            {
                // Optionally, log the exception
                Console.WriteLine($"Error reading CSV file: {ex.Message}");
                return new List<CSVModel.ScimagojrCategory>();
            }
        }
        public class GroupedJournalData
        {
            public string Sourceid { get; set; }
            public string Title { get; set; }
            public string Type { get; set; }
            public string ISSN { get; set; }
            public string Publisher { get; set; }
            public string Country { get; set; }
            public string Region { get; set; }
            public string CoverageStartYear { get; set; }
            public string CoverageEndYear { get; set; }
            public Dictionary<int, Dictionary<string, object>> YearlyData { get; set; } = new Dictionary<int, Dictionary<string, object>>();
        }

        public class StringListConverter : DefaultTypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return new List<string>();

                return text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                           .ToList();
            }

            public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
            {
                if (value is List<string> list)
                    return string.Join(",", list);
                return base.ConvertToString(value, row, memberMapData);
            }
        }

        public class CoverageConverter : DefaultTypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return new Dictionary<string, string>();

                var parts = text.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                return new Dictionary<string, string>
            {
                { "StartYear", parts.Length > 0 ? parts[0] : null },
                { "EndYear", parts.Length > 1 ? parts[1] : null }
            };
            }

            public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
            {
                if (value is Dictionary<string, string> coverage)
                    return $"{coverage.GetValueOrDefault("StartYear")}-{coverage.GetValueOrDefault("EndYear")}";
                return base.ConvertToString(value, row, memberMapData);
            }
        }

        public class CategoryListConverter : DefaultTypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return new List<Dictionary<string, string>>();

                var categories = new List<Dictionary<string, string>>();
                var items = text.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                foreach (var item in items)
                {
                    var match = Regex.Match(item, @"^(.*?)\s*\((Q\d)\)$");
                    categories.Add(new Dictionary<string, string>
                {
                    { "Name", match.Success ? match.Groups[1].Value.Trim() : item.Trim() },
                    { "Quartile", match.Success ? match.Groups[2].Value : null }
                });
                }

                return categories;
            }

            public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
            {
                if (value is List<Dictionary<string, string>> categories)
                {
                    return string.Join("; ", categories.Select(c => $"{c["Name"]} ({c["Quartile"]})"));
                }
                return base.ConvertToString(value, row, memberMapData);
            }
        }
    }

    public class JournalWosScraper
    {
        private readonly DynamicDbContext _dbContext;
        private readonly WebScraper _scraper;

        public JournalWosScraper(DynamicDbContext dbContext, WebScraper webScraper)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _scraper = webScraper;
        }
        private async Task<List<(Journal, JournalDetailWos)>> ExtractJournalData(List<IWebElement> rows)
        {
            var results = new List<(Journal, JournalDetailWos)>();

            foreach (var row in rows)
            {
                try
                {
                    var cells = row.FindElements(By.CssSelector("mat-cell"));

                    var journal = new Journal
                    {
                        Title_EN = GetCellText(cells[0], "table-cell-journalName"),
                        ISSN = GetCellText(cells[1], "table-cell-issn"),
                        EISSN = GetCellText(cells[2], "table-cell-eissn"),
                        LastUpdate = DateTime.Now
                    };

                    var journalDetail = new JournalDetailWos()
                    {
                        TotalCitations = int.Parse(GetCellText(cells[5], "table-cell-totalCites").Replace(",", "")),
                        PercentCitableOA = double.Parse(GetCellText(cells[9], "table-cell-percentageOAGold").Replace(" ", "").Split("%").LastOrDefault()),
                        CitableItems = int.Parse(GetCellText(cells[24], "table-cell-citableItems").Replace(",", "")),
                        PercentArticlesInCitableItems = double.Parse(GetCellText(cells[25], "table-cell-percentageOfArticlesInCitableItems")),
                        CitedHalfLife = double.Parse(GetCellText(cells[26], "table-cell-citedHalfLife")),
                        CitingHalfLife = double.Parse(GetCellText(cells[27], "table-cell-citingHalfLife")),
                        TotalArticles = int.Parse(GetCellText(cells[28], "table-cell-totalArticles").Replace(",", "")),
                        Eigenfactor = double.Parse(GetCellText(cells[18], "table-cell-eigenFactor")),
                        ArticleInfluenceScore = double.Parse(GetCellText(cells[20], "table-cell-articleInfluenceScore")),
                        ImmediacyIndex = double.Parse(GetCellText(cells[14], "table-cell-immediacyIndex")),
                        JIFWithoutSelfCites = double.Parse(GetCellText(cells[13], "table-cell-jifWithoutSelfCites")),
                        FiveYearJIF = double.Parse(GetCellText(cells[11], "table-cell-jif5Years"))
                    };

                    // استخراج کتگوری‌ها
                    var categories = GetMultipleCellText(cells.ToList());
                    //categories.ForEach(x =>
                    //{
                    //    x.Journal = journal;
                    //});
                    //journalDetail.Categories.AddRange(categories);
                    await _dbContext.JournalCategoriesWos.AddRangeAsync(categories);
                    results.Add((journal, journalDetail));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing row: {ex.Message}");
                }
            }

            return results;
        }

        private string GetCellText(IWebElement cell, string className)
        {
            try
            {
                var element = cell.FindElement(By.CssSelector($".{className}"));
                return element.GetAttribute("title").Trim().Replace("N/A", "");
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        private List<JournalCategoryWos> GetMultipleCellText(List<IWebElement> cell)
        {
            var categories = new List<JournalCategoryWos>();
            try
            {
                // برای حالتی که چندین کتگوری وجود دارد
                var expansionPanel = cell[3].FindElement(By.CssSelector($".table-cell-category.multiple"));

                var categorySpansName = expansionPanel.FindElements(By.CssSelector("div.mat-expansion-panel-body span[title]"));
                var JIFQuartile = cell[7].FindElements(By.CssSelector("div.mat-expansion-panel-body span[title]"));
                var JCIRank = cell[10].FindElements(By.CssSelector("div.mat-expansion-panel-body span[title]"));
                var JCIQuartile = cell[16].FindElements(By.CssSelector("div.mat-expansion-panel-body span[title]"));
                var JCIPercentile = cell[17].FindElements(By.CssSelector("div.mat-expansion-panel-body span[title]"));
                var JIFPercentile = cell[21].FindElements(By.CssSelector("div.mat-expansion-panel-body span[title]"));
                var AISRank = cell[23].FindElements(By.CssSelector("div.mat-expansion-panel-body span[title]"));
                var AISQuartile = cell[22].FindElements(By.CssSelector("div.mat-expansion-panel-body span[title]"));
                var FiveYearJIFQuartile = cell[12].FindElements(By.CssSelector("div.mat-expansion-panel-body span[title]"));
                var JIFRank = cell[10].FindElements(By.CssSelector("div.mat-expansion-panel-body span[title]"));
                var Edition = cell[4].FindElements(By.CssSelector("div.mat-expansion-panel-body span[title]"));

                for (int i = 0; i < categorySpansName.Count; i++)
                {
                    var cat = new JournalCategoryWos();
                    cat.Name = categorySpansName[i].GetAttribute("title").Trim();
                    cat.Edition = Edition[i].GetAttribute("title").Trim();
                    cat.JIFQuartile = JIFQuartile[i].GetAttribute("title").Trim();
                    //	cat.JCI2023 = double.Parse(JCI2023[i].GetAttribute("title").Trim());
                    cat.JCIRank = JCIRank[i].GetAttribute("title").Trim();
                    cat.JCIQuartile = JCIQuartile[i].GetAttribute("title").Trim();
                    cat.JCIPercentile = double.Parse(JCIPercentile[i].GetAttribute("title").Trim());
                    cat.JIFPercentile = double.Parse(JIFPercentile[i].GetAttribute("title").Trim());
                    cat.AISRank = AISRank[i].GetAttribute("title").Trim();
                    cat.AISQuartile = AISQuartile[i].GetAttribute("title").Trim();
                    cat.FiveYearJIFQuartile = FiveYearJIFQuartile[i].GetAttribute("title").Trim();
                    cat.JIFRank = JIFRank[i].GetAttribute("title").Trim();

                    categories.Add(cat);
                }
                return categories;
            }
            catch
            {

                var category = new JournalCategoryWos();

                category.Name = GetCellText(cell[3], "table-cell-category");
                category.JIFQuartile = GetCellText(cell[7], "table-cell-quartile");
                category.JCI2023 = double.Parse(GetCellText(cell[8], "table-cell-jci"));
                category.JCIRank = GetCellText(cell[10], "table-cell-jciRank");
                category.JCIQuartile = GetCellText(cell[16], "table-cell-jciQuartile");
                category.JCIPercentile = double.Parse(GetCellText(cell[17], "table-cell-jciPercentile"));
                category.JIFPercentile = double.Parse(GetCellText(cell[21], "table-cell-jifPercentile"));
                category.AISRank = GetCellText(cell[23], "table-cell-aisRank");
                category.AISQuartile = GetCellText(cell[22], "table-cell-aisQuartile");
                category.FiveYearJIFQuartile = GetCellText(cell[12], "table-cell-fiveYearJifQuartile");
                category.JIFRank = GetCellText(cell[10], "table-cell-jifRank");
                category.Edition = GetCellText(cell[4], "table-cell-edition");


                categories.Add(category);
                return categories;
            }
        }
        // اصلاح تابع ExecuteWOSAsync
        public async Task ExecuteWOSAsync()
        {
            var rows = new List<IWebElement>();
            await _scraper.OpenUrlAsync("https://jcr-clarivate-com.access.semantak.com/jcr/browse-journals", ".mat-focus-indicator.mat-tooltip-trigger.mat-paginator-navigation-next.mat-icon-button.mat-button-base");
            while (true)
            {
                try
                {
                    long totalHeight = (long)((IJavaScriptExecutor)_scraper.Driver).ExecuteScript("return document.body.scrollHeight;");
                    int steps = 10;
                    long stepSize = totalHeight / steps;

                    for (int i = 1; i <= steps; i++)
                    {
                        long scrollPosition = stepSize * i;
                        ((IJavaScriptExecutor)_scraper.Driver).ExecuteScript($"window.scrollTo(0, {scrollPosition});");
                        await Task.Delay(100);
                    }

                    for (int i = steps; i >= 0; i--)
                    {
                        long scrollPosition = stepSize * i;
                        ((IJavaScriptExecutor)_scraper.Driver).ExecuteScript($"window.scrollTo(0, {scrollPosition});");
                        await Task.Delay(100);
                    }

                    rows.AddRange(_scraper.FindMany(".mat-row.cdk-row.mat-row-overflow.ng-star-inserted"));


                    var nextButton = _scraper.FindOne(".mat-focus-indicator.mat-tooltip-trigger.mat-paginator-navigation-next.mat-icon-button.mat-button-base");
                    if (nextButton?.GetAttribute("disabled") == "true")
                    {
                        break;
                    }
                    else
                    {
                        nextButton.Click();
                    }

                    // Process extracted rows
                    var journalDataa = await ExtractJournalData(rows);

                    // Save to database
                    foreach (var (journal, detail) in journalDataa)
                    {
                        // Check if journal already exists
                        var existingJournal = await _dbContext.Journals
                            .FirstOrDefaultAsync(j => j.ISSN.Contains(journal.ISSN.Replace("-", "")) || j.EISSN.Contains(journal.EISSN.Replace("-", "")));

                        if (existingJournal == null)
                        {
                            await _dbContext.Journals.AddAsync(journal);
                            await _dbContext.JournalDetailsWos.AddAsync(detail);
                        }
                        else
                        {
                            //existingJournal.JournalYears.JournalDetailWos = new List<JournalDetailWos>();
                            //existingJournal.JournalDetailWos.Add(detail);
                        }
                    }
                    rows = new List<IWebElement>();
                    await _dbContext.SaveChangesAsync();
                    await Task.Delay(10000);
                }
                catch (Exception ex)
                {
                    await Task.Delay(10000);
                }
            }

            // Process extracted rows
            var journalData = await ExtractJournalData(rows);

            // Save to database
            foreach (var (journal, detail) in journalData)
            {
                // Check if journal already exists
                var existingJournal = await _dbContext.Journals
                    .FirstOrDefaultAsync(j => j.ISSN.Contains(journal.ISSN) || j.EISSN.Contains(journal.EISSN));

                if (existingJournal == null)
                {
                    await _dbContext.Journals.AddAsync(journal);
                    await _dbContext.JournalDetailsWos.AddAsync(detail);
                }
                else
                {
                    //journal.JournalDetailWoss.Add(detail);
                }
            }

            await _dbContext.SaveChangesAsync();
        }


    }
    #endregion
}


#region mapExcel
public class MapExcelData
{
    private readonly DynamicDbContext _dbContext;

    public MapExcelData(DynamicDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async void Execute()
    {

        string filePath = @"D:\نهایی -پناهی.xlsx"; // مسیر فایل اکسل

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // تنظیم لایسنس برای استفاده غیرتجاری

        using (var package = new ExcelPackage(new FileInfo(filePath)))
        {
            var worksheet = package.Workbook.Worksheets[0]; // انتخاب اولین شیت
            int rowCount = worksheet.Dimension.Rows;

            for (int row = 1; row <= rowCount; row++)
            {
                var cellValue = worksheet.Cells[row, 4].Text; // خواندن مقدار ستون A

                var profesor = await _dbContext.Professors.FirstOrDefaultAsync(x => x.EmployeeNumber == int.Parse(cellValue));

                if (profesor != null)
                {
                    profesor.LastNameEn = worksheet.Cells[row, 4].Text;
                }
                else
                {

                }
            }
        }
    }
}
#endregion