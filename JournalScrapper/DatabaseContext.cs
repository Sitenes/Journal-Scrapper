using CSV2Sql.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static JournalScrapper.Entity.Entity;
using static Profile_Shakhsi.Models.Entity.Profile;

namespace JournalScrapper
{
    public class AppDbContext : DbContext
    {
        internal DbSet<Article> Articles { get; set; }
        internal DbSet<Author> Authors { get; set; }
        internal DbSet<Keyword> Keywords { get; set; }
        internal DbSet<Journal> Journals { get; set; }
        internal DbSet<ISCJournal> ISCJournals { get; set; }
        internal DbSet<Quality> Qualities { get; set; }
        internal DbSet<Year> Years { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Data Source=.;Initial Catalog=JournalScrapper_DB;Integrated Security=true;MultipleActiveResultSets=true;TrustServerCertificate=true;");
        }
    }
    public class ProfileShakhsiDbContext : DbContext
    {
        public DbSet<ProfessorProfile> ProfessorProfiles { get; set; }
        public DbSet<Articles> Articles { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<ResearchArea> ResearchAreas { get; set; }
        public DbSet<WebLink> WebLinks { get; set; }
        public DbSet<Education> Educations { get; set; }
        public DbSet<ProfessorLink> ProfessorLinks { get; set; }
        public DbSet<TeachingInterest> TeachingInterests { get; set; }
        public DbSet<Book> Books { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Data Source=.;Initial Catalog=Professor_DB;Integrated Security=true;MultipleActiveResultSets=true;TrustServerCertificate=true;");
        }
    }
}