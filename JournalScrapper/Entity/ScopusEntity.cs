using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JournalScrapper.Entity
{
    public class ScopusEntity
    {
        public class ScopusProfile
        {
            [Ignore]
            public int Id { get; set; }
            public string Name { get; set; }
            public string Affiliation { get; set; }
            public string ResearcherId { get; set; }
            public string ScopusResearcherId { get; set; }
            public string Orcid { get; set; }
            public string CitationsBy { get; set; }
            public int Documents { get; set; } = 0;
            public int HIndex { get; set; } = 0;
            public string AuthorPositionSource { get; set; }
            public int FirstAuthor { get; set; } = 0;
            public int LastAuthor { get; set; } = 0;
            public int CoAuthor { get; set; } = 0;
            public int SingleAuthor { get; set; } = 0;
            public string Timestamp { get; set; }
            public string DocumentsSeries { get; set; }
            public string CitationsSeries { get; set; }
            public string Articles { get; set; }
            public int Document { get; set; }
            public int CitationBy { get; set; }
        }
        public class ScopusHIndex
        {
            [Ignore]
            public int Id { get; set; }
            public string ResearcherId { get; set; }
            public int Year { get; set; }
            public int HIndex { get; set; }
            public DateTime Timestamp { get; set; }
        }
        public class ScopusCitations
        {
            [Ignore]
            public int Id { get; set; }
            public string ResearcherId { get; set; }
            public string Document { get; set; }
            public int Year { get; set; }
            public string CitationsYear { get; set; }
            public int Citations { get; set; }
            public DateTime Timestamp { get; set; }
            public string Condition { get; set; }
        }
        public class ScopusArticle
        {
            [Ignore]
            public int Id { get; set; }
            public string ArticleId { get; set; }
            public string ResearcherIds { get; set; }
            public string Title { get; set; }
            public string Authors { get; set; }
            public string AuthorsEmail { get; set; }
            public string AuthorsAddress { get; set; }
            public string CorrespondingAuthor { get; set; }
            public string CorrespondingAddress { get; set; }
            public string CorrespondingEmail { get; set; }
            public string DocumentType { get; set; }
            public string SourceType { get; set; }
            public string ISSN { get; set; }
            public string DOI { get; set; }
            public string Publisher { get; set; }
            public string CODEN { get; set; }
            public string OriginalLanguage { get; set; }
            public string Abstract { get; set; }
            public string AuthorKeywords { get; set; }
            public string TopicName { get; set; }
            public string ProminencePercentile { get; set; }
            public string Readers { get; set; }
            public string NewsMentions { get; set; }
            public string Source { get; set; }
            public string SourceId { get; set; }
            public string SourceVolume { get; set; }
            public string SourceDate { get; set; }
            public DateTime Timestamp { get; set; }
            public string CitationInScopus { get; set; }
            public string FieldWeightedCitationImpact { get; set; }
            public string PubMedId { get; set; }
            public string SourceIssue { get; set; }
            public string SourcePages { get; set; }
            public string BlogMentions { get; set; }
            public string CitationsInScopus { get; set; }
            public string ISBN { get; set; }
            public string Source2024 { get; set; }
            public string ViewsCount2016_2025 { get; set; }
            public string CitationIndexes { get; set; }
            public string Source2023 { get; set; }
            public string References { get; set; }
            public string Source2021 { get; set; }
            public string PolicyCitations { get; set; }
            public string FullTextViews { get; set; }
            public string AbstractViews { get; set; }
            public string SharesLikesComments { get; set; }
            public string Downloads { get; set; }
            public string VolumeEditors { get; set; }
            public string Sponsors { get; set; }
            public string Source2013 { get; set; }
            public string PatentFamilyCitations { get; set; }
            public string Source2009 { get; set; }
            public string Source2008 { get; set; }
            public string Source2012 { get; set; }
            public string ClinicalCitations { get; set; }
            public string Source2001 { get; set; }
            public string Source2022 { get; set; }
            public string Source15 { get; set; }
            public string Source2011 { get; set; }
            public string Source2018 { get; set; }
            public string Source2010 { get; set; }
            public string SourceJanuary { get; set; }
            public string Source2006 { get; set; }
            public string Source19 { get; set; }
            public string Source29 { get; set; }
            public string Source2014 { get; set; }
            public string Source2 { get; set; }
            public string Source3 { get; set; }
            public string Source14 { get; set; }
            public string Source26 { get; set; }
            public string Source2025 { get; set; }
            public string SourceOctober { get; set; }
            public string Source8 { get; set; }
            public string Source6 { get; set; }
            public string Source4 { get; set; }
            public string Source2005 { get; set; }
            public string Source2004 { get; set; }
            public string Source2019 { get; set; }
            public string SourceDecember { get; set; }
            public string Source2016 { get; set; }
            public string Source2015 { get; set; }
            public string Source5 { get; set; }
            public string SourceMay { get; set; }
            public string Source16 { get; set; }
            public string Source2017 { get; set; }
            public string SourceFebruary { get; set; }
            public string SourceJune { get; set; }
            public string Source1 { get; set; }
            public string Source2020 { get; set; }
            public string Source23 { get; set; }
            public string Source17 { get; set; }
            public string SourceApril { get; set; }
        }
        public class ScopusJournal
        {
            [Ignore]
            public int Id { get; set; }
            public string JournalId { get; set; }
            public string Name { get; set; }
            public string YearsCurrentlyCoveredByScopus { get; set; }
            public string Publisher { get; set; }
            public string ISSN { get; set; }
            public string SubjectArea { get; set; }
            public string SourceType { get; set; }
            public DateTime Timestamp { get; set; }
            public string CiteScores { get; set; }
            public string PercentilesInCategory { get; set; }
            public string CiteScore { get; set; }
            public string SJR { get; set; }
            public string SNIP { get; set; }
            public string EISSN { get; set; }
        }
    }
}
