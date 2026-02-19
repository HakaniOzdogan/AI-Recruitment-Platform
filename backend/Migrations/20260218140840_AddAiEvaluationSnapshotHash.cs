using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IkOtomasyon.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAiEvaluationSnapshotHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InputSnapshotHash",
                table: "AiEvaluationReports",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AiEvaluationReports_JobId_CandidateId_InputSnapshotHash",
                table: "AiEvaluationReports",
                columns: new[] { "JobId", "CandidateId", "InputSnapshotHash" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AiEvaluationReports_JobId_CandidateId_InputSnapshotHash",
                table: "AiEvaluationReports");

            migrationBuilder.DropColumn(
                name: "InputSnapshotHash",
                table: "AiEvaluationReports");
        }
    }
}
