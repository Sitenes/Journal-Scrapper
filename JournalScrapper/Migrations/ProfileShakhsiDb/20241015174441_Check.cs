using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JournalScrapper.Migrations.ProfileShakhsiDb
{
    /// <inheritdoc />
    public partial class Check : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProfessorProfiles_Faculties_FacultyId",
                table: "ProfessorProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_WebLinks_ProfessorProfiles_ProfessorProfileId",
                table: "WebLinks");

            migrationBuilder.DropTable(
                name: "ProfessorLinks");

            migrationBuilder.DropIndex(
                name: "IX_WebLinks_ProfessorProfileId",
                table: "WebLinks");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "ProfessorProfiles");

            migrationBuilder.RenameColumn(
                name: "TitleFa",
                table: "WebLinks",
                newName: "Twitter");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "WebLinks",
                newName: "Scopus");

            migrationBuilder.RenameColumn(
                name: "Link",
                table: "WebLinks",
                newName: "Scholar");

            migrationBuilder.RenameColumn(
                name: "PostalCode",
                table: "ProfessorProfiles",
                newName: "UniversityEmail");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "ProfessorProfiles",
                newName: "PersonalEmail");

            migrationBuilder.RenameColumn(
                name: "Biography",
                table: "ProfessorProfiles",
                newName: "BiographyFa");

            migrationBuilder.RenameColumn(
                name: "AddressFA",
                table: "ProfessorProfiles",
                newName: "BiographyEn");

            migrationBuilder.AddColumn<string>(
                name: "FaceBook",
                table: "WebLinks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Gmail",
                table: "WebLinks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ISI",
                table: "WebLinks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Instagram",
                table: "WebLinks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LinkedIn",
                table: "WebLinks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Orcid",
                table: "WebLinks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PersonalWebsite",
                table: "WebLinks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResearchGate",
                table: "WebLinks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "FacultyId",
                table: "ProfessorProfiles",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "ProfessorProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WebLinkId",
                table: "ProfessorProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CityEn",
                table: "Educations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CityFa",
                table: "Educations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CountryEn",
                table: "Educations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CountryFa",
                table: "Educations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DegreeEn",
                table: "Educations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DegreeFa",
                table: "Educations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UniversityEn",
                table: "Educations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UniversityFa",
                table: "Educations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ProfessorProfiles_WebLinkId",
                table: "ProfessorProfiles",
                column: "WebLinkId",
                unique: true,
                filter: "[WebLinkId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ProfessorProfiles_Faculties_FacultyId",
                table: "ProfessorProfiles",
                column: "FacultyId",
                principalTable: "Faculties",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProfessorProfiles_WebLinks_WebLinkId",
                table: "ProfessorProfiles",
                column: "WebLinkId",
                principalTable: "WebLinks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProfessorProfiles_Faculties_FacultyId",
                table: "ProfessorProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfessorProfiles_WebLinks_WebLinkId",
                table: "ProfessorProfiles");

            migrationBuilder.DropIndex(
                name: "IX_ProfessorProfiles_WebLinkId",
                table: "ProfessorProfiles");

            migrationBuilder.DropColumn(
                name: "FaceBook",
                table: "WebLinks");

            migrationBuilder.DropColumn(
                name: "Gmail",
                table: "WebLinks");

            migrationBuilder.DropColumn(
                name: "ISI",
                table: "WebLinks");

            migrationBuilder.DropColumn(
                name: "Instagram",
                table: "WebLinks");

            migrationBuilder.DropColumn(
                name: "LinkedIn",
                table: "WebLinks");

            migrationBuilder.DropColumn(
                name: "Orcid",
                table: "WebLinks");

            migrationBuilder.DropColumn(
                name: "PersonalWebsite",
                table: "WebLinks");

            migrationBuilder.DropColumn(
                name: "ResearchGate",
                table: "WebLinks");

            migrationBuilder.DropColumn(
                name: "Password",
                table: "ProfessorProfiles");

            migrationBuilder.DropColumn(
                name: "WebLinkId",
                table: "ProfessorProfiles");

            migrationBuilder.DropColumn(
                name: "CityEn",
                table: "Educations");

            migrationBuilder.DropColumn(
                name: "CityFa",
                table: "Educations");

            migrationBuilder.DropColumn(
                name: "CountryEn",
                table: "Educations");

            migrationBuilder.DropColumn(
                name: "CountryFa",
                table: "Educations");

            migrationBuilder.DropColumn(
                name: "DegreeEn",
                table: "Educations");

            migrationBuilder.DropColumn(
                name: "DegreeFa",
                table: "Educations");

            migrationBuilder.DropColumn(
                name: "UniversityEn",
                table: "Educations");

            migrationBuilder.DropColumn(
                name: "UniversityFa",
                table: "Educations");

            migrationBuilder.RenameColumn(
                name: "Twitter",
                table: "WebLinks",
                newName: "TitleFa");

            migrationBuilder.RenameColumn(
                name: "Scopus",
                table: "WebLinks",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "Scholar",
                table: "WebLinks",
                newName: "Link");

            migrationBuilder.RenameColumn(
                name: "UniversityEmail",
                table: "ProfessorProfiles",
                newName: "PostalCode");

            migrationBuilder.RenameColumn(
                name: "PersonalEmail",
                table: "ProfessorProfiles",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "BiographyFa",
                table: "ProfessorProfiles",
                newName: "Biography");

            migrationBuilder.RenameColumn(
                name: "BiographyEn",
                table: "ProfessorProfiles",
                newName: "AddressFA");

            migrationBuilder.AlterColumn<int>(
                name: "FacultyId",
                table: "ProfessorProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "ProfessorProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ProfessorLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfessorProfileId = table.Column<int>(type: "int", nullable: false),
                    Link = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleFa = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessorLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfessorLinks_ProfessorProfiles_ProfessorProfileId",
                        column: x => x.ProfessorProfileId,
                        principalTable: "ProfessorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WebLinks_ProfessorProfileId",
                table: "WebLinks",
                column: "ProfessorProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfessorLinks_ProfessorProfileId",
                table: "ProfessorLinks",
                column: "ProfessorProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProfessorProfiles_Faculties_FacultyId",
                table: "ProfessorProfiles",
                column: "FacultyId",
                principalTable: "Faculties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WebLinks_ProfessorProfiles_ProfessorProfileId",
                table: "WebLinks",
                column: "ProfessorProfileId",
                principalTable: "ProfessorProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
