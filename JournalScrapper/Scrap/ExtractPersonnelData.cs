using CsvHelper;
using JournalScrapper.Entity;
using Microsoft.EntityFrameworkCore;
using Profile_Shakhsi.Models.Entity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JournalScrapper
{
    internal class ExtractPersonnelData
    {
        public static void ReadPersonnelDataFromCsv()
        {
            var _context = new ProfileShakhsiDbContext();
            string extraDirectoryPath = WebScraper.FindDirectoryInParents() + "\\Info asatid.csv";
            List<FacultyMember> records = new List<FacultyMember>();
            using (var reader = new StreamReader(extraDirectoryPath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                // نگاشت ستون‌های CSV به کلاس FacultyMember
                csv.Context.RegisterClassMap<FacultyMemberMap>();
                records = csv.GetRecords<FacultyMember>().ToList();
            }
            foreach (var record in records)
            {
                var properties = typeof(FacultyMember).GetProperties();
                foreach (var property in properties)
                {
                    if (property.PropertyType == typeof(string))
                    {
                        var value = property.GetValue(record) as string;
                        if (!string.IsNullOrEmpty(value))
                        {
                            var updatedValue = value.Replace("ي", "ی");
                            property.SetValue(record, updatedValue);
                        }
                    }
                }
                // بررسی و به‌روزرسانی دپارتمان
                var department = _context.Departments
                    .FirstOrDefault(d => d.TitleFa == record.Department);

                if (department == null)
                {
                    // اگر دپارتمان وجود ندارد، آن را ایجاد کنید
                    department = new Department
                    {
                        TitleFa = record.Department ?? "",
                    };
                    _context.Departments.Add(department);
                    _context.SaveChanges(); // ذخیره‌سازی دپارتمان جدید
                }

                // بررسی و به‌روزرسانی فاکتوری
                var faculty = _context.Faculties
                    .FirstOrDefault(f => f.TitleFa == record.Faculty);

                if (faculty == null)
                {
                    // اگر فاکتوری وجود ندارد، آن را ایجاد کنید
                    faculty = new Faculty
                    {
                        TitleFa = record.Faculty ?? "",
                        DepartmentId = department.Id
                    };
                    _context.Faculties.Add(faculty);
                    _context.SaveChanges(); // ذخیره‌سازی فاکتوری جدید
                }

                var existingProfessor = _context.ProfessorProfiles.Include(x => x.Faculty)
                    .FirstOrDefault(p => p.NationalCode == record.IdentificationNumber ||
                                          (p.FirstNameFa == record.FirstName && p.LastNameFa == record.LastName && p.Faculty.TitleFa == record.Faculty));

                if (existingProfessor == null)
                {

                    // پیدا کردن یا ایجاد دپارتمان
                    // ایجاد پروفایل جدید استاد
                    var newProfessor = new ProfessorProfile
                    {
                        FirstNameFa = record.FirstName ?? "",
                        LastNameFa = record.LastName ?? "",
                        PersonnelCode = record.UserNumber ?? "",
                        NationalCode = record.NationalCode ?? "",
                        FacultyId = faculty.Id, // انتساب فاکتوری به استاد
                        EmployeeNumber = record.EmployeeNumber ?? "",
                        FinancialCode = record.FinancialCode ?? "",
                        IdentificationNumber = record.IdentificationNumber ?? "",
                    };

                    _context.ProfessorProfiles.Add(newProfessor);
                }
                else
                {
                    // به‌روزرسانی پروفایل استاد موجود
                    existingProfessor.FirstNameFa = !string.IsNullOrWhiteSpace(record.FirstName) ? record.FirstName : existingProfessor.FirstNameFa;
                    existingProfessor.LastNameFa = !string.IsNullOrWhiteSpace(record.LastName) ? record.LastName : existingProfessor.LastNameFa;
                    existingProfessor.PersonnelCode = !string.IsNullOrWhiteSpace(record.UserNumber) ? record.UserNumber : existingProfessor.PersonnelCode;
                    existingProfessor.NationalCode = !string.IsNullOrWhiteSpace(record.IdentificationNumber) ? record.IdentificationNumber : existingProfessor.NationalCode;
                    existingProfessor.EmployeeNumber = !string.IsNullOrWhiteSpace(record.EmployeeNumber) ? record.EmployeeNumber : existingProfessor.EmployeeNumber;
                    existingProfessor.FinancialCode = !string.IsNullOrWhiteSpace(record.FinancialCode) ? record.FinancialCode : existingProfessor.FinancialCode;
                    existingProfessor.IdentificationNumber = !string.IsNullOrWhiteSpace(record.IdentificationNumber) ? record.IdentificationNumber : existingProfessor.IdentificationNumber;



                    existingProfessor.FacultyId = faculty.Id;

                    _context.ProfessorProfiles.Update(existingProfessor);
                }

                // ذخیره تغییرات در پایگاه داده
                _context.SaveChanges();

            }
        }
    }
}