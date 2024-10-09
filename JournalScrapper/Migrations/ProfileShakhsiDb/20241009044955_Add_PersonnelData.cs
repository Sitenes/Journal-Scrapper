using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JournalScrapper.Migrations.ProfileShakhsiDb
{
    /// <inheritdoc />
    public partial class Add_PersonnelData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FullNameFA",
                table: "ProfessorProfiles",
                newName: "WebOfScienceID");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "ProfessorProfiles",
                newName: "ScopusID");

            migrationBuilder.AddColumn<string>(
                name: "Faculty",
                table: "ProfessorProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FirstNameEn",
                table: "ProfessorProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FirstNameFa",
                table: "ProfessorProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FullNameEn",
                table: "ProfessorProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GoogleScholarID",
                table: "ProfessorProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastNameEn",
                table: "ProfessorProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastNameFa",
                table: "ProfessorProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NationalCode",
                table: "ProfessorProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PersonnelCode",
                table: "ProfessorProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Faculty",
                table: "ProfessorProfiles");

            migrationBuilder.DropColumn(
                name: "FirstNameEn",
                table: "ProfessorProfiles");

            migrationBuilder.DropColumn(
                name: "FirstNameFa",
                table: "ProfessorProfiles");

            migrationBuilder.DropColumn(
                name: "FullNameEn",
                table: "ProfessorProfiles");

            migrationBuilder.DropColumn(
                name: "GoogleScholarID",
                table: "ProfessorProfiles");

            migrationBuilder.DropColumn(
                name: "LastNameEn",
                table: "ProfessorProfiles");

            migrationBuilder.DropColumn(
                name: "LastNameFa",
                table: "ProfessorProfiles");

            migrationBuilder.DropColumn(
                name: "NationalCode",
                table: "ProfessorProfiles");

            migrationBuilder.DropColumn(
                name: "PersonnelCode",
                table: "ProfessorProfiles");

            migrationBuilder.RenameColumn(
                name: "WebOfScienceID",
                table: "ProfessorProfiles",
                newName: "FullNameFA");

            migrationBuilder.RenameColumn(
                name: "ScopusID",
                table: "ProfessorProfiles",
                newName: "FullName");
        }
    }
}
