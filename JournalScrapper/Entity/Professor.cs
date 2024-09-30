namespace Profile_Shakhsi.Models.Entity
{
    public class Professor
    {
        public class ProfessorProfile
        {
            public int Id { get; set; }
            public string FullName { get; set; }
            public string FullNameFA { get; set; }
            public string Degree { get; set; }
            public string DegreeFA { get; set; }
            public string ImageUrl { get; set; }
            public string Position { get; set; }
            public string PositionFA { get; set; }
            public string Department { get; set; }
            public string DepartmentFA { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string Address { get; set; }
            public string AddressFA { get; set; }
            public string PostalCode { get; set; }
            public string AreaOfStudy { get; set; }
            public string AreaOfStudyFA { get; set; }
            public string Research { get; set; }
            public string ResearchFA { get; set; }
            public string Membership { get; set; }
            public string MembershipFA { get; set; }
            public string ProfessionalActivities { get; set; }
            public string ProfessionalActivitiesFA { get; set; }

            public virtual ICollection<Articles> Articles { get; set; }
            public virtual ICollection<TeachingInterest> TeachingInterests { get; set; }
            public virtual ICollection<Course> Courses { get; set; }
            public virtual ICollection<ResearchArea> ResearchAreas { get; set; }
            public virtual ICollection<WebLink> WebLinks { get; set; }
            public virtual ICollection<ProfessorLink> Links { get; set; }
            public virtual ICollection<Education> Educations { get; set; }
            public virtual ICollection<Book> Books { get; set; }
        }

        public class Articles
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string TitleFA { get; set; }
            public string Link { get; set; }
            public int ProfessorProfileId { get; set; }
            public virtual ProfessorProfile ProfessorProfile { get; set; }
        }

        public class Course
        {
            public int Id { get; set; }
            public string CourseName { get; set; }
            public string CourseNameFA { get; set; }
            public int ProfessorProfileId { get; set; }
            public virtual ProfessorProfile ProfessorProfile { get; set; }
        }

        public class TeachingInterest
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string TitleFA { get; set; }
            public int ProfessorProfileId { get; set; }
            public virtual ProfessorProfile ProfessorProfile { get; set; }
        }

        public class ResearchArea
        {
            public int Id { get; set; }
            public string AreaName { get; set; }
            public string AreaNameFA { get; set; }
            public int ProfessorProfileId { get; set; }
            public virtual ProfessorProfile ProfessorProfile { get; set; }
        }

        public class WebLink
        {
            public int Id { get; set; }
            public string Link { get; set; }
            public string Name { get; set; }
            public string NameFA { get; set; }
            public int ProfessorProfileId { get; set; }
            public virtual ProfessorProfile ProfessorProfile { get; set; }
        }

        public class ProfessorLink
        {
            public int Id { get; set; }
            public string Link { get; set; }
            public string Name { get; set; }
            public string NameFA { get; set; }
            public int ProfessorProfileId { get; set; }
            public virtual ProfessorProfile ProfessorProfile { get; set; }
        }

        public class Education
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string TitleFA { get; set; }
            public int ProfessorProfileId { get; set; }
            public virtual ProfessorProfile ProfessorProfile { get; set; }
        }
        public class Book
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string TitleFA { get; set; }
            public int ProfessorProfileId { get; set; }
            public virtual ProfessorProfile ProfessorProfile { get; set; }
        }
    }
}
