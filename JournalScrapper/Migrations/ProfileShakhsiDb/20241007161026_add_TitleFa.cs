using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JournalScrapper.Migrations.ProfileShakhsiDb
{
    /// <inheritdoc />
    public partial class add_TitleFa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfessorProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullNameFA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Degree = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DegreeFA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PositionFA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentFA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddressFA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AreaOfStudy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AreaOfStudyFA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Research = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResearchFA = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessorProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Articles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Link = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleFa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProfessorProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Articles_ProfessorProfiles_ProfessorProfileId",
                        column: x => x.ProfessorProfileId,
                        principalTable: "ProfessorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleFa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProfessorProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Books_ProfessorProfiles_ProfessorProfileId",
                        column: x => x.ProfessorProfileId,
                        principalTable: "ProfessorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleFa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProfessorProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Courses_ProfessorProfiles_ProfessorProfileId",
                        column: x => x.ProfessorProfileId,
                        principalTable: "ProfessorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Educations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleFa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProfessorProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Educations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Educations_ProfessorProfiles_ProfessorProfileId",
                        column: x => x.ProfessorProfileId,
                        principalTable: "ProfessorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Membership",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleFa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProfessorProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Membership", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Membership_ProfessorProfiles_ProfessorProfileId",
                        column: x => x.ProfessorProfileId,
                        principalTable: "ProfessorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfessionalActivity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleFa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProfessorProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessionalActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfessionalActivity_ProfessorProfiles_ProfessorProfileId",
                        column: x => x.ProfessorProfileId,
                        principalTable: "ProfessorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfessorLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Link = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleFa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProfessorProfileId = table.Column<int>(type: "int", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "ResearchAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleFa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProfessorProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResearchAreas_ProfessorProfiles_ProfessorProfileId",
                        column: x => x.ProfessorProfileId,
                        principalTable: "ProfessorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeachingInterests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleFa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProfessorProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeachingInterests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeachingInterests_ProfessorProfiles_ProfessorProfileId",
                        column: x => x.ProfessorProfileId,
                        principalTable: "ProfessorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Link = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TitleFa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProfessorProfileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebLinks_ProfessorProfiles_ProfessorProfileId",
                        column: x => x.ProfessorProfileId,
                        principalTable: "ProfessorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ProfessorProfileId",
                table: "Articles",
                column: "ProfessorProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Books_ProfessorProfileId",
                table: "Books",
                column: "ProfessorProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_ProfessorProfileId",
                table: "Courses",
                column: "ProfessorProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Educations_ProfessorProfileId",
                table: "Educations",
                column: "ProfessorProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Membership_ProfessorProfileId",
                table: "Membership",
                column: "ProfessorProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalActivity_ProfessorProfileId",
                table: "ProfessionalActivity",
                column: "ProfessorProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfessorLinks_ProfessorProfileId",
                table: "ProfessorLinks",
                column: "ProfessorProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchAreas_ProfessorProfileId",
                table: "ResearchAreas",
                column: "ProfessorProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingInterests_ProfessorProfileId",
                table: "TeachingInterests",
                column: "ProfessorProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_WebLinks_ProfessorProfileId",
                table: "WebLinks",
                column: "ProfessorProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Articles");

            migrationBuilder.DropTable(
                name: "Books");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Educations");

            migrationBuilder.DropTable(
                name: "Membership");

            migrationBuilder.DropTable(
                name: "ProfessionalActivity");

            migrationBuilder.DropTable(
                name: "ProfessorLinks");

            migrationBuilder.DropTable(
                name: "ResearchAreas");

            migrationBuilder.DropTable(
                name: "TeachingInterests");

            migrationBuilder.DropTable(
                name: "WebLinks");

            migrationBuilder.DropTable(
                name: "ProfessorProfiles");
        }
    }
}
