using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JournalScrapper.Entity
{
    public class ISCMySql
    {
        public class ResearcherFavorite
        {

            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }
            public string Title { get; set; }
            public string URL { get; set; }
            public int AuthorId { get; set; }
        }

        public class All_Article
        {
            public string Authors { get; set; }
            public string Journal { get; set; }
            public int Year { get; set; }
            public string Title { get; set; }
            public string URL { get; set; }
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }
            public string ArticleScholarId { get; set; }
            public int Citation { get; set; }
            public string MainAuthor { get; set; }
            public string Publication_Date { get; set; }
            public string Volume { get; set; }
            public string Issue { get; set; }
            public string Pages { get; set; }
            public string Publisher { get; set; }
            public string Description { get; set; }
            public string MLA { get; set; }
            public string MPA { get; set; }
            public string Chicago { get; set; }
            public string Harvard { get; set; }
            public string Vancouver { get; set; }

            //        public All_Article(int id, string title, string url, int citation, string journal,
            //                           int year, Date publication_Date, string volume, string issue, string pages, string publisher,
            //                           string description, int totalCitations, string authors, string mainAuthor, string articleScholarId) {
            //            this.Id = id { get; set; }
            //            this.Title = title { get; set; }
            //            this.URL = url { get; set; }
            //            this.Citation = citation { get; set; }
            //            this.Journal = journal { get; set; }
            //            this.Year = year { get; set; }
            //            this.Publication_Date = publication_Date { get; set; }
            //            this.Volume = volume { get; set; }
            //            this.Issue = issue { get; set; }
            //            this.Pages = pages { get; set; }
            //            this.Publisher = publisher { get; set; }
            //            this.Description = description { get; set; }
            //            this.TotalCitations = totalCitations { get; set; }
            //            this.Authors = authors { get; set; }
            //            this.MainAuthor = mainAuthor { get; set; }
            //            this.ArticleScholarId = articleScholarId { get; set; }
            //        }

            //public All_Article()
            //{
            //}
        }

        public class Keyword
        {

            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }
            public int ArticleId { get; set; }
            public string ValueFA { get; set; }
            public string ValueEN { get; set; }
        }

        public class Author
        {
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }
            public int Citations { get; set; }
            public int Hindex { get; set; }
            public int i10index { get; set; }
            public int CitationsSince2019 { get; set; }
            public int HindexSince2019 { get; set; }
            public int i10indexSince2019 { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string AuthorScholarId { get; set; }

            public List<ResearcherFavorite> ResearcherFavorites { get; set; }
        }

        public class ISC_Article
        {
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }
            public string PublisherNameFA { get; set; }
            public string PublisherNameEN { get; set; }
            public string Issn { get; set; }
            public string Volume { get; set; }
            public string Issue { get; set; }
            public DateTime PubDate { get; set; }
            public DateTime PubDateReceived { get; set; }
            public string ArticleTitleFA { get; set; }
            public string VernacularTitleEN { get; set; }
            public string FirstPage { get; set; }
            public string LastPage { get; set; }
            public string pii { get; set; }
            public string doi { get; set; }
            public string PublicationType { get; set; }
            public string Abstract { get; set; }
            public string OtherAbstract { get; set; }
            public string ArchiveCopySource { get; set; }
            public string JournalTitleFA { get; set; }
            public string CorrespondingAuthor { get; set; }
            public string CorrespondingAuthorEmail { get; set; }
            public string PDFFilePath { get; set; }
            public int AllArticleId { get; set; } // اضافه کردن فیلد برای کلید خارجی

            //public ISC_Article()
            //{
            //}

            //public ISC_Article(string publisherNameFA, string publisherNameEN, string issn, string volume, string issue,
            //                   DateTime pubDate, string articleTitleFA, string vernacularTitleEN, string firstPage, string lastPage,
            //                   string pii, string doi, string publicationType, string abstractEN, string otherAbstract,
            //                   string archiveCopySource, string journalTitleFA, string correspondingAuthor,
            //                   string correspondingAuthorEmail, string pdfFilePath, int allArticleId)
            //{
            //    this.PublisherNameFA = publisherNameFA { get; set; }
            //    this.PublisherNameEN = publisherNameEN { get; set; }
            //    this.Issn = issn { get; set; }
            //    this.Volume = volume { get; set; }
            //    this.Issue = issue { get; set; }
            //    this.PubDate = pubDate { get; set; }
            //    this.ArticleTitleFA = articleTitleFA { get; set; }
            //    this.VernacularTitleEN = vernacularTitleEN { get; set; }
            //    this.FirstPage = firstPage { get; set; }
            //    this.LastPage = lastPage { get; set; }
            //    this.pii = pii { get; set; }
            //    this.doi = doi { get; set; }
            //    this.PublicationType = publicationType { get; set; }
            //    this.Abstract = abstractEN { get; set; }
            //    this.OtherAbstract = otherAbstract { get; set; }
            //    this.ArchiveCopySource = archiveCopySource { get; set; }
            //    this.JournalTitleFA = journalTitleFA { get; set; }
            //    this.CorrespondingAuthor = correspondingAuthor { get; set; }
            //    this.CorrespondingAuthorEmail = correspondingAuthorEmail { get; set; }
            //    this.PDFFilePath = pdfFilePath { get; set; }
            //    this.AllArticleId = allArticleId { get; set; }
            //}
        }

        public class Author_Article
        {
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }
            public int AuthorId { get; set; }
            public int ArticleId { get; set; }
            public string ArticleScholarId { get; set; }

            //public Author_Article(int AuthorId, int articleId, string articleScholarId)
            //{
            //    this.AuthorId = AuthorId { get; set; }
            //    this.ArticleId = articleId { get; set; }
            //    this.ArticleScholarId = articleScholarId { get; set; }
            //}
        }

        public class Journal
        {
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }
            public string Title { get; set; }
            public string URL { get; set; }
            public bool AzadJournal { get; set; }
            public bool Msrt { get; set; }
            public bool HozeJournal { get; set; }
            public bool MedicalJournal { get; set; }
            public int GroupId { get; set; }
            public string GroupName { get; set; }
            public int SubGroupId { get; set; }
            public string SubGroupName { get; set; }
            public string PIssn { get; set; }
            public string EIssn { get; set; }
            //public Journal(int id, string title, string url, bool azadJournal, bool msrt, bool hozeJournal, bool medicalJournal, int groupId, string groupName, int subGroupId, string subGroupName, string pIssn, string eIssn)
            //{
            //    this.Id = id { get; set; }
            //    this.Title = title { get; set; }
            //    this.URL = url { get; set; }
            //    this.AzadJournal = azadJournal { get; set; }
            //    this.Msrt = msrt { get; set; }
            //    this.HozeJournal = hozeJournal { get; set; }
            //    this.MedicalJournal = medicalJournal { get; set; }
            //    this.GroupId = groupId { get; set; }
            //    this.GroupName = groupName { get; set; }
            //    this.SubGroupId = subGroupId { get; set; }
            //    this.SubGroupName = subGroupName { get; set; }
            //    this.PIssn = pIssn { get; set; }
            //    this.EIssn = eIssn { get; set; }
            //}
            //public Journal()
            //{
            //}
        }

        public class Author_ISC
        {
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }
            public string FirstNameFA { get; set; }
            public string LastNameFA { get; set; }
            public string FirstNameEN { get; set; }
            public string LastNameEN { get; set; }
            public string IdentifierORCID { get; set; }
            public string AffiliationFA { get; set; }
            public string AffiliationEN { get; set; }

            //public Author_ISC(int id, string firstNameFA, string lastNameFA, string firstNameEN, string lastNameEN, string identifierORCID, string affiliationFA, string affiliationEN)
            //{
            //    this.Id = id { get; set; }
            //    this.FirstNameFA = firstNameFA { get; set; }
            //    this.LastNameFA = lastNameFA { get; set; }
            //    this.FirstNameEN = firstNameEN { get; set; }
            //    this.LastNameEN = lastNameEN { get; set; }
            //    this.IdentifierORCID = identifierORCID { get; set; }
            //    this.AffiliationFA = affiliationFA { get; set; }
            //    this.AffiliationEN = affiliationEN { get; set; }
            //}

            //public Author_ISC()
            //{
            //}
        }

        public class CitationAll_Article
        {
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }
            public int Year { get; set; }
            public int Value { get; set; }
            public int ArticleId { get; set; }

            //public CitationAll_Article(int id, int year, int value, int articleId)
            //{
            //    this.Id = id { get; set; }
            //    this.Year = year { get; set; }
            //    this.Value = value { get; set; }
            //    this.ArticleId = articleId { get; set; }
            //}

            //public CitationAll_Article()
            //{
            //}
        }

        public class CitationAuthor
        {
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }
            public int year { get; set; }
            public int value { get; set; }
            public int authorId { get; set; }

            //public CitationAuthor(int year, int value, int authorId)
            //{
            //    this.year = year { get; set; }
            //    this.value = value { get; set; }
            //    this.authorId = authorId { get; set; }
            //}

            //public CitationAuthor()
            //{
            //}
        }

        public class InputMaster
        {
            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public string AuthorScholarId { get; set; }
            public int GroupId { get; set; }
            public int SubGroupId { get; set; }

            //public InputMaster(string authorScholarId, int groupId, int subGroupId)
            //{
            //    this.AuthorScholarId = authorScholarId { get; set; }
            //    this.GroupId = groupId { get; set; }
            //    this.SubGroupId = subGroupId { get; set; }
            //}

            //public InputMaster()
            //{
            //}
        }

        public class Author_Article_ISC
        {
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }
            public int AuthorId { get; set; }
            public int ArticleId { get; set; }
            public int OrderNumber { get; set; }


            //public Author_Article_ISC(int AuthorId, int ArticleId, int OrderNumber)
            //{
            //    this.AuthorId = AuthorId { get; set; }
            //    this.ArticleId = ArticleId { get; set; }
            //    this.OrderNumber = OrderNumber { get; set; }
            //}

            //public Author_Article_ISC()
            //{
            //}
        }

    }
}
