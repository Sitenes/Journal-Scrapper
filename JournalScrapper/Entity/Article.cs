using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models.Entities
{

	#region Models
	public class Log
	{
		public int Id { get; set; }
		public string Message { get; set; }
		public string Link { get; set; }
        public int? WorkflowUserId { get; set; }
    }

	public class Article
	{
		public int Id { get; set; }
		public string? Doi { get; set; }
		public string? TitleFa { get; set; }
		public string? TitleEn { get; set; }
		public int? Volume { get; set; }
		public string? Issue { get; set; }
		public int? PageStart { get; set; }
		public int? PageEnd { get; set; }
		public string? Type { get; set; }
		public string? Publication { get; set; }
		public int PublicationYear { get; set; }
		public string? Abstract { get; set; }
		public string? OpenAccess { get; set; }
		public double? Prominence { get; set; }
		public double? Percentile { get; set; }
		public string? SourceType { get; set; }
		public string? OriginalLanguage { get; set; }
		public string? FullTextUrl { get; set; }
		public string? Description { get; set; }
		public DateTime? LastUpdate { get; set; }
		public string? ScopusArticleId { get; set; }
		public string? WosArticleId { get; set; }
		public bool? isScopus { get; set; }
		public bool? isWos { get; set; }
        public bool? isIsc { get; set; }
        // Relations
        public int? JournalId { get; set; }
		[ForeignKey(nameof(JournalId))]
		public Journal? Journal { get; set; }
		public List<ScopusArticleCitation> ScopusCitations { get; set; } = new();
		public List<WOSArticleCitation> WOSArticleCitations { get; set; } = new();
		public List<ScholarArticleCitation> ScholarCitations { get; set; } = new();
		public List<ArticleAuthor> ArticleAuthors { get; set; } = new();
		public List<ArticleKeyword> Keywords { get; set; } = new();
		public List<ArticleTopic> Topics { get; set; } = new();
		public List<FundingSponsor> FundingSponsors { get; set; } = new();
		public List<Affiliation> Affiliations { get; set; } = new();
        public int? WorkflowUserId { get; set; }
    }

	public class ArticleAuthor
	{
		public int Id { get; set; }
		public int? Order { get; set; }
		public bool? IsCorrespondingAuthor { get; set; }
		public DateTime? LastUpdate { get; set; }
		// Relations
		public int? ArticleId { get; set; }
		[ForeignKey(nameof(ArticleId))]
		public Article? Article { get; set; }

		public int? ProfessorId { get; set; }
		[ForeignKey(nameof(ProfessorId))]


		public int? CoAuthorId { get; set; }
		[ForeignKey(nameof(CoAuthorId))]
		public CoAuthor? CoAuthor { get; set; }
        public int? WorkflowUserId { get; set; }
    }

	//keyword (no rpeat - just read for first)
	public class ArticleKeyword
	{
		public int Id { get; set; }
		public int? ArticleId { get; set; }
		[MaxLength(100)]
		public string? Keyword { get; set; }
		public bool? IsAuthorKeyword { get; set; }
		public DateTime? LastUpdate { get; set; }
		[ForeignKey(nameof(ArticleId))]
		public Article? Article { get; set; }
        public int? WorkflowUserId { get; set; }
    }

	//topic (no rpeat - just read for first)
	public class ArticleTopic
	{
		public int Id { get; set; }
		public int? ArticleId { get; set; }
		[MaxLength(100)]
		public string? Topic { get; set; }
		public DateTime? LastUpdate { get; set; }
		[ForeignKey(nameof(ArticleId))]
		public Article? Article { get; set; }
        public int? WorkflowUserId { get; set; }
    }

	public class ScopusArticleCitation
	{
		public int Id { get; set; }
		public int? ArticleId { get; set; }
		public int? ScopusCitation { get; set; }
		public double? Fwci { get; set; }
		public int? ScopusPercentileCitation { get; set; }
		public int? Readers { get; set; }
		public int? Mentions { get; set; }
		public int? PatentFamilyCitations { get; set; }
		public int? PolicyCitations { get; set; }
		public int? References { get; set; }
		public int? CitationIndexes { get; set; }
		public DateTime? LastUpdate { get; set; }

		[ForeignKey(nameof(ArticleId))]
		public Article? Article { get; set; }
        public int? WorkflowUserId { get; set; }
    }

	public class WOSArticleCitation
	{
		public int Id { get; set; }
		public int? ArticleId { get; set; }
		public DateTime? LastUpdate { get; set; }

		[ForeignKey(nameof(ArticleId))]
		public Article? Article { get; set; }
        public int? WorkflowUserId { get; set; }
    }

	public class ScholarArticleCitation
	{
		public int Id { get; set; }
		public int? ArticleId { get; set; }
		public int? ScholarCitation { get; set; }
		public DateTime? LastUpdate { get; set; }

		[ForeignKey(nameof(ArticleId))]
		public Article? Article { get; set; }
        public int? WorkflowUserId { get; set; }
    }

	public class ProfileCitationByYear
	{
		public int Id { get; set; }
		public int Year { get; set; }
		public int CitationCount { get; set; }
		public DateTime? LastUpdate { get; set; }
		public int ProfessorId { get; set; }
		[ForeignKey(nameof(ProfessorId))]

        public int? WorkflowUserId { get; set; }
    }

	//Journal (no rpeat - just read for first)
	public class Journal
	{
		[Key]
		public int Id { get; set; }
		public string? Sourceid { get; set; }
		public string? Title { get; set; }
		public string? Type { get; set; }
		public string? Issn { get; set; }
		public string? EIssn { get; set; }
		public string? Publisher { get; set; }
		public string? Country { get; set; }
		public string? Region { get; set; }
		public string? CoverageStartYear { get; set; }
		public string? CoverageEndYear { get; set; }

		public DateTime? LastUpdate { get; set; }

		public List<ScopusJournalDetail> ScopusJournalDetails { get; set; }
		public List<WOSJournalCategory> WOSJournalCategories { get; set; }
		public List<WOSJournalDetail> WOSJournalDetails { get; set; }

		public List<ScopusJournalCategoryRelation> JournalCategoryRelations { get; set; } = new();
		public List<Article> Articles { get; set; }
        public int? WorkflowUserId { get; set; }
    }

	public class WOSJournalDetail
	{
		[Key]
		public int Id { get; set; }
		public int TotalCitations { get; set; }
		public double PercentCitableOA { get; set; }
		public int CitableItems { get; set; }
		public double PercentArticlesInCitableItems { get; set; }
		public double CitedHalfLife { get; set; }
		public double CitingHalfLife { get; set; }
		public int TotalArticles { get; set; }
		public double Eigenfactor { get; set; }
		public double NormalizedEigenfactor { get; set; }
		public double ArticleInfluenceScore { get; set; }
		public double JIFPercentile { get; set; }
		public double ImmediacyIndex { get; set; }
		public double JIFWithoutSelfCites { get; set; }
		public double FiveYearJIF { get; set; }

		public List<WOSJournalCategory> Categories = new List<WOSJournalCategory>();
        public int? WorkflowUserId { get; set; }
    }

	public class WOSJournalCategory
	{
		public int Id { get; set; }

		public string Name { get; set; }
		public string Edition { get; set; }
		public string JIFQuartile { get; set; }
		public double JCI2023 { get; set; }
		public string JCIRank { get; set; }
		public string JCIQuartile { get; set; }
		public double JCIPercentile { get; set; }
		public double JIFPercentile { get; set; }
		public string AISRank { get; set; }
		public string AISQuartile { get; set; }
		public string FiveYearJIFQuartile { get; set; }
		public string JIFRank { get; set; }

		public int JournalId { get; set; }
		[ForeignKey(nameof(JournalId))]
		public Journal Journal { get; set; }
        public int? WorkflowUserId { get; set; }
    }

	public class ScopusJournalDetail
	{
		[Key]
		public int Id { get; set; }
		public int? Year { get; set; }
		public double? SJR { get; set; }
		public string? SJRBestQuartile { get; set; }
		public int? HIndex { get; set; }
		public int? TotalRefs { get; set; }
		public double? RefPerDoc { get; set; }
		public double? PercentFemale { get; set; }
		public string? Overton { get; set; }
		public string? SDG { get; set; }

		public int JournalId { get; set; }
		public Journal Journal { get; set; }
        public int? WorkflowUserId { get; set; }
    }

	public class ScopusSubjectArea
	{
		[Key]
		public int Id { get; set; }
		public string? Name { get; set; }
		public int SourceId { get; set; }
		public List<ScopusJournalCategory> JournalCategories { get; set; } = new();
        public int? WorkflowUserId { get; set; }
    }

	public class ScopusJournalCategoryRelation
	{
		[Key]
		public int Id { get; set; }
		public int? Year { get; set; }
		public int JournalCategoryId { get; set; }
		public int JournalId { get; set; }
		public ScopusJournalCategory JournalCategory { get; set; } = new();
		public Journal Journal { get; set; }
        public int? WorkflowUserId { get; set; }
    }

	public class ScopusJournalCategory
	{
		public int Id { get; set; }

		public string Name { get; set; }
		public int SourceId { get; set; }
		public List<ScopusCategoryQurtile> Qurtiles { get; set; } = new();
		public List<ScopusCategorySnip> CategorySnips { get; set; } = new();
		public List<ScopusCategoryRank> CategoryRank { get; set; } = new();

		public int? SubjectAreaId { get; set; }
		[ForeignKey(nameof(SubjectAreaId))]
		public ScopusSubjectArea? SubjectArea { get; set; }
        public int? WorkflowUserId { get; set; }
    }

	public class ScopusCategoryQurtile
	{
		public int Id { get; set; }
		public int QLevel { get; set; }
		public string? Year { get; set; }
		public int CategoryId { get; set; }
		[ForeignKey(nameof(CategoryId))]
		public ScopusJournalCategory Category { get; set; }
        public int? WorkflowUserId { get; set; }
    }

	public class ScopusCategorySnip
	{
		public int Id { get; set; }
		public int Count { get; set; }
		public string? Year { get; set; }
		public int CategoryId { get; set; }
		[ForeignKey(nameof(CategoryId))]
		public ScopusJournalCategory Category { get; set; }
        public int? WorkflowUserId { get; set; }
    }

	public class ScopusCategoryRank
	{
		public int Id { get; set; }
		public string Rank { get; set; }
		public string TotalRank { get; set; }
		public int Price { get; set; }
		public int CategoryId { get; set; }
		[ForeignKey(nameof(CategoryId))]
		public ScopusJournalCategory Category { get; set; }
        public int? WorkflowUserId { get; set; }
    }

	public class ScopusProfile
	{
		public int Id { get; set; }
		public int? FirstAuthorScore { get; set; }
		public int? FirstAuthorArticleCount { get; set; }
		public int? FirstAuthorAverageCitations { get; set; }
		public double? FirstAuthorFwci { get; set; }
		public int? LastAuthorScore { get; set; }
		public int? LastAuthorArticleCount { get; set; }
		public int? LastAuthorAverageCitations { get; set; }
		public double? LastAuthorFwci { get; set; }
		public int? CoAuthorScore { get; set; }
		public int? CoAuthorArticleCount { get; set; }
		public int? CoAuthorAverageCitations { get; set; }
		public double? CoAuthorFwci { get; set; }
		public int? SingleAuthorScore { get; set; }
		public int? SingleAuthorArticleCount { get; set; }
		public int? SingleAuthorAverageCitations { get; set; }
		public double? SingleAuthorFwci { get; set; }
		public int? CitationCounts { get; set; } = new();
		public int? Documents { get; set; }
		public DateTime Lastupdate { get; set; }

		public int? ProfessorId { get; set; }
		[ForeignKey(nameof(ProfessorId))]

        public int? WorkflowUserId { get; set; }
    }

	public class ScholarProfile
	{
		public int Id { get; set; }
		public int? CitationAll { get; set; }
		public int? CitationSince2020 { get; set; }
		public int? HIndexAll { get; set; }
		public int? HIndexSince2020 { get; set; }
		public int? I10IndexAll { get; set; }
		public int? I10IndexSince2020 { get; set; }
		public DateTime LastUpdate { get; set; }

		[MaxLength(100)]
		public string? OtherName { get; set; }

		public int? ProfessorId { get; set; }
		[ForeignKey(nameof(ProfessorId))]

        public int? WorkflowUserId { get; set; }
    }

	public class WOSProfile
	{
		public int Id { get; set; }
		public int? TotalDocuments { get; set; }
		public int? PublicationsIndexedInWebOfScience { get; set; }
		public int? WebOfScienceCoreCollectionPublications { get; set; }
		public int? Preprints { get; set; }
		public int? DissertationsOrTheses { get; set; }
		public int? NonIndexedPublications { get; set; }
		public int? VerifiedPeerReviews { get; set; }
		public int? VerifiedEditorRecords { get; set; }
		public int? AwardedGrants { get; set; }
		public int? HIndex { get; set; }
		public int? Publications { get; set; }
		public int? SumOfTimesCited { get; set; }
		public int? CitingArticles { get; set; }
		public int? withoutSelfCitations { get; set; }
		public int? SumOfTimesCitedByPatents { get; set; }
		public int? CitingPatents { get; set; }
		public int? SumOfTimesCitedByPolicy { get; set; }
		public int? CitingPolicyDocuments { get; set; }
		public int? AuthorPositionFirst { get; set; }
		public int? AuthorPositionLast { get; set; }
		public int? AuthorPositionCorsponding { get; set; }
		public DateTime Lastupdate { get; set; }

		public int? ProfessorId { get; set; }
		[ForeignKey(nameof(ProfessorId))]

        public int? WorkflowUserId { get; set; }
    }

	public class Affiliation
	{
		public int Id { get; set; }
		public string? Country { get; set; }
		public string? City { get; set; }
		public string? University { get; set; }
		public int order { get; set; }
		public DateTime? LastUpdate { get; set; }

		// Relations
		public int ArticleId { get; set; }
		[ForeignKey(nameof(ArticleId))]
		public Article Articles { get; set; } = new();

		public int professorId { set; get; }
		[ForeignKey(nameof(professorId))]

        public int? WorkflowUserId { get; set; }

    }

	public class CoAuthor
	{
		public int Id { get; set; }
		public string? Name { get; set; }
		public DateTime? LastUpdate { get; set; }
		public string? Country { get; set; }
		public string? City { get; set; }
		public string? University { get; set; }
		public string? ScopusId { get; set; }
		public string? WebOfScienceID { get; set; }
        public int? WorkflowUserId { get; set; }
    }

	public class ScopusHIndex
	{
		public int Id { get; set; }
		public int? Year { get; set; }
		public int? HIndex { get; set; }
		public DateTime? LastUpdate { get; set; }
		public int? ProfessorId { get; set; }
		[ForeignKey(nameof(ProfessorId))]

        public int? WorkflowUserId { get; set; }
    }

	public class FundingSponsor
	{
		public int Id { get; set; }
		public string? OrganName { get; set; }
		public int? FundingNumber { get; set; }
		public string? Acronym { get; set; }
		public string? FundingText { get; set; }
		public string? FundingLinke { get; set; }
		public DateTime? LastUpdate { get; set; }
		public int ArticleId { get; set; }
		[ForeignKey(nameof(ArticleId))]
		public Article Article { get; set; }
        public int? WorkflowUserId { get; set; }
    }
	#endregion

}
