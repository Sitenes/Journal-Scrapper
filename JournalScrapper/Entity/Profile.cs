namespace Profile_Shakhsi.Models.Entity
{
    public class Profile
    {
        public class ProfessorProfile
        {
            public int Id { get; set; }
            public string FullNameEn { get; set; } = "";
            public string FirstNameEn { get; set; } = "";
            public string LastNameEn { get; set; } = "";
            public string Degree { get; set; } = "";
            public string DegreeFA { get; set; } = "";
            public string ImageUrl { get; set; } = "";
            public string Position { get; set; } = "";
            public string PositionFA { get; set; } = "";
            public string Department { get; set; } = "";
            public string DepartmentFA { get; set; } = "";
            public string Email { get; set; } = "";
            public string Phone { get; set; } = "";
            public string Address { get; set; } = "";
            public string AddressFA { get; set; } = "";
            public string PostalCode { get; set; } = "";
            public string AreaOfStudy { get; set; } = "";
            public string AreaOfStudyFA { get; set; } = "";
            public string Research { get; set; } = "";
            public string ResearchFA { get; set; } = "";

            public string PersonnelCode { get; set; } = "";
            public string NationalCode { get; set; } = "";
            public string FirstNameFa { get; set; } = "";
            public string LastNameFa { get; set; } = "";
            public string ScopusID { get; set; } = "";
            public string WebOfScienceID { get; set; } = "";
            public string GoogleScholarID { get; set; } = "";
            public string Faculty { get; set; } = "";



            public virtual ICollection<Articles>? Articles { get; set; }
            public virtual ICollection<Membership>? Memberships { get; set; }
            public virtual ICollection<ProfessionalActivity>? ProfessionalActivities { get; set; }
            public virtual ICollection<TeachingInterest>? TeachingInterests { get; set; }
            public virtual ICollection<Course>? Courses { get; set; }
            public virtual ICollection<ResearchArea>? ResearchAreas { get; set; }
            public virtual ICollection<WebLink>? WebLinks { get; set; }
            public virtual ICollection<ProfessorLink>? Links { get; set; }
            public virtual ICollection<Education>? Educations { get; set; }
            public virtual ICollection<Book>? Books { get; set; }
        }

        public class Articles : IProfessorEntity
        {
            public string Link { get; set; } = "";
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

        public class WebLink : IProfessorEntity
        {
            public string Link { get; set; } = "";
        }

        public class ProfessorLink : IProfessorEntity
        {
            public string Link { get; set; } = "";
        }

        public class Education : IProfessorEntity
        {
        }
        public class Book : IProfessorEntity
        {

        }
        public abstract class IProfessorEntity
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string TitleFa { get; set; } = "";
            public int ProfessorProfileId { get; set; }
            public virtual ProfessorProfile? ProfessorProfile { get; set; }
        }
    }
}
