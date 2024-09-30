using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JournalScrapper.Migrations
{
    /// <inheritdoc />
    public partial class addISC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ISCJournals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ISSN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EISSN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Province = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Publisher = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ISCJournals", x => x.Id);
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
                        name: "FK_Years_ISCJournals_JournalId",
                        column: x => x.JournalId,
                        principalTable: "ISCJournals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Qualities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Q = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YearId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Qualities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Qualities_Years_YearId",
                        column: x => x.YearId,
                        principalTable: "Years",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Qualities_YearId",
                table: "Qualities",
                column: "YearId");

            migrationBuilder.CreateIndex(
                name: "IX_Years_JournalId",
                table: "Years",
                column: "JournalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Qualities");

            migrationBuilder.DropTable(
                name: "Years");

            migrationBuilder.DropTable(
                name: "ISCJournals");
        }
    }
}
