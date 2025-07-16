using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JournalScrapper.Migrations
{
    /// <inheritdoc />
    public partial class @as : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Qualities_Years_YearId",
                table: "Qualities");

            migrationBuilder.DropTable(
                name: "Years");

            migrationBuilder.RenameColumn(
                name: "IntermediateLevelIssue",
                table: "Journals",
                newName: "MidLevelIssue");

            migrationBuilder.AddColumn<string>(
                name: "AverageImpactFactorMacroLevelTopic",
                table: "Qualities",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AverageImpactFactorMidLevelTopic",
                table: "Qualities",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "JournalIscDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImpactFactor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YearPublished = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CumulativeCitations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImmediateImpactFactor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SelfCitationFactor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImpactFactorWithoutSelfCitation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JournalStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HIndex = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JournalId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalIscDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalIscDetails_Journals_JournalId",
                        column: x => x.JournalId,
                        principalTable: "Journals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JournalIscDetails_JournalId",
                table: "JournalIscDetails",
                column: "JournalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Qualities_JournalIscDetails_YearId",
                table: "Qualities",
                column: "YearId",
                principalTable: "JournalIscDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Qualities_JournalIscDetails_YearId",
                table: "Qualities");

            migrationBuilder.DropTable(
                name: "JournalIscDetails");

            migrationBuilder.DropColumn(
                name: "AverageImpactFactorMacroLevelTopic",
                table: "Qualities");

            migrationBuilder.DropColumn(
                name: "AverageImpactFactorMidLevelTopic",
                table: "Qualities");

            migrationBuilder.RenameColumn(
                name: "MidLevelIssue",
                table: "Journals",
                newName: "IntermediateLevelIssue");

            migrationBuilder.CreateTable(
                name: "Years",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JournalId = table.Column<int>(type: "int", nullable: false),
                    CumulativeCitations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HIndex = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImmediateImpactFactor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImpactFactor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImpactFactorWithoutSelfCitation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JournalStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SelfCitationFactor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YearPublished = table.Column<string>(type: "nvarchar(max)", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_Years_JournalId",
                table: "Years",
                column: "JournalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Qualities_Years_YearId",
                table: "Qualities",
                column: "YearId",
                principalTable: "Years",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
