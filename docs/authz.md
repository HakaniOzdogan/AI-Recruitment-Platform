# Authorization Model (RBAC + ABAC)

## 1) Role Catalog (roller + sorumluluk)

- `Applicant` (Aday)
  - `Job.Read` ile ilan listesi/detay görür.
  - Sadece kendi `Candidate` kaydını (`ownerUserId == userId`) okuyup günceller.
  - Sadece kendi `Application` kayıtlarını okuyup oluşturur.
  - Sadece kendi `InterviewSession` kayıtlarına erişip mesaj gönderir.
  - Kendi adayına ait CV upload/parse yapar.
- `Recruiter` (İK Uzmanı)
  - Job CRUD/publish/weights/rubric işlemleri.
  - Candidate CRUD + CV upload/parse.
  - Application stage yönetimi.
  - Interview başlatma/okuma/mesaj.
  - Scorecard okuma/auto/override.
  - AI evaluation run/read.
- `HiringManager`
  - Sadece atandığı (`assignedManagerUserId`) veya sahibi olduğu (`createdByUserId`) job’lar üzerinde erişim.
  - Job kapsamındaki application/candidate/scorecard görünürlüğü.
  - Stage update, interview read/create, scorecard read/override (policy bazlı).
- `Interviewer`
  - Sadece kendine atanmış (`interviewerUserId == userId`) session’lar.
  - Interview read/message.
  - Scorecard read (override yok).
- `Admin`
  - Global erişim + kullanıcı/rol yönetimi + audit/report.

Not:
- Tenant varsa tüm roller `tenantId` claim’i ile scoped çalışır.
- Geri uyumluluk için `HR -> Recruiter`, `User -> Applicant` aliasları seed’de korunmuştur.

## 2) Permission Keys (tek tek isimler)

- `Audit.Read`
- `User.Manage`
- `Role.Manage`
- `Job.Read`
- `Job.Create`
- `Job.Update`
- `Job.Publish`
- `Job.Weights.Read`
- `Job.Weights.Update`
- `Candidate.Read`
- `Candidate.Create`
- `Candidate.Update`
- `Candidate.Cv.Upload`
- `Candidate.Cv.Parse`
- `Application.Create`
- `Application.Read`
- `Application.UpdateStage`
- `Interview.Read`
- `Interview.Create`
- `Interview.Message.Send`
- `Scorecard.Read`
- `Scorecard.RunAuto`
- `Scorecard.Override`
- `AiEvaluation.Read`
- `AiEvaluation.Run`

Kural:
- Endpoint-level: `[RequirePermission(...)]`
- Object-level: `IAuthorizationService.AuthorizeAsync(User, resource, "Resource.*")`

## 3) Resource Ownership & Relationships

- `User { id, tenantId }`
- `JobPosting { id, tenantId, createdByUserId, assignedManagerUserId? }`
- `Candidate { id, tenantId, ownerUserId?, createdByUserId? }`
- `Application { id, tenantId, jobId, candidateId, createdByUserId?, lastUpdatedByUserId }`
- `InterviewSession { id, tenantId, applicationId, interviewerUserId?, applicantUserId? }`
- `InterviewScorecard { id, tenantId, sessionId }`
- `AiEvaluationReport { id, tenantId, jobId, candidateId, applicationId? }`
- `CvDocument { id, tenantId, candidateId, uploadedByUserId }`

ABAC kararları:
- Her resource policy önce ID’den kaydı DB’den çeker.
- Sonra `tenantId` eşleşmesi ve ilişki kontrolü (`owner/assigned/interviewer/applicant`) yapar.

## 4) Authorization Matrix (Resource x Action x Role)

- `Job`
  - `Read`: Applicant/Recruiter/HiringManager(assigned/owner)/Admin
  - `Manage`: Recruiter/HiringManager(assigned/owner)/Admin
- `Candidate`
  - `Read`: Applicant(own)/Recruiter/HiringManager(job-scope)/Interviewer(session-scope)/Admin
  - `Edit`: Applicant(own)/Recruiter/Admin
- `Application`
  - `Create`: Applicant(own candidate)/Recruiter/Admin
  - `Read`: Applicant(own)/Recruiter/HiringManager(job-scope)/Interviewer(session-scope)/Admin
  - `ManageStage`: Recruiter/HiringManager(job-scope)/Admin
- `InterviewSession`
  - `Access`: Applicant(own)/Interviewer(assigned)/Recruiter/HiringManager(job-scope)/Admin
- `Scorecard`
  - `Read`: Applicant(own session)/Interviewer(assigned)/Recruiter/HiringManager(job-scope)/Admin
  - `Override`: Recruiter/HiringManager(job-scope)/Admin
- `AiEvaluation`
  - `Run/Read`: Job + Candidate erişimi olan roller (Recruiter/HiringManager scoped/Admin)

## 5) Uygulama Tasarımı (.NET policy + resource-based handlers)

