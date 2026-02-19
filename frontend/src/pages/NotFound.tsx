import { Link } from "react-router-dom";

export function NotFoundPage(): JSX.Element {
  return (
    <section className="card">
      <h1>404</h1>
      <p>Sayfa bulunamadı.</p>
      <Link to="/jobs">Jobs'a dön</Link>
    </section>
  );
}
