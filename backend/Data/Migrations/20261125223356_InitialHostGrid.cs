using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HostGrid.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialHostGrid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "property");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.EnsureSchema(
                name: "system");

            migrationBuilder.EnsureSchema(
                name: "booking");

            migrationBuilder.EnsureSchema(
                name: "operation");

            migrationBuilder.EnsureSchema(
                name: "finance");

            migrationBuilder.EnsureSchema(
                name: "reporting");

            migrationBuilder.EnsureSchema(
                name: "communication");

            migrationBuilder.EnsureSchema(
                name: "expense");

            migrationBuilder.EnsureSchema(
                name: "organization");

            migrationBuilder.EnsureSchema(
                name: "document");

            migrationBuilder.CreateTable(
                name: "Amenity",
                schema: "property",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Amenity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationRole",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationRole", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationUser",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organization",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Reference = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organization", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropertyType",
                schema: "property",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaim",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaim_ApplicationRole_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "ApplicationRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshToken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshToken_ApplicationUser_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefreshToken_RefreshToken_ReplacedById",
                        column: x => x.ReplacedById,
                        principalSchema: "identity",
                        principalTable: "RefreshToken",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserClaim",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaim_ApplicationUser_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogin",
                schema: "identity",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogin", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogin_ApplicationUser_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Theme = table.Column<short>(type: "smallint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                    table.CheckConstraint("CK_UserPreferences_Theme", "[Theme] BETWEEN 0 AND 2");
                    table.ForeignKey(
                        name: "FK_UserPreferences_ApplicationUser_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserRole",
                schema: "identity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRole", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRole_ApplicationRole_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "ApplicationRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRole_ApplicationUser_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserToken",
                schema: "identity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserToken", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserToken_ApplicationUser_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                schema: "system",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OldValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                    table.UniqueConstraint("AK_AuditLog_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_AuditLog_ApplicationUser_ActorId",
                        column: x => x.ActorId,
                        principalSchema: "identity",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditLog_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundJobExecution",
                schema: "system",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobKey = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeaseUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LeaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackgroundJobExecution", x => x.Id);
                    table.UniqueConstraint("AK_BackgroundJobExecution_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_BackgroundJobExecution_Lease", "[Attempts] >= 0 AND (([LeaseId] IS NULL AND [LeaseUntil] IS NULL) OR ([LeaseId] IS NOT NULL AND [LeaseUntil] IS NOT NULL))");
                    table.CheckConstraint("CK_BackgroundJobExecution_Status", "[Status] BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_BackgroundJobExecution_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmailOutbox",
                schema: "communication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Template = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeaseUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LeaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailOutbox", x => x.Id);
                    table.UniqueConstraint("AK_EmailOutbox_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_EmailOutbox_Lease", "[Attempts] >= 0 AND (([LeaseId] IS NULL AND [LeaseUntil] IS NULL) OR ([LeaseId] IS NOT NULL AND [LeaseUntil] IS NOT NULL))");
                    table.CheckConstraint("CK_EmailOutbox_Status", "[Status] BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_EmailOutbox_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseCategory",
                schema: "expense",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseCategory", x => x.Id);
                    table.UniqueConstraint("AK_ExpenseCategory_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_ExpenseCategory_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExternalContact",
                schema: "operation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Specialty = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalContact", x => x.Id);
                    table.UniqueConstraint("AK_ExternalContact_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_ExternalContact_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialTransaction",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: false),
                    SourceType = table.Column<short>(type: "smallint", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Payer = table.Column<short>(type: "smallint", nullable: false),
                    Receiver = table.Column<short>(type: "smallint", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialTransaction", x => x.Id);
                    table.UniqueConstraint("AK_FinancialTransaction_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_FinancialTransaction_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_FinancialTransaction_Payer", "[Payer] BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_FinancialTransaction_Receiver", "[Receiver] BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_FinancialTransaction_SourceType", "[SourceType] BETWEEN 0 AND 10");
                    table.CheckConstraint("CK_FinancialTransaction_Type", "[Type] BETWEEN 0 AND 5");
                    table.ForeignKey(
                        name: "FK_FinancialTransaction_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Guest",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guest", x => x.Id);
                    table.UniqueConstraint("AK_Guest_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_Guest_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationMember",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<short>(type: "smallint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationMember", x => x.Id);
                    table.UniqueConstraint("AK_OrganizationMember_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.UniqueConstraint("AK_OrganizationMember_OrganizationId_UserId", x => new { x.OrganizationId, x.UserId });
                    table.CheckConstraint("CK_OrganizationMember_Role", "[Role] BETWEEN 0 AND 1");
                    table.ForeignKey(
                        name: "FK_OrganizationMember_ApplicationUser_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationMember_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoredFile",
                schema: "document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OriginalName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(127)", maxLength: 127, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ScanStatus = table.Column<short>(type: "smallint", nullable: false),
                    UploaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredFile", x => x.Id);
                    table.UniqueConstraint("AK_StoredFile_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_StoredFile_ScanStatus", "[ScanStatus] BETWEEN 0 AND 2");
                    table.CheckConstraint("CK_StoredFile_Size", "[SizeBytes] > 0");
                    table.ForeignKey(
                        name: "FK_StoredFile_ApplicationUser_UploaderId",
                        column: x => x.UploaderId,
                        principalSchema: "identity",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoredFile_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DashboardPreference",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LayoutJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardPreference", x => x.Id);
                    table.UniqueConstraint("AK_DashboardPreference_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_DashboardPreference_OrganizationMember_OrganizationId_UserId",
                        columns: x => new { x.OrganizationId, x.UserId },
                        principalSchema: "organization",
                        principalTable: "OrganizationMember",
                        principalColumns: new[] { "OrganizationId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DashboardPreference_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                schema: "communication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    MessageKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.Id);
                    table.UniqueConstraint("AK_Notification_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_Notification_OrganizationMember_OrganizationId_UserId",
                        columns: x => new { x.OrganizationId, x.UserId },
                        principalSchema: "organization",
                        principalTable: "OrganizationMember",
                        principalColumns: new[] { "OrganizationId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notification_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationPreference",
                schema: "communication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InAppEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreference", x => x.Id);
                    table.UniqueConstraint("AK_NotificationPreference_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_NotificationPreference_OrganizationMember_OrganizationId_UserId",
                        columns: x => new { x.OrganizationId, x.UserId },
                        principalSchema: "organization",
                        principalTable: "OrganizationMember",
                        principalColumns: new[] { "OrganizationId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationPreference_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Owner",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Owner", x => x.Id);
                    table.UniqueConstraint("AK_Owner_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_Owner_OrganizationMember_OrganizationId_UserId",
                        columns: x => new { x.OrganizationId, x.UserId },
                        principalSchema: "organization",
                        principalTable: "OrganizationMember",
                        principalColumns: new[] { "OrganizationId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Owner_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationSettings",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrandName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LogoFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DefaultCommissionRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 20m),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationSettings", x => x.Id);
                    table.UniqueConstraint("AK_OrganizationSettings_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_OrganizationSettings_DefaultCommissionRate", "[DefaultCommissionRate] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_OrganizationSettings_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationSettings_StoredFile_OrganizationId_LogoFileId",
                        columns: x => new { x.OrganizationId, x.LogoFileId },
                        principalSchema: "document",
                        principalTable: "StoredFile",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OwnerPayout",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Method = table.Column<short>(type: "smallint", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerPayout", x => x.Id);
                    table.UniqueConstraint("AK_OwnerPayout_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.UniqueConstraint("AK_OwnerPayout_OrganizationId_Id_OwnerId", x => new { x.OrganizationId, x.Id, x.OwnerId });
                    table.CheckConstraint("CK_OwnerPayout_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_OwnerPayout_Method", "[Method] BETWEEN 0 AND 3");
                    table.CheckConstraint("CK_OwnerPayout_Status", "[Status] BETWEEN 0 AND 2");
                    table.ForeignKey(
                        name: "FK_OwnerPayout_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OwnerPayout_Owner_OrganizationId_OwnerId",
                        columns: x => new { x.OrganizationId, x.OwnerId },
                        principalSchema: "organization",
                        principalTable: "Owner",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Property",
                schema: "property",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    City = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    MaxGuests = table.Column<short>(type: "smallint", nullable: false),
                    Bedrooms = table.Column<short>(type: "smallint", nullable: false),
                    Bathrooms = table.Column<short>(type: "smallint", nullable: false),
                    BaseNightlyRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CheckInTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    CheckOutTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    OperationalStatus = table.Column<short>(type: "smallint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Property", x => x.Id);
                    table.UniqueConstraint("AK_Property_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_Property_BaseNightlyRate", "[BaseNightlyRate] >= 0");
                    table.CheckConstraint("CK_Property_Capacity", "[MaxGuests] > 0 AND [Bedrooms] >= 0 AND [Bathrooms] >= 0");
                    table.CheckConstraint("CK_Property_Coordinates", "[Latitude] BETWEEN -90 AND 90 AND [Longitude] BETWEEN -180 AND 180");
                    table.CheckConstraint("CK_Property_OperationalStatus", "[OperationalStatus] BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_Property_Status", "[Status] BETWEEN 0 AND 2");
                    table.ForeignKey(
                        name: "FK_Property_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Property_Owner_OrganizationId_OwnerId",
                        columns: x => new { x.OrganizationId, x.OwnerId },
                        principalSchema: "organization",
                        principalTable: "Owner",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Property_PropertyType_TypeId",
                        column: x => x.TypeId,
                        principalSchema: "property",
                        principalTable: "PropertyType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CalendarBlock",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarBlock", x => x.Id);
                    table.UniqueConstraint("AK_CalendarBlock_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_CalendarBlock_Dates", "[EndDate] > [StartDate]");
                    table.CheckConstraint("CK_CalendarBlock_Status", "[Status] BETWEEN 0 AND 1");
                    table.ForeignKey(
                        name: "FK_CalendarBlock_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CalendarBlock_Property_OrganizationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.PropertyId },
                        principalSchema: "property",
                        principalTable: "Property",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChecklistTemplate",
                schema: "operation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistTemplate", x => x.Id);
                    table.UniqueConstraint("AK_ChecklistTemplate_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_ChecklistTemplate_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChecklistTemplate_Property_OrganizationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.PropertyId },
                        principalSchema: "property",
                        principalTable: "Property",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommissionAdjustment",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<short>(type: "smallint", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginPeriod = table.Column<DateOnly>(type: "date", nullable: false),
                    TargetPeriod = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionAdjustment", x => x.Id);
                    table.UniqueConstraint("AK_CommissionAdjustment_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_CommissionAdjustment_Periods", "DAY([OriginPeriod]) = 1 AND DAY([TargetPeriod]) = 1 AND [OriginPeriod] < [TargetPeriod]");
                    table.CheckConstraint("CK_CommissionAdjustment_SourceType", "[SourceType] BETWEEN 0 AND 10");
                    table.ForeignKey(
                        name: "FK_CommissionAdjustment_ApplicationUser_ActorId",
                        column: x => x.ActorId,
                        principalSchema: "identity",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommissionAdjustment_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommissionAdjustment_Property_OrganizationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.PropertyId },
                        principalSchema: "property",
                        principalTable: "Property",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommissionRateHistory",
                schema: "property",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Rate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionRateHistory", x => x.Id);
                    table.UniqueConstraint("AK_CommissionRateHistory_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_CommissionRateHistory_Dates", "[EffectiveTo] > [EffectiveFrom]");
                    table.CheckConstraint("CK_CommissionRateHistory_Rate", "[Rate] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_CommissionRateHistory_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommissionRateHistory_Property_OrganizationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.PropertyId },
                        principalSchema: "property",
                        principalTable: "Property",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialAdjustment",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<short>(type: "smallint", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginPeriod = table.Column<DateOnly>(type: "date", nullable: false),
                    TargetPeriod = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialAdjustment", x => x.Id);
                    table.UniqueConstraint("AK_FinancialAdjustment_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_FinancialAdjustment_Periods", "DAY([OriginPeriod]) = 1 AND DAY([TargetPeriod]) = 1 AND [OriginPeriod] < [TargetPeriod]");
                    table.CheckConstraint("CK_FinancialAdjustment_SourceType", "[SourceType] BETWEEN 0 AND 10");
                    table.ForeignKey(
                        name: "FK_FinancialAdjustment_ApplicationUser_ActorId",
                        column: x => x.ActorId,
                        principalSchema: "identity",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialAdjustment_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialAdjustment_Property_OrganizationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.PropertyId },
                        principalSchema: "property",
                        principalTable: "Property",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceTicket",
                schema: "operation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Priority = table.Column<short>(type: "smallint", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceTicket", x => x.Id);
                    table.UniqueConstraint("AK_MaintenanceTicket_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.UniqueConstraint("AK_MaintenanceTicket_OrganizationId_Id_PropertyId", x => new { x.OrganizationId, x.Id, x.PropertyId });
                    table.CheckConstraint("CK_MaintenanceTicket_Priority", "[Priority] BETWEEN 0 AND 3");
                    table.CheckConstraint("CK_MaintenanceTicket_Status", "[Status] BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_MaintenanceTicket_ExternalContact_OrganizationId_ContactId",
                        columns: x => new { x.OrganizationId, x.ContactId },
                        principalSchema: "operation",
                        principalTable: "ExternalContact",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceTicket_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceTicket_Property_OrganizationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.PropertyId },
                        principalSchema: "property",
                        principalTable: "Property",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ManagementContract",
                schema: "property",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Rate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagementContract", x => x.Id);
                    table.UniqueConstraint("AK_ManagementContract_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_ManagementContract_Dates", "[EndDate] > [StartDate]");
                    table.CheckConstraint("CK_ManagementContract_Rate", "[Rate] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_ManagementContract_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagementContract_Owner_OrganizationId_OwnerId",
                        columns: x => new { x.OrganizationId, x.OwnerId },
                        principalSchema: "organization",
                        principalTable: "Owner",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagementContract_Property_OrganizationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.PropertyId },
                        principalSchema: "property",
                        principalTable: "Property",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagementContract_StoredFile_OrganizationId_FileId",
                        columns: x => new { x.OrganizationId, x.FileId },
                        principalSchema: "document",
                        principalTable: "StoredFile",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyClosure",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<short>(type: "smallint", nullable: false),
                    Month = table.Column<short>(type: "smallint", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PlatformFees = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    NetPlatformRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OwnerExpenses = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OwnerProfit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Advances = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OwnerDirectPayments = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OpeningSettlementBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PayoutDueAtClosing = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyClosure", x => x.Id);
                    table.UniqueConstraint("AK_MonthlyClosure_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.UniqueConstraint("AK_MonthlyClosure_OrganizationId_Id_OwnerId", x => new { x.OrganizationId, x.Id, x.OwnerId });
                    table.CheckConstraint("CK_MonthlyClosure_Advances", "[Advances] >= 0");
                    table.CheckConstraint("CK_MonthlyClosure_ClosedMetadata", "([Status] = 0 AND [ClosedAt] IS NULL AND [ClosedById] IS NULL) OR ([Status] = 1 AND [ClosedAt] IS NOT NULL AND [ClosedById] IS NOT NULL)");
                    table.CheckConstraint("CK_MonthlyClosure_CommissionAmount", "[CommissionAmount] >= 0");
                    table.CheckConstraint("CK_MonthlyClosure_GrossAmount", "[GrossAmount] >= 0");
                    table.CheckConstraint("CK_MonthlyClosure_NetPlatformRevenue", "[NetPlatformRevenue] >= 0");
                    table.CheckConstraint("CK_MonthlyClosure_OwnerDirectPayments", "[OwnerDirectPayments] >= 0");
                    table.CheckConstraint("CK_MonthlyClosure_OwnerExpenses", "[OwnerExpenses] >= 0");
                    table.CheckConstraint("CK_MonthlyClosure_Period", "[Year] BETWEEN 1 AND 9999 AND [Month] BETWEEN 1 AND 12");
                    table.CheckConstraint("CK_MonthlyClosure_PlatformFees", "[PlatformFees] >= 0");
                    table.CheckConstraint("CK_MonthlyClosure_Profit", "[OwnerProfit] = [NetPlatformRevenue] - [CommissionAmount] - [OwnerExpenses]");
                    table.CheckConstraint("CK_MonthlyClosure_Status", "[Status] BETWEEN 0 AND 1");
                    table.ForeignKey(
                        name: "FK_MonthlyClosure_ApplicationUser_ClosedById",
                        column: x => x.ClosedById,
                        principalSchema: "identity",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonthlyClosure_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonthlyClosure_Owner_OrganizationId_OwnerId",
                        columns: x => new { x.OrganizationId, x.OwnerId },
                        principalSchema: "organization",
                        principalTable: "Owner",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonthlyClosure_Property_OrganizationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.PropertyId },
                        principalSchema: "property",
                        principalTable: "Property",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OwnerRequest",
                schema: "communication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerRequest", x => x.Id);
                    table.UniqueConstraint("AK_OwnerRequest_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_OwnerRequest_Status", "[Status] BETWEEN 0 AND 5");
                    table.CheckConstraint("CK_OwnerRequest_Type", "[Type] BETWEEN 0 AND 5");
                    table.ForeignKey(
                        name: "FK_OwnerRequest_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OwnerRequest_Owner_OrganizationId_OwnerId",
                        columns: x => new { x.OrganizationId, x.OwnerId },
                        principalSchema: "organization",
                        principalTable: "Owner",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OwnerRequest_Property_OrganizationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.PropertyId },
                        principalSchema: "property",
                        principalTable: "Property",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PropertyAmenity",
                schema: "property",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmenityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyAmenity", x => x.Id);
                    table.UniqueConstraint("AK_PropertyAmenity_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_PropertyAmenity_Amenity_AmenityId",
                        column: x => x.AmenityId,
                        principalSchema: "property",
                        principalTable: "Amenity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PropertyAmenity_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PropertyAmenity_Property_OrganizationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.PropertyId },
                        principalSchema: "property",
                        principalTable: "Property",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PropertyDocument",
                schema: "document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyDocument", x => x.Id);
                    table.UniqueConstraint("AK_PropertyDocument_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_PropertyDocument_Type", "[Type] BETWEEN 0 AND 1");
                    table.ForeignKey(
                        name: "FK_PropertyDocument_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PropertyDocument_Property_OrganizationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.PropertyId },
                        principalSchema: "property",
                        principalTable: "Property",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PropertyDocument_StoredFile_OrganizationId_FileId",
                        columns: x => new { x.OrganizationId, x.FileId },
                        principalSchema: "document",
                        principalTable: "StoredFile",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PropertyInventoryItem",
                schema: "property",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Condition = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyInventoryItem", x => x.Id);
                    table.UniqueConstraint("AK_PropertyInventoryItem_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_PropertyInventoryItem_Quantity", "[Quantity] >= 0");
                    table.ForeignKey(
                        name: "FK_PropertyInventoryItem_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PropertyInventoryItem_Property_OrganizationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.PropertyId },
                        principalSchema: "property",
                        principalTable: "Property",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PropertyListing",
                schema: "property",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<short>(type: "smallint", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    ExternalReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyListing", x => x.Id);
                    table.UniqueConstraint("AK_PropertyListing_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_PropertyListing_Source", "[Source] BETWEEN 0 AND 2");
                    table.ForeignKey(
                        name: "FK_PropertyListing_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PropertyListing_Property_OrganizationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.PropertyId },
                        principalSchema: "property",
                        principalTable: "Property",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PropertyPhoto",
                schema: "property",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyPhoto", x => x.Id);
                    table.UniqueConstraint("AK_PropertyPhoto_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_PropertyPhoto_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PropertyPhoto_Property_OrganizationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.PropertyId },
                        principalSchema: "property",
                        principalTable: "Property",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PropertyPhoto_StoredFile_OrganizationId_FileId",
                        columns: x => new { x.OrganizationId, x.FileId },
                        principalSchema: "document",
                        principalTable: "StoredFile",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecurringExpenseTemplate",
                schema: "expense",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Cadence = table.Column<short>(type: "smallint", nullable: false),
                    NextDueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringExpenseTemplate", x => x.Id);
                    table.UniqueConstraint("AK_RecurringExpenseTemplate_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.UniqueConstraint("AK_RecurringExpenseTemplate_OrganizationId_Id_PropertyId", x => new { x.OrganizationId, x.Id, x.PropertyId });
                    table.CheckConstraint("CK_RecurringExpenseTemplate_Amount", "[Amount] >= 0");
                    table.CheckConstraint("CK_RecurringExpenseTemplate_Cadence", "[Cadence] BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_RecurringExpenseTemplate_ExpenseCategory_OrganizationId_CategoryId",
                        columns: x => new { x.OrganizationId, x.CategoryId },
                        principalSchema: "expense",
                        principalTable: "ExpenseCategory",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringExpenseTemplate_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringExpenseTemplate_Property_OrganizationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.PropertyId },
                        principalSchema: "property",
                        principalTable: "Property",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Report",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<short>(type: "smallint", nullable: false),
                    FiltersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Report", x => x.Id);
                    table.UniqueConstraint("AK_Report_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_Report_Type", "[Type] BETWEEN 0 AND 6");
                    table.ForeignKey(
                        name: "FK_Report_OrganizationMember_OrganizationId_UserId",
                        columns: x => new { x.OrganizationId, x.UserId },
                        principalSchema: "organization",
                        principalTable: "OrganizationMember",
                        principalColumns: new[] { "OrganizationId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Report_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Report_Owner_OrganizationId_OwnerId",
                        columns: x => new { x.OrganizationId, x.OwnerId },
                        principalSchema: "organization",
                        principalTable: "Owner",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Report_Property_OrganizationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.PropertyId },
                        principalSchema: "property",
                        principalTable: "Property",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Report_StoredFile_OrganizationId_FileId",
                        columns: x => new { x.OrganizationId, x.FileId },
                        principalSchema: "document",
                        principalTable: "StoredFile",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reservation",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Source = table.Column<short>(type: "smallint", nullable: false),
                    ExternalBookingId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CheckInDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckOutDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Adults = table.Column<short>(type: "smallint", nullable: false),
                    Children = table.Column<short>(type: "smallint", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PlatformFees = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PlatformFeeRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    RevenueInputMode = table.Column<short>(type: "smallint", nullable: false),
                    NetPlatformRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionRateSnapshot = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    PaymentStatus = table.Column<short>(type: "smallint", nullable: false),
                    ActualCheckInAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualCheckOutAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservation", x => x.Id);
                    table.UniqueConstraint("AK_Reservation_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.UniqueConstraint("AK_Reservation_OrganizationId_Id_PropertyId", x => new { x.OrganizationId, x.Id, x.PropertyId });
                    table.CheckConstraint("CK_Reservation_ActualStay", "[ActualCheckOutAt] IS NULL OR ([ActualCheckInAt] IS NOT NULL AND [ActualCheckOutAt] >= [ActualCheckInAt])");
                    table.CheckConstraint("CK_Reservation_CommissionAmount", "[CommissionAmount] >= 0");
                    table.CheckConstraint("CK_Reservation_CommissionRateSnapshot", "[CommissionRateSnapshot] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_Reservation_Dates", "[CheckOutDate] > [CheckInDate]");
                    table.CheckConstraint("CK_Reservation_GrossAmount", "[GrossAmount] >= 0");
                    table.CheckConstraint("CK_Reservation_Guests", "[Adults] > 0 AND [Children] >= 0");
                    table.CheckConstraint("CK_Reservation_NetPlatformRevenue", "[NetPlatformRevenue] >= 0");
                    table.CheckConstraint("CK_Reservation_PaymentStatus", "[PaymentStatus] BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_Reservation_PlatformFeeRate", "[PlatformFeeRate] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_Reservation_PlatformFees", "[PlatformFees] >= 0");
                    table.CheckConstraint("CK_Reservation_RevenueInput", "([RevenueInputMode] = 2 AND [GrossAmount] IS NULL AND [PlatformFees] IS NULL AND [PlatformFeeRate] IS NULL) OR ([RevenueInputMode] IN (0, 1) AND [GrossAmount] IS NOT NULL AND [PlatformFees] IS NOT NULL AND [GrossAmount] - [PlatformFees] = [NetPlatformRevenue] AND (([RevenueInputMode] = 0 AND [PlatformFeeRate] IS NULL) OR ([RevenueInputMode] = 1 AND [PlatformFeeRate] IS NOT NULL)))");
                    table.CheckConstraint("CK_Reservation_RevenueInputMode", "[RevenueInputMode] BETWEEN 0 AND 2");
                    table.CheckConstraint("CK_Reservation_Source", "[Source] BETWEEN 0 AND 2");
                    table.CheckConstraint("CK_Reservation_Status", "[Status] BETWEEN 0 AND 5");
                    table.ForeignKey(
                        name: "FK_Reservation_Guest_OrganizationId_GuestId",
                        columns: x => new { x.OrganizationId, x.GuestId },
                        principalSchema: "booking",
                        principalTable: "Guest",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_Property_OrganizationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.PropertyId },
                        principalSchema: "property",
                        principalTable: "Property",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SeasonalRate",
                schema: "property",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DaysOfWeek = table.Column<short>(type: "smallint", nullable: false),
                    NightlyRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Priority = table.Column<short>(type: "smallint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonalRate", x => x.Id);
                    table.UniqueConstraint("AK_SeasonalRate_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_SeasonalRate_Dates", "[EndDate] > [StartDate] AND [DaysOfWeek] BETWEEN 1 AND 127");
                    table.CheckConstraint("CK_SeasonalRate_NightlyRate", "[NightlyRate] >= 0");
                    table.ForeignKey(
                        name: "FK_SeasonalRate_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeasonalRate_Property_OrganizationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.PropertyId },
                        principalSchema: "property",
                        principalTable: "Property",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChecklistTemplateItem",
                schema: "operation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistTemplateItem", x => x.Id);
                    table.UniqueConstraint("AK_ChecklistTemplateItem_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_ChecklistTemplateItem_ChecklistTemplate_OrganizationId_TemplateId",
                        columns: x => new { x.OrganizationId, x.TemplateId },
                        principalSchema: "operation",
                        principalTable: "ChecklistTemplate",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChecklistTemplateItem_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyClosureLine",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClosureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<short>(type: "smallint", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyClosureLine", x => x.Id);
                    table.UniqueConstraint("AK_MonthlyClosureLine_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_MonthlyClosureLine_Period", "DAY([PeriodStart]) = 1");
                    table.CheckConstraint("CK_MonthlyClosureLine_SourceType", "[SourceType] BETWEEN 0 AND 10");
                    table.ForeignKey(
                        name: "FK_MonthlyClosureLine_MonthlyClosure_OrganizationId_ClosureId",
                        columns: x => new { x.OrganizationId, x.ClosureId },
                        principalSchema: "finance",
                        principalTable: "MonthlyClosure",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonthlyClosureLine_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OwnerPayoutAllocation",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayoutId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClosureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerPayoutAllocation", x => x.Id);
                    table.UniqueConstraint("AK_OwnerPayoutAllocation_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_OwnerPayoutAllocation_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_OwnerPayoutAllocation_MonthlyClosure_OrganizationId_ClosureId_OwnerId",
                        columns: x => new { x.OrganizationId, x.ClosureId, x.OwnerId },
                        principalSchema: "finance",
                        principalTable: "MonthlyClosure",
                        principalColumns: new[] { "OrganizationId", "Id", "OwnerId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OwnerPayoutAllocation_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OwnerPayoutAllocation_OwnerPayout_OrganizationId_PayoutId_OwnerId",
                        columns: x => new { x.OrganizationId, x.PayoutId, x.OwnerId },
                        principalSchema: "finance",
                        principalTable: "OwnerPayout",
                        principalColumns: new[] { "OrganizationId", "Id", "OwnerId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OwnerRequestMessage",
                schema: "communication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerRequestMessage", x => x.Id);
                    table.UniqueConstraint("AK_OwnerRequestMessage_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_OwnerRequestMessage_ApplicationUser_AuthorId",
                        column: x => x.AuthorId,
                        principalSchema: "identity",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OwnerRequestMessage_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OwnerRequestMessage_OwnerRequest_OrganizationId_RequestId",
                        columns: x => new { x.OrganizationId, x.RequestId },
                        principalSchema: "communication",
                        principalTable: "OwnerRequest",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OwnerRequestStatusHistory",
                schema: "communication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldStatus = table.Column<short>(type: "smallint", nullable: false),
                    NewStatus = table.Column<short>(type: "smallint", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerRequestStatusHistory", x => x.Id);
                    table.UniqueConstraint("AK_OwnerRequestStatusHistory_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_OwnerRequestStatusHistory_NewStatus", "[NewStatus] BETWEEN 0 AND 5");
                    table.CheckConstraint("CK_OwnerRequestStatusHistory_OldStatus", "[OldStatus] BETWEEN 0 AND 5");
                    table.ForeignKey(
                        name: "FK_OwnerRequestStatusHistory_ApplicationUser_ActorId",
                        column: x => x.ActorId,
                        principalSchema: "identity",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OwnerRequestStatusHistory_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OwnerRequestStatusHistory_OwnerRequest_OrganizationId_RequestId",
                        columns: x => new { x.OrganizationId, x.RequestId },
                        principalSchema: "communication",
                        principalTable: "OwnerRequest",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Expense",
                schema: "expense",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpenseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RecurringTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurrenceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expense", x => x.Id);
                    table.UniqueConstraint("AK_Expense_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_Expense_Amount", "[Amount] >= 0");
                    table.CheckConstraint("CK_Expense_RecurringOccurrence", "([RecurringTemplateId] IS NULL AND [OccurrenceDate] IS NULL) OR ([RecurringTemplateId] IS NOT NULL AND [OccurrenceDate] IS NOT NULL)");
                    table.CheckConstraint("CK_Expense_Status", "[Status] BETWEEN 0 AND 2");
                    table.ForeignKey(
                        name: "FK_Expense_ApplicationUser_ActorId",
                        column: x => x.ActorId,
                        principalSchema: "identity",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Expense_ExpenseCategory_OrganizationId_CategoryId",
                        columns: x => new { x.OrganizationId, x.CategoryId },
                        principalSchema: "expense",
                        principalTable: "ExpenseCategory",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Expense_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Expense_Property_OrganizationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.PropertyId },
                        principalSchema: "property",
                        principalTable: "Property",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Expense_RecurringExpenseTemplate_OrganizationId_RecurringTemplateId_PropertyId",
                        columns: x => new { x.OrganizationId, x.RecurringTemplateId, x.PropertyId },
                        principalSchema: "expense",
                        principalTable: "RecurringExpenseTemplate",
                        principalColumns: new[] { "OrganizationId", "Id", "PropertyId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CleaningTask",
                schema: "operation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsAutomatic = table.Column<bool>(type: "bit", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CleaningTask", x => x.Id);
                    table.UniqueConstraint("AK_CleaningTask_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.UniqueConstraint("AK_CleaningTask_OrganizationId_Id_PropertyId", x => new { x.OrganizationId, x.Id, x.PropertyId });
                    table.CheckConstraint("CK_CleaningTask_AutomaticReservation", "[IsAutomatic] = 0 OR [ReservationId] IS NOT NULL");
                    table.CheckConstraint("CK_CleaningTask_Status", "[Status] BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_CleaningTask_ExternalContact_OrganizationId_ContactId",
                        columns: x => new { x.OrganizationId, x.ContactId },
                        principalSchema: "operation",
                        principalTable: "ExternalContact",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CleaningTask_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CleaningTask_Property_OrganizationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.PropertyId },
                        principalSchema: "property",
                        principalTable: "Property",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CleaningTask_Reservation_OrganizationId_ReservationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.ReservationId, x.PropertyId },
                        principalSchema: "booking",
                        principalTable: "Reservation",
                        principalColumns: new[] { "OrganizationId", "Id", "PropertyId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReservationCancellation",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RetainedFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationCancellation", x => x.Id);
                    table.UniqueConstraint("AK_ReservationCancellation_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_ReservationCancellation_RetainedFee", "[RetainedFee] >= 0");
                    table.ForeignKey(
                        name: "FK_ReservationCancellation_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationCancellation_Reservation_OrganizationId_ReservationId",
                        columns: x => new { x.OrganizationId, x.ReservationId },
                        principalSchema: "booking",
                        principalTable: "Reservation",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReservationNight",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NightDate = table.Column<DateOnly>(type: "date", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionRateSnapshot = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationNight", x => x.Id);
                    table.UniqueConstraint("AK_ReservationNight_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_ReservationNight_CommissionAmount", "[CommissionAmount] >= 0");
                    table.CheckConstraint("CK_ReservationNight_CommissionRateSnapshot", "[CommissionRateSnapshot] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_ReservationNight_NetAmount", "[NetAmount] >= 0");
                    table.ForeignKey(
                        name: "FK_ReservationNight_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationNight_Reservation_OrganizationId_ReservationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.ReservationId, x.PropertyId },
                        principalSchema: "booking",
                        principalTable: "Reservation",
                        principalColumns: new[] { "OrganizationId", "Id", "PropertyId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReservationPayment",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Receiver = table.Column<short>(type: "smallint", nullable: false),
                    Method = table.Column<short>(type: "smallint", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationPayment", x => x.Id);
                    table.UniqueConstraint("AK_ReservationPayment_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.UniqueConstraint("AK_ReservationPayment_OrganizationId_Id_ReservationId", x => new { x.OrganizationId, x.Id, x.ReservationId });
                    table.CheckConstraint("CK_ReservationPayment_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_ReservationPayment_Method", "[Method] BETWEEN 0 AND 3");
                    table.CheckConstraint("CK_ReservationPayment_Receiver", "[Receiver] BETWEEN 0 AND 4");
                    table.ForeignKey(
                        name: "FK_ReservationPayment_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationPayment_Reservation_OrganizationId_ReservationId",
                        columns: x => new { x.OrganizationId, x.ReservationId },
                        principalSchema: "booking",
                        principalTable: "Reservation",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReservationStatusHistory",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldStatus = table.Column<short>(type: "smallint", nullable: false),
                    NewStatus = table.Column<short>(type: "smallint", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationStatusHistory", x => x.Id);
                    table.UniqueConstraint("AK_ReservationStatusHistory_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_ReservationStatusHistory_NewStatus", "[NewStatus] BETWEEN 0 AND 5");
                    table.CheckConstraint("CK_ReservationStatusHistory_OldStatus", "[OldStatus] BETWEEN 0 AND 5");
                    table.ForeignKey(
                        name: "FK_ReservationStatusHistory_ApplicationUser_ActorId",
                        column: x => x.ActorId,
                        principalSchema: "identity",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationStatusHistory_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationStatusHistory_Reservation_OrganizationId_ReservationId",
                        columns: x => new { x.OrganizationId, x.ReservationId },
                        principalSchema: "booking",
                        principalTable: "Reservation",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseAttachment",
                schema: "expense",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpenseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseAttachment", x => x.Id);
                    table.UniqueConstraint("AK_ExpenseAttachment_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_ExpenseAttachment_Expense_OrganizationId_ExpenseId",
                        columns: x => new { x.OrganizationId, x.ExpenseId },
                        principalSchema: "expense",
                        principalTable: "Expense",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseAttachment_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseAttachment_StoredFile_OrganizationId_FileId",
                        columns: x => new { x.OrganizationId, x.FileId },
                        principalSchema: "document",
                        principalTable: "StoredFile",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpensePayment",
                schema: "expense",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpenseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Payer = table.Column<short>(type: "smallint", nullable: false),
                    Method = table.Column<short>(type: "smallint", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpensePayment", x => x.Id);
                    table.UniqueConstraint("AK_ExpensePayment_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_ExpensePayment_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_ExpensePayment_Method", "[Method] BETWEEN 0 AND 3");
                    table.CheckConstraint("CK_ExpensePayment_Payer", "[Payer] BETWEEN 0 AND 2");
                    table.ForeignKey(
                        name: "FK_ExpensePayment_Expense_OrganizationId_ExpenseId",
                        columns: x => new { x.OrganizationId, x.ExpenseId },
                        principalSchema: "expense",
                        principalTable: "Expense",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpensePayment_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CleaningChecklistItem",
                schema: "operation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CleaningTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsChecked = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CleaningChecklistItem", x => x.Id);
                    table.UniqueConstraint("AK_CleaningChecklistItem_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_CleaningChecklistItem_CleaningTask_OrganizationId_CleaningTaskId",
                        columns: x => new { x.OrganizationId, x.CleaningTaskId },
                        principalSchema: "operation",
                        principalTable: "CleaningTask",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CleaningChecklistItem_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkTask",
                schema: "operation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: false),
                    CleaningTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MaintenanceTicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    DueAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTask", x => x.Id);
                    table.UniqueConstraint("AK_WorkTask_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_WorkTask_Source", "([Type] = 0 AND [CleaningTaskId] IS NULL AND [MaintenanceTicketId] IS NULL) OR ([Type] = 1 AND [CleaningTaskId] IS NOT NULL AND [MaintenanceTicketId] IS NULL) OR ([Type] = 2 AND [CleaningTaskId] IS NULL AND [MaintenanceTicketId] IS NOT NULL)");
                    table.CheckConstraint("CK_WorkTask_Status", "[Status] BETWEEN 0 AND 3");
                    table.CheckConstraint("CK_WorkTask_Type", "[Type] BETWEEN 0 AND 2");
                    table.ForeignKey(
                        name: "FK_WorkTask_CleaningTask_OrganizationId_CleaningTaskId_PropertyId",
                        columns: x => new { x.OrganizationId, x.CleaningTaskId, x.PropertyId },
                        principalSchema: "operation",
                        principalTable: "CleaningTask",
                        principalColumns: new[] { "OrganizationId", "Id", "PropertyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkTask_ExternalContact_OrganizationId_ContactId",
                        columns: x => new { x.OrganizationId, x.ContactId },
                        principalSchema: "operation",
                        principalTable: "ExternalContact",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkTask_MaintenanceTicket_OrganizationId_MaintenanceTicketId_PropertyId",
                        columns: x => new { x.OrganizationId, x.MaintenanceTicketId, x.PropertyId },
                        principalSchema: "operation",
                        principalTable: "MaintenanceTicket",
                        principalColumns: new[] { "OrganizationId", "Id", "PropertyId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkTask_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkTask_Property_OrganizationId_PropertyId",
                        columns: x => new { x.OrganizationId, x.PropertyId },
                        principalSchema: "property",
                        principalTable: "Property",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReservationRefund",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Payer = table.Column<short>(type: "smallint", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationRefund", x => x.Id);
                    table.UniqueConstraint("AK_ReservationRefund_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_ReservationRefund_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_ReservationRefund_Payer", "[Payer] BETWEEN 0 AND 4");
                    table.ForeignKey(
                        name: "FK_ReservationRefund_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationRefund_ReservationPayment_OrganizationId_PaymentId_ReservationId",
                        columns: x => new { x.OrganizationId, x.PaymentId, x.ReservationId },
                        principalSchema: "booking",
                        principalTable: "ReservationPayment",
                        principalColumns: new[] { "OrganizationId", "Id", "ReservationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservationRefund_Reservation_OrganizationId_ReservationId",
                        columns: x => new { x.OrganizationId, x.ReservationId },
                        principalSchema: "booking",
                        principalTable: "Reservation",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseReimbursement",
                schema: "expense",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpensePaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Beneficiary = table.Column<short>(type: "smallint", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseReimbursement", x => x.Id);
                    table.UniqueConstraint("AK_ExpenseReimbursement_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.CheckConstraint("CK_ExpenseReimbursement_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_ExpenseReimbursement_Beneficiary", "[Beneficiary] BETWEEN 0 AND 4");
                    table.ForeignKey(
                        name: "FK_ExpenseReimbursement_ExpensePayment_OrganizationId_ExpensePaymentId",
                        columns: x => new { x.OrganizationId, x.ExpensePaymentId },
                        principalSchema: "expense",
                        principalTable: "ExpensePayment",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseReimbursement_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperationAttachment",
                schema: "operation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationAttachment", x => x.Id);
                    table.UniqueConstraint("AK_OperationAttachment_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_OperationAttachment_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationAttachment_StoredFile_OrganizationId_FileId",
                        columns: x => new { x.OrganizationId, x.FileId },
                        principalSchema: "document",
                        principalTable: "StoredFile",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationAttachment_WorkTask_OrganizationId_WorkTaskId",
                        columns: x => new { x.OrganizationId, x.WorkTaskId },
                        principalSchema: "operation",
                        principalTable: "WorkTask",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperationExpenseLink",
                schema: "operation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpenseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationExpenseLink", x => x.Id);
                    table.UniqueConstraint("AK_OperationExpenseLink_OrganizationId_Id", x => new { x.OrganizationId, x.Id });
                    table.ForeignKey(
                        name: "FK_OperationExpenseLink_Expense_OrganizationId_ExpenseId",
                        columns: x => new { x.OrganizationId, x.ExpenseId },
                        principalSchema: "expense",
                        principalTable: "Expense",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationExpenseLink_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "organization",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationExpenseLink_WorkTask_OrganizationId_WorkTaskId",
                        columns: x => new { x.OrganizationId, x.WorkTaskId },
                        principalSchema: "operation",
                        principalTable: "WorkTask",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "ApplicationRole",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "SuperAdmin", "SuperAdmin", "SUPERADMIN" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "Manager", "Manager", "MANAGER" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "Owner", "Owner", "OWNER" }
                });

            migrationBuilder.InsertData(
                schema: "property",
                table: "PropertyType",
                columns: new[] { "Id", "Code" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), "Studio" },
                    { new Guid("20000000-0000-0000-0000-000000000002"), "Apartment" },
                    { new Guid("20000000-0000-0000-0000-000000000003"), "Villa" },
                    { new Guid("20000000-0000-0000-0000-000000000004"), "House" },
                    { new Guid("20000000-0000-0000-0000-000000000005"), "Riad" },
                    { new Guid("20000000-0000-0000-0000-000000000006"), "Room" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Amenity_Code",
                schema: "property",
                table: "Amenity",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "identity",
                table: "ApplicationRole",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "identity",
                table: "ApplicationUser",
                column: "NormalizedEmail",
                unique: true,
                filter: "[NormalizedEmail] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "identity",
                table: "ApplicationUser",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_ActorId",
                schema: "system",
                table: "AuditLog",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_OrganizationId_OccurredAt",
                schema: "system",
                table: "AuditLog",
                columns: new[] { "OrganizationId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobExecution_OrganizationId_JobKey",
                schema: "system",
                table: "BackgroundJobExecution",
                columns: new[] { "OrganizationId", "JobKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobExecution_OrganizationId_Status_NextAttemptAt",
                schema: "system",
                table: "BackgroundJobExecution",
                columns: new[] { "OrganizationId", "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarBlock_OrganizationId_PropertyId_StartDate_EndDate",
                schema: "booking",
                table: "CalendarBlock",
                columns: new[] { "OrganizationId", "PropertyId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistTemplate_OrganizationId_PropertyId",
                schema: "operation",
                table: "ChecklistTemplate",
                columns: new[] { "OrganizationId", "PropertyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistTemplateItem_OrganizationId_TemplateId_SortOrder",
                schema: "operation",
                table: "ChecklistTemplateItem",
                columns: new[] { "OrganizationId", "TemplateId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CleaningChecklistItem_OrganizationId_CleaningTaskId_SortOrder",
                schema: "operation",
                table: "CleaningChecklistItem",
                columns: new[] { "OrganizationId", "CleaningTaskId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CleaningTask_OrganizationId_ContactId",
                schema: "operation",
                table: "CleaningTask",
                columns: new[] { "OrganizationId", "ContactId" });

            migrationBuilder.CreateIndex(
                name: "IX_CleaningTask_OrganizationId_PropertyId_ScheduledAt",
                schema: "operation",
                table: "CleaningTask",
                columns: new[] { "OrganizationId", "PropertyId", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CleaningTask_OrganizationId_ReservationId",
                schema: "operation",
                table: "CleaningTask",
                columns: new[] { "OrganizationId", "ReservationId" },
                unique: true,
                filter: "[IsAutomatic] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CleaningTask_OrganizationId_ReservationId_PropertyId",
                schema: "operation",
                table: "CleaningTask",
                columns: new[] { "OrganizationId", "ReservationId", "PropertyId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionAdjustment_ActorId",
                schema: "finance",
                table: "CommissionAdjustment",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionAdjustment_OrganizationId_PropertyId_TargetPeriod",
                schema: "finance",
                table: "CommissionAdjustment",
                columns: new[] { "OrganizationId", "PropertyId", "TargetPeriod" });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRateHistory_OrganizationId_PropertyId_EffectiveFrom",
                schema: "property",
                table: "CommissionRateHistory",
                columns: new[] { "OrganizationId", "PropertyId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DashboardPreference_OrganizationId_UserId",
                schema: "reporting",
                table: "DashboardPreference",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailOutbox_OrganizationId_Status_NextAttemptAt",
                schema: "communication",
                table: "EmailOutbox",
                columns: new[] { "OrganizationId", "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Expense_ActorId",
                schema: "expense",
                table: "Expense",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_Expense_OrganizationId_CategoryId",
                schema: "expense",
                table: "Expense",
                columns: new[] { "OrganizationId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Expense_OrganizationId_PropertyId_ExpenseDate",
                schema: "expense",
                table: "Expense",
                columns: new[] { "OrganizationId", "PropertyId", "ExpenseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Expense_OrganizationId_RecurringTemplateId_OccurrenceDate",
                schema: "expense",
                table: "Expense",
                columns: new[] { "OrganizationId", "RecurringTemplateId", "OccurrenceDate" },
                unique: true,
                filter: "[RecurringTemplateId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Expense_OrganizationId_RecurringTemplateId_PropertyId",
                schema: "expense",
                table: "Expense",
                columns: new[] { "OrganizationId", "RecurringTemplateId", "PropertyId" });

            migrationBuilder.CreateIndex(
                name: "IX_Expense_OrganizationId_Reference",
                schema: "expense",
                table: "Expense",
                columns: new[] { "OrganizationId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expense_OrganizationId_Status_DueDate",
                schema: "expense",
                table: "Expense",
                columns: new[] { "OrganizationId", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseAttachment_OrganizationId_ExpenseId_FileId",
                schema: "expense",
                table: "ExpenseAttachment",
                columns: new[] { "OrganizationId", "ExpenseId", "FileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseAttachment_OrganizationId_FileId",
                schema: "expense",
                table: "ExpenseAttachment",
                columns: new[] { "OrganizationId", "FileId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategory_OrganizationId_Name",
                schema: "expense",
                table: "ExpenseCategory",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpensePayment_OrganizationId_ExpenseId",
                schema: "expense",
                table: "ExpensePayment",
                columns: new[] { "OrganizationId", "ExpenseId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReimbursement_OrganizationId_ExpensePaymentId",
                schema: "expense",
                table: "ExpenseReimbursement",
                columns: new[] { "OrganizationId", "ExpensePaymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseReimbursement_OrganizationId_IdempotencyKey",
                schema: "expense",
                table: "ExpenseReimbursement",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAdjustment_ActorId",
                schema: "finance",
                table: "FinancialAdjustment",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAdjustment_OrganizationId_PropertyId_TargetPeriod",
                schema: "finance",
                table: "FinancialAdjustment",
                columns: new[] { "OrganizationId", "PropertyId", "TargetPeriod" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransaction_OrganizationId_IdempotencyKey",
                schema: "finance",
                table: "FinancialTransaction",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransaction_OrganizationId_Type_SourceType_SourceId",
                schema: "finance",
                table: "FinancialTransaction",
                columns: new[] { "OrganizationId", "Type", "SourceType", "SourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTicket_OrganizationId_ContactId",
                schema: "operation",
                table: "MaintenanceTicket",
                columns: new[] { "OrganizationId", "ContactId" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTicket_OrganizationId_PropertyId_Status",
                schema: "operation",
                table: "MaintenanceTicket",
                columns: new[] { "OrganizationId", "PropertyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagementContract_OrganizationId_FileId",
                schema: "property",
                table: "ManagementContract",
                columns: new[] { "OrganizationId", "FileId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagementContract_OrganizationId_OwnerId",
                schema: "property",
                table: "ManagementContract",
                columns: new[] { "OrganizationId", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagementContract_OrganizationId_PropertyId_StartDate",
                schema: "property",
                table: "ManagementContract",
                columns: new[] { "OrganizationId", "PropertyId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyClosure_ClosedById",
                schema: "finance",
                table: "MonthlyClosure",
                column: "ClosedById");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyClosure_OrganizationId_OwnerId",
                schema: "finance",
                table: "MonthlyClosure",
                columns: new[] { "OrganizationId", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyClosure_OrganizationId_PropertyId_Year_Month",
                schema: "finance",
                table: "MonthlyClosure",
                columns: new[] { "OrganizationId", "PropertyId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyClosureLine_OrganizationId_ClosureId",
                schema: "finance",
                table: "MonthlyClosureLine",
                columns: new[] { "OrganizationId", "ClosureId" });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyClosureLine_OrganizationId_SourceType_SourceId_PeriodStart",
                schema: "finance",
                table: "MonthlyClosureLine",
                columns: new[] { "OrganizationId", "SourceType", "SourceId", "PeriodStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notification_OrganizationId_UserId_ReadAt_CreatedAt",
                schema: "communication",
                table: "Notification",
                columns: new[] { "OrganizationId", "UserId", "ReadAt", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreference_OrganizationId_UserId",
                schema: "communication",
                table: "NotificationPreference",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperationAttachment_OrganizationId_FileId",
                schema: "operation",
                table: "OperationAttachment",
                columns: new[] { "OrganizationId", "FileId" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationAttachment_OrganizationId_WorkTaskId_FileId",
                schema: "operation",
                table: "OperationAttachment",
                columns: new[] { "OrganizationId", "WorkTaskId", "FileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperationExpenseLink_OrganizationId_ExpenseId",
                schema: "operation",
                table: "OperationExpenseLink",
                columns: new[] { "OrganizationId", "ExpenseId" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationExpenseLink_OrganizationId_WorkTaskId_ExpenseId",
                schema: "operation",
                table: "OperationExpenseLink",
                columns: new[] { "OrganizationId", "WorkTaskId", "ExpenseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organization_Reference",
                schema: "organization",
                table: "Organization",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMember_OrganizationId_UserId",
                schema: "organization",
                table: "OrganizationMember",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMember_UserId",
                schema: "organization",
                table: "OrganizationMember",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSettings_OrganizationId",
                schema: "organization",
                table: "OrganizationSettings",
                column: "OrganizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSettings_OrganizationId_LogoFileId",
                schema: "organization",
                table: "OrganizationSettings",
                columns: new[] { "OrganizationId", "LogoFileId" });

            migrationBuilder.CreateIndex(
                name: "IX_Owner_OrganizationId_Reference",
                schema: "organization",
                table: "Owner",
                columns: new[] { "OrganizationId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Owner_OrganizationId_UserId",
                schema: "organization",
                table: "Owner",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OwnerPayout_OrganizationId_IdempotencyKey",
                schema: "finance",
                table: "OwnerPayout",
                columns: new[] { "OrganizationId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OwnerPayout_OrganizationId_OwnerId",
                schema: "finance",
                table: "OwnerPayout",
                columns: new[] { "OrganizationId", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_OwnerPayout_OrganizationId_Reference",
                schema: "finance",
                table: "OwnerPayout",
                columns: new[] { "OrganizationId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OwnerPayoutAllocation_OrganizationId_ClosureId_OwnerId",
                schema: "finance",
                table: "OwnerPayoutAllocation",
                columns: new[] { "OrganizationId", "ClosureId", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_OwnerPayoutAllocation_OrganizationId_PayoutId_ClosureId",
                schema: "finance",
                table: "OwnerPayoutAllocation",
                columns: new[] { "OrganizationId", "PayoutId", "ClosureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OwnerPayoutAllocation_OrganizationId_PayoutId_OwnerId",
                schema: "finance",
                table: "OwnerPayoutAllocation",
                columns: new[] { "OrganizationId", "PayoutId", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_OwnerRequest_OrganizationId_OwnerId_Status",
                schema: "communication",
                table: "OwnerRequest",
                columns: new[] { "OrganizationId", "OwnerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OwnerRequest_OrganizationId_PropertyId",
                schema: "communication",
                table: "OwnerRequest",
                columns: new[] { "OrganizationId", "PropertyId" });

            migrationBuilder.CreateIndex(
                name: "IX_OwnerRequestMessage_AuthorId",
                schema: "communication",
                table: "OwnerRequestMessage",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerRequestMessage_OrganizationId_RequestId_CreatedAt",
                schema: "communication",
                table: "OwnerRequestMessage",
                columns: new[] { "OrganizationId", "RequestId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OwnerRequestStatusHistory_ActorId",
                schema: "communication",
                table: "OwnerRequestStatusHistory",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerRequestStatusHistory_OrganizationId_RequestId_ChangedAt",
                schema: "communication",
                table: "OwnerRequestStatusHistory",
                columns: new[] { "OrganizationId", "RequestId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Property_OrganizationId_OwnerId",
                schema: "property",
                table: "Property",
                columns: new[] { "OrganizationId", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Property_OrganizationId_Reference",
                schema: "property",
                table: "Property",
                columns: new[] { "OrganizationId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Property_TypeId",
                schema: "property",
                table: "Property",
                column: "TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyAmenity_AmenityId",
                schema: "property",
                table: "PropertyAmenity",
                column: "AmenityId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyAmenity_OrganizationId_PropertyId_AmenityId",
                schema: "property",
                table: "PropertyAmenity",
                columns: new[] { "OrganizationId", "PropertyId", "AmenityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyDocument_OrganizationId_FileId",
                schema: "document",
                table: "PropertyDocument",
                columns: new[] { "OrganizationId", "FileId" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyDocument_OrganizationId_PropertyId_FileId_Type",
                schema: "document",
                table: "PropertyDocument",
                columns: new[] { "OrganizationId", "PropertyId", "FileId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyInventoryItem_OrganizationId_PropertyId_Name",
                schema: "property",
                table: "PropertyInventoryItem",
                columns: new[] { "OrganizationId", "PropertyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyListing_OrganizationId_PropertyId_Source",
                schema: "property",
                table: "PropertyListing",
                columns: new[] { "OrganizationId", "PropertyId", "Source" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyPhoto_OrganizationId_FileId",
                schema: "property",
                table: "PropertyPhoto",
                columns: new[] { "OrganizationId", "FileId" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyPhoto_OrganizationId_PropertyId_FileId",
                schema: "property",
                table: "PropertyPhoto",
                columns: new[] { "OrganizationId", "PropertyId", "FileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropertyPhoto_OrganizationId_PropertyId_SortOrder",
                schema: "property",
                table: "PropertyPhoto",
                columns: new[] { "OrganizationId", "PropertyId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyType_Code",
                schema: "property",
                table: "PropertyType",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpenseTemplate_OrganizationId_CategoryId",
                schema: "expense",
                table: "RecurringExpenseTemplate",
                columns: new[] { "OrganizationId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpenseTemplate_OrganizationId_IsActive_NextDueDate",
                schema: "expense",
                table: "RecurringExpenseTemplate",
                columns: new[] { "OrganizationId", "IsActive", "NextDueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringExpenseTemplate_OrganizationId_PropertyId",
                schema: "expense",
                table: "RecurringExpenseTemplate",
                columns: new[] { "OrganizationId", "PropertyId" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_FamilyId",
                schema: "identity",
                table: "RefreshToken",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_ReplacedById",
                schema: "identity",
                table: "RefreshToken",
                column: "ReplacedById");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_TokenHash",
                schema: "identity",
                table: "RefreshToken",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_UserId_ExpiresAt",
                schema: "identity",
                table: "RefreshToken",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Report_OrganizationId_FileId",
                schema: "reporting",
                table: "Report",
                columns: new[] { "OrganizationId", "FileId" });

            migrationBuilder.CreateIndex(
                name: "IX_Report_OrganizationId_OwnerId",
                schema: "reporting",
                table: "Report",
                columns: new[] { "OrganizationId", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Report_OrganizationId_PropertyId",
                schema: "reporting",
                table: "Report",
                columns: new[] { "OrganizationId", "PropertyId" });

            migrationBuilder.CreateIndex(
                name: "IX_Report_OrganizationId_UserId_CreatedAt",
                schema: "reporting",
                table: "Report",
                columns: new[] { "OrganizationId", "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_OrganizationId_GuestId",
                schema: "booking",
                table: "Reservation",
                columns: new[] { "OrganizationId", "GuestId" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_OrganizationId_PropertyId_CheckInDate_CheckOutDate",
                schema: "booking",
                table: "Reservation",
                columns: new[] { "OrganizationId", "PropertyId", "CheckInDate", "CheckOutDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_OrganizationId_PropertyId_Source_ExternalBookingId",
                schema: "booking",
                table: "Reservation",
                columns: new[] { "OrganizationId", "PropertyId", "Source", "ExternalBookingId" },
                unique: true,
                filter: "[ExternalBookingId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_OrganizationId_Reference",
                schema: "booking",
                table: "Reservation",
                columns: new[] { "OrganizationId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservationCancellation_OrganizationId_ReservationId",
                schema: "booking",
                table: "ReservationCancellation",
                columns: new[] { "OrganizationId", "ReservationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservationNight_OrganizationId_PropertyId_NightDate",
                schema: "booking",
                table: "ReservationNight",
                columns: new[] { "OrganizationId", "PropertyId", "NightDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationNight_OrganizationId_ReservationId_NightDate",
                schema: "booking",
                table: "ReservationNight",
                columns: new[] { "OrganizationId", "ReservationId", "NightDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservationNight_OrganizationId_ReservationId_PropertyId",
                schema: "booking",
                table: "ReservationNight",
                columns: new[] { "OrganizationId", "ReservationId", "PropertyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationPayment_OrganizationId_ReservationId",
                schema: "booking",
                table: "ReservationPayment",
                columns: new[] { "OrganizationId", "ReservationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationRefund_OrganizationId_PaymentId_ReservationId",
                schema: "booking",
                table: "ReservationRefund",
                columns: new[] { "OrganizationId", "PaymentId", "ReservationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationRefund_OrganizationId_ReservationId",
                schema: "booking",
                table: "ReservationRefund",
                columns: new[] { "OrganizationId", "ReservationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationStatusHistory_ActorId",
                schema: "booking",
                table: "ReservationStatusHistory",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationStatusHistory_OrganizationId_ReservationId",
                schema: "booking",
                table: "ReservationStatusHistory",
                columns: new[] { "OrganizationId", "ReservationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaim_RoleId",
                schema: "identity",
                table: "RoleClaim",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonalRate_OrganizationId_PropertyId_StartDate_EndDate",
                schema: "property",
                table: "SeasonalRate",
                columns: new[] { "OrganizationId", "PropertyId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredFile_OrganizationId_StorageKey",
                schema: "document",
                table: "StoredFile",
                columns: new[] { "OrganizationId", "StorageKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredFile_UploaderId",
                schema: "document",
                table: "StoredFile",
                column: "UploaderId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClaim_UserId",
                schema: "identity",
                table: "UserClaim",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogin_UserId",
                schema: "identity",
                table: "UserLogin",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId",
                schema: "identity",
                table: "UserPreferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_RoleId",
                schema: "identity",
                table: "UserRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTask_OrganizationId_CleaningTaskId_PropertyId",
                schema: "operation",
                table: "WorkTask",
                columns: new[] { "OrganizationId", "CleaningTaskId", "PropertyId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkTask_OrganizationId_ContactId",
                schema: "operation",
                table: "WorkTask",
                columns: new[] { "OrganizationId", "ContactId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkTask_OrganizationId_MaintenanceTicketId_PropertyId",
                schema: "operation",
                table: "WorkTask",
                columns: new[] { "OrganizationId", "MaintenanceTicketId", "PropertyId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkTask_OrganizationId_PropertyId_Status_DueAt",
                schema: "operation",
                table: "WorkTask",
                columns: new[] { "OrganizationId", "PropertyId", "Status", "DueAt" });

            migrationBuilder.Sql("""
                EXEC(N'
                    CREATE TRIGGER [finance].[ProtectClosedMonthlyClosure]
                    ON [finance].[MonthlyClosure]
                    AFTER UPDATE, DELETE
                    AS
                    BEGIN
                        SET NOCOUNT ON;
                        IF EXISTS (SELECT 1 FROM deleted WHERE [Status] = 1)
                            THROW 51001, ''Closed monthly closures are immutable.'', 1;
                    END
                ');
                """);

            migrationBuilder.Sql("""
                EXEC(N'
                    CREATE TRIGGER [finance].[ProtectClosedMonthlyClosureLines]
                    ON [finance].[MonthlyClosureLine]
                    AFTER INSERT, UPDATE, DELETE
                    AS
                    BEGIN
                        SET NOCOUNT ON;
                        IF EXISTS (
                            SELECT 1
                            FROM [finance].[MonthlyClosure] closure WITH (UPDLOCK, HOLDLOCK)
                            JOIN (
                                SELECT OrganizationId, ClosureId FROM inserted
                                UNION
                                SELECT OrganizationId, ClosureId FROM deleted
                            ) changed ON changed.OrganizationId = closure.OrganizationId
                                      AND changed.ClosureId = closure.Id
                            WHERE closure.[Status] = 1
                        )
                            THROW 51002, ''Lines in closed monthly closures are immutable.'', 1;
                    END
                ');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLog",
                schema: "system");

            migrationBuilder.DropTable(
                name: "BackgroundJobExecution",
                schema: "system");

            migrationBuilder.DropTable(
                name: "CalendarBlock",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "ChecklistTemplateItem",
                schema: "operation");

            migrationBuilder.DropTable(
                name: "CleaningChecklistItem",
                schema: "operation");

            migrationBuilder.DropTable(
                name: "CommissionAdjustment",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "CommissionRateHistory",
                schema: "property");

            migrationBuilder.DropTable(
                name: "DashboardPreference",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "EmailOutbox",
                schema: "communication");

            migrationBuilder.DropTable(
                name: "ExpenseAttachment",
                schema: "expense");

            migrationBuilder.DropTable(
                name: "ExpenseReimbursement",
                schema: "expense");

            migrationBuilder.DropTable(
                name: "FinancialAdjustment",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "FinancialTransaction",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "ManagementContract",
                schema: "property");

            migrationBuilder.DropTable(
                name: "MonthlyClosureLine",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "Notification",
                schema: "communication");

            migrationBuilder.DropTable(
                name: "NotificationPreference",
                schema: "communication");

            migrationBuilder.DropTable(
                name: "OperationAttachment",
                schema: "operation");

            migrationBuilder.DropTable(
                name: "OperationExpenseLink",
                schema: "operation");

            migrationBuilder.DropTable(
                name: "OrganizationSettings",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "OwnerPayoutAllocation",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "OwnerRequestMessage",
                schema: "communication");

            migrationBuilder.DropTable(
                name: "OwnerRequestStatusHistory",
                schema: "communication");

            migrationBuilder.DropTable(
                name: "PropertyAmenity",
                schema: "property");

            migrationBuilder.DropTable(
                name: "PropertyDocument",
                schema: "document");

            migrationBuilder.DropTable(
                name: "PropertyInventoryItem",
                schema: "property");

            migrationBuilder.DropTable(
                name: "PropertyListing",
                schema: "property");

            migrationBuilder.DropTable(
                name: "PropertyPhoto",
                schema: "property");

            migrationBuilder.DropTable(
                name: "RefreshToken",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Report",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "ReservationCancellation",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "ReservationNight",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "ReservationRefund",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "ReservationStatusHistory",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "RoleClaim",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "SeasonalRate",
                schema: "property");

            migrationBuilder.DropTable(
                name: "UserClaim",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserLogin",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserPreferences",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserRole",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserToken",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "ChecklistTemplate",
                schema: "operation");

            migrationBuilder.DropTable(
                name: "ExpensePayment",
                schema: "expense");

            migrationBuilder.DropTable(
                name: "WorkTask",
                schema: "operation");

            migrationBuilder.DropTable(
                name: "MonthlyClosure",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "OwnerPayout",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "OwnerRequest",
                schema: "communication");

            migrationBuilder.DropTable(
                name: "Amenity",
                schema: "property");

            migrationBuilder.DropTable(
                name: "StoredFile",
                schema: "document");

            migrationBuilder.DropTable(
                name: "ReservationPayment",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "ApplicationRole",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Expense",
                schema: "expense");

            migrationBuilder.DropTable(
                name: "CleaningTask",
                schema: "operation");

            migrationBuilder.DropTable(
                name: "MaintenanceTicket",
                schema: "operation");

            migrationBuilder.DropTable(
                name: "RecurringExpenseTemplate",
                schema: "expense");

            migrationBuilder.DropTable(
                name: "Reservation",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "ExternalContact",
                schema: "operation");

            migrationBuilder.DropTable(
                name: "ExpenseCategory",
                schema: "expense");

            migrationBuilder.DropTable(
                name: "Guest",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "Property",
                schema: "property");

            migrationBuilder.DropTable(
                name: "Owner",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "PropertyType",
                schema: "property");

            migrationBuilder.DropTable(
                name: "OrganizationMember",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "ApplicationUser",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Organization",
                schema: "organization");
        }
    }
}
