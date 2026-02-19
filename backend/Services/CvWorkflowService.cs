using System.Text.Json;
using IkOtomasyon.Api.Contracts;
using IkOtomasyon.Api.Data;
using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace IkOtomasyon.Api.Services;

public class CvWorkflowService
{
    private readonly AppDbContext _db;
    private readonly CvStorageService _storage;
    private readonly CvParserService _parser;

    public CvWorkflowService(AppDbContext db, CvStorageService storage, CvParserService parser)
    {
        _db = db;
        _storage = storage;
        _parser = parser;
    }

    public async Task<CvDocument> UploadCvAsync(Guid candidateId, Guid userId, IFormFile file, CancellationToken ct = default)
    {
        var candidate = await _db.Candidates.FirstOrDefaultAsync(x => x.Id == candidateId, ct)
            ?? throw new NotFoundException("Candidate not found.");

        if (file.Length <= 0)
        {
            throw new InvalidOperationException("File is empty.");
        }

        if (file.Length > _storage.MaxFileSizeBytes)
        {
            throw new RequestEntityTooLargeException("File size exceeds 10MB limit.");
        }

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || !_storage.IsAllowedExtension(extension))
        {
            throw new InvalidOperationException("Only pdf, doc and docx files are allowed.");
        }

        var stored = await _storage.SaveAsync(file, ct);

        var document = new CvDocument
        {
            Id = Guid.NewGuid(),
            CandidateId = candidate.Id,
            OriginalFileName = stored.OriginalFileName,
            StoredFileName = stored.StoredFileName,
            FileType = stored.FileType,
            FileSize = stored.FileSize,
            StoragePath = stored.StoragePath,
            UploadedAt = DateTime.UtcNow,
            UploadedByUserId = userId,
            ParseStatus = CvParseStatus.Pending
        };

        _db.CvDocuments.Add(document);
        await _db.SaveChangesAsync(ct);

        return document;
    }

    public async Task<ParseCvResponse> ParseCvAsync(Guid cvDocumentId, bool force, CancellationToken ct = default)
    {
        var doc = await _db.CvDocuments
            .Include(x => x.Candidate)
            .FirstOrDefaultAsync(x => x.Id == cvDocumentId, ct)
            ?? throw new NotFoundException("CV document not found.");

        if (doc.ParseStatus == CvParseStatus.Parsed && !force)
        {
            var existingProfile = await _db.CandidateProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.CandidateId == doc.CandidateId, ct);
            return BuildParseResponse(doc, existingProfile);
        }

        if (doc.ParseStatus == CvParseStatus.Failed && !force)
        {
            var failedProfile = await _db.CandidateProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.CandidateId == doc.CandidateId, ct);
            return BuildParseResponse(doc, failedProfile);
        }

        try
        {
            var parsed = await _parser.ParseAsync(doc.StoragePath, doc.FileType, doc.Candidate?.FullName ?? string.Empty, doc.Candidate?.Email, ct);

            var profile = await _db.CandidateProfiles.FirstOrDefaultAsync(x => x.CandidateId == doc.CandidateId, ct);
            if (profile is null)
            {
                profile = new CandidateProfile { CandidateId = doc.CandidateId };
                _db.CandidateProfiles.Add(profile);
            }

            profile.FullNameSnapshot = parsed.FullNameSnapshot ?? doc.Candidate?.FullName ?? string.Empty;
            profile.EmailSnapshot = parsed.EmailSnapshot;
            profile.Summary = parsed.Summary;
            profile.TotalExperienceMonths = parsed.TotalExperienceMonths;
            profile.SkillsJson = JsonSerializer.Serialize(parsed.Skills);
            profile.EducationJson = JsonSerializer.Serialize(parsed.Education);
            profile.ExperienceJson = JsonSerializer.Serialize(parsed.Experience);
            profile.LanguagesJson = JsonSerializer.Serialize(parsed.Languages);
            profile.LinksJson = JsonSerializer.Serialize(parsed.Links);
            profile.UpdatedAt = DateTime.UtcNow;

            doc.ParseStatus = CvParseStatus.Parsed;
            doc.ParseError = null;
            doc.ParsedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return BuildParseResponse(doc, profile);
        }
        catch (Exception ex)
        {
            doc.ParseStatus = CvParseStatus.Failed;
            doc.ParseError = SanitizeError(ex.Message);
            doc.ParsedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            var failedProfile = await _db.CandidateProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.CandidateId == doc.CandidateId, ct);
            return BuildParseResponse(doc, failedProfile);
        }
    }

    private static ParseCvResponse BuildParseResponse(CvDocument doc, CandidateProfile? profile)
    {
        return new ParseCvResponse(
            doc.Id,
            doc.CandidateId,
            doc.ParseStatus,
            doc.ParseError,
            doc.ParsedAt,
            profile is null ? null : new CandidateProfileResponse(
                profile.CandidateId,
                profile.FullNameSnapshot,
                profile.EmailSnapshot,
                profile.Summary,
                profile.TotalExperienceMonths,
                profile.SkillsJson,
                profile.EducationJson,
                profile.ExperienceJson,
                profile.LanguagesJson,
                profile.LinksJson,
                profile.UpdatedAt));
    }

    private static string SanitizeError(string message)
    {
        var clean = message.Replace("\r", " ").Replace("\n", " ").Trim();
        return clean.Length <= 300 ? clean : clean[..300];
    }
}
