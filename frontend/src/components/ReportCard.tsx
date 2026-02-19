import { AiEvaluationReport } from "../api/endpoints/ai";
import { KeyValueList } from "./KeyValueList";

type JsonItem = Record<string, unknown>;

function parseList(json: string): JsonItem[] {
  try {
    const value = JSON.parse(json);
    return Array.isArray(value) ? (value as JsonItem[]) : [];
  } catch {
    return [];
  }
}

function parseObject(json: string): Record<string, unknown> {
  try {
    const value = JSON.parse(json);
    return value && typeof value === "object" ? (value as Record<string, unknown>) : {};
  } catch {
    return {};
  }
}

export function ReportCard({ report }: { report: AiEvaluationReport }): JSX.Element {
  const strengths = parseList(report.strengthsJson);
  const risks = parseList(report.risksJson);
  const questions = parseList(report.verificationQuestionsJson);
  const competency = parseList(report.competencyAssessmentJson);
  const skillAssessment = parseObject(report.skillAssessmentJson);

  return (
    <div className="report-card">
      <h3>AI Evaluation</h3>
      <p>
        Recommendation: <strong>{report.overallRecommendation}</strong> | Confidence: {report.confidence}
      </p>

      <KeyValueList
        title="Strengths"
        items={strengths.map((item, i) => ({
          key: String(item.title ?? `strength-${i}`),
          value: String(item.detail ?? "-")
        }))}
      />

      <KeyValueList
        title="Risks"
        items={risks.map((item, i) => ({
          key: `${item.title ?? `risk-${i}`} (sev:${item.severity ?? "-"})`,
          value: String(item.detail ?? "-")
        }))}
      />

      <KeyValueList
        title="Verification Questions"
        items={questions.map((item, i) => ({
          key: String(item.category ?? `q-${i}`),
          value: String(item.question ?? "-")
        }))}
      />

      <KeyValueList
        title="Competency Assessment"
        items={competency.map((item, i) => ({
          key: `${item.key ?? `competency-${i}`} (${item.score ?? "-"})`,
          value: String(item.rationale ?? "-")
        }))}
      />

      <div>
        <h4>Skill Assessment</h4>
        <pre>{JSON.stringify(skillAssessment, null, 2)}</pre>
      </div>
    </div>
  );
}
