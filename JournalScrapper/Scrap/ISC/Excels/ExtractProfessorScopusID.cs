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
    public class ExtractProfessorScopusID
    {
        private readonly DynamicDbContext _context;

        public ExtractProfessorScopusID(DynamicDbContext context)
        {
            _context = context;
        }

        public void ReadDataFromExcel()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            string scopusExcelPath = FileTools.FindDirectoryInParents() + @"\ScopusID.xlsx";

            // دیکشنری برای نگهداری ScopusID بر اساس نام کامل انگلیسی
            Dictionary<string, string> scopusIdMap = new(StringComparer.OrdinalIgnoreCase);

            using (var scopusPackage = new ExcelPackage(new FileInfo(scopusExcelPath)))
            {
                var scopusSheet = scopusPackage.Workbook.Worksheets[0];
                int scopusRowCount = scopusSheet.Dimension.End.Row;

                for (int row = 2; row <= scopusRowCount; row++)
                {
                    string firstNameEn = scopusSheet.Cells[row, 1].Text.Trim();
                    string lastNameEn = scopusSheet.Cells[row, 2].Text.Trim();
                    string scopusId = scopusSheet.Cells[row, 3].Text.Trim();

                    if (!string.IsNullOrEmpty(firstNameEn) && !string.IsNullOrEmpty(lastNameEn) && !string.IsNullOrEmpty(scopusId))
                    {
                        string key = (firstNameEn + " " + lastNameEn).ToLower();
                        if (!scopusIdMap.ContainsKey(key))
                            scopusIdMap[key] = scopusId;
                    }
                }
            }

            // دریافت همه اساتید از دیتابیس
            var professors = _context.Professors.ToList();
            int updatedCount = 0;

            foreach (var professor in professors)
            {
                string fullNameEn = (professor.FirstNameEn + " " + professor.LastNameEn).Trim().ToLower();

                if (string.IsNullOrWhiteSpace(professor.ScopusID) && scopusIdMap.ContainsKey(fullNameEn))
                {
                    professor.ScopusID = scopusIdMap[fullNameEn];
                    updatedCount++;
                }
            }

            _context.SaveChanges();
            Console.WriteLine($"✅ Done. Updated {updatedCount} professors' ScopusID.");
        }


    }
}
