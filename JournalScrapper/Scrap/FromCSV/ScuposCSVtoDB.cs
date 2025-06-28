using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static JournalScrapper.Entity.ScopusEntity;

namespace JournalScrapper.Scrap.FromCSV
{
    public static class ScuposCSVtoDB
    {
        public static void ImportCsvData(string csvFilePath, string csvType)
        {

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => Regex.Replace(args.Header, "[^a-zA-Z]", "").ToLower()
            };
            using (var reader = new StreamReader(csvFilePath))
            using (var csv = new CsvReader(reader, config))
            using (var context = new ScopusContext())
            {
                //csv.Context.RegisterClassMap<ScopusProfileMap>();
                csv.Context.TypeConverterCache.AddConverter<DateTime>(new CustomDateTimeConverter());
                csv.Context.TypeConverterCache.AddConverter<int>(new CustomInt32Converter());
                switch (csvType)
                {
                    case "ScopusProfile":
                        var profileRecords = csv.GetRecords<ScopusProfile>().ToList();
                        context.ScopusProfiles.AddRange(profileRecords);
                        break;

                    case "ScopusHIndex":
                        var hIndexRecords = csv.GetRecords<ScopusHIndex>().ToList();
                        context.ScopusHIndices.AddRange(hIndexRecords);
                        break;

                    case "ScopusCitations":
                        var citationRecords = csv.GetRecords<ScopusCitations>().ToList();
                        context.ScopusCitations.AddRange(citationRecords);
                        break;

                    case "ScopusArticle":
                        var articleRecords = csv.GetRecords<ScopusArticle>().ToList();
                        context.ScopusArticles.AddRange(articleRecords);
                        break;

                    case "ScopusJournal":
                        var journalRecords = csv.GetRecords<ScopusJournal>().ToList();
                        context.ScopusJournals.AddRange(journalRecords);
                        break;

                    default:
                        throw new ArgumentException("Invalid CSV type.");
                }

                context.SaveChanges();
            }
        }

        public static void ExctractScuposCSVtoDB()
        {
            string csvFilePath1 = WebScraper.FindDirectoryInParents() + "\\scopus\\profiles.csv";
            string csvFilePath2 = WebScraper.FindDirectoryInParents() + "\\scopus\\hindex.csv";
            string csvFilePath3 = WebScraper.FindDirectoryInParents() + "\\scopus\\citations.csv";
            string csvFilePath4 = WebScraper.FindDirectoryInParents() + "\\scopus\\articles.csv";
            string csvFilePath5 = WebScraper.FindDirectoryInParents() + "\\scopus\\journals.csv";

            ScuposCSVtoDB.ImportCsvData(csvFilePath1, "ScopusProfile");
            ScuposCSVtoDB.ImportCsvData(csvFilePath2, "ScopusHIndex");
            ScuposCSVtoDB.ImportCsvData(csvFilePath3, "ScopusCitations");
            ScuposCSVtoDB.ImportCsvData(csvFilePath4, "ScopusArticle");
            ScuposCSVtoDB.ImportCsvData(csvFilePath5, "ScopusJournal");

            Console.WriteLine("Data imported successfully.");
        }

        public class CustomInt32Converter : DefaultTypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                // اگر مقدار خالی یا null است، صفر برگردانید
                if (string.IsNullOrWhiteSpace(text))
                {
                    return 0;
                }
                text = text.Replace(",","");
                // اگر مقدار معتبر است، آن را به int تبدیل کنید
                if (int.TryParse(text, out int result))
                {
                    return result;
                }

                // اگر تبدیل ممکن نبود، صفر برگردانید
                return 0;
            }
        }
        public class CustomDateTimeConverter : DefaultTypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                // اگر مقدار خالی یا null است، مقدار پیش‌فرض برگردانید
                if (string.IsNullOrWhiteSpace(text))
                {
                    return DateTime.MinValue; // یا می‌توانید `null` برگردانید اگر نوع `DateTime?` باشد
                }

                // اگر مقدار معتبر است، آن را به DateTime تبدیل کنید
                if (DateTime.TryParse(text, out DateTime result))
                {
                    return result;
                }

                // اگر تبدیل ممکن نبود، مقدار پیش‌فرض برگردانید
                return DateTime.MinValue; // یا `null` اگر نوع `DateTime?` باشد
            }
        }
        //public class ScopusProfileMap : ClassMap<ScopusProfile>
        //{
        //    public ScopusProfileMap()
        //    {
        //        Map(m => m.Documents).TypeConverter<CustomInt32Converter>();
        //    }
        //}
    }
}
