import { AxiosError } from "axios";
import { FormEvent, useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { login, register } from "../auth/auth";
import { notifyError } from "../store/uiFeedback";

export function LoginPage(): JSX.Element {
  const [mode, setMode] = useState<"login" | "register">("login");
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const navigate = useNavigate();
  const location = useLocation();
  const isRegister = mode === "register";
  const returnUrl = useMemo(() => new URLSearchParams(location.search).get("returnUrl"), [location.search]);
  const from = (location.state as { from?: string } | null)?.from;

  function showPopup(message: string, caught?: unknown): void {
    if (caught instanceof AxiosError) {
      const ref = caught.response?.headers?.["x-correlation-id"];
      notifyError(message, typeof ref === "string" ? ref : null);
      return;
    }

    notifyError(message);
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);

    const normalizedFullName = fullName.trim();
    const normalizedEmail = email.trim();
    const normalizedPassword = password.trim();

    if (!normalizedEmail || !normalizedPassword) {
      const message = "Email ve şifre zorunlu";
      setError(message);
      showPopup(message);
      return;
    }

    if (isRegister) {
      if (!normalizedFullName) {
        const message = "Ad soyad zorunlu";
        setError(message);
        showPopup(message);
        return;
      }

      if (normalizedPassword.length < 8) {
        const message = "Şifre en az 8 karakter olmalı";
        setError(message);
        showPopup(message);
        return;
      }

      if (normalizedPassword !== confirmPassword.trim()) {
        const message = "Şifreler eşleşmiyor";
        setError(message);
        showPopup(message);
        return;
      }
    }

    setLoading(true);
    try {
      if (isRegister) {
        await register(normalizedFullName, normalizedEmail, normalizedPassword);
      } else {
        await login(normalizedEmail, normalizedPassword);
      }

      navigate(returnUrl || from || "/dashboard", { replace: true });
    } catch (caught) {
      const fallback = isRegister ? "Kayıt başarısız" : "Giriş başarısız";
      const message =
        typeof caught === "object" &&
        caught !== null &&
        "response" in caught &&
        typeof (caught as { response?: { data?: { message?: string } } }).response?.data?.message === "string"
          ? (caught as { response?: { data?: { message?: string } } }).response!.data!.message!
          : fallback;

      setError(message);
      showPopup(message, caught);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="login-wrap">
      <form className="card" onSubmit={handleSubmit}>
        <h1>{isRegister ? "Hesap Oluştur" : "Login"}</h1>

        <div style={{ display: "flex", gap: 8, marginBottom: 8 }}>
          <button type="button" onClick={() => setMode("login")} disabled={loading || !isRegister}>
            Giriş
          </button>
          <button type="button" onClick={() => setMode("register")} disabled={loading || isRegister}>
            Kayıt Ol
          </button>
        </div>

        {isRegister ? (
          <label>
            Ad Soyad
            <input value={fullName} onChange={(event) => setFullName(event.target.value)} />
          </label>
        ) : null}

        <label>
          Email
          <input value={email} onChange={(event) => setEmail(event.target.value)} />
        </label>

        <label>
          Password
          <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} />
        </label>

        {isRegister ? (
          <label>
            Password (Tekrar)
            <input
              type="password"
              value={confirmPassword}
              onChange={(event) => setConfirmPassword(event.target.value)}
            />
          </label>
        ) : null}

        {error ? <p className="inline-error">{error}</p> : null}

        <button type="submit" disabled={loading}>
          {loading ? "Bekleyin..." : isRegister ? "Hesap Oluştur" : "Login"}
        </button>
      </form>
    </div>
  );
}
