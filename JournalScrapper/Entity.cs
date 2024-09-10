using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JournalScrapper
{
    internal class Entity
    {
        public class Journal
        {

            [Key]
            public int Journal_id { get; set; }
            public string? URL { get; set; }
            public bool AzadJournal { get; set; }
            public bool Msrt { get; set; }
            public bool HozeJournal { get; set; }
            public bool MedicalJournal { get; set; }
            public int GroupId { get; set; }
            public string? GroupName { get; set; }
            public int SubGroupId { get; set; }
            public string? SubGroupName { get; set; }
            public string? Title_Fa { get; set; }
            public string? ISSN { get; set; }
            public string? EISSN { get; set; }
            public string? Title_EN { get; set; }
        }


        public class Article
        {
            public string Url { get; set; } = "";
            public string Title_FA { get; set; } = "";
            public string Abstract_FA { get; set; } = "";
            public string Title_EN { get; set; } = "";
            public string Abstract_EN { get; set; } = "";
            public int Id { get; set; }
            public string PublisherName_FA { get; set; } = "";
            public string PublisherName_EN { get; set; } = "";
            public string Issn { get; set; } = "";
            public string Volume { get; set; } = "";
            public string Issue { get; set; } = "";
            public string PubDate { get; set; } = "";
            public string PubDateReceived { get; set; } = "";
            public string FirstPage { get; set; } = "";
            public string LastPage { get; set; } = "";
            public string pii { get; set; } = "";
            public string doi { get; set; } = "";
            public string PublicationType { get; set; } = "";
            public string ArchiveCopySource { get; set; } = "";
            public string JournalTitle_FA { get; set; } = "";
            public string JournalTitle_EN { get; set; } = "";
            public string CorrespondingAuthorName { get; set; } = "";
            public string CorrespondingAuthorEmail { get; set; } = "";
            public string PDFFilePath { get; set; } = "";
            
            public int JournalId { get; set; }
            public Journal? Journal { get; set; }
        }

        public class Author
        {
            public int Id { get; set; }
            public string FirstName_FA { get; set; } = "";
            public string LastName_FA { get; set; } = "";
            public string FirstName_EN { get; set; } = "";
            public string LastName_EN { get; set; } = "";
            public string Identifier { get; set; } = "";
            public string Affiliation_FA { get; set; } = "";
            public string Affiliation_EN { get; set; } = "";
            public int Order { get; set; }
            public int ArticleId { get; set; }
            public Article? Article { get; set; }
        }


        //public class Affiliation
        //{
        //    public int Id { get; set; }
        //    public string? Affiliation_name { get; set; }
        //    public string? Affiliation_Address { get; set; }
        //}
        //public class Author_Affiliation_Relation
        //{
        //    public int Id { get; set; }
        //    public int? Author_id { get; set; }
        //    public int? Affiliation_id { get; set; }
        //}

        //public class Author_Atricle_Relation
        //{
        //    public int Id { get; set; }
        //    public int? Author_id { get; set; }
        //    public int? Article_id { get; set; }
        //}

        //public class Authors
        //{
        //    public int Author_Id { get; set; }
        //    public string Frist_name { get; set; } = "";
        //    public string Last_name { get; set; } = "";
        //    public int? Articles_Id { get; set; }
        //}

        //public class Issues
        //{
        //    public int Issues_Id { get; set; }
        //    public string Issues_Number { get; set; } = "";
        //    public string Publication_date { get; set; } = "";
        //    public int? Volumes_Id { get; set; }
        //}


        //public class Volumes
        //{
        //    public int Volumes_Id { get; set; }
        //    public string Volumes_Number { get; set; } = "";
        //    public string Publication_date { get; set; } = "";
        //    public int? Journal_Id { get; set; }
        //}
        public class Keyword
        {
            public int Id { get; set; }
            public int ArticleId { get; set; }
            public string Value { get; set; } = "";
            public bool IsPersian { get; set; } = false;
            public Article? Atricle { get; set; }
        }

    }
}
