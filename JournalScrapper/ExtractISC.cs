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

        using (var reader = new StreamReader("C:\\Projects\\ProjectC#\\JournalScrapper\\JournalScrapper\\ExtraData\\journals(3).csv"))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
        {
            var records = csv.GetRecords<dynamic>().ToList();

            for (var i = 0; i < records.Count; i++)
            {
                var recordDictionary = (IDictionary<string, object>)records[i];

                // Check if ISCJournal exists
                var ISCJournalTitle = recordDictionary["عنوان"].ToString();
                var ISCJournal = await _context.ISCJournals
                    .FirstOrDefaultAsync(j => j.Title == ISCJournalTitle);

                if (ISCJournal == null)
                {
                    ISCJournal = new ISCJournal
                    {
                        Title = ISCJournalTitle,
                        ISSN = recordDictionary["شاپا"].ToString(),
                        EISSN = recordDictionary["شاپای الکترونیکی"].ToString(),
                        Language = recordDictionary["زبان"].ToString(),
                        Country = recordDictionary["کشور"].ToString(),
                        Province = recordDictionary["استان"].ToString(),
                        Publisher = recordDictionary["ناشر"].ToString(),
                    };
                    await _context.ISCJournals.AddAsync(ISCJournal);
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
                        ImpactFactor = recordDictionary["ضریب تاثیر"].ToString(),
                        YearPublished = yearValue,
                        CumulativeCitations = recordDictionary["استنادهای تجمعی"].ToString(),
                        ImmediateImpactFactor = recordDictionary["ضريب تاثير آنی"].ToString(),
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
                        var qualityName = match.Groups[1].Value.Trim();
                        var qualityQ = "Q" + match.Groups[2].Value.Trim();
                        if (!string.IsNullOrEmpty(qualityName))
                        {
                            var quality = _context.Qualities
                            .FirstOrDefault(q => q.Name == qualityName && q.Q == qualityQ && q.YearId == year.Id);

                            if (quality == null)
                            {
                                quality = new Quality
                                {
                                    Name = qualityName,
                                    Q = qualityQ,
                                    YearId = year.Id
                                };
                                _context.Qualities.Add(quality);
                            }

                            return quality;
                        }
                        // Check if Quality already exists
                        return null;
                    })
                    .Where(q => q != null)  // Filter out null values (existing qualities)
                    .ToList();
                foreach (var x in qualities)
                {
                    var exist = await _context.Qualities.AnyAsync(xx => xx.YearId == x.YearId && xx.Name.Equals(x.Name));
                    if (!exist)
                        await _context.Qualities.AddAsync(x);
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
