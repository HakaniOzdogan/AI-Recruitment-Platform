using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IkOtomasyon.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddScoringEnrichmentColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "InterviewCriterionScores",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "SCORED",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<string>(
                name: "EnrichedByModel",
                table: "InterviewCriterionScores",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnrichmentErrorsJson",
                table: "InterviewCriterionScores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnrichmentStatus",
                table: "InterviewCriterionScores",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NONE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnrichedByModel",
                table: "InterviewCriterionScores");

            migrationBuilder.DropColumn(
                name: "EnrichmentErrorsJson",
                table: "InterviewCriterionScores");

            migrationBuilder.DropColumn(
                name: "EnrichmentStatus",
                table: "InterviewCriterionScores");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "InterviewCriterionScores",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldDefaultValue: "SCORED");
        }
    }
}
