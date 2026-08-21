import { Link } from "react-router-dom";
import type { ResultDto } from "../api/types";
import { ChevronRightIcon } from "./icons";

export default function ResultCard({
  result, lastAvailable,
}: { result: ResultDto; lastAvailable?: { value: string; date: string } | null }) {
  const published = result.status === "Published";
  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        gap: 6,
        background: "var(--surface)",
        borderRadius: "var(--radius)",
        padding: "14px 14px 12px",
        boxShadow: "var(--shadow-sm)",
      }}
    >
      <div style={{ fontSize: 20, fontWeight: 800 }}>{result.drawTime}</div>
      <div style={{ fontSize: 22, fontWeight: 700, letterSpacing: 1, minHeight: 28 }}>
        {published ? result.resultValue : "--"}
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
