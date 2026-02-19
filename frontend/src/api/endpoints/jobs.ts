import { http } from "../http";

export type JobStatus = "Draft" | "Published" | "Closed";

export type Job = {
  id: string;
  title: string;
  department?: string | null;
  location?: string | null;
  employmentType?: string | null;
  description: string;
  requiredSkills: string[];
  niceToHaveSkills: string[];
  minExperienceMonths?: number | null;
  status: JobStatus;
  createdByUserId: string;
  createdAt: string;
  publishedAt?: string | null;
  closedAt?: string | null;
};

export type CreateJobPayload = {
  title: string;
  description: string;
  department?: string | null;
  location?: string | null;
  employmentType?: string | null;
  requiredSkills: string[];
  niceToHaveSkills: string[];
  minExperienceMonths?: number | null;
};

export type JobSkillWeight = {
  name: string;
  weight: number;
  isRequired: boolean;
  createdAt?: string;
};

export type JobWeightsResponse = {
  jobId: string;
  competencies: Record<string, number>;
  skills: JobSkillWeight[];
};

export type MatchListItem = {
  candidateId: string;
  candidateFullName: string;
  applicationId: string;
  stageId: string;
  stageName: string;
  score: number;
  computedAt: string;
  reasons: string[];
  gaps: string[];
};

export type GetMatchesParams = {
  minScore?: number;
  stageId?: string;
  limit?: number;
  offset?: number;
};

export function toApiMinScore(uiPercent: number): number {
  if (!Number.isFinite(uiPercent)) {
    return 0;
  }

  return Math.max(0, Math.min(100, Math.round(uiPercent)));
}

export async function getJobs(): Promise<Job[]> {
  const response = await http.get<Job[]>("/jobs");
  return response.data;
}

export async function createJob(payload: CreateJobPayload): Promise<Job> {
  const response = await http.post<Job>("/jobs", payload);
  return response.data;
}

export async function publishJob(id: string): Promise<Job> {
  const response = await http.post<Job>(`/jobs/${id}/publish`);
  return response.data;
}

export async function getJob(id: string): Promise<Job> {
  const response = await http.get<Job>(`/jobs/${id}`);
  return response.data;
}

export async function getJobWeights(id: string): Promise<JobWeightsResponse> {
  const response = await http.get<JobWeightsResponse>(`/jobs/${id}/weights`);
  return response.data;
}

export async function updateCompetencyWeights(id: string, weights: Record<string, number>): Promise<JobWeightsResponse> {
  const response = await http.put<JobWeightsResponse>(`/jobs/${id}/weights/competencies`, { weights });
  return response.data;
}

export async function updateSkillWeights(id: string, skills: JobSkillWeight[]): Promise<JobWeightsResponse> {
  const response = await http.put<JobWeightsResponse>(`/jobs/${id}/weights/skills`, { skills });
  return response.data;
}

export async function getMatches(id: string, params: GetMatchesParams = {}, signal?: AbortSignal): Promise<MatchListItem[]> {
  const response = await http.get<MatchListItem[]>(`/jobs/${id}/matches`, { params, signal });
  return response.data;
}
