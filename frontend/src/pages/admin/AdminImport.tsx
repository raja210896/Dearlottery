import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { adminAuth } from "../../api/admin";
import { importApi } from "../../api/import";
import { ApiError } from "../../api/client";
import type { ImportSummary } from "../../api/types";

export default function AdminImport() {
  const navigate = useNavigate();
  useEffect(() => {
    if (!adminAuth.isLoggedIn()) navigate("/admin/login");
  }, [navigate]);

  const csvInputRef = useRef<HTMLInputElement>(null);
  const jsonInputRef = useRef<HTMLInputElement>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [summary, setSummary] = useState<ImportSummary | null>(null);

  async function handleImport(kind: "csv" | "json") {
    const input = kind === "csv" ? csvInputRef.current : jsonInputRef.current;
    const file = input?.files?.[0];
    if (!file) {
      setError("Choose a file first.");
      return;
    }
    setLoading(true);
    setError(null);
    setSummary(null);
    try {
      const result = kind === "csv" ? await importApi.csv(file) : await importApi.json(file);
      setSummary(result);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Import failed.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="container">
      <h1 className="section-title" style={{ marginTop: 4 }}>Historical Data Import</h1>

      <div className="card" style={{ display: "flex", flexDirection: "column", gap: 10, marginBottom: 12 }}>
        <div style={{ fontSize: 13, fontWeight: 600 }}>CSV Upload</div>
        <p style={{ fontSize: 12, color: "var(--text-muted)", margin: 0 }}>Columns: DrawDate, DrawTime, ResultValue</p>
        <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
          <input ref={csvInputRef} type="file" accept=".csv,text/csv" />
          <button className="btn btn-primary" disabled={loading} onClick={() => handleImport("csv")}>
            {loading ? "Importing..." : "Import CSV"}
          </button>
        </div>
      </div>

      <div className="card" style={{ display: "flex", flexDirection: "column", gap: 10, marginBottom: 12 }}>
        <div style={{ fontSize: 13, fontWeight: 600 }}>JSON Upload</div>
        <p style={{ fontSize: 12, color: "var(--text-muted)", margin: 0 }}>Array of {"{ drawDate, drawTime, resultValue }"}</p>
        <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
          <input ref={jsonInputRef} type="file" accept=".json,application/json" />
          <button className="btn btn-primary" disabled={loading} onClick={() => handleImport("json")}>
            {loading ? "Importing..." : "Import JSON"}
          </button>
        </div>
      </div>

      {error && (
        <div className="card" style={{ color: "var(--danger)", fontSize: 13 }}>{error}</div>
      )}

      {summary && (
        <div className="card" style={{ display: "flex", flexDirection: "column", gap: 10 }}>
          <div className="section-title" style={{ marginTop: 0 }}>Import Summary</div>
          <div className="grid-3">
            <div><div style={{ fontSize: 12, color: "var(--text-muted)" }}>Total Rows</div><div style={{ fontSize: 18, fontWeight: 700 }}>{summary.totalRows}</div></div>
            <div><div style={{ fontSize: 12, color: "var(--text-muted)" }}>Imported</div><div style={{ fontSize: 18, fontWeight: 700, color: "var(--success)" }}>{summary.imported}</div></div>
            <div><div style={{ fontSize: 12, color: "var(--text-muted)" }}>Skipped</div><div style={{ fontSize: 18, fontWeight: 700 }}>{summary.skipped}</div></div>
            <div><div style={{ fontSize: 12, color: "var(--text-muted)" }}>Duplicates</div><div style={{ fontSize: 18, fontWeight: 700 }}>{summary.duplicates}</div></div>
            <div><div style={{ fontSize: 12, color: "var(--text-muted)" }}>Invalid</div><div style={{ fontSize: 18, fontWeight: 700 }}>{summary.invalid}</div></div>
          </div>

          {summary.errors.length > 0 && (
            <div>
              <div style={{ fontSize: 12, fontWeight: 600, color: "var(--text-muted)", marginBottom: 6 }}>Row Issues</div>
              <div style={{ display: "flex", flexDirection: "column", gap: 4, maxHeight: 240, overflowY: "auto" }}>
                {summary.errors.map((e, i) => (
                  <div key={i} style={{ fontSize: 12, color: "var(--text-muted)" }}>
                    Row {e.rowNumber}: {e.reason}
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
