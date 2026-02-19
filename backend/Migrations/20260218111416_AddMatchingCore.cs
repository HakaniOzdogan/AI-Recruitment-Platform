using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IkOtomasyon.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchingCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinExperienceMonths",
                table: "JobPostings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NiceToHaveSkillsJson",
                table: "JobPostings",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "RequiredSkillsJson",
                table: "JobPostings",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "MatchResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    ReasonsJson = table.Column<string>(type: "text", nullable: false),
                    GapsJson = table.Column<string>(type: "text", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ComputedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchResults_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "Candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchResults_JobPostings_JobId",
                        column: x => x.JobId,
                        principalTable: "JobPostings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchResults_CandidateId",
                table: "MatchResults",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchResults_JobId_CandidateId",
                table: "MatchResults",
                columns: new[] { "JobId", "CandidateId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchResults");

            migrationBuilder.DropColumn(
                name: "MinExperienceMonths",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "NiceToHaveSkillsJson",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "RequiredSkillsJson",
                table: "JobPostings");
        }
    }
}
