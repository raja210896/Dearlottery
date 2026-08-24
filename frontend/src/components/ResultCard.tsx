import { Link } from "react-router-dom";
import type { ResultDto } from "../api/types";
import { ChevronRightIcon } from "./icons";

export default function ResultCard({
  result, lastAvailable, active = false,
}: { result: ResultDto; lastAvailable?: { value: string; date: string } | null; active?: boolean }) {
  const published = result.status === "Published";
  const value = published ? result.resultValue : null;

  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        gap: 6,
        background: "var(--surface)",
        borderRadius: "var(--radius)",
        padding: "14px 14px 12px",
        boxShadow: active ? "0 0 0 2px var(--primary), var(--shadow-md)" : "var(--shadow-sm)",
        position: "relative",
      }}
    >
      {active && (
        <span
          style={{
            position: "absolute",
            top: -9,
            left: 12,
            background: "var(--primary)",
            color: "#fff",
            fontSize: 9,
            fontWeight: 700,
            letterSpacing: 0.5,
            padding: "2px 8px",
            borderRadius: 999,
            textTransform: "uppercase",
          }}
        >
          Active
        </span>
      )}
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <span style={{ fontSize: 20, fontWeight: 800 }}>{result.drawTime}</span>
        <span style={{ display: "inline-flex", alignItems: "center", gap: 5, fontSize: 10, fontWeight: 700, color: "var(--text-muted)", textTransform: "uppercase" }}>
          <span className={`status-dot ${published ? "success" : "warning"}`} />
          {published ? "Published" : "Pending"}
        </span>
      </div>
      <div style={{ fontSize: 22, fontWeight: 700, letterSpacing: 1, minHeight: 28 }}>
        {value ?? "--"}
      </div>
      {!published && (
        <div style={{ fontSize: 11, opacity: 0.85 }}>
          {lastAvailable ? (
            <>Latest available: <b>{lastAvailable.value}</b> ({lastAvailable.date})</>
          ) : (
            "Waiting for result"
          )}
        </div>
      )}
      <Link
        to={`/results?date=${result.drawDate}&drawTime=${encodeURIComponent(result.drawTime)}`}
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          marginTop: 6,
          textDecoration: "none",
          color: "inherit",
          fontSize: 13,
          fontWeight: 600,
        }}
      >
        <span>Lottery Result</span>
        <span
          style={{
            display: "inline-flex",
            alignItems: "center",
            justifyContent: "center",
            width: 22,
            height: 22,
            borderRadius: "50%",
            background: "var(--accent)",
            color: "#fff",
            flexShrink: 0,
          }}
        >
          <ChevronRightIcon size={14} />
        </span>
      </Link>
    </div>
  );
}
