import { http } from "../http";

export type ReportEvidence = {
  quote: string;
  relatedTo: string;
};

export type ReportCriterion = {
  criterionKey: string;
  status: string;
  score: number | null;
  rationale: string | null;
  evidenceQuotes: ReportEvidence[];
  evaluatorType: string;
  enrichmentStatus: string;
  enrichedByModel: string | null;
  enrichmentErrors: string[];
  createdAt: string;
  createdByUserId: string | null;
};

export type InterviewScoreReportItem = {
  scorecardId: string;
  sessionId: string;
  applicationId: string;
  jobId: string;
  jobTitle: string;
  candidateId: string;
  candidateFullName: string;
  stageId: string;
  stageName: string;
  overallScore: number | null;
  scorecardCreatedAt: string;
  criteria: ReportCriterion[];
};

export async function getInterviewScoreReports(limit = 50): Promise<InterviewScoreReportItem[]> {
  const response = await http.get<InterviewScoreReportItem[]>("/reports/interview-scores", {
    params: { limit }
  });
  return response.data;
}
