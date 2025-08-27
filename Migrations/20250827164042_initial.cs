using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SchoolProject.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResetPin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsTwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorSecretKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TwoFactorRecoveryCodes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailVerificationTokenHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailVerificationTokenExpires = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LockoutEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExternalProvider = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalProviderId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastExternalLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResetPinExpiration = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.UserID);
                });

            migrationBuilder.CreateTable(
                name: "assessmentTypes",
                columns: table => new
                {
                    AssessmentTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentTypeDescription = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AssessmentTypeStatus = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assessmentTypes", x => x.AssessmentTypeID);
                });

            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    ModuleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    ModuleType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModuleStatus = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => x.ModuleID);
                });

            migrationBuilder.CreateTable(
                name: "RememberedDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Revoked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RememberedDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RememberedDevices_Accounts_UserId",
                        column: x => x.UserId,
                        principalTable: "Accounts",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LecturerModules",
                columns: table => new
                {
                    LecturerModuleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModLecturerStatus = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LecturerModules", x => x.LecturerModuleID);
                    table.ForeignKey(
                        name: "FK_LecturerModules_Accounts_UserID",
                        column: x => x.UserID,
                        principalTable: "Accounts",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LecturerModules_Modules_ModuleID",
                        column: x => x.ModuleID,
                        principalTable: "Modules",
                        principalColumn: "ModuleID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentModules",
                columns: table => new
                {
                    StudentModuleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LecturerModuleID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StudModStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountUserID = table.Column<int>(type: "int", nullable: true),
                    ModuleID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentModules", x => x.StudentModuleID);
                    table.ForeignKey(
                        name: "FK_StudentModules_Accounts_AccountUserID",
                        column: x => x.AccountUserID,
                        principalTable: "Accounts",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_StudentModules_Accounts_UserID",
                        column: x => x.UserID,
                        principalTable: "Accounts",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_StudentModules_LecturerModules_LecturerModuleID",
                        column: x => x.LecturerModuleID,
                        principalTable: "LecturerModules",
                        principalColumn: "LecturerModuleID");
                    table.ForeignKey(
                        name: "FK_StudentModules_Modules_ModuleID",
                        column: x => x.ModuleID,
                        principalTable: "Modules",
                        principalColumn: "ModuleID");
                });

            migrationBuilder.CreateTable(
                name: "Assessments",
                columns: table => new
                {
                    AssessmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentModuleID = table.Column<int>(type: "int", nullable: false),
                    AssessmentTypeID = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssessmentStatus = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assessments", x => x.AssessmentID);
                    table.ForeignKey(
                        name: "FK_Assessments_StudentModules_StudentModuleID",
                        column: x => x.StudentModuleID,
                        principalTable: "StudentModules",
                        principalColumn: "StudentModuleID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Assessments_assessmentTypes_AssessmentTypeID",
                        column: x => x.AssessmentTypeID,
                        principalTable: "assessmentTypes",
                        principalColumn: "AssessmentTypeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Accounts",
                columns: new[] { "UserID", "Email", "EmailVerificationTokenExpires", "EmailVerificationTokenHash", "ExternalProvider", "ExternalProviderId", "FailedLoginAttempts", "IsTwoFactorEnabled", "LastExternalLogin", "LockoutEnd", "Name", "Password", "ResetPin", "ResetPinExpiration", "Role", "Surname", "Title", "TwoFactorRecoveryCodes", "TwoFactorSecretKey", "UserStatus" },
                values: new object[,]
                {
                    { 1, "admin@school.com", null, null, null, null, 0, false, null, null, "Admin", "admin123", null, null, "Administrator", "User", "System Admin", null, null, "Active" },
                    { 2, "lecturer@school.com", null, null, null, null, 0, false, null, null, "lECTURE", "admin123", null, null, "Lecturer", "User", "System Lecturer", null, null, "Active" },
                    { 3, "student@school.com", null, null, null, null, 0, false, null, null, "Student", "admin123", null, null, "Student", "User", "System Student", null, null, "Active" }
                });

            migrationBuilder.InsertData(
                table: "Modules",
                columns: new[] { "ModuleID", "Duration", "ModuleName", "ModuleStatus", "ModuleType" },
                values: new object[,]
                {
                    { 1, 12, "Mathematics 101", "Active", "Core" },
                    { 2, 14, "Physics 101", "Active", "Core" },
                    { 3, 15, "Chemistry 101", "Active", "Core" },
                    { 4, 10, "Biology 101", "Active", "Core" },
                    { 5, 16, "Computer Science 101", "Active", "Elective" },
                    { 6, 12, "English Literature", "Active", "Elective" },
                    { 7, 18, "History 101", "Active", "Core" },
                    { 8, 12, "Philosophy 101", "Active", "Elective" },
                    { 9, 12, "Psychology 101", "Active", "Elective" },
                    { 10, 14, "Sociology 101", "Active", "Core" },
                    { 11, 12, "Political Science 101", "Active", "Elective" },
                    { 12, 16, "Economics 101", "Active", "Core" },
                    { 13, 13, "Geography 101", "Active", "Elective" },
                    { 14, 20, "Engineering 101", "Active", "Core" },
                    { 15, 12, "Law 101", "Active", "Elective" },
                    { 16, 18, "Medicine 101", "Active", "Core" },
                    { 17, 14, "Nursing 101", "Active", "Core" },
                    { 18, 14, "Business Management 101", "Active", "Elective" },
                    { 19, 12, "Accounting 101", "Active", "Core" },
                    { 20, 13, "Marketing 101", "Active", "Elective" },
                    { 21, 12, "Design 101", "Active", "Core" },
                    { 22, 12, "Art 101", "Active", "Elective" },
                    { 23, 14, "Music 101", "Active", "Core" },
                    { 24, 13, "Theater 101", "Active", "Elective" },
                    { 25, 12, "Dance 101", "Active", "Elective" },
                    { 26, 24, "Architecture 101", "Active", "Core" },
                    { 27, 15, "Physics 201", "Active", "Core" },
                    { 28, 12, "Statistics 101", "Active", "Elective" },
                    { 29, 14, "Data Science 101", "Active", "Core" },
                    { 30, 16, "Artificial Intelligence 101", "Active", "Elective" },
                    { 31, 14, "Machine Learning 101", "Active", "Core" },
                    { 32, 16, "Cloud Computing 101", "Active", "Elective" },
                    { 33, 16, "Cyber Security 101", "Active", "Core" },
                    { 34, 12, "Networking 101", "Active", "Elective" },
                    { 35, 15, "Database Management 101", "Active", "Core" },
                    { 36, 14, "Web Development 101", "Active", "Elective" },
                    { 37, 14, "Game Development 101", "Active", "Core" },
                    { 38, 15, "Cloud Architecture 101", "Active", "Elective" },
                    { 39, 18, "Blockchain 101", "Active", "Core" },
                    { 40, 12, "Digital Marketing 101", "Active", "Elective" },
                    { 41, 13, "Human Resources 101", "Active", "Core" },
                    { 42, 14, "Project Management 101", "Active", "Elective" },
                    { 43, 15, "Operations Management 101", "Active", "Core" },
                    { 44, 12, "Entrepreneurship 101", "Active", "Elective" },
                    { 45, 12, "Public Relations 101", "Active", "Core" },
                    { 46, 13, "Leadership 101", "Active", "Elective" },
                    { 47, 14, "Sustainability 101", "Active", "Core" },
                    { 48, 12, "Logistics 101", "Active", "Elective" },
                    { 49, 16, "Supply Chain Management 101", "Active", "Core" },
                    { 50, 15, "Business Analytics 101", "Active", "Elective" }
                });

            migrationBuilder.InsertData(
                table: "assessmentTypes",
                columns: new[] { "AssessmentTypeID", "AssessmentTypeDescription", "AssessmentTypeStatus" },
                values: new object[,]
                {
                    { 1, "Formal written examination", "Active" },
                    { 2, "Short knowledge test", "Active" },
                    { 3, "Practical or theoretical work assignment", "Active" },
                    { 4, "Extended practical project work", "Active" },
                    { 5, "Oral presentation of work", "Active" },
                    { 6, "Hands-on practical assessment", "Active" },
                    { 7, "Analysis of real-world scenarios", "Active" },
                    { 8, "Collection of work samples", "Active" },
                    { 9, "Scientific laboratory report", "Active" },
                    { 10, "Academic research paper", "Active" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_AssessmentTypeID",
                table: "Assessments",
                column: "AssessmentTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_StudentModuleID",
                table: "Assessments",
                column: "StudentModuleID");

            migrationBuilder.CreateIndex(
                name: "IX_LecturerModules_ModuleID",
                table: "LecturerModules",
                column: "ModuleID");

            migrationBuilder.CreateIndex(
                name: "IX_LecturerModules_UserID",
                table: "LecturerModules",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_RememberedDevices_UserId",
                table: "RememberedDevices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentModules_AccountUserID",
                table: "StudentModules",
                column: "AccountUserID");

            migrationBuilder.CreateIndex(
                name: "IX_StudentModules_LecturerModuleID",
                table: "StudentModules",
                column: "LecturerModuleID");

            migrationBuilder.CreateIndex(
                name: "IX_StudentModules_ModuleID",
                table: "StudentModules",
                column: "ModuleID");

            migrationBuilder.CreateIndex(
                name: "IX_StudentModules_UserID",
                table: "StudentModules",
                column: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assessments");

            migrationBuilder.DropTable(
                name: "RememberedDevices");

            migrationBuilder.DropTable(
                name: "StudentModules");

            migrationBuilder.DropTable(
                name: "assessmentTypes");

            migrationBuilder.DropTable(
                name: "LecturerModules");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Modules");
        }
    }
}
