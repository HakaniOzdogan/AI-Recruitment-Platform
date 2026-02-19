import { http } from "../http";

export type InterviewMessage = {
  id?: string;
  role: "system" | "candidate" | "hiring";
  content: string;
  createdAt?: string;
};

export type ManualInterviewMessageResponse = {
  id: string;
  role: "system" | "candidate" | "hiring";
  content: string;
  createdAt: string;
};

export type InterviewTurnResponse = {
  sessionId: string;
  candidateMessageId: string;
  systemMessageId?: string | null;
  systemQuestion?: string | null;
  aiMode: string;
  usedFallback: boolean;
  planGenerated: boolean;
  guardrailBlocked: boolean;
};

export type InterviewAiState = {
  sessionId: string;
  aiMode: string;
  aiModelName?: string | null;
  aiProvider?: string | null;
  aiLastPlanAt?: string | null;
  latestInsightJson?: string | null;
  latestPlanJson?: string | null;
  fallbackUsed: boolean;
  schemaValid: boolean;
  guardrailBlocked: boolean;
};

export type InterviewSession = {
  id: string;
  status?: string;
};

export type InterviewMode = "OFF" | "ASSIST" | "ADAPTIVE";
export type SetInterviewModePayload = {
  aiMode: InterviewMode;
};

export type PostInterviewMessagePayload = {
  text: string;
};

export async function getInterview(sessionId: string): Promise<InterviewSession> {
  const response = await http.get<InterviewSession>(`/interviews/${sessionId}`);
  return response.data;
}

export async function getInterviewMessages(sessionId: string): Promise<InterviewMessage[]> {
  const response = await http.get<InterviewMessage[]>(`/interviews/${sessionId}/messages`);
  return response.data;
}

export async function postInterviewMessage(sessionId: string, payload: PostInterviewMessagePayload): Promise<InterviewTurnResponse> {
  const text = payload.text.trim();
  const response = await http.post<InterviewTurnResponse>(`/interviews/${sessionId}/messages`, {
    text,
    content: text,
    message: text
  });
  return response.data;
}

export async function postHiringMessage(sessionId: string, payload: PostInterviewMessagePayload): Promise<ManualInterviewMessageResponse> {
  const text = payload.text.trim();
  const response = await http.post<ManualInterviewMessageResponse>(`/interviews/${sessionId}/messages/hiring`, {
    content: text
  });
  return response.data;
}

export async function postCandidateAnswer(sessionId: string, payload: PostInterviewMessagePayload): Promise<ManualInterviewMessageResponse> {
  const text = payload.text.trim();
  const response = await http.post<ManualInterviewMessageResponse>(`/interviews/${sessionId}/messages/candidate`, {
    content: text
  });
  return response.data;
}

export async function setInterviewMode(sessionId: string, payload: SetInterviewModePayload): Promise<void> {
  await http.patch(`/interviews/${sessionId}/mode`, payload);
}

export async function getInterviewAiDebug(sessionId: string): Promise<InterviewAiState> {
  const response = await http.get<InterviewAiState>(`/interviews/${sessionId}/ai`);
  return response.data;
}
