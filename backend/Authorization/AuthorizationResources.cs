namespace IkOtomasyon.Api.Authorization;

public interface IAuthorizationResource;

public readonly record struct JobAuthorizationResource(Guid JobId) : IAuthorizationResource;
public readonly record struct CandidateAuthorizationResource(Guid CandidateId) : IAuthorizationResource;
public readonly record struct ApplicationAuthorizationResource(Guid ApplicationId) : IAuthorizationResource;
public readonly record struct ApplyAuthorizationResource(Guid JobId, Guid CandidateId) : IAuthorizationResource;
public readonly record struct InterviewAuthorizationResource(Guid SessionId) : IAuthorizationResource;
public readonly record struct ScorecardAuthorizationResource(Guid ScorecardId) : IAuthorizationResource;
public readonly record struct AiEvaluationAuthorizationResource(Guid JobId, Guid CandidateId) : IAuthorizationResource;
public readonly record struct CvDocumentAuthorizationResource(Guid CvDocumentId) : IAuthorizationResource;
