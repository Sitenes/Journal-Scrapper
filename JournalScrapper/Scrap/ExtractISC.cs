using CSV2Sql.Models;
using CsvHelper;
using CsvHelper.Configuration;
using JournalScrapper;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;

class ExtractISC
{
    public static async Task ScrapISC()
    {
        var _context = new AppDbContext();
        string extraDirectoryPath = WebScraper.FindDirectoryInParents() + "\\journals(3).csv";
        using (var reader = new StreamReader(extraDirectoryPath))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
        {
            var records = csv.GetRecords<dynamic>().ToList();

            for (var i = 0; i < records.Count; i++)
            {
                var recordDictionary = (IDictionary<string, object>)records[i];

                // Check if ISCJournal exists
                var ISCJournalTitle = recordDictionary["عنوان"].ToString();
                var ISCJournal = await _context.Journals
                    .FirstOrDefaultAsync(j => j.Title_Fa == ISCJournalTitle);

                if (ISCJournal == null)
                {
                    ISCJournal = new Journal
                    {
                        Title_Fa = ISCJournalTitle ?? "",
                        ISSN = recordDictionary["شاپا"].ToString() ?? "",
                        EISSN = recordDictionary["شاپای الکترونیکی"].ToString() ?? "",
                        Language = recordDictionary["زبان"].ToString() ?? "",
                        Country = recordDictionary["کشور"].ToString() ?? "",
                        Region = recordDictionary["استان"].ToString() ?? "",
                        Publisher = recordDictionary["ناشر"].ToString() ?? "",
                    };
                    await _context.Journals.AddAsync(ISCJournal);
                    await _context.SaveChangesAsync();
                }

                // Check if Year exists
                var yearValue = recordDictionary["سال"].ToString();
                var year = await _context.Years
                    .FirstOrDefaultAsync(y => y.YearPublished == yearValue && y.JournalId == ISCJournal.Id);

                if (year == null)
                {
                    year = new Year
                    {
                        ImpactFactor = recordDictionary["ضریب تاثیر"].ToString() ?? "",
                        YearPublished = yearValue ?? "",
                        CumulativeCitations = recordDictionary["استنادهای تجمعی"].ToString() ?? "",
                        ImmediateImpactFactor = recordDictionary["ضريب تاثير آنی"].ToString() ?? "",
                        JournalId = ISCJournal.Id
                    };
                    await _context.Years.AddAsync(year);
                    await _context.SaveChangesAsync();
                }

                // Parse and check if Quality exists
                var qualities = recordDictionary["کیفیت در موضوع سطح میانی"]
                   .ToString()?
                   .Split(',')
                   .Select(q =>
                   {
                       var match = Regex.Match(q.Trim(), @"(.+)\s*\(Q(\d)\)");
                       if (!match.Success) return null;

                       var qualityName = match.Groups[1].Value.Trim();
                       var qNum = int.Parse(match.Groups[2].Value);

                       var category = _context.ScopusJournalCategories
                           .FirstOrDefault(qq => qq.Name == qualityName);

                       if (category == null)
                       {
                           _context.ScopusJournalCategories
                            .Add(new ScopusJournalCategory
                            {
                                Name = qualityName,
                                
                            });
                           _context.SaveChanges();
                       }

                       var quality = _context.Qualities.Include(x => x.JournalCategory)
                           .FirstOrDefault(qq => qq.JournalCategory.Name == qualityName && qq.QLevel == qNum && qq.YearId == year.Id);

                       if (quality == null)
                       {
                           quality = new Qurtile
                           {
                               QLevel = qNum,
                               YearId = year.Id
                           };
                       }

                       return quality;
                   })
                   .Where(q => q != null)
                   .ToList();
                foreach (var x in qualities)
                {
                    var exist = await _context.Qualities.Include(x=>x.JournalCategory).AnyAsync(xx => xx.YearId == x.YearId && xx.JournalCategory.Name.Equals(xx.JournalCategory));
                    if (!exist)
                        await _context.Qualities.AddAsync(x);
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