- Permission policy:
  - `RequirePermissionAttribute` -> `PermissionPolicyProvider` -> `PermissionAuthorizationHandler`
- Resource policy:
  - `ResourceAuthorizationRequirement`
  - `ResourceAuthorizationHandler : AuthorizationHandler<ResourceAuthorizationRequirement, IAuthorizationResource>`
  - Resource kayıtları:
    - `JobAuthorizationResource`
    - `CandidateAuthorizationResource`
    - `ApplicationAuthorizationResource`
    - `ApplyAuthorizationResource`
    - `InterviewAuthorizationResource`
    - `ScorecardAuthorizationResource`
    - `AiEvaluationAuthorizationResource`
    - `CvDocumentAuthorizationResource`

Karar stratejisi:
- Function-level yetki yoksa framework `403`.
- Object-level yetki yoksa controller `404` döner (resource existence leak engelleme).

## 6) Endpoint Checklist (hangi endpoint hangi policy/handler)

`backend/Controllers/JobsController.cs`
- `GET /jobs` -> `Job.Read`
- `GET /jobs/{id}` -> `Job.Read` + `Resource.CanReadJob`
- `GET /jobs/{id}/weights` -> `Job.Weights.Read` + `Resource.CanReadJob`
- `PUT /jobs/{id}/weights/*` -> `Job.Weights.Update` + `Resource.CanManageJob`
- `PATCH /jobs/{id}` -> `Job.Update` + `Resource.CanManageJob`
- `POST /jobs/{id}/publish|close` -> `Job.Publish` + `Resource.CanManageJob`

`backend/Controllers/CandidatesController.cs`
- `GET /candidates/{id}` -> `Candidate.Read` + `Resource.CanReadCandidate`
- `PATCH /candidates/{id}` -> `Candidate.Update` + `Resource.CanEditCandidate`
- `POST /candidates/{id}/cv` -> `Candidate.Cv.Upload` + `Resource.CanEditCandidate`
- `GET /candidates/{id}/cv` -> `Candidate.Read` + `Resource.CanReadCandidate`
- `GET /candidates/{id}/profile` -> `Candidate.Read` + `Resource.CanReadCandidate`

`backend/Controllers/ApplicationsController.cs`
- `POST /applications` -> `Application.Create` + `Resource.CanApplyApplication`
- `GET /applications/{id}` -> `Application.Read` + `Resource.CanReadApplication`
- `PATCH /applications/{id}/stage` -> `Application.UpdateStage` + `Resource.CanManageApplication`
- `POST /applications/{id}/interviews/start` -> `Interview.Create` + `Resource.CanManageApplication`

`backend/Controllers/InterviewsController.cs`
- `GET /interviews/{sessionId}` -> `Interview.Read` + `Resource.CanAccessInterview`
- `POST /interviews/{sessionId}/messages*` -> `Interview.Message.Send` + `Resource.CanAccessInterview`
- `POST /interviews/{sessionId}/score/auto` -> `Scorecard.RunAuto` + `Resource.CanAccessInterview`
- `POST /interviews/{sessionId}/score/human` -> `Scorecard.Override` + `Resource.CanOverrideScorecard`
- `GET /interviews/{sessionId}/score` -> `Scorecard.Read` + `Resource.CanAccessInterview`

`backend/Controllers/AiEvaluationsController.cs`
- `POST .../ai-evaluate` -> `AiEvaluation.Run` + `Resource.CanRunAiEvaluation`
- `GET .../ai-evaluate/latest|history` -> `AiEvaluation.Read` + `Resource.CanRunAiEvaluation`
- `GET /applications/{id}/ai-evaluate/latest` -> `AiEvaluation.Read` + `Resource.CanReadApplication`

`backend/Controllers/CvController.cs`
- `POST /cv/{cvDocumentId}/parse` -> `Candidate.Cv.Parse` + `Resource.CanParseCvDocument`

## 7) Test Plan (unit + integration; BOLA testleri)

Unit tests:
- `ResourceAuthorizationHandler` için role+relation+tenant kombinasyonları:
  - Applicant only own candidate/application/interview.
  - HiringManager only assigned/owned job scope.
  - Interviewer only assigned sessions.
  - Tenant mismatch always deny.

Integration tests (`WebApplicationFactory`):
- BOLA (OWASP API1:2023):
  - User A token ile User B’ye ait:
    - `/candidates/{id}`
    - `/applications/{id}`
    - `/interviews/{id}`
    - `/jobs/{id}/weights`
  - Beklenen: object-level deny -> `404` (veya function-level deny -> `403`)
- Role escalation:
  - Applicant token ile `PATCH /applications/{id}/stage` -> `403`
  - Applicant token ile `GET /jobs/{id}/weights` -> `403`
- Tenant isolation:
  - Tenant1 user ile Tenant2 resource erişimi -> `404`
- Pipeline validation:
  - Auth + audit middleware + authorization birlikte aktifken HTTP seviyesinde doğrulama.
