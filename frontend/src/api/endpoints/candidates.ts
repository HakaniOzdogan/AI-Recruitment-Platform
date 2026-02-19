import { http } from "../http";

export type Candidate = {
  id: string;
  fullName: string;
  email?: string | null;
  phone?: string | null;
  source?: string | null;
  createdAt: string;
};

export type CandidateCreatePayload = {
  fullName: string;
  email?: string | null;
  phone?: string | null;
  source?: string | null;
};

export type CandidateProfile = {
  candidateId: string;
  fullNameSnapshot: string;
  emailSnapshot?: string | null;
  summary?: string | null;
  totalExperienceMonths?: number | null;
  skillsJson: string;
  educationJson: string;
  experienceJson: string;
  languagesJson: string;
  linksJson: string;
  updatedAt: string;
};

export type CvUploadResponse = {
  cvDocumentId: string;
  candidateId: string;
  originalFileName: string;
  fileType: string;
  fileSize: number;
  parseStatus: string;
  uploadedAt: string;
};

export type ParseCvResponse = {
  cvDocumentId: string;
  candidateId: string;
  parseStatus: string;
  parseError?: string | null;
  parsedAt?: string | null;
  profile?: CandidateProfile | null;
};

export async function getCandidates(): Promise<Candidate[]> {
  const response = await http.get<Candidate[]>("/candidates");
  return response.data;
}

export async function createCandidate(payload: CandidateCreatePayload): Promise<Candidate> {
  const response = await http.post<Candidate>("/candidates", payload);
  return response.data;
}

export async function getCandidate(id: string): Promise<Candidate> {
  const response = await http.get<Candidate>(`/candidates/${id}`);
  return response.data;
}

export async function getCandidateProfile(id: string): Promise<CandidateProfile> {
  const response = await http.get<CandidateProfile>(`/candidates/${id}/profile`);
  return response.data;
}

export async function uploadCandidateCv(candidateId: string, file: File, onProgress?: (value: number) => void): Promise<CvUploadResponse> {
  const formData = new FormData();
  formData.append("file", file);

  const response = await http.post<CvUploadResponse>(`/candidates/${candidateId}/cv`, formData, {
    headers: { "Content-Type": "multipart/form-data" },
    onUploadProgress: (evt) => {
      if (!onProgress) {
        return;
      }

      if (!evt.total || evt.total <= 0) {
        onProgress(0);
        return;
      }

      onProgress((evt.loaded / evt.total) * 100);
    }
  });

  return response.data;
}

export async function parseCv(cvDocumentId: string): Promise<ParseCvResponse> {
  const response = await http.post<ParseCvResponse>(`/cv/${cvDocumentId}/parse`);
  return response.data;
}
