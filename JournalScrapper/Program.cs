using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DataLayer;
using JournalScrappers.Scrap.ISC.Articles;
using System;
using System.Text;
using System.Threading.Tasks;
using JournalScrappers;
using JournalScrappers.Scrap.ISC.Journals;
using Serilog;
using JournalScrapper.Scrap.ISC.Articles.CrawlerLinks;
using ResearchScraper;
class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        ILogger<Program>? logger = null;
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            using IHost host = CreateHostBuilder(args, configuration).Build();
            await host.StartAsync();

            using var scope = host.Services.CreateScope();
            logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("شروع برنامه");

            var extractArticles = scope.ServiceProvider.GetRequiredService<ScopusScraper>();
            await extractArticles.ScrapeAllProfessors();

            //var extractArticles = scope.ServiceProvider.GetRequiredService<JournalsCrawler>();
            //extractArticles.ScrapArticles();

            //var extractArticles = scope.ServiceProvider.GetRequiredService<JournalCoverScrapper>();
            //extractArticles.ScrapAllJournalCovers();
            logger.LogInformation("پایان برنامه");
            await host.StopAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطای بحرانی رخ داده است: {ex.Message}");
            logger?.LogCritical($"خطای بحرانی رخ داده است: {ex.Message}");
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddConfiguration(configuration);
            })
            .ConfigureServices((context, services) =>
            {
                Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console() // Optional: اگر در json هست، حذف کن
                .CreateLogger();

                var connectionString = configuration.GetConnectionString("BasicLocal");
                services.AddDbContext<Context>(options => options.UseSqlServer(connectionString));

                var dynamicConnectionString = configuration.GetConnectionString("DynamicLocal");
                services.AddDbContext<DynamicDbContext>(options => options.UseSqlServer(dynamicConnectionString));

                services.AddScoped<CrawlXml>();
                services.AddScoped<CrawlXml>();
                services.AddScoped<ExtractArticles>();
                services.AddScoped<JournalsCrawler>();
                services.AddScoped<JournalCoverScrapper>();
                services.AddScoped<WebScraper>();
                services.AddScoped<ScopusScraper>();

                services.AddLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddConsole(options => options.IncludeScopes = true);
                    builder.AddSerilog();
                    builder.SetMinimumLevel(LogLevel.Information); // یا Debug برای لاگ بیشتر
                });
            }).UseSerilog();
}