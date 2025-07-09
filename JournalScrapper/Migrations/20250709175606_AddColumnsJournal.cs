using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JournalScrapper.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnsJournal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HIndex",
                table: "Years",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImpactFactorWithoutSelfCitation",
                table: "Years",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "JournalStatus",
                table: "Years",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SelfCitationFactor",
                table: "Years",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HIndex",
                table: "Years");

            migrationBuilder.DropColumn(
                name: "ImpactFactorWithoutSelfCitation",
                table: "Years");

            migrationBuilder.DropColumn(
                name: "JournalStatus",
                table: "Years");

            migrationBuilder.DropColumn(
                name: "SelfCitationFactor",
                table: "Years");
        }
    }
}
