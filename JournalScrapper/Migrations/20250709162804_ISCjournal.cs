using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JournalScrapper.Migrations
{
    /// <inheritdoc />
    public partial class ISCjournal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Authors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName_FA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName_FA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstName_EN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName_EN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Identifier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Affiliation_FA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Affiliation_EN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    ArticleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Journals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Sourceid = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title_EN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title_Fa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ISSN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EISSN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    URL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Publisher = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Region = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MacroLevelIssue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IntermediateLevelIssue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MicroLevelIssue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoverageStartYear = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoverageEndYear = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Journals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Keywords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPersian = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Keywords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScopusSubjectAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScopusSubjectAreas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Articles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title_FA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Abstract_FA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title_EN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Abstract_EN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublisherName_FA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublisherName_EN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Issn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Volume = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Issue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PubDate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PubDateReceived = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstPage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastPage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    pii = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    doi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublicationType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArchiveCopySource = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JournalTitle_FA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JournalTitle_EN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrespondingAuthorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrespondingAuthorEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PDFFilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JournalId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Articles_Journals_JournalId",
                        column: x => x.JournalId,
                        principalTable: "Journals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Years",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImpactFactor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YearPublished = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CumulativeCitations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImmediateImpactFactor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JournalId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Years", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Years_Journals_JournalId",
                        column: x => x.JournalId,
                        principalTable: "Journals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScopusJournalCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceId = table.Column<int>(type: "int", nullable: false),
                    SubjectAreaId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScopusJournalCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScopusJournalCategories_ScopusSubjectAreas_SubjectAreaId",
                        column: x => x.SubjectAreaId,
                        principalTable: "ScopusSubjectAreas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Qualities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QLevel = table.Column<int>(type: "int", nullable: false),
                    JournalCategoryId = table.Column<int>(type: "int", nullable: false),
                    YearId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Qualities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Qualities_ScopusJournalCategories_JournalCategoryId",
                        column: x => x.JournalCategoryId,
                        principalTable: "ScopusJournalCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Qualities_Years_YearId",
                        column: x => x.YearId,
                        principalTable: "Years",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Articles_JournalId",
                table: "Articles",
                column: "JournalId");

            migrationBuilder.CreateIndex(
                name: "IX_Qualities_JournalCategoryId",
                table: "Qualities",
                column: "JournalCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Qualities_YearId",
                table: "Qualities",
                column: "YearId");

            migrationBuilder.CreateIndex(
                name: "IX_ScopusJournalCategories_SubjectAreaId",
                table: "ScopusJournalCategories",
                column: "SubjectAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Years_JournalId",
                table: "Years",
                column: "JournalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Articles");

            migrationBuilder.DropTable(
                name: "Authors");

            migrationBuilder.DropTable(
                name: "Keywords");

            migrationBuilder.DropTable(
                name: "Qualities");

            migrationBuilder.DropTable(
                name: "ScopusJournalCategories");

            migrationBuilder.DropTable(
                name: "Years");

            migrationBuilder.DropTable(
                name: "ScopusSubjectAreas");

            migrationBuilder.DropTable(
                name: "Journals");
        }
    }
}
