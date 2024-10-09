using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static JournalScrapper.Entity.Personnel;

namespace JournalScrapper.Entity
{
    public class Personnel
    {
        public class PersonnelRecord
        {
            public string Timestamp { get; set; }
            public string FirstNameEn { get; set; }
            public string LastNameEn { get; set; }
            public string PersonnelCode { get; set; }
            public string NationalCode { get; set; }
            public string FirstNameFa { get; set; }
            public string LastNameFa { get; set; }
            public string ScopusID { get; set; }
            public string WebOfScienceID { get; set; }
            public string GoogleScholarID { get; set; }
            public string Faculty { get; set; }
            public string Group { get; set; }
        }

    }
    public class ProfessorMap : ClassMap<PersonnelRecord>
    {
        public ProfessorMap()
        {
            Map(m => m.FirstNameFa).Name("نام فارسی:");
            Map(m => m.LastNameFa).Name("نام خانوادگی فارسی:");
            Map(m => m.FirstNameEn).Name("نام لاتین");
            Map(m => m.LastNameEn).Name("نام خانوادگی لاتین:");
            Map(m => m.PersonnelCode).Name("کد پرسنلی:");
            Map(m => m.NationalCode).Name("کد ملی:");
            Map(m => m.ScopusID).Name("Scopus Author ID");
            Map(m => m.WebOfScienceID).Name("Web of Science ResearcherID");
            Map(m => m.GoogleScholarID).Name("Google Scholar ID");
            Map(m => m.Faculty).Name("دانشکده:");
            Map(m => m.Group).Name("گروه:");
        }
    }
}
