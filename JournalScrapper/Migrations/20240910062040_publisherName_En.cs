using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JournalScrapper.Migrations
{
    /// <inheritdoc />
    public partial class publisherName_En : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastNameFA",
                table: "Authors",
                newName: "LastName_FA");

            migrationBuilder.RenameColumn(
                name: "LastNameEN",
                table: "Authors",
                newName: "LastName_EN");

            migrationBuilder.RenameColumn(
                name: "FirstNameFA",
                table: "Authors",
                newName: "FirstName_FA");

            migrationBuilder.RenameColumn(
                name: "FirstNameEN",
                table: "Authors",
                newName: "FirstName_EN");

            migrationBuilder.RenameColumn(
                name: "AffiliationFA",
                table: "Authors",
                newName: "Affiliation_FA");

            migrationBuilder.RenameColumn(
                name: "AffiliationEN",
                table: "Authors",
                newName: "Affiliation_EN");

            migrationBuilder.RenameColumn(
                name: "JournalTitleFA",
                table: "Articles",
                newName: "JournalTitle_FA");

            migrationBuilder.AddColumn<string>(
                name: "JournalTitle_EN",
                table: "Articles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JournalTitle_EN",
                table: "Articles");

            migrationBuilder.RenameColumn(
                name: "LastName_FA",
                table: "Authors",
                newName: "LastNameFA");

            migrationBuilder.RenameColumn(
                name: "LastName_EN",
                table: "Authors",
                newName: "LastNameEN");

            migrationBuilder.RenameColumn(
                name: "FirstName_FA",
                table: "Authors",
                newName: "FirstNameFA");

            migrationBuilder.RenameColumn(
                name: "FirstName_EN",
                table: "Authors",
                newName: "FirstNameEN");

            migrationBuilder.RenameColumn(
                name: "Affiliation_FA",
                table: "Authors",
                newName: "AffiliationFA");

            migrationBuilder.RenameColumn(
                name: "Affiliation_EN",
                table: "Authors",
                newName: "AffiliationEN");

            migrationBuilder.RenameColumn(
                name: "JournalTitle_FA",
                table: "Articles",
                newName: "JournalTitleFA");
        }
    }
}
