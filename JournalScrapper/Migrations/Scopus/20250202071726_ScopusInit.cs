using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JournalScrapper.Migrations.Scopus
{
    /// <inheritdoc />
    public partial class ScopusInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScopusArticles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResearcherIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Authors = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuthorsEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuthorsAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrespondingAuthor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrespondingAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrespondingEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ISSN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DOI = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Publisher = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CODEN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalLanguage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Abstract = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuthorKeywords = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TopicName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProminencePercentile = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Readers = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewsMentions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceVolume = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceDate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CitationInScopus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FieldWeightedCitationImpact = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PubMedId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceIssue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourcePages = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BlogMentions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CitationsInScopus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ISBN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2024 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ViewsCount2016_2025 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CitationIndexes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2023 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    References = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2021 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PolicyCitations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullTextViews = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AbstractViews = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SharesLikesComments = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Downloads = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VolumeEditors = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sponsors = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2013 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PatentFamilyCitations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2009 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2008 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2012 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClinicalCitations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2001 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2022 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source15 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2011 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2018 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2010 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceJanuary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2006 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source19 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source29 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2014 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source3 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source14 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source26 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2025 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceOctober = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source8 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source6 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source4 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2005 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2004 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2019 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceDecember = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2016 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2015 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source5 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceMay = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source16 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2017 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceFebruary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceJune = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source2020 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source23 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source17 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceApril = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScopusArticles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScopusCitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearcherId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Document = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    CitationsYear = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Citations = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScopusCitations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScopusHIndices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearcherId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    HIndex = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScopusHIndices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScopusJournals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JournalId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YearsCurrentlyCoveredByScopus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Publisher = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ISSN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubjectArea = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CiteScores = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PercentilesInCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CiteScore = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SJR = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SNIP = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EISSN = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScopusJournals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScopusProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Affiliation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResearcherId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScopusResearcherId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Orcid = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CitationsBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Documents = table.Column<int>(type: "int", nullable: false),
                    HIndex = table.Column<int>(type: "int", nullable: false),
                    AuthorPositionSource = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstAuthor = table.Column<int>(type: "int", nullable: false),
                    LastAuthor = table.Column<int>(type: "int", nullable: false),
                    CoAuthor = table.Column<int>(type: "int", nullable: false),
                    SingleAuthor = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentsSeries = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CitationsSeries = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Articles = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Document = table.Column<int>(type: "int", nullable: false),
                    CitationBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScopusProfiles", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScopusArticles");

            migrationBuilder.DropTable(
                name: "ScopusCitations");

            migrationBuilder.DropTable(
                name: "ScopusHIndices");

            migrationBuilder.DropTable(
                name: "ScopusJournals");

            migrationBuilder.DropTable(
                name: "ScopusProfiles");
        }
    }
}
