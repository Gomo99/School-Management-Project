using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SchoolProject.Migrations
{
    /// <inheritdoc />
    public partial class AddMessagesTable : Migration
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
                    IsTwoFactorEnabled = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
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
                name: "AssessmentTypes",
                columns: table => new
                {
                    AssessmentTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentTypeDescription = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AssessmentTypeStatus = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentTypes", x => x.AssessmentTypeID);
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
                name: "Messages",
                columns: table => new
                {
                    MessageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderId = table.Column<int>(type: "int", nullable: false),
                    ReceiverId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeletedBySender = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    IsDeletedByReceiver = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.MessageId);
                    table.ForeignKey(
                        name: "FK_Messages_Accounts_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "Accounts",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Messages_Accounts_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Accounts",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
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
                    Revoked = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false)
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
                        name: "FK_Assessments_AssessmentTypes_AssessmentTypeID",
                        column: x => x.AssessmentTypeID,
                        principalTable: "AssessmentTypes",
                        principalColumn: "AssessmentTypeID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Assessments_StudentModules_StudentModuleID",
                        column: x => x.StudentModuleID,
                        principalTable: "StudentModules",
                        principalColumn: "StudentModuleID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Accounts",
                columns: new[] { "UserID", "Email", "EmailVerificationTokenExpires", "EmailVerificationTokenHash", "ExternalProvider", "ExternalProviderId", "FailedLoginAttempts", "IsTwoFactorEnabled", "LastExternalLogin", "LockoutEnd", "Name", "Password", "ResetPin", "ResetPinExpiration", "Role", "Surname", "Title", "TwoFactorRecoveryCodes", "TwoFactorSecretKey", "UserStatus" },
                values: new object[,]
                {
                    { 1, "admin@school.com", null, null, null, null, 0, "False", null, null, "Admin", "admin123", null, null, "Administrator", "User", "System Admin", null, null, "Active" },
                    { 2, "lecturer@school.com", null, null, null, null, 0, "False", null, null, "Lecturer", "admin123", null, null, "Lecturer", "User", "System Lecturer", null, null, "Active" },
                    { 3, "student@school.com", null, null, null, null, 0, "False", null, null, "Student", "admin123", null, null, "Student", "User", "System Student", null, null, "Active" }
                });

            migrationBuilder.InsertData(
                table: "AssessmentTypes",
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

            migrationBuilder.InsertData(
                table: "Modules",
                columns: new[] { "ModuleID", "Duration", "ModuleName", "ModuleStatus", "ModuleType" },
                values: new object[,]
                {
                    { 1, 12, "Mathematics 101", "Active", "Core" },
                    { 2, 14, "Physics 101", "Active", "Core" },
                    { 3, 15, "Chemistry 101", "Active", "Core" },
                    { 4, 10, "Biology 101", "Active", "Core" },
                    { 5, 16, "Computer Science 101", "Active", "Elective" }
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
                name: "IX_Messages_ReceiverId",
                table: "Messages",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderId",
                table: "Messages",
                column: "SenderId");

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
                name: "Messages");

            migrationBuilder.DropTable(
                name: "RememberedDevices");

            migrationBuilder.DropTable(
                name: "AssessmentTypes");

            migrationBuilder.DropTable(
                name: "StudentModules");

            migrationBuilder.DropTable(
                name: "LecturerModules");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Modules");
        }
    }
}
