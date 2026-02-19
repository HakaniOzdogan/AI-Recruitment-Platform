import { http } from "../http";

export type EvidenceQuote = {
  quote: string;
  relatedTo: string;
};

export type ScoreCriterionLatest = {
  criterionKey: string;
  status: "SCORED" | "INSUFFICIENT_EVIDENCE" | string;
  score: number | null;
  rationale: string | null;
  evidenceQuotes: EvidenceQuote[];
  evaluatorType: "AI" | "HUMAN" | string;
  enrichmentStatus: string;
  enrichedByModel: string | null;
  enrichmentErrors: string[];
  createdAt: string;
  createdByUserId: string | null;
};

export type ScoreCriterionHistory = {
  id: string;
  criterionKey: string;
  status: "SCORED" | "INSUFFICIENT_EVIDENCE" | string;
  score: number | null;
  rationale: string | null;
  evidenceQuotes: EvidenceQuote[];
  evaluatorType: "AI" | "HUMAN" | string;
  enrichmentStatus: string;
  enrichedByModel: string | null;
  enrichmentErrors: string[];
  createdAt: string;
  createdByUserId: string | null;
  replacesScoreId: string | null;
};

export type ScorecardResponse = {
  scorecardId: string;
  sessionId: string;
  rubricTemplateId: string;
  overallScore: number | null;
  insufficientAll: boolean;
  llmEnrichment: string;
  llmEnrichmentEvents: string[];
  createdAt: string;
  latest: ScoreCriterionLatest[];
  history: ScoreCriterionHistory[];
};

export type HumanOverridePayload = {
  criterionKey: "technical" | "problem_solving" | "communication" | "culture_fit" | "domain_knowledge";
  score: number;
  rationale: string;
  evidenceQuotes: Array<{ quote: string; relatedTo?: string; related_to?: string }>;
};

export async function runAutoScore(sessionId: string, force = true): Promise<ScorecardResponse> {
  const suffix = force ? "?force=true" : "?force=false";
  const response = await http.post<ScorecardResponse>(`/interviews/${sessionId}/score/auto${suffix}`);
  return response.data;
}

export async function getScorecard(sessionId: string): Promise<ScorecardResponse> {
  const response = await http.get<ScorecardResponse>(`/interviews/${sessionId}/score`, {
    meta: { suppressErrorStatuses: [404] }
  });
  return response.data;
}

export async function postHumanOverride(sessionId: string, payload: HumanOverridePayload): Promise<ScorecardResponse> {
  const normalized = {
    ...payload,
    evidenceQuotes: payload.evidenceQuotes.map((item) => ({
      quote: item.quote,
      relatedTo: item.relatedTo ?? item.related_to,
      related_to: item.related_to ?? item.relatedTo
    }))
  };
  const response = await http.post<ScorecardResponse>(`/interviews/${sessionId}/score/human`, normalized);
  return response.data;
}
