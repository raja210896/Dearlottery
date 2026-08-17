import { useState } from "react";
import { useSearchParams } from "react-router-dom";
import { useApi, useDebounced } from "../hooks/useApi";
import { resultsApi } from "../api/results";
import { LoadingSkeleton, ErrorState, EmptyState } from "../components/StateViews";
import Pagination from "../components/Pagination";
import { DRAW_TIMES } from "../api/types";

export default function Results() {
  const [params] = useSearchParams();
  const [page, setPage] = useState(1);
  const [drawTime, setDrawTime] = useState(params.get("drawTime") || "");
  const [search, setSearch] = useState("");
  const debouncedSearch = useDebounced(search);

  const results = useApi(
    () => resultsApi.list({ page, pageSize: 20, drawTime: drawTime || undefined, search: debouncedSearch || undefined }),
    [page, drawTime, debouncedSearch]
  );

  return (
    <div className="container">
      <h1 className="section-title" style={{ marginTop: 4 }}>Results</h1>

      <div className="card" style={{ display: "flex", gap: 8, flexWrap: "wrap", marginBottom: 12 }}>
        <select value={drawTime} onChange={(e) => { setDrawTime(e.target.value); setPage(1); }}>
          <option value="">All draws</option>
          {DRAW_TIMES.map((d) => <option key={d} value={d}>{d}</option>)}
        </select>
        <input
          placeholder="Search number..."
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          style={{ flex: 1, minWidth: 140 }}
        />
      </div>

      {results.loading && <LoadingSkeleton rows={6} height={44} />}
      {results.error && <ErrorState message={results.error} onRetry={results.reload} />}
      {results.data && results.data.items.length === 0 && <EmptyState message="No results found." />}
      {results.data && results.data.items.length > 0 && (
        <div className="card" style={{ padding: 0, overflow: "hidden" }}>
          {results.data.items.map((r, i) => (
            <div
              key={r.id}
              style={{
                display: "flex", justifyContent: "space-between", padding: "12px 14px",
                borderTop: i === 0 ? "none" : "1px solid var(--border)",
              }}
            >
              <div>
                <div style={{ fontSize: 13, fontWeight: 600 }}>{r.drawTime}</div>
                <div style={{ fontSize: 12, color: "var(--text-muted)" }}>{r.drawDate}</div>
              </div>
              <div style={{ fontSize: 20, fontWeight: 700, alignSelf: "center" }}>{r.resultValue}</div>
            </div>
          ))}
        </div>
      )}

      {results.data && (
        <Pagination page={page} pageSize={20} totalCount={results.data.totalCount} onPageChange={setPage} />
      )}
    </div>
  );
}
