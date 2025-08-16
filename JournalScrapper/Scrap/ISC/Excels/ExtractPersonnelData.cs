using CsvHelper;
using DataLayer;
using Entities.Models.Entities;
using JournalScrapper.Tool;
using JournalScrappers.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;

namespace JournalScrappers
{
    public class ExtractPersonnelData
    {
        private readonly DynamicDbContext _context;
        private readonly WebScraper _scraper;

        public ExtractPersonnelData(DynamicDbContext context, WebScraper scraper)
        {
            this._context = context;
            this._scraper = scraper;
        }
        public void ReadPersonnelDataFromCsv()
        {
            string extraDirectoryPath = FileTools.FindDirectoryInParents() + "\\Info asatid.csv";
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
                try
                {
                    //if (Convert.ToInt64(record.EmployeeNumber) > 10000)
                    //    continue;
                    //Convert.ToInt64(record.EmployeeNumber);

                }
                catch (Exception)
                {
                    continue;
                }

                var properties = typeof(FacultyMember).GetProperties();
                foreach (var property in properties)
                {
                    if (property.PropertyType == typeof(string))
                    {
                        var value = property.GetValue(record) as string;
                        if (!string.IsNullOrEmpty(value))
                        {
                            var updatedValue = value.Replace("ي", "ی").Replace("faculty of", "", StringComparison.CurrentCultureIgnoreCase).Replace("department of", "", StringComparison.CurrentCultureIgnoreCase).ToLower();
                            property.SetValue(record, updatedValue);
                        }
                    }
                }
                // بررسی و به‌روزرسانی دپارتمان


                // بررسی و به‌روزرسانی فاکتوری
                var faculty = _context.Faculties
                    .FirstOrDefault(f => f.TitleFa == record.Faculty);

                if (faculty == null)
                {
                    // اگر فاکتوری وجود ندارد، آن را ایجاد کنید
                    faculty = new Faculty
                    {
                        TitleFa = record.Faculty.Trim() ?? "",
                        Title = record.FacultyEN.Trim()
                    };
                    _context.Faculties.Add(faculty);
                    _context.SaveChanges(); // ذخیره‌سازی فاکتوری جدید
                }

                var department = _context.Departments
                .FirstOrDefault(d => d.TitleFa == record.Department);

                if (department == null)
                {
                    // اگر دپارتمان وجود ندارد، آن را ایجاد کنید
                    department = new Department
                    {
                        TitleFa = record.Department.Trim() ?? "",
                        FacultyId = faculty.Id,
                        Title = record.DepartmentEN.Trim()
                    };
                    _context.Departments.Add(department);
                    _context.SaveChanges(); // ذخیره‌سازی دپارتمان جدید
                }
                Professor? existingProfessor = null;
                try
                {
                    long.TryParse(record.EmployeeNumber, out long employeenum);
                    existingProfessor = _context.Professors.Include(x => x.Department)
                    .FirstOrDefault(p => p.PersonnelCode == record.UserNumber || p.EmployeeNumber == employeenum);
                }
                catch (Exception)
                {
                }


                if (existingProfessor == null)
                {
                    var employeeNumber = 0;

                    try
                    {
                        employeeNumber = Convert.ToInt32(record.EmployeeNumber.Split("-").FirstOrDefault());
                    }
                    catch (Exception)
                    {
                    }

                    var newProfessor = new Professor
                    {
                        FirstNameFa = record.FirstName ?? "",
                        LastNameFa = record.LastName ?? "",
                        PersonnelCode = record.UserNumber ?? "",
                        NationalCode = Convert.ToInt64(record.NationalCode.IsNullOrEmpty() ? 0 : record.NationalCode),
                        DepartmentId = department.Id, // انتساب فاکتوری به استاد
                        EmployeeNumber = employeeNumber,
                        FinancialCode = Convert.ToInt32(record.FinancialCode.IsNullOrEmpty() ? 0 : record.FinancialCode),
                        IdentificationNumber = Convert.ToInt32(record.IdentificationNumber.IsNullOrEmpty() ? 0 : record.IdentificationNumber),
                        UserIdentifierEn = (record.FirstName?.Trim() + "-" + record.LastName?.Trim()).Replace(" ", "-"),
                    };
                    newProfessor.UserIdentifierEn = record.FirstName?.RemoveNonLettersWithSpace().Replace(" ", "-") + "-" + record.LastName?.RemoveNonLettersWithSpace().Replace(" ", "-");
                    if (_context.Professors.Any(x => x.UserIdentifierEn == newProfessor.UserIdentifierEn))
                        newProfessor.UserIdentifierEn = newProfessor.UserIdentifierEn + "-2";
                    if (_context.Professors.Any(x => x.UserIdentifierEn == newProfessor.UserIdentifierEn))
                        newProfessor.UserIdentifierEn = newProfessor.UserIdentifierEn.Replace("-2", "") + "-3";

                    _context.Professors.Add(newProfessor);
                }
                //else
                //{
                //    // به‌روزرسانی پروفایل استاد موجود
                //    existingProfessor.FirstNameFa = !string.IsNullOrWhiteSpace(record.FirstName) ? record.FirstName : existingProfessor.FirstNameFa;
                //    existingProfessor.LastNameFa = !string.IsNullOrWhiteSpace(record.LastName) ? record.LastName : existingProfessor.LastNameFa;
                //    existingProfessor.PersonnelCode = !string.IsNullOrWhiteSpace(record.UserNumber) ? record.UserNumber : existingProfessor.PersonnelCode;
                //    existingProfessor.NationalCode = !string.IsNullOrWhiteSpace(record.IdentificationNumber) ? Convert.ToInt64(record.IdentificationNumber.IsNullOrEmpty() ? 0 : record.IdentificationNumber) : existingProfessor.NationalCode;
                //    existingProfessor.EmployeeNumber = !string.IsNullOrWhiteSpace(record.EmployeeNumber) ? Convert.ToInt32(record.EmployeeNumber.IsNullOrEmpty() ? 0 : record.EmployeeNumber) : existingProfessor.EmployeeNumber;
                //    existingProfessor.FinancialCode = !string.IsNullOrWhiteSpace(record.FinancialCode) ? Convert.ToInt32(record.FinancialCode.IsNullOrEmpty() ? 0 : record.FinancialCode) : existingProfessor.FinancialCode;
                //    existingProfessor.IdentificationNumber = !string.IsNullOrWhiteSpace(record.IdentificationNumber) ? Convert.ToInt32(record.IdentificationNumber.IsNullOrEmpty() ? 0 : record.IdentificationNumber) : existingProfessor.IdentificationNumber;

                //    existingProfessor.DepartmentId = department.Id;

                //    _context.Professors.Update(existingProfessor);
                //}

                // ذخیره تغییرات در پایگاه داده
                _context.SaveChanges();

            }
        }
    }
}