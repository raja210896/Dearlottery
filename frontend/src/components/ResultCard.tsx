import { Link } from "react-router-dom";
import type { ResultDto } from "../api/types";

function timeAgo(iso: string | null): string {
  if (!iso) return "—";
  const diffMs = Date.now() - new Date(iso).getTime();
  const mins = Math.floor(diffMs / 60000);
  if (mins < 1) return "just now";
  if (mins < 60) return `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  return `${Math.floor(hrs / 24)}d ago`;
}

export default function ResultCard({
  result, lastAvailable,
}: { result: ResultDto; lastAvailable?: { value: string; date: string } | null }) {
  const published = result.status === "Published";
  return (
    <div className="card" style={{ display: "flex", flexDirection: "column", gap: 8 }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <span style={{ fontWeight: 600, fontSize: 14 }}>{result.drawTime}</span>
        <span className={`badge ${published ? "badge-success" : "badge-warning"}`}>
          {published ? "Published" : "Waiting for result"}
        </span>
      </div>
      <div style={{ fontSize: 30, fontWeight: 700, letterSpacing: 1 }}>
        {published ? result.resultValue : "--"}
      </div>
      {!published && lastAvailable && (
        <div style={{ fontSize: 11, color: "var(--text-muted)" }}>
          Latest available: <b style={{ color: "var(--text)" }}>{lastAvailable.value}</b> ({lastAvailable.date})
        </div>
      )}
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <span style={{ fontSize: 12, color: "var(--text-muted)" }}>Updated {timeAgo(result.lastUpdated)}</span>
        <Link to={`/results?date=${result.drawDate}&drawTime=${encodeURIComponent(result.drawTime)}`} className="btn btn-outline" style={{ padding: "5px 10px", minHeight: 30, fontSize: 12 }}>
          View
        </Link>
      </div>
    </div>
  );
}
