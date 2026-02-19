using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IkOtomasyon.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAiEvaluationModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiEvaluationReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModelName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InputSnapshotJson = table.Column<string>(type: "text", nullable: false),
                    OverallRecommendation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StrengthsJson = table.Column<string>(type: "text", nullable: false),
                    RisksJson = table.Column<string>(type: "text", nullable: false),
                    VerificationQuestionsJson = table.Column<string>(type: "text", nullable: false),
                    EvidenceQuotesJson = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiEvaluationReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiEvaluationReports_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AiEvaluationReports_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "Candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AiEvaluationReports_JobPostings_JobId",
                        column: x => x.JobId,
                        principalTable: "JobPostings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CandidateConsents",
                columns: table => new
                {
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsentGiven = table.Column<bool>(type: "boolean", nullable: false),
                    ConsentTextVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConsentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataRetentionDays = table.Column<int>(type: "integer", nullable: false),
                    DeleteRequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateConsents", x => x.CandidateId);
                    table.ForeignKey(
                        name: "FK_CandidateConsents_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "Candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiEvaluationReports_ApplicationId",
                table: "AiEvaluationReports",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_AiEvaluationReports_CandidateId",
                table: "AiEvaluationReports",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_AiEvaluationReports_JobId_CandidateId_Version",
                table: "AiEvaluationReports",
                columns: new[] { "JobId", "CandidateId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiEvaluationReports");

            migrationBuilder.DropTable(
                name: "CandidateConsents");
        }
    }
}
