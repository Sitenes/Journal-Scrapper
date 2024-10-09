using CsvHelper;
using JournalScrapper.Entity;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static JournalScrapper.Entity.Personnel;
using static Profile_Shakhsi.Models.Entity.Profile;

namespace JournalScrapper
{
    public class CsvToDatabase
    {
        public static void ReadProfessorInfoFromCsv()
        {
            var _context = new ProfileShakhsiDbContext();
            string extraDirectoryPath = WebScraper.FindDirectoryInParents();
            string csvFilePath = Path.Combine(extraDirectoryPath, "اطلاعات اعضاء هیات علمی.csv"); // مسیر فایل CSV را اینجا مشخص کنید

            using (var reader = new StreamReader(csvFilePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<ProfessorMap>();
                var records = csv.GetRecords<PersonnelRecord>().ToList();

                foreach (var record in records)
                {
     //               var professorDB = _context.ProfessorProfiles.ToList()
     //.FirstOrDefault(f =>
     //    RemoveNonLetters(f.FullNameEn).ToLower().Contains(RemoveNonLetters(record.FirstNameEn).ToLower()) &&
     //    RemoveNonLetters(f.FullNameEn).ToLower().Contains(RemoveNonLetters(record.LastNameEn).ToLower()));
     //               if (professorDB != null)
     //               {
     //                   professorDB.PersonnelCode = record.PersonnelCode;
     //                   professorDB.NationalCode = record.NationalCode;
     //                   professorDB.FirstNameFa = record.FirstNameFa;
     //                   professorDB.LastNameFa = record.LastNameFa;
     //                   professorDB.ScopusID = record.ScopusID;
     //                   professorDB.WebOfScienceID = record.WebOfScienceID;
     //                   professorDB.GoogleScholarID = FixFormatScholarId(record.GoogleScholarID);
     //                   professorDB.Faculty = record.Faculty;
     //                   professorDB.FirstNameEn = record.FirstNameEn;
     //                   professorDB.LastNameEn = record.LastNameEn;
     //               }
     //               else
     //               {
                        var professor = new ProfessorProfile();
                        professor.PersonnelCode = record.PersonnelCode;
                        professor.NationalCode = record.NationalCode;
                        professor.FirstNameFa = record.FirstNameFa;
                        professor.LastNameFa = record.LastNameFa;
                        professor.ScopusID = record.ScopusID;
                        professor.WebOfScienceID = record.WebOfScienceID;
                        professor.GoogleScholarID = FixFormatScholarId(record.GoogleScholarID);
                        professor.Faculty = record.Faculty;
                        professor.FirstNameEn = record.FirstNameEn;
                        professor.LastNameEn = record.LastNameEn;
                        if (ExtractXml.ContainsPersianCharacters(record.Group) ?? false)
                            professor.DepartmentFA = record.Group;
                        else
                            professor.Department = record.Group;
                    _context.Add(professor);
                    //}
                    
                }
                _context.SaveChanges();
            }
        }
        public static string RemoveNonLetters(string input)
        {
            return new string(input.Where(c => char.IsLetter(c)).ToArray());
        }

        public static string FixFormatScholarId(string input)
        {
            if (input.Contains("user="))
            {
                // استخراج user id از لینک
                string[] parts = input.Split(new[] { "user=" }, StringSplitOptions.None);
                string id = parts[1].Split('&')[0].Trim();
                return id;
            }
            else
            {
                string id = input.Split('&')[0].Trim();
                if (System.Text.RegularExpressions.Regex.IsMatch(id, @"[a-zA-Z0-9_-]{11,12}"))
                    return id;
            }
            return string.Empty;
        }
    }


}

