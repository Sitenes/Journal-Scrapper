using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JournalScrapper.Entity
{
    public class ISCMySql
    {
        public  class ResearcherFavorite
        {
            public int Id;
            public string Title;
            public string URL;
            public int AuthorId;
        }

        public  class All_Article
        {
            public string Authors;
            public string Journal;
            public int Year;
            public string Title;
            public string URL;
            [Key]
            public int Id;
            public string ArticleScholarId;
            public int Citation;
            public string MainAuthor;
            public string Publication_Date;
            public string Volume;
            public string Issue;
            public string Pages;
            public string Publisher;
            public string Description;
            public string MLA;
            public string MPA;
            public string Chicago;
            public string Harvard;
            public string Vancouver;

            //        public All_Article(int id, string title, string url, int citation, string journal,
            //                           int year, Date publication_Date, string volume, string issue, string pages, string publisher,
            //                           string description, int totalCitations, string authors, string mainAuthor, string articleScholarId) {
            //            this.Id = id;
            //            this.Title = title;
            //            this.URL = url;
            //            this.Citation = citation;
            //            this.Journal = journal;
            //            this.Year = year;
            //            this.Publication_Date = publication_Date;
            //            this.Volume = volume;
            //            this.Issue = issue;
            //            this.Pages = pages;
            //            this.Publisher = publisher;
            //            this.Description = description;
            //            this.TotalCitations = totalCitations;
            //            this.Authors = authors;
            //            this.MainAuthor = mainAuthor;
            //            this.ArticleScholarId = articleScholarId;
            //        }

            //public All_Article()
            //{
            //}
        }

        public  class Keyword
        {
            public int Id;
            public int ArticleId;
            public string ValueFA;
            public string ValueEN;
        }

        public  class Author
        {
            public int Id;
            public int Citations;
            public int Hindex;
            public int i10index;
            public int CitationsSince2019;
            public int HindexSince2019;
            public int i10indexSince2019;
            public string FirstName;
            public string LastName;
            public string AuthorScholarId;

            public List<ResearcherFavorite> ResearcherFavorites;
        }

        public  class ISC_Article
        {
            public int Id;
            public string PublisherNameFA;
            public string PublisherNameEN;
            public string Issn;
            public string Volume;
            public string Issue;
            public DateTime PubDate;
            public DateTime PubDateReceived;
            public string ArticleTitleFA;
            public string VernacularTitleEN;
            public string FirstPage;
            public string LastPage;
            public string pii;
            public string doi;
            public string PublicationType;
            public string Abstract;
            public string OtherAbstract;
            public string ArchiveCopySource;
            public string JournalTitleFA;
            public string CorrespondingAuthor;
            public string CorrespondingAuthorEmail;
            public string PDFFilePath;
            public int AllArticleId; // اضافه کردن فیلد برای کلید خارجی

            //public ISC_Article()
            //{
            //}

            //public ISC_Article(string publisherNameFA, string publisherNameEN, string issn, string volume, string issue,
            //                   DateTime pubDate, string articleTitleFA, string vernacularTitleEN, string firstPage, string lastPage,
            //                   string pii, string doi, string publicationType, string abstractEN, string otherAbstract,
            //                   string archiveCopySource, string journalTitleFA, string correspondingAuthor,
            //                   string correspondingAuthorEmail, string pdfFilePath, int allArticleId)
            //{
            //    this.PublisherNameFA = publisherNameFA;
            //    this.PublisherNameEN = publisherNameEN;
            //    this.Issn = issn;
            //    this.Volume = volume;
            //    this.Issue = issue;
            //    this.PubDate = pubDate;
            //    this.ArticleTitleFA = articleTitleFA;
            //    this.VernacularTitleEN = vernacularTitleEN;
            //    this.FirstPage = firstPage;
            //    this.LastPage = lastPage;
            //    this.pii = pii;
            //    this.doi = doi;
            //    this.PublicationType = publicationType;
            //    this.Abstract = abstractEN;
            //    this.OtherAbstract = otherAbstract;
            //    this.ArchiveCopySource = archiveCopySource;
            //    this.JournalTitleFA = journalTitleFA;
            //    this.CorrespondingAuthor = correspondingAuthor;
            //    this.CorrespondingAuthorEmail = correspondingAuthorEmail;
            //    this.PDFFilePath = pdfFilePath;
            //    this.AllArticleId = allArticleId;
            //}
        }

        public  class Author_Article
        {
            public int Id;
            public int AuthorId;
            public int ArticleId;
            public string ArticleScholarId;

            //public Author_Article(int AuthorId, int articleId, string articleScholarId)
            //{
            //    this.AuthorId = AuthorId;
            //    this.ArticleId = articleId;
            //    this.ArticleScholarId = articleScholarId;
            //}
        }

        public class Journal
        {
            public int Id;
            public string Title;
            public string URL;
            public bool AzadJournal;
            public bool Msrt;
            public bool HozeJournal;
            public bool MedicalJournal;
            public int GroupId;
            public string GroupName;
            public int SubGroupId;
            public string SubGroupName;
            public string PIssn;
            public string EIssn;
            //public Journal(int id, string title, string url, bool azadJournal, bool msrt, bool hozeJournal, bool medicalJournal, int groupId, string groupName, int subGroupId, string subGroupName, string pIssn, string eIssn)
            //{
            //    this.Id = id;
            //    this.Title = title;
            //    this.URL = url;
            //    this.AzadJournal = azadJournal;
            //    this.Msrt = msrt;
            //    this.HozeJournal = hozeJournal;
            //    this.MedicalJournal = medicalJournal;
            //    this.GroupId = groupId;
            //    this.GroupName = groupName;
            //    this.SubGroupId = subGroupId;
            //    this.SubGroupName = subGroupName;
            //    this.PIssn = pIssn;
            //    this.EIssn = eIssn;
            //}
            //public Journal()
            //{
            //}
        }

        public  class Author_ISC
        {
            public int Id;
            public string FirstNameFA;
            public string LastNameFA;
            public string FirstNameEN;
            public string LastNameEN;
            public string IdentifierORCID;
            public string AffiliationFA;
            public string AffiliationEN;

            //public Author_ISC(int id, string firstNameFA, string lastNameFA, string firstNameEN, string lastNameEN, string identifierORCID, string affiliationFA, string affiliationEN)
            //{
            //    this.Id = id;
            //    this.FirstNameFA = firstNameFA;
            //    this.LastNameFA = lastNameFA;
            //    this.FirstNameEN = firstNameEN;
            //    this.LastNameEN = lastNameEN;
            //    this.IdentifierORCID = identifierORCID;
            //    this.AffiliationFA = affiliationFA;
            //    this.AffiliationEN = affiliationEN;
            //}

            //public Author_ISC()
            //{
            //}
        }

        public  class CitationAll_Article
        {
            public int Id;
            public int Year;
            public int Value;
            public int ArticleId;

            //public CitationAll_Article(int id, int year, int value, int articleId)
            //{
            //    this.Id = id;
            //    this.Year = year;
            //    this.Value = value;
            //    this.ArticleId = articleId;
            //}

            //public CitationAll_Article()
            //{
            //}
        }

        public  class CitationAuthor
        {
            public int Id;
            public int year;
            public int value;
            public int authorId;

            //public CitationAuthor(int year, int value, int authorId)
            //{
            //    this.year = year;
            //    this.value = value;
            //    this.authorId = authorId;
            //}

            //public CitationAuthor()
            //{
            //}
        }

        public  class InputMaster
        {
            public string AuthorScholarId;
            public int GroupId;
            public int SubGroupId;

            //public InputMaster(string authorScholarId, int groupId, int subGroupId)
            //{
            //    this.AuthorScholarId = authorScholarId;
            //    this.GroupId = groupId;
            //    this.SubGroupId = subGroupId;
            //}

            //public InputMaster()
            //{
            //}
        }

        public  class Author_Article_ISC
        {
            public int Id;
            public int AuthorId;
            public int ArticleId;
            public int OrderNumber;


            //public Author_Article_ISC(int AuthorId, int ArticleId, int OrderNumber)
            //{
            //    this.AuthorId = AuthorId;
            //    this.ArticleId = ArticleId;
            //    this.OrderNumber = OrderNumber;
            //}

            //public Author_Article_ISC()
            //{
            //}
        }

    }
}
