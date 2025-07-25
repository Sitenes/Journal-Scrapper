using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DataLayer;
using JournalScrappers.Scrap.Scholar;
using JournalScrappers.Scrap.ISC.Journals;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager;

class Program
{
	static async Task Main(string[] args)
	{
		using IHost host = CreateHostBuilder(args).Build();

		// Resolve DbContext from DI
		using var scope = host.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<Context>();
		var dynamicDbContext = scope.ServiceProvider.GetRequiredService<DynamicDbContext>();

		new DriverManager().SetUpDriver(new ChromeConfig());


		var configuration = new ConfigurationBuilder()
		.AddInMemoryCollection([new KeyValuePair<string, string?>("ArticleUrl", "https://jcr.isc.ac/main.aspx")]).Build();
		var journalScrapper = new JournalScrapper(configuration, dynamicDbContext);
		await journalScrapper.Scrap();

		var journalCoverScraper = new ScrapeImageFromScholar(dynamicDbContext);
		journalCoverScraper.ScrapAllProfileImages();

	}

	static IHostBuilder CreateHostBuilder(string[] args) =>
		Host.CreateDefaultBuilder(args)
			.ConfigureAppConfiguration((context, config) =>
			{
				config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
			})
			.ConfigureServices((context, services) =>
			{
				var connectionString = context.Configuration.GetConnectionString("BasicLocal");

				services.AddDbContext<Context>(options =>
					options.UseSqlServer(connectionString));


				var dynamicConnectionString = context.Configuration.GetConnectionString("DynamicLocal");

				services.AddDbContext<DynamicDbContext>(options =>
					options.UseSqlServer(dynamicConnectionString));
			});
}

////ExtractArticles extract = new ExtractArticles();
////extract.ScrapArticles();
////await ExtractISC.ScrapISC();
////CsvToDatabase.ReadProfessorInfoFromCsv();

//var journalCoverScraper = new JournalScrapper.Scrap.Scholar.ScrapeImageFromScholar();
//journalCoverScraper.ScrapAllProfileImages();

////var journalCoverScraper = new JournalScrapper.Scrap.ISC.Journal.JournalCoverScrapper();
////journalCoverScraper.ScrapAllJournalCovers();

////var configuration = new ConfigurationBuilder()
////    .AddInMemoryCollection([new KeyValuePair<string, string?>("ArticleUrl", "https://jcr.isc.ac/main.aspx")]).Build();
////var journalScrapper = new JournalScrapper.Scrap.ISC.JournalJournalScrapper(configuration);
////await journalScrapper.Scrap();

////await ExtractProfessor.ScrapProfessor();
////MySqlToSQL sql = new MySqlToSQL();
////await sql.MigrateDataAsync();
////ExtractPersonnelData.ReadPersonnelDataFromCsv();
////CsvToDatabase.ReadProfessorInfoFromCsv();
////ScuposCSVtoDB.ExctractScuposCSVtoDB();