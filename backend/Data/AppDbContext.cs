using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace IkOtomasyon.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<PipelineStage> PipelineStages => Set<PipelineStage>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<CvDocument> CvDocuments => Set<CvDocument>();
    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
    public DbSet<MatchResult> MatchResults => Set<MatchResult>();
    public DbSet<JobSkillWeight> JobSkillWeights => Set<JobSkillWeight>();
    public DbSet<CandidateConsent> CandidateConsents => Set<CandidateConsent>();
    public DbSet<AiEvaluationReport> AiEvaluationReports => Set<AiEvaluationReport>();
    public DbSet<InterviewSession> InterviewSessions => Set<InterviewSession>();
    public DbSet<InterviewMessage> InterviewMessages => Set<InterviewMessage>();
    public DbSet<InterviewInsight> InterviewInsights => Set<InterviewInsight>();
    public DbSet<InterviewPlan> InterviewPlans => Set<InterviewPlan>();
    public DbSet<InterviewQuestionBank> InterviewQuestionBanks => Set<InterviewQuestionBank>();
    public DbSet<RubricTemplate> RubricTemplates => Set<RubricTemplate>();
    public DbSet<RubricCriterion> RubricCriteria => Set<RubricCriterion>();
    public DbSet<InterviewScorecard> InterviewScorecards => Set<InterviewScorecard>();
    public DbSet<InterviewCriterionScore> InterviewCriterionScores => Set<InterviewCriterionScore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
