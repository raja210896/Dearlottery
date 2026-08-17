export default function About() {
  return (
    <div className="container">
      <h1 className="section-title" style={{ marginTop: 4 }}>About</h1>
      <div className="card" style={{ display: "flex", flexDirection: "column", gap: 10, fontSize: 14, lineHeight: 1.6 }}>
        <p>LotteryAnalytics tracks published lottery draw results and provides statistical pattern analysis: frequency, recency, and digit-pattern breakdowns.</p>
        <p>All analysis — including "Statistical Candidates" and "Model Score" — reflects historical pattern weighting only. It is not a prediction of future outcomes and does not guarantee winnings.</p>
      </div>
      <p className="disclaimer">Statistical analysis only. Past results do not guarantee future results.</p>
    </div>
  );
}
