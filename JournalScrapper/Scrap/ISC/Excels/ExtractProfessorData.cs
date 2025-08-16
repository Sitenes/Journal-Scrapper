using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DataLayer;
using Entities.Models.Entities;
using JournalScrapper.Tool;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace ExcelImporter
{
    public class ExtractPersonnelDataFromExcel
    {
        private readonly DynamicDbContext _context;

        public ExtractPersonnelDataFromExcel(DynamicDbContext context)
        {
            _context = context;
        }

        public void ReadPersonnelDataFromExcel()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            string excelPath = FileTools.FindDirectoryInParents() + @"\ProfessorsData.xlsx";

            // دریافت کل داده‌ها در رم
            var faculties = _context.Faculties.AsNoTracking().ToList();
            var departments = _context.Departments.AsNoTracking().ToList();
            var professors = _context.Professors.ToList();

            using (var package = new ExcelPackage(new FileInfo(excelPath)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension.End.Row;

                for (int row = 2; row <= rowCount; row++)
                {
                    string lastNameFa = worksheet.Cells[row, 1].Text.NormalizeText();
                    string firstNameFa = worksheet.Cells[row, 2].Text.NormalizeText();
                    string personnelCode = worksheet.Cells[row, 4].Text.NormalizeText();
                    string rankFa = worksheet.Cells[row, 15].Text.NormalizeText();
                    string firstNameEn = worksheet.Cells[row, 7].Text.NormalizeText();
                    string lastNameEn = worksheet.Cells[row, 8].Text.NormalizeText();
                    string universityEmail = worksheet.Cells[row, 12].Text.NormalizeText();
                    string departmentFa = worksheet.Cells[row, 16].Text.NormalizeText().Replace("آموزشی", "").Replace("گروه", "").Trim();
                    string facultyFa = worksheet.Cells[row, 18].Text.NormalizeText().Replace("دانشکده", "").Trim();
                    string departmentEn = worksheet.Cells[row, 5].Text.NormalizeText();
                    string facultyEn = worksheet.Cells[row, 19].Text.NormalizeText();
                    string gender = worksheet.Cells[row, 3].Text.NormalizeText(); // مثلا Mr, Ms

                    // پیدا کردن یا ساختن دانشکده با تطبیق تقریبی
                    var faculty = faculties.FirstOrDefault(f => f.TitleFa == facultyFa);
                    if (faculty == null)
                    {
                        faculty = new Faculty
                        {
                            TitleFa = facultyFa,
                            Title = facultyEn,
                            UnitIdentifier = facultyEn.Replace("  ", " ").Replace("  ", " ").Replace(" ","-"),
                            EstablishmentYear = 0
                        };
                        _context.Faculties.Add(faculty);
                        _context.SaveChanges();
                        faculties.Add(faculty); // افزودن به حافظه
                    }

                    // پیدا کردن یا ساختن گروه آموزشی با تطبیق تقریبی
                    var department = departments.FirstOrDefault(d =>
      //d.FacultyId == faculty.Id &&
      d.TitleFa.Trim() == departmentFa);

                    if (department == null)
                    {
                        department = new Department
                        {
                            TitleFa = departmentFa,
                            Title = departmentEn,
                            UnitIdentifier = departmentEn.Replace("  ", " ").Replace("  ", " ").Replace(" ","-"),
                            EstablishmentYear = 0,
                            FacultyId = faculty.Id
                        };
                        _context.Departments.Add(department);
                        _context.SaveChanges();
                        departments.Add(department); // افزودن به حافظه
                    }
                    else
                    {
                        //if (string.IsNullOrEmpty(department.Title))
                        {
                            department.Title = departmentEn;
                            department.UnitIdentifier = departmentEn;
                            department.FacultyId = faculty.Id;
                            _context.Departments.Update(department);
                            _context.SaveChanges();
                        }
                    }



                    // آپدیت یا ایجاد استاد
                    Professor professor = null;

                    if (!string.IsNullOrWhiteSpace(personnelCode.Trim()))
                    {
                        // اگر کد پرسنلی وجود دارد با آن جستجو کن
                        professor = professors.FirstOrDefault(p => p.PersonnelCode == personnelCode);
                    }
                    else
                    {
                        // اگر کد پرسنلی خالی بود، با نام و نام خانوادگی فارسی جستجو کن (می‌توان اینجا تطبیق دقیق یا با شباهت استفاده کرد)
                        professor = professors.FirstOrDefault(p =>
                            string.Equals(p.FirstNameFa, firstNameFa, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(p.LastNameFa, lastNameFa, StringComparison.OrdinalIgnoreCase));
                    }

                    if (professor != null)
                    {
                        // اگر پیدا شد، آپدیت کن
                        professor.FirstNameFa = firstNameFa;
                        professor.LastNameFa = lastNameFa;
                        professor.FirstNameEn = firstNameEn;
                        professor.LastNameEn = lastNameEn;
                        professor.PositionFA = rankFa;
                        professor.UniversityEmail = universityEmail;
                        professor.DepartmentId = department.Id;
                        professor.Gender = gender;

                        _context.Professors.Update(professor);
                    }
                    else
                    {
                        // اگر پیدا نشد، ایتم جدید بساز و اضافه کن
                        professor = new Professor
                        {
                            PersonnelCode = personnelCode, // ممکنه خالی باشه ولی بذار
                            FirstNameFa = firstNameFa,
                            LastNameFa = lastNameFa,
                            FirstNameEn = firstNameEn,
                            LastNameEn = lastNameEn,
                            PositionFA = rankFa,
                            UniversityEmail = universityEmail,
                            DepartmentId = department.Id
                        };
                        _context.Professors.Add(professor);
                        professors.Add(professor); // برای به‌روزرسانی لیست در حافظه
                    }

                }

                _context.SaveChanges(); // ذخیره یک‌باره
            }

            Console.WriteLine("✅ Done.");
        }

    }
}
