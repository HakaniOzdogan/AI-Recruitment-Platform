import { http } from "../http";

export type ApplicationStatus = "Active" | "Hired" | "Rejected";
export type ApplicationStage = "Applied" | "Screening" | "Interview" | "Offer" | "Hired" | "Rejected";

export type ApplicationCreatePayload = {
  jobId: string;
  candidateId: string;
};

export type ApplicationStageUpdatePayload = {
  stage: ApplicationStage | string;
};

export type Application = {
  id: string;
  jobId: string;
  jobTitle?: string;
  candidateId: string;
  candidateFullName: string;
  stageId: string;
  stageName: string;
  status: ApplicationStatus;
  appliedAt: string;
  updatedAt: string;
  lastUpdatedByUserId?: string;
  sessionId?: string | null;
  interviewSessionId?: string | null;
  interviewId?: string | null;
};

export type StartInterviewResponse = {
  sessionId: string;
};

export async function getApplicationsByJob(jobId: string): Promise<Application[]> {
  const response = await http.get<Application[]>("/applications", { params: { jobId } });
  return response.data;
}

export async function getApplications(): Promise<Application[]> {
  const response = await http.get<Application[]>("/applications");
  return response.data;
}

export async function createApplication(payload: ApplicationCreatePayload): Promise<Application> {
  const response = await http.post<Application>("/applications", payload);
  return response.data;
}

export async function updateApplicationStage(appId: string, payload: ApplicationStageUpdatePayload): Promise<Application> {
  const normalized = payload.stage.trim();
  const response = await http.patch<Application>(`/applications/${appId}/stage`, {
    stage: normalized,
    stageId: normalized
  });
  return response.data;
}

export async function startInterview(appId: string): Promise<StartInterviewResponse> {
  const response = await http.post<StartInterviewResponse>(`/applications/${appId}/interviews/start`);
  return response.data;
}
