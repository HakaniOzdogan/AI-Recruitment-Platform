using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IkOtomasyon.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRubricAndInterviewScoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InterviewSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewSessions_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RubricTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RubricTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RubricTemplates_JobPostings_JobId",
                        column: x => x.JobId,
                        principalTable: "JobPostings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InterviewMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewMessages_InterviewSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "InterviewSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InterviewScorecards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RubricTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverallScore = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewScorecards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewScorecards_InterviewSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "InterviewSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewScorecards_RubricTemplates_RubricTemplateId",
                        column: x => x.RubricTemplateId,
                        principalTable: "RubricTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RubricCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RubricCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RubricCriteria_RubricTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "RubricTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InterviewCriterionScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScorecardId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    Rationale = table.Column<string>(type: "text", nullable: false),
                    EvidenceQuotesJson = table.Column<string>(type: "text", nullable: false),
                    EvaluatorType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReplacesScoreId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewCriterionScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewCriterionScores_InterviewCriterionScores_ReplacesS~",
                        column: x => x.ReplacesScoreId,
                        principalTable: "InterviewCriterionScores",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InterviewCriterionScores_InterviewScorecards_ScorecardId",
                        column: x => x.ScorecardId,
                        principalTable: "InterviewScorecards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewCriterionScores_ReplacesScoreId",
                table: "InterviewCriterionScores",
                column: "ReplacesScoreId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewCriterionScores_ScorecardId_CriterionKey_CreatedAt",
                table: "InterviewCriterionScores",
                columns: new[] { "ScorecardId", "CriterionKey", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewMessages_SessionId_CreatedAt",
                table: "InterviewMessages",
                columns: new[] { "SessionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewScorecards_RubricTemplateId",
                table: "InterviewScorecards",
                column: "RubricTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewScorecards_SessionId_RubricTemplateId",
                table: "InterviewScorecards",
                columns: new[] { "SessionId", "RubricTemplateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSessions_ApplicationId",
                table: "InterviewSessions",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_RubricCriteria_TemplateId_Key",
                table: "RubricCriteria",
                columns: new[] { "TemplateId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RubricCriteria_TemplateId_Order",
                table: "RubricCriteria",
                columns: new[] { "TemplateId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_RubricTemplates_JobId_IsDefault",
                table: "RubricTemplates",
                columns: new[] { "JobId", "IsDefault" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewCriterionScores");

            migrationBuilder.DropTable(
                name: "InterviewMessages");

            migrationBuilder.DropTable(
                name: "RubricCriteria");

            migrationBuilder.DropTable(
                name: "InterviewScorecards");

            migrationBuilder.DropTable(
                name: "InterviewSessions");

            migrationBuilder.DropTable(
                name: "RubricTemplates");
        }
    }
}
