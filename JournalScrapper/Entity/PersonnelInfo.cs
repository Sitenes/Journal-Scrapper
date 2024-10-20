using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JournalScrapper.Entity
{
    public class FacultyMember
    {
        public string Faculty { get; set; }           // دانشكده
        public string Department { get; set; }        // گروه آموزشي
        public string IdentificationNumber { get; set; }         // شماره شناسايي
        public string LastName { get; set; }          // نام خانوادگي استاد
        public string FirstName { get; set; }         // نام استاد
        public string UserNumber { get; set; }        // شماره كاربري
        public string EmployeeNumber { get; set; }    // شماره مستخدم
        public string FinancialCode { get; set; }     // كد مالي
        public string NationalCode { get; set; }        // شماره ملي
    }

    public class FacultyMemberMap : ClassMap<FacultyMember>
    {
        public FacultyMemberMap()
        {
            Map(m => m.Faculty).Name("دانشكده").Index(2);
            Map(m => m.Department).Name("گروه آموزشي").Index(4);
            Map(m => m.IdentificationNumber).Name("شماره شناسايي");
            Map(m => m.LastName).Name("نام خانوادگي استاد");
            Map(m => m.FirstName).Name("نام استاد");
            Map(m => m.UserNumber).Name("شماره كاربري");
            Map(m => m.EmployeeNumber).Name("شماره مستخدم");
            Map(m => m.FinancialCode).Name("كد مالي");
            Map(m => m.NationalCode).Name("شماره ملي");
        }
    }

}
