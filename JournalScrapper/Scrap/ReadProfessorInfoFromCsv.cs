using CsvHelper;
using JournalScrapper.Entity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Profile_Shakhsi.Models.Entity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static JournalScrapper.Entity.Personnel;

namespace JournalScrapper
{
	public class CsvToDatabase
	{
		public static void ReadProfessorInfoFromCsv()
		{
			var _context = new ProfileShakhsiDbContext();
			string extraDirectoryPath = WebScraper.FindDirectoryInParents();
			string csvFilePath = Path.Combine(extraDirectoryPath, "اعضای هیئت علمی.csv"); // مسیر فایل CSV را اینجا مشخص کنید
																								  //string csvFilePath = Path.Combine(extraDirectoryPath, "اعضای هیئت علمی scholar scopus.csv"); // مسیر فایل CSV را اینجا مشخص کنید

			using (var reader = new StreamReader(csvFilePath))
			using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
			{
				csv.Context.RegisterClassMap<ProfessorMap>();
				var records = csv.GetRecords<PersonnelRecord>().ToList();

				foreach (var record in records)
				{
					var professorDB = _context.ProfessorProfiles
	 .FirstOrDefault(f =>
					   RemoveNonLetters(f.FirstNameFa).ToLower().Contains(RemoveNonLetters(record.FirstNameFa).ToLower()) &&
					   RemoveNonLetters(f.LastNameFa).ToLower().Contains(RemoveNonLetters(record.LastNameFa).ToLower()));
					if (professorDB == null)
					{

						var faculty = _context.Faculties
						.FirstOrDefault(f => f.Title == record.Faculty);
						var department= _context.Departments
.FirstOrDefault(f => f.Title == record.Group);

						if (faculty == null)
							_context.Faculties.Add(new Faculty
							{ 
								TitleFa = record.Faculty,
								Title = record.FacultyEn
							});
						if (department == null)
							_context.Departments.Add(new Department
							{
								FacultyId = faculty?.Id ?? 0,
								TitleFa = record.Group,
								Title = record.GroupEn
							});

						professorDB.PersonnelCode = record.PersonnelCode;
						professorDB.NationalCode = Convert.ToInt64(record.NationalCode);
						professorDB.FirstNameFa = record.FirstNameFa;
						professorDB.LastNameFa = record.LastNameFa;
						professorDB.ScopusID = record.ScopusID;
						professorDB.WebOfScienceID = record.WebOfScienceID;
						professorDB.GoogleScholarID = FixFormatScholarId(record.GoogleScholarID);
						professorDB.DepartmentId = department?.Id;
						professorDB.FirstNameEn = record.FirstNameEn;
						professorDB.LastNameEn = record.LastNameEn;
					}
				}

			}
			_context.SaveChanges();
		}
		public static void ReadProfessorScholarFromCsv()
		{
			var _context = new ProfileShakhsiDbContext();
			string extraDirectoryPath = WebScraper.FindDirectoryInParents();
			//string csvFilePath = Path.Combine(extraDirectoryPath, "اطلاعات اعضاء هیات علمی.csv"); // مسیر فایل CSV را اینجا مشخص کنید
			string csvFilePath = Path.Combine(extraDirectoryPath, "اعضای هیئت علمی scholar scopus.csv"); // مسیر فایل CSV را اینجا مشخص کنید

			using (var reader = new StreamReader(csvFilePath))
			using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
			{
				//csv.Context.RegisterClassMap<ProfessorMap>();
				//var records = csv.GetRecords<PersonnelRecord>().ToList();
				var records = csv.GetRecords<ScholarRecord>().ToList();
				var all = _context.ProfessorProfiles.Include(x => x.Department).ToList();
				foreach (var record in records)
				{
					var professorDB = all
	 .FirstOrDefault(f =>
					   RemoveNonLetters(f.FirstNameFa).ToLower().Contains(RemoveNonLetters(record.Name).ToLower()) &&
					   RemoveNonLetters(f.LastNameFa).ToLower().Contains(RemoveNonLetters(record.LastName).ToLower()));
					if (professorDB != null)
					{

						//var faculty = _context.Faculties
						//.FirstOrDefault(f => f.Title == record.Faculty);
						professorDB.GoogleScholarID = record.ScholarID;
						professorDB.ScopusID = record.ScopusID;
						professorDB.FirstNameEn = record.NameEn.Split(" ").FirstOrDefault();
						professorDB.LastNameEn = string.Join(" ", record.NameEn.Split(" ").Skip(1));
						if (int.TryParse(record.Citation, out int cite))
							professorDB.CitedBy = 0;
						professorDB.CitedBy = cite;
						professorDB.UniversityEmail = record.Email;
						professorDB.Affiliation = record.Affiliation;
						//professor.UserIdentifierEn = record.FirstNameEn.RemoveNonLettersWithSpace().Replace(" ", "-") + "-" + record.LastNameEn.RemoveNonLettersWithSpace().Replace(" ", "-");
						//if (_context.ProfessorProfiles.Any(x => x.UserIdentifierEn == professor.UserIdentifierEn))
						//	professor.UserIdentifierEn = professor.UserIdentifierEn + "-2";
						//if (_context.ProfessorProfiles.Any(x => x.UserIdentifierEn == professor.UserIdentifierEn))
						//	professor.UserIdentifierEn = professor.UserIdentifierEn.Replace("-2", "") + "-3";

						//professorDB.FacultyId = faculty?.Id ?? 0;
						//professorDB.FirstNameEn = Tool.RemoveNonLettersWithSpace(record.FirstNameEn);
						//professorDB.LastNameEn = Tool.RemoveNonLettersWithSpace(record.LastNameEn);
						//professorDB.UserIdentifierEn = record.FirstNameEn.RemoveNonLettersWithSpace().Replace(" ", "-") + "-" + record.LastNameEn.RemoveNonLettersWithSpace().Replace(" ", "-");

					}
				}

				//foreach (var record in records)
				//            {
				//                var professorDB = _context.ProfessorProfiles.Include(x=>x.Department)
				// .FirstOrDefault(f =>
				//       //RemoveNonLetters(f.FirstNameEn).ToLower().Contains(RemoveNonLetters(record.FirstNameEn).ToLower()) &&
				//       //RemoveNonLetters(f.LastNameEn).ToLower().Contains(RemoveNonLetters(record.LastNameEn).ToLower()));
				//       f.EmployeeNumber == Convert.ToInt32(record.PersonnelCode));
				//                if (professorDB != null)
				//                {

				//                    //var faculty = _context.Faculties
				//                    //.FirstOrDefault(f => f.Title == record.Faculty);

				//                    professorDB.ScopusID = record.ScopusID;
				//                    professorDB.WebOfScienceID = record.WebOfScienceID;
				//                    professorDB.GoogleScholarID = FixFormatScholarId(record.GoogleScholarID);
				//                    //professorDB.FacultyId = faculty?.Id ?? 0;
				//                    professorDB.FirstNameEn = Tool.RemoveNonLettersWithSpace(record.FirstNameEn);
				//                    professorDB.LastNameEn = Tool.RemoveNonLettersWithSpace(record.LastNameEn);
				//                    professorDB.UserIdentifierEn = record.FirstNameEn.RemoveNonLettersWithSpace().Replace(" ", "-") + "-" + record.LastNameEn.RemoveNonLettersWithSpace().Replace(" ", "-");

				//                }
				//                else
				//                {
				//                    record.Faculty = Tool.RemoveNonLettersWithSpace(record.Faculty);
				//                    record.Group = Tool.RemoveNonLettersWithSpace(record.Group);
				//                    record.FirstNameEn = Tool.RemoveNonLettersWithSpace(record.FirstNameEn);
				//                    record.FirstNameFa = Tool.RemoveNonLettersWithSpace(record.FirstNameFa);
				//                    record.LastNameEn = Tool.RemoveNonLettersWithSpace(record.LastNameEn);
				//                    record.LastNameFa = Tool.RemoveNonLettersWithSpace(record.LastNameFa);

				//                    var faculty = _context.Faculties.FirstOrDefault(x => x.TitleFa.Contains(record.Faculty) || x.Title.Contains(record.Faculty));
				//                    if (faculty == null)
				//                    {
				//                        faculty = new Faculty();
				//                        if (ExtractXml.ContainsPersianCharacters(record.Faculty) ?? false)
				//                            faculty.TitleFa = record.Faculty;
				//                        else
				//                            faculty.Title = record.Faculty;
				//                        _context.Add(faculty);
				//                        _context.SaveChanges();
				//                    }

				//                    var department = _context.Departments.FirstOrDefault(x => x.TitleFa.Contains(record.Group) || x.Title.Contains(record.Group));
				//                    if (department == null)
				//                    {
				//                        department = new Department() { FacultyId = faculty.Id };
				//                        if (ExtractXml.ContainsPersianCharacters(record.Group) ?? false)
				//                            department.TitleFa = record.Group;
				//                        else
				//                            department.Title = record.Group;
				//                        _context.Add(department);
				//                        _context.SaveChanges();
				//                    }

				//                    var professor = new ProfessorProfile();
				//                    professor.EmployeeNumber = record.PersonnelCode.ToInt();
				//                    professor.NationalCode = record.NationalCode.ToLong();
				//                    professor.FirstNameFa = record.FirstNameFa.RemoveNonLettersWithSpace();
				//                    professor.LastNameFa = record.LastNameFa.RemoveNonLettersWithSpace();
				//                    professor.ScopusID = record.ScopusID;
				//                    professor.WebOfScienceID = record.WebOfScienceID;
				//                    professor.GoogleScholarID = FixFormatScholarId(record.GoogleScholarID);
				//                    professor.DepartmentId = department.Id;
				//                    professor.FirstNameEn = record.FirstNameEn.RemoveNonLettersWithSpace();
				//                    professor.LastNameEn = record.LastNameEn.RemoveNonLettersWithSpace();
				//                    professor.UserIdentifierEn = record.FirstNameEn.RemoveNonLettersWithSpace().Replace(" ", "-") + "-" + record.LastNameEn.RemoveNonLettersWithSpace().Replace(" ", "-");
				//                    if (_context.ProfessorProfiles.Any(x => x.UserIdentifierEn == professor.UserIdentifierEn))
				//                        professor.UserIdentifierEn = professor.UserIdentifierEn + "-2";
				//                    if (_context.ProfessorProfiles.Any(x => x.UserIdentifierEn == professor.UserIdentifierEn))
				//                        professor.UserIdentifierEn = professor.UserIdentifierEn.Replace("-2", "") + "-3";

				//                    _context.Add(professor);
				//                }

				//}
				_context.SaveChanges();
			}
		}
		public static string RemoveNonLetters(string input)
		{
			return new string(input.Where(c => char.IsLetter(c)).ToArray());
		}


		public static string EnglishNumber(string input)
		{
			return (new string(input.Where(c => char.IsLetter(c) || c.Equals(" ")).ToArray())).Trim().ToLower();
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
	public static class Tool
	{
		public static int ToInt(this string input)
		{
			return Convert.ToInt32(input.IsNullOrEmpty() ? 0 : new string(input.Where(c => char.IsDigit(c)).ToArray()));
		}
		public static long ToLong(this string input)
		{
			return Convert.ToInt64(input.IsNullOrEmpty() ? 0 : new string(input.Where(c => char.IsDigit(c)).ToArray()));
		}
		public static string RemoveNonLettersWithSpace(this string input)
		{
			return (new string(input.Where(c => char.IsLetter(c) || c == ' ').ToArray())).Trim().ToLower();
		}
	}

}

