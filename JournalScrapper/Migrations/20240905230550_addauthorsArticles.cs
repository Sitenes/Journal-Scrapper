using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JournalScrapper.Migrations
{
    /// <inheritdoc />
    public partial class addauthorsArticles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Atricles");

            migrationBuilder.CreateTable(
                name: "Articles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
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
                    JournalTitleFA = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                        principalColumn: "Journal_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Authors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstNameFA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastNameFA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstNameEN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastNameEN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Identifier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AffiliationFA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AffiliationEN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    ArticleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Authors_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    table.ForeignKey(
                        name: "FK_Keywords_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Articles_JournalId",
                table: "Articles",
                column: "JournalId");

            migrationBuilder.CreateIndex(
                name: "IX_Authors_ArticleId",
                table: "Authors",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_Keywords_ArticleId",
                table: "Keywords",
                column: "ArticleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Authors");

            migrationBuilder.DropTable(
                name: "Keywords");

            migrationBuilder.DropTable(
                name: "Articles");

            migrationBuilder.CreateTable(
                name: "Atricles",
                columns: table => new
                {
                    Articles_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Abstract_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Abstract_Fa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Issue_Id = table.Column<int>(type: "int", nullable: true),
                    Title_EN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title_Fa = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Atricles", x => x.Articles_Id);
                });
        }
    }
}
