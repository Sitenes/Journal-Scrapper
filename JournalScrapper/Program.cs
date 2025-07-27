using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DataLayer;
using JournalScrappers.Scrap.Scholar;
using JournalScrappers.Scrap.ISC.Journals;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager;
using ExcelImporter;
using Serilog;

class Program
{
    static async Task Main(string[] args)
    {
        // 1. ابتدا تنظیم Logger از appsettings.json
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Log.Information("شروع برنامه");

            // 2. ساخت هاست با UseSerilog که حالا Log.Logger تنظیم شده
            using IHost host = CreateHostBuilder(args, configuration).Build();

            await host.StartAsync();

            // 3. دسترسی به سرویس‌ها و اجرای برنامه
            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<Context>();
            var dynamicDbContext = scope.ServiceProvider.GetRequiredService<DynamicDbContext>();

            var journalScrapper = new JournalScrapper(configuration, dynamicDbContext);
            await journalScrapper.Scrap();

            var journalCoverScraper = new ScrapeImageFromScholar(dynamicDbContext);
            journalCoverScraper.ScrapAllProfileImages();

            await host.StopAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "خطای بحرانی رخ داده است و برنامه متوقف می‌شود");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog() // اینجا UseSerilog صدا زده می‌شود و از Log.Logger تنظیم شده استفاده می‌کند
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddConfiguration(configuration);
            })
            .ConfigureServices((context, services) =>
            {
                var connectionString = configuration.GetConnectionString("BasicLocal");
                services.AddDbContext<Context>(options => options.UseSqlServer(connectionString));

                var dynamicConnectionString = configuration.GetConnectionString("DynamicLocal");
                services.AddDbContext<DynamicDbContext>(options => options.UseSqlServer(dynamicConnectionString));
            });
}
