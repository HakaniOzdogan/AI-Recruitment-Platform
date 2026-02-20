using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IkOtomasyon.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceOwnershipAndTenantScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedManagerUserId",
                table: "JobPostings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "JobPostings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicantUserId",
                table: "InterviewSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InterviewerUserId",
                table: "InterviewSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "InterviewSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "InterviewScorecards",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "CvDocuments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Candidates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "Candidates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Candidates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Applications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Applications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AiEvaluationReports",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_AssignedManagerUserId",
                table: "JobPostings",
                column: "AssignedManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_TenantId",
                table: "JobPostings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSessions_ApplicantUserId",
                table: "InterviewSessions",
                column: "ApplicantUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSessions_InterviewerUserId",
                table: "InterviewSessions",
                column: "InterviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSessions_TenantId",
                table: "InterviewSessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewScorecards_TenantId",
                table: "InterviewScorecards",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CvDocuments_TenantId",
                table: "CvDocuments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_OwnerUserId",
                table: "Candidates",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_TenantId",
                table: "Candidates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_TenantId",
                table: "Applications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AiEvaluationReports_TenantId",
                table: "AiEvaluationReports",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobPostings_AssignedManagerUserId",
                table: "JobPostings");

            migrationBuilder.DropIndex(
                name: "IX_JobPostings_TenantId",
                table: "JobPostings");

            migrationBuilder.DropIndex(
                name: "IX_InterviewSessions_ApplicantUserId",
                table: "InterviewSessions");

            migrationBuilder.DropIndex(
                name: "IX_InterviewSessions_InterviewerUserId",
                table: "InterviewSessions");

            migrationBuilder.DropIndex(
                name: "IX_InterviewSessions_TenantId",
                table: "InterviewSessions");

            migrationBuilder.DropIndex(
                name: "IX_InterviewScorecards_TenantId",
                table: "InterviewScorecards");

            migrationBuilder.DropIndex(
                name: "IX_CvDocuments_TenantId",
                table: "CvDocuments");

            migrationBuilder.DropIndex(
                name: "IX_Candidates_OwnerUserId",
                table: "Candidates");

            migrationBuilder.DropIndex(
                name: "IX_Candidates_TenantId",
                table: "Candidates");

            migrationBuilder.DropIndex(
                name: "IX_Applications_TenantId",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_AiEvaluationReports_TenantId",
                table: "AiEvaluationReports");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AssignedManagerUserId",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "ApplicantUserId",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "InterviewerUserId",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "InterviewScorecards");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CvDocuments");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AiEvaluationReports");
        }
    }
}
