using CSV2Sql.Models;
using JournalScrapper.Entity;
using Microsoft.EntityFrameworkCore;
using Profile_Shakhsi.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static JournalScrapper.Entity.ISCMySql;

namespace JournalScrapper
{
    public class AppDbContext : DbContext
    {
        internal DbSet<Article> Articles { get; set; }
        internal DbSet<Entity.Author> Authors { get; set; }
        internal DbSet<Entity.Keyword> Keywords { get; set; }
        internal DbSet<Entity.Journal> Journals { get; set; }
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

        public DbSet<ISCMySql.All_Article> All_Articles { get; set; }
        public DbSet<ISCMySql.ResearcherFavorite> ResearcherFavorites { get; set; }
        public DbSet<ISCMySql.Keyword> Keywords { get; set; }
        public DbSet<ISCMySql.Author> Authors { get; set; }
        public DbSet<ISCMySql.ISC_Article> ISC_Articles { get; set; }
        public DbSet<ISCMySql.Author_Article> Author_Articles { get; set; }
        public DbSet<ISCMySql.Journal> Journals { get; set; }
        public DbSet<ISCMySql.Author_ISC> Author_ISCs { get; set; }
        public DbSet<ISCMySql.CitationAll_Article> CitationAll_Articles { get; set; }
        public DbSet<ISCMySql.CitationAuthor> CitationAuthors { get; set; }
        public DbSet<ISCMySql.InputMaster> InputMasters { get; set; }
        public DbSet<ISCMySql.Author_Article_ISC> Author_Article_ISCs { get; set; }

        public DbSet<ProfessorProfile> ProfessorProfiles { get; set; }
        public DbSet<ProfessionalActivity> ProfessionalActivities { get; set; }
        public DbSet<Articles> Articles { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<ResearchArea> ResearchAreas { get; set; }
        public DbSet<WebLink> WebLinks { get; set; }
        public DbSet<Education> Educations { get; set; }
        public DbSet<TeachingInterest> TeachingInterests { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Faculty> Faculties { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Membership> Memberships { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Data Source=.;Initial Catalog=Professor_DB;Integrated Security=true;MultipleActiveResultSets=true;TrustServerCertificate=true;");
        }
    }
    public class ISCMySqlDbContext : DbContext
    {

        public DbSet<ISCMySql.All_Article> scholar_all_article { get; set; }
        public DbSet<ISCMySql.ResearcherFavorite> areas_of_interest { get; set; }
        public DbSet<ISCMySql.Keyword> keywords_articles_isc_xml { get; set; }
        public DbSet<ISCMySql.Author> scholar_profile_authors { get; set; }
        public DbSet<ISCMySql.ISC_Article> article_isc_xml { get; set; }
        public DbSet<ISCMySql.Author_Article> author_article_relation { get; set; }
        public DbSet<ISCMySql.Journal> iranian_journals { get; set; }
        public DbSet<ISCMySql.Author_ISC> authors_isc_xml { get; set; }
        public DbSet<ISCMySql.CitationAll_Article> citation_article_scholar { get; set; }
        public DbSet<ISCMySql.CitationAuthor> all_citation_authors { get; set; }
        //public DbSet<ISCMySql.InputMaster> InputMasters { get; set; }
        public DbSet<ISCMySql.Author_Article_ISC> author_article_isc_relation { get; set; }
 
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql("server=localhost;port=3306;database=info_mysql;user=root;password=", new MySqlServerVersion(new Version(8, 0, 21)));

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<InputMaster>(entity =>
            {
                entity.Property(e => e.AuthorScholarId)
                      .HasColumnType("varchar(255) CHARACTER SET utf8");
            });
            // تنظیمات مدل‌ها (مثال)
            modelBuilder.Entity<ISCMySql.ResearcherFavorite>().HasKey(rf => rf.Id);
            modelBuilder.Entity<ISCMySql.All_Article>().HasKey(a => a.Id);
            modelBuilder.Entity<ISCMySql.Keyword>().HasKey(k => k.Id);
            modelBuilder.Entity<ISCMySql.Author>().HasKey(a => a.Id);
            modelBuilder.Entity<ISCMySql.ISC_Article>().HasKey(i => i.Id);
            modelBuilder.Entity<ISCMySql.Author_Article>().HasKey(aa => aa.Id);
            modelBuilder.Entity<ISCMySql.Journal>().HasKey(j => j.Id);
            modelBuilder.Entity<ISCMySql.Author_ISC>().HasKey(ai => ai.Id);
            modelBuilder.Entity<ISCMySql.CitationAll_Article>().HasKey(ca => ca.Id);
            modelBuilder.Entity<ISCMySql.CitationAuthor>().HasKey(c => c.Id);
            modelBuilder.Entity<ISCMySql.InputMaster>().HasKey(im => im.AuthorScholarId);
            modelBuilder.Entity<ISCMySql.Author_Article_ISC>().HasKey(aai => aai.Id);
        }
    }
}