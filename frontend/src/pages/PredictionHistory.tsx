import { useState } from "react";
import { useApi } from "../hooks/useApi";
import { predictionsApi } from "../api/predictions";
import { LoadingSkeleton, ErrorState, EmptyState } from "../components/StateViews";
import Pagination from "../components/Pagination";
import { DRAW_TIMES } from "../api/types";

const MATCH_STATUSES = [
  { value: "", label: "All" },
  { value: "matched", label: "Matched" },
  { value: "unmatched", label: "Unmatched" },
  { value: "pending", label: "Pending" },
] as const;

export default function PredictionHistory() {
  const [page, setPage] = useState(1);
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [drawTime, setDrawTime] = useState("");
  const [digitLength, setDigitLength] = useState("");
  const [matchStatus, setMatchStatus] = useState("");
  const [expandedId, setExpandedId] = useState<number | null>(null);

  const history = useApi(
    () => predictionsApi.history({
      page, pageSize: 20,
      from: from || undefined, to: to || undefined,
      drawTime: drawTime || undefined,
      digitLength: digitLength ? Number(digitLength) : undefined,
      matchStatus: matchStatus || undefined,
    }),
    [page, from, to, drawTime, digitLength, matchStatus]
  );

  function matchBadge(item: { isEvaluated: boolean; matchFound: boolean | null }) {
    if (!item.isEvaluated) return <span className="badge badge-warning">Pending</span>;
    return item.matchFound
      ? <span className="badge badge-success">Match</span>
      : <span className="badge badge-muted">No match</span>;
  }

  return (
    <div className="container">
      <h1 className="section-title" style={{ marginTop: 4 }}>Prediction History</h1>

      <div className="card" style={{ display: "flex", gap: 8, flexWrap: "wrap", marginBottom: 12 }}>
        <input type="date" value={from} onChange={(e) => { setFrom(e.target.value); setPage(1); }} />
        <input type="date" value={to} onChange={(e) => { setTo(e.target.value); setPage(1); }} />
        <select value={drawTime} onChange={(e) => { setDrawTime(e.target.value); setPage(1); }}>
          <option value="">All draws</option>
          {DRAW_TIMES.map((d) => <option key={d} value={d}>{d}</option>)}
        </select>
        <select value={digitLength} onChange={(e) => { setDigitLength(e.target.value); setPage(1); }}>
          <option value="">Any digit length</option>
          <option value="1">1 digit</option>
          <option value="2">2 digits</option>
          <option value="3">3 digits</option>
        </select>
        <select value={matchStatus} onChange={(e) => { setMatchStatus(e.target.value); setPage(1); }}>
          {MATCH_STATUSES.map((s) => <option key={s.value} value={s.value}>{s.label}</option>)}
        </select>
      </div>

      {history.loading && <LoadingSkeleton rows={6} height={56} />}
      {history.error && <ErrorState message={history.error} onRetry={history.reload} />}
      {history.data && history.data.items.length === 0 && <EmptyState message="No predictions saved yet." />}
      {history.data && history.data.items.length > 0 && (
        <div className="card" style={{ padding: 0, overflowX: "auto" }}>
          {history.data.items.map((p, i) => (
            <div key={p.id} style={{ padding: "12px 14px", borderTop: i === 0 ? "none" : "1px solid var(--border)" }}>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", flexWrap: "wrap", gap: 6 }}>
                <div>
                  <div style={{ fontSize: 13, fontWeight: 600 }}>{p.drawDate} · {p.drawTime} · {p.digitLength}-digit</div>
                  <div style={{ fontSize: 12, color: "var(--text-muted)" }}>
                    Generated {new Date(p.generatedAt).toLocaleString()} · Top candidate: <b style={{ color: "var(--text)" }}>{p.candidates[0]?.value ?? "—"}</b>
                    {p.candidates[0] && <> · Model Score <b style={{ color: "var(--text)" }}>{p.candidates[0].modelScore}</b></>}
                  </div>
                </div>
                {matchBadge(p)}
              </div>
              {p.isEvaluated && (
                <div style={{ fontSize: 12, color: "var(--text-muted)", marginTop: 4 }}>
                  Actual result: <b style={{ color: "var(--text)" }}>{p.actualResult}</b>
                  {p.matchFound && p.matchPosition && <> · matched at rank #{p.matchPosition}</>}
                  <div style={{ display: "flex", gap: 6, marginTop: 4 }}>
                    <span className={`badge ${p.exactMatch ? "badge-success" : "badge-muted"}`}>Exact: {p.exactMatch === null ? "N/A" : p.exactMatch ? "Match" : "No match"}</span>
                    <span className={`badge ${p.last3Match ? "badge-success" : "badge-muted"}`}>Last-3: {p.last3Match === null ? "N/A" : p.last3Match ? "Match" : "No match"}</span>
                    <span className={`badge ${p.last2Match ? "badge-success" : "badge-muted"}`}>Last-2: {p.last2Match === null ? "N/A" : p.last2Match ? "Match" : "No match"}</span>
                  </div>
                </div>
              )}
              {p.candidates.length > 0 && (
                <button
                  className="btn btn-outline"
                  style={{ padding: "4px 10px", minHeight: 26, fontSize: 11, marginTop: 6 }}
                  onClick={() => setExpandedId(expandedId === p.id ? null : p.id)}
                >
                  {expandedId === p.id ? "Hide candidates" : `Show all ${p.candidates.length} candidates`}
                </button>
              )}
              {expandedId === p.id && (
                <div style={{ marginTop: 8, display: "flex", flexDirection: "column", gap: 8 }}>
                  {p.candidates.map((c) => (
                    <div key={c.value} style={{ padding: "8px 10px", background: "var(--bg)", borderRadius: 8 }}>
                      <div style={{ display: "flex", justifyContent: "space-between" }}>
                        <span style={{ fontWeight: 700 }}>{c.value}</span>
                        <span style={{ fontSize: 12, color: "var(--text-muted)" }}>Model Score <b style={{ color: "var(--text)" }}>{c.modelScore}</b></span>
                      </div>
                      <div style={{ fontSize: 11, color: "var(--text-muted)", marginTop: 2 }}>
                        Historical Frequency <b style={{ color: "var(--text)" }}>{c.historicalFrequency}</b> · Recent Frequency <b style={{ color: "var(--text)" }}>{c.recentFrequency}</b>
                      </div>
                      <div style={{ fontSize: 11, color: "var(--text-muted)", marginTop: 2 }}>{c.reason}</div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {history.data && (
        <Pagination page={page} pageSize={20} totalCount={history.data.totalCount} onPageChange={setPage} />
      )}

      <p className="disclaimer">Historical comparison only. Match Rate and Model Score are not a winning probability.</p>
    </div>
  );
}
