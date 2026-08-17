import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { adminApi, adminAuth } from "../../api/admin";
import { ApiError } from "../../api/client";

export default function AdminLogin() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const res = await adminApi.login(username, password);
      adminAuth.setToken(res.token);
      navigate("/admin");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Login failed.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="container" style={{ maxWidth: 360, paddingTop: 60 }}>
      <h1 className="section-title" style={{ marginTop: 0, textAlign: "center" }}>Admin Login</h1>
      <form onSubmit={handleSubmit} className="card" style={{ display: "flex", flexDirection: "column", gap: 10 }}>
        <input placeholder="Username" value={username} onChange={(e) => setUsername(e.target.value)} required />
        <input placeholder="Password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
        {error && <span style={{ color: "var(--danger)", fontSize: 13 }}>{error}</span>}
        <button className="btn btn-primary" type="submit" disabled={loading}>{loading ? "Signing in..." : "Sign in"}</button>
      </form>
    </div>
  );
}
