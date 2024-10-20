using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JournalScrapper.Migrations.ProfileShakhsiDb
{
    /// <inheritdoc />
    public partial class Add_Identifier_ProfessorProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Department",
                table: "ProfessorProfiles");

            migrationBuilder.DropColumn(
                name: "DepartmentFA",
                table: "ProfessorProfiles");

            migrationBuilder.DropColumn(
                name: "Faculty",
                table: "ProfessorProfiles");

            migrationBuilder.DropColumn(
                name: "FacultyFa",
                table: "ProfessorProfiles");

            migrationBuilder.RenameColumn(
                name: "NationalId",
                table: "ProfessorProfiles",
                newName: "IdentificationNumber");

            migrationBuilder.AddColumn<int>(
                name: "FacultyId",
                table: "ProfessorProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleFa = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Faculties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleFa = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faculties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Faculties_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfessorProfiles_FacultyId",
                table: "ProfessorProfiles",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_Faculties_DepartmentId",
                table: "Faculties",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProfessorProfiles_Faculties_FacultyId",
                table: "ProfessorProfiles",
                column: "FacultyId",
                principalTable: "Faculties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProfessorProfiles_Faculties_FacultyId",
                table: "ProfessorProfiles");

            migrationBuilder.DropTable(
                name: "Faculties");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_ProfessorProfiles_FacultyId",
                table: "ProfessorProfiles");

            migrationBuilder.DropColumn(
                name: "FacultyId",
                table: "ProfessorProfiles");

            migrationBuilder.RenameColumn(
                name: "IdentificationNumber",
                table: "ProfessorProfiles",
                newName: "NationalId");

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "ProfessorProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DepartmentFA",
                table: "ProfessorProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Faculty",
                table: "ProfessorProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FacultyFa",
                table: "ProfessorProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
