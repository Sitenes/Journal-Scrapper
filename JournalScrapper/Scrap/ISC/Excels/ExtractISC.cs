using CsvHelper;
using CsvHelper.Configuration;
using DataLayer;
using Entities.Models.Entities;
using JournalScrappers;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;

class ExtractISC
{
    private readonly DynamicDbContext _context;

    public ExtractISC(DynamicDbContext context)
    {
        this._context = context;
    }
    public async Task ScrapISC()
    {
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
                var year = await _context.JournalDetailsIsc
                    .FirstOrDefaultAsync(y => y.JournalYear.Year.ToString() == yearValue && y.JournalYear.JournalId == ISCJournal.Id);

                if (year == null)
                {
                    year = new JournalDetailIsc
                    {
                        ImpactFactor = recordDictionary["ضریب تاثیر"].ToString() ?? "",
                        //YearPublished = yearValue ?? "",
                        CumulativeCitations = recordDictionary["استنادهای تجمعی"].ToString() ?? "",
                        ImmediateImpactFactor = recordDictionary["ضريب تاثير آنی"].ToString() ?? "",
                        //JournalId = ISCJournal.Id
                    };
                    await _context.JournalDetailsIsc.AddAsync(year);
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

                       var category = _context.JournalCategories
                           .FirstOrDefault(qq => qq.Name == qualityName);

                       if (category == null)
                       {
                           _context.JournalCategories
                            .Add(new JournalCategory
                            {
                                Name = qualityName,

                            });
                           _context.SaveChanges();
                       }

                       var quality = _context.JournalQurtiles.Include(x => x.JournalCategory)
                           .FirstOrDefault(qq => qq.JournalCategory.Name == qualityName && qq.QLevel == qNum && qq.JournalYearId == year.Id);

                       if (quality == null)
                       {
                           quality = new JournalQurtile
                           {
                               QLevel = qNum,
                               //YearId = year.Id
                           };
                       }

                       return quality;
                   })
                   .Where(q => q != null)
                   .ToList();
                foreach (var x in qualities)
                {
                    var exist = await _context.JournalQurtiles.Include(x => x.JournalCategory).AnyAsync(xx => xx.JournalYearId == x.JournalYearId && xx.JournalCategory.Name.Equals(xx.JournalCategory));
                    if (!exist)
                        await _context.JournalQurtiles.AddAsync(x);
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
