using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JournalScrapper.Migrations.ProfileShakhsiDb
{
    /// <inheritdoc />
    public partial class Add_Elements_To_ProfessorProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Biography",
                table: "ProfessorProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DepartmentOfProfessor",
                table: "ProfessorProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeNumber",
                table: "ProfessorProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FacultyOfProfessor",
                table: "ProfessorProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FinancialCode",
                table: "ProfessorProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NationalId",
                table: "ProfessorProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Biography",
                table: "ProfessorProfiles");

            migrationBuilder.DropColumn(
                name: "DepartmentOfProfessor",
                table: "ProfessorProfiles");

            migrationBuilder.DropColumn(
                name: "EmployeeNumber",
                table: "ProfessorProfiles");

            migrationBuilder.DropColumn(
                name: "FacultyOfProfessor",
                table: "ProfessorProfiles");

            migrationBuilder.DropColumn(
                name: "FinancialCode",
                table: "ProfessorProfiles");

            migrationBuilder.DropColumn(
                name: "NationalId",
                table: "ProfessorProfiles");
        }
    }
}
