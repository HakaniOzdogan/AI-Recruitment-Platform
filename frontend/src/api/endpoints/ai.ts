import { http } from "../http";
import { Job } from "./jobs";

export type AiEvaluationReport = {
  id: string;
  jobId: string;
  candidateId: string;
  applicationId?: string | null;
  modelName: string;
  overallRecommendation: string;
  strengthsJson: string;
  risksJson: string;
  verificationQuestionsJson: string;
  evidenceQuotesJson: string;
  competencyAssessmentJson: string;
  skillAssessmentJson: string;
  confidence: number;
  createdAt: string;
  version: number;
};

export async function listJobs(): Promise<Job[]> {
  const response = await http.get<Job[]>("/jobs");
  return response.data;
}

export async function getAiEvalLatest(jobId: string, candidateId: string, signal?: AbortSignal): Promise<AiEvaluationReport> {
  const response = await http.get<AiEvaluationReport>(`/jobs/${jobId}/candidates/${candidateId}/ai-evaluate/latest`, { signal });
  return response.data;
}

export async function runAiEval(jobId: string, candidateId: string, force = true, signal?: AbortSignal): Promise<AiEvaluationReport> {
  const suffix = force ? "?force=true" : "";
  const response = await http.post<AiEvaluationReport>(`/jobs/${jobId}/candidates/${candidateId}/ai-evaluate${suffix}`, undefined, { signal });
  return response.data;
}
