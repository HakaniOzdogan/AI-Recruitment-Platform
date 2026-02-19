import { FormEvent, useState } from "react";
import { FieldError } from "./FieldError";
import { ProgressBar } from "./ProgressBar";

type Props = {
  onUpload: (file: File, onProgress: (value: number) => void) => Promise<void>;
  disabled?: boolean;
};

export function FileUploadBox({ onUpload, disabled = false }: Props): JSX.Element {
  const [file, setFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);
  const [progress, setProgress] = useState(0);
  const [error, setError] = useState<string | null>(null);

  function validateFile(nextFile: File | null): string | null {
    if (!nextFile) {
      return "Dosya seçmelisin.";
    }

    const lower = nextFile.name.toLowerCase();
    if (!(lower.endsWith(".pdf") || lower.endsWith(".docx"))) {
      return "Sadece PDF veya DOCX yükleyebilirsin.";
    }

    if (nextFile.size > 10 * 1024 * 1024) {
      return "Dosya çok büyük (max 10MB).";
    }

    return null;
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (disabled) {
      return;
    }

    const validationError = validateFile(file);
    if (validationError) {
      setError(validationError);
      return;
    }

    setError(null);
    setProgress(0);
    setUploading(true);
    try {
      await onUpload(file as File, setProgress);
      setFile(null);
      setProgress(100);
    } finally {
      setUploading(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="form-grid">
      <input
        type="file"
        accept=".pdf,.docx"
        onChange={(event) => {
          const nextFile = event.target.files?.[0] ?? null;
          setFile(nextFile);
          setError(validateFile(nextFile));
        }}
        disabled={disabled || uploading}
      />
      <FieldError message={error} />
      {uploading ? <ProgressBar value={progress} /> : null}
      <div className="row-actions">
        <button type="submit" disabled={!file || !!error || disabled || uploading}>
          {uploading ? "Uploading..." : "Upload CV"}
        </button>
      </div>
    </form>
  );
}
