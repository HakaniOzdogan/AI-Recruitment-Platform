using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IkOtomasyon.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddJobWeightsAndAiEvaluationEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompetencyWeightsJson",
                table: "JobPostings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompetencyAssessmentJson",
                table: "AiEvaluationReports",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "SkillAssessmentJson",
                table: "AiEvaluationReports",
                type: "text",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.CreateTable(
                name: "JobSkillWeights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillNameNormalized = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobSkillWeights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobSkillWeights_JobPostings_JobId",
                        column: x => x.JobId,
                        principalTable: "JobPostings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobSkillWeights_JobId_SkillNameNormalized",
                table: "JobSkillWeights",
                columns: new[] { "JobId", "SkillNameNormalized" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobSkillWeights");

            migrationBuilder.DropColumn(
                name: "CompetencyWeightsJson",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "CompetencyAssessmentJson",
                table: "AiEvaluationReports");

            migrationBuilder.DropColumn(
                name: "SkillAssessmentJson",
                table: "AiEvaluationReports");
        }
    }
}
