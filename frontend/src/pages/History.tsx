import { useState } from "react";
import { useApi } from "../hooks/useApi";
import { resultsApi } from "../api/results";
import { LoadingSkeleton, ErrorState, EmptyState } from "../components/StateViews";
import Pagination from "../components/Pagination";
import { DRAW_TIMES } from "../api/types";

export default function History() {
  const [page, setPage] = useState(1);
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [drawTime, setDrawTime] = useState("");
  const [sort, setSort] = useState("date_desc");

  const history = useApi(
    () => resultsApi.history({ page, pageSize: 25, from: from || undefined, to: to || undefined, drawTime: drawTime || undefined, sort }),
    [page, from, to, drawTime, sort]
  );

  return (
    <div className="container">
      <h1 className="section-title" style={{ marginTop: 4 }}>History</h1>

      <div className="card" style={{ display: "flex", gap: 8, flexWrap: "wrap", marginBottom: 12 }}>
        <input type="date" value={from} onChange={(e) => { setFrom(e.target.value); setPage(1); }} />
        <input type="date" value={to} onChange={(e) => { setTo(e.target.value); setPage(1); }} />
        <select value={drawTime} onChange={(e) => { setDrawTime(e.target.value); setPage(1); }}>
          <option value="">All draws</option>
          {DRAW_TIMES.map((d) => <option key={d} value={d}>{d}</option>)}
        </select>
        <select value={sort} onChange={(e) => setSort(e.target.value)}>
          <option value="date_desc">Newest first</option>
          <option value="date_asc">Oldest first</option>
        </select>
      </div>

      {history.loading && <LoadingSkeleton rows={6} height={44} />}
      {history.error && <ErrorState message={history.error} onRetry={history.reload} />}
      {history.data && history.data.items.length === 0 && <EmptyState message="No results in this range." />}
      {history.data && history.data.items.length > 0 && (
        <div className="card" style={{ padding: 0, overflowX: "auto" }}>
          <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 13 }}>
            <thead>
              <tr style={{ textAlign: "left", color: "var(--text-muted)" }}>
                <th style={{ padding: "10px 14px" }}>Date</th>
                <th style={{ padding: "10px 14px" }}>Draw</th>
                <th style={{ padding: "10px 14px" }}>Result</th>
              </tr>
            </thead>
            <tbody>
              {history.data.items.map((r) => (
                <tr key={r.id} style={{ borderTop: "1px solid var(--border)" }}>
                  <td style={{ padding: "10px 14px" }}>{r.drawDate}</td>
                  <td style={{ padding: "10px 14px" }}>{r.drawTime}</td>
                  <td style={{ padding: "10px 14px", fontWeight: 700 }}>{r.resultValue}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {history.data && (
        <Pagination page={page} pageSize={25} totalCount={history.data.totalCount} onPageChange={setPage} />
      )}
    </div>
  );
}
