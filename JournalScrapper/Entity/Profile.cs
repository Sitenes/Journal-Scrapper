using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Profile_Shakhsi.Models.Entity

{
    public class ProfessorProfile
    {
        public int Id { get; set; }
        public string FirstNameEn { get; set; } = "";
        public string LastNameEn { get; set; } = "";
        public string UserIdentifierEn { get; set; } = "";
        public EducationDegree? Degree { get; set; }

        public string ImageUrl { get; set; } = "";
        public string Position { get; set; } = "";
        public string PositionFA { get; set; } = "";
        public string UniversityEmail { get; set; } = "";
        public string PersonalEmail { get; set; } = "";
        public string Phone { get; set; } = "";
        public string MobileNumber { get; set; } = "";
        public string AreaOfStudy { get; set; } = "";
        public string AreaOfStudyFA { get; set; } = "";
        public string Research { get; set; } = "";
        public string ResearchFA { get; set; } = "";

        public string PersonnelCode { get; set; } = "";// کد استادی
        public long NationalCode { get; set; }
        public string FirstNameFa { get; set; } = "";
        public string LastNameFa { get; set; } = "";
        public string ScopusID { get; set; } = "";
        public string WebOfScienceID { get; set; } = "";
        public string GoogleScholarID { get; set; } = "";
        public string BiographyEn { get; set; } = "";
        public string BiographyFa { get; set; } = "";
        public int EmployeeNumber { get; set; }
        public int FinancialCode { get; set; }
        public int IdentificationNumber { get; set; }
        public string? Password { get; set; } = "";
		public string Affiliation { get; set; } = "";
		public int CitedBy { get; set; }
		public int? DepartmentId { get; set; }
        [ForeignKey(nameof(DepartmentId))]
        public Department? Department { get; set; }

        public SocialMedia? SocialMedia { get; set; }

        public virtual ICollection<WebLink>? WebLinks { get; set; }
        public virtual ICollection<Articles>? Articles { get; set; }
        public virtual ICollection<Membership>? Memberships { get; set; }
        public virtual ICollection<ProfessionalActivity>? ProfessionalActivities { get; set; }
        public virtual ICollection<TeachingInterest>? TeachingInterests { get; set; }
        public virtual ICollection<Course>? Courses { get; set; }
        public virtual ICollection<ResearchArea>? ResearchAreas { get; set; }
        public virtual ICollection<Education>? Educations { get; set; }
        public virtual ICollection<Book>? Books { get; set; }
    }

    public class Articles : IProfessorLinkEntity
    {
    }

    public class Course : IProfessorEntity
    {
    }
    public class Membership : IProfessorEntity
    {
    }
    public class ProfessionalActivity : IProfessorEntity
    {
    }

    public class TeachingInterest : IProfessorEntity
    {
    }

    public class ResearchArea : IProfessorEntity
    {
    }
    public class WebLink : IProfessorLinkEntity
    {

    }
    public class SocialMedia
    {
        [Key]
        public int Id { get; set; }
        public string LinkedIn { get; set; } = "";
        public string Orcid { get; set; } = "";
        public string ResearchGate { get; set; } = "";
        public string PersonalWebsite { get; set; } = "";
        public string Scholar { get; set; } = "";
        public string Scopus { get; set; } = "";
        public string ISI { get; set; } = "";
        public string FaceBook { get; set; } = "";
        public string Gmail { get; set; } = "";
        public string Instagram { get; set; } = "";
        public string Twitter { get; set; } = "";
        public string WebOfScience { get; set; } = "";
        [JsonIgnore]
        public int ProfessorProfileId { get; set; }
        [JsonIgnore]
        public virtual ProfessorProfile? ProfessorProfile { get; set; }
    }
    public class Education : IProfessorEntity
    {
        public string UniversityFa { get; set; } = "";
        public string UniversityEn { get; set; } = "";

        public EducationDegree? Degree { get; set; }

        public string CountryFa { get; set; } = "";
        public string CountryEn { get; set; } = "";

        public string CityFa { get; set; } = "";
        public string CityEn { get; set; } = "";
    }
    public class Book : IProfessorEntity
    {

    }
    public abstract class IProfessorEntity : IPersianEntity
    {
        [JsonIgnore]
        public int ProfessorProfileId { get; set; }
        [JsonIgnore]
        [ForeignKey(nameof(ProfessorProfileId))]
        public virtual ProfessorProfile? ProfessorProfile { get; set; }
    }
    public abstract class IProfessorLinkEntity : IProfessorEntity
    {
        public string Link { get; set; } = "";
    }
    public abstract class IPersianEntity
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string TitleFa { get; set; } = "";
    }
    public class Faculty : IPersianEntity
    {

    }
    public class Department : IPersianEntity
    {
        public int FacultyId { get; set; }
        public Faculty? Faculty { get; set; }
    }
    public enum EducationDegree// TODO : هر ایتمی که خالی بود موقع اپدیت اضافه نشود
    {
        BA, MA, PhD, Postdoctoral
    }

}