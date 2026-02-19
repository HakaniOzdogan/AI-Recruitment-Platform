using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IkOtomasyon.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAdaptiveInterviewAi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AiLastPlanAt",
                table: "InterviewSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiMode",
                table: "InterviewSessions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "OFF");

            migrationBuilder.AddColumn<string>(
                name: "AiModelName",
                table: "InterviewSessions",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "QuestionBankId",
                table: "InterviewMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InterviewInsights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TurnIndex = table.Column<int>(type: "integer", nullable: false),
                    SignalsJson = table.Column<string>(type: "text", nullable: false),
                    CompetencyJson = table.Column<string>(type: "text", nullable: false),
                    DepthJson = table.Column<string>(type: "text", nullable: false),
                    RiskFlagsJson = table.Column<string>(type: "text", nullable: false),
                    EvidenceSnippetsJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewInsights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewInsights_InterviewSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "InterviewSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InterviewPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromTurnIndex = table.Column<int>(type: "integer", nullable: false),
                    PlannedQuestionsJson = table.Column<string>(type: "text", nullable: false),
                    PlanRationaleJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewPlans_InterviewSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "InterviewSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InterviewQuestionBanks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TopicKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    QuestionText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TagsJson = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewQuestionBanks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewMessages_QuestionBankId",
                table: "InterviewMessages",
                column: "QuestionBankId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewInsights_SessionId_TurnIndex",
                table: "InterviewInsights",
                columns: new[] { "SessionId", "TurnIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewPlans_SessionId_FromTurnIndex",
                table: "InterviewPlans",
                columns: new[] { "SessionId", "FromTurnIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestionBanks_Category_TopicKey_IsActive",
                table: "InterviewQuestionBanks",
                columns: new[] { "Category", "TopicKey", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewMessages_InterviewQuestionBanks_QuestionBankId",
                table: "InterviewMessages",
                column: "QuestionBankId",
                principalTable: "InterviewQuestionBanks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewMessages_InterviewQuestionBanks_QuestionBankId",
                table: "InterviewMessages");

            migrationBuilder.DropTable(
                name: "InterviewInsights");

            migrationBuilder.DropTable(
                name: "InterviewPlans");

            migrationBuilder.DropTable(
                name: "InterviewQuestionBanks");

            migrationBuilder.DropIndex(
                name: "IX_InterviewMessages_QuestionBankId",
                table: "InterviewMessages");

            migrationBuilder.DropColumn(
                name: "AiLastPlanAt",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "AiMode",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "AiModelName",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "QuestionBankId",
                table: "InterviewMessages");
        }
    }
}
