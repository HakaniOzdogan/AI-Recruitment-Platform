export function buildCandidateUrl(id: string, jobId?: string | null): string {
  if (!jobId) {
    return `/candidates/${id}`;
  }

  const params = new URLSearchParams({ jobId });
  return `/candidates/${id}?${params.toString()}`;
}

export function buildInterviewUrl(sessionId: string, jobId?: string | null, candidateId?: string | null, appId?: string | null): string {
  const params = new URLSearchParams();
  if (jobId) {
    params.set("jobId", jobId);
  }
  if (candidateId) {
    params.set("candidateId", candidateId);
  }
  if (appId) {
    params.set("appId", appId);
  }

  const query = params.toString();
  return query.length > 0 ? `/interviews/${sessionId}?${query}` : `/interviews/${sessionId}`;
}
