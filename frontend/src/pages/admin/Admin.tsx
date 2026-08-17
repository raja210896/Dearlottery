import { useEffect } from "react";
import { Link, useNavigate } from "react-router-dom";
import { adminApi, adminAuth } from "../../api/admin";
import { useApi } from "../../hooks/useApi";
import { LoadingSkeleton, ErrorState } from "../../components/StateViews";

export default function Admin() {
  const navigate = useNavigate();

  useEffect(() => {
    if (!adminAuth.isLoggedIn()) navigate("/admin/login");
  }, [navigate]);

  const dashboard = useApi(() => adminApi.dashboard(), []);
  const logs = useApi(() => adminApi.syncLogs(1, 10), []);

  async function handleRunSync() {
    await adminApi.runSync();
    dashboard.reload();
    logs.reload();
  }

  function handleLogout() {
    adminAuth.clearToken();
    navigate("/admin/login");
  }

  return (
    <div className="container">
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <h1 className="section-title" style={{ marginTop: 4 }}>Admin Dashboard</h1>
        <button className="btn btn-outline" onClick={handleLogout}>Logout</button>
      </div>

      {dashboard.loading && <LoadingSkeleton rows={2} height={80} />}
      {dashboard.error && <ErrorState message={dashboard.error} onRetry={dashboard.reload} />}
      {dashboard.data && (
        <div className="grid-3" style={{ marginBottom: 16 }}>
          <div className="card"><div style={{ fontSize: 12, color: "var(--text-muted)" }}>Total Results</div><div style={{ fontSize: 24, fontWeight: 700 }}>{dashboard.data.totalResults}</div></div>
          <div className="card"><div style={{ fontSize: 12, color: "var(--text-muted)" }}>Latest Sync</div><div style={{ fontSize: 13, fontWeight: 600 }}>{dashboard.data.latestSyncAt ? new Date(dashboard.data.latestSyncAt).toLocaleString() : "Never"}</div></div>
          <div className="card">
            <div style={{ fontSize: 12, color: "var(--text-muted)" }}>Sync Status</div>
            <span className={`badge ${dashboard.data.latestSyncSuccess ? "badge-success" : "badge-warning"}`}>
              {dashboard.data.latestSyncAt ? (dashboard.data.latestSyncSuccess ? "Success" : "Failed") : "N/A"}
            </span>
          </div>
        </div>
      )}

      <div style={{ display: "flex", gap: 8, marginBottom: 16 }}>
        <button className="btn btn-primary" onClick={handleRunSync}>Run Sync Now</button>
        <Link to="/admin/results" className="btn btn-outline">Manual Results</Link>
        <Link to="/admin/import" className="btn btn-outline">Import Data</Link>
      </div>

      <h2 className="section-title">Sync Logs</h2>
      {logs.loading && <LoadingSkeleton rows={4} height={40} />}
      {logs.error && <ErrorState message={logs.error} onRetry={logs.reload} />}
      {logs.data && (
        <div className="card" style={{ padding: 0 }}>
          {logs.data.items.map((l, i) => (
            <div key={l.id} style={{ display: "flex", justifyContent: "space-between", padding: "10px 14px", borderTop: i === 0 ? "none" : "1px solid var(--border)", fontSize: 13 }}>
              <span>{new Date(l.startedAt).toLocaleString()} · {l.trigger}</span>
              <span className={`badge ${l.success ? "badge-success" : "badge-warning"}`}>{l.success ? `${l.recordsImported} imported` : "Failed"}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
