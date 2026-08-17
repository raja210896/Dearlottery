import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { describe, it, expect, vi } from "vitest";
import Analysis from "./Analysis";
import { analysisApi } from "../api/analysis";

vi.mock("../api/analysis");
const mockedAnalysis = vi.mocked(analysisApi);

describe("Analysis page", () => {
  it("switches tabs and loads candidate data for the Candidates tab", async () => {
    mockedAnalysis.overview.mockResolvedValue({
      frequency: { fullNumberFrequency: [], lastDigitFrequency: [], last2DigitFrequency: [], last3DigitFrequency: [], hotNumbers: [], coldNumbers: [], sampleSize: 0 },
      recency: [],
      patterns: { oddCount: 0, evenCount: 0, digitSumDistribution: {}, repeatedDigitCount: 0, recentRepeats: [] },
    });
    mockedAnalysis.candidates.mockResolvedValue({
      drawTime: "All draws",
      candidates: [{
        value: "27", modelScore: 82,
        breakdown: { frequencyScore: 90, recencyScore: 80, digitScore: 70, repeatScore: 60, patternScore: 50 },
        historicalFrequency: 5, recentFrequency: 2, reason: "appeared 5x historically",
      }],
      disclaimer: "Statistical Candidates only.",
    });

    render(<MemoryRouter><Analysis /></MemoryRouter>);

    await userEvent.click(screen.getByRole("button", { name: "Candidates" }));

    expect(await screen.findByText("27")).toBeInTheDocument();
    expect(screen.getByText("82")).toBeInTheDocument();
    expect(mockedAnalysis.candidates).toHaveBeenCalled();
  });

  it("renders backtest performance metrics and refetches on preset filter change", async () => {
    mockedAnalysis.dataQuality.mockResolvedValue({
      totalDraws: 50, earliestDate: "2026-01-01", latestDate: "2026-02-19",
      countsByDrawTime: [{ drawTime: "1 PM", count: 50 }],
      missingSlotCount: 0, sampleMissingDates: [], duplicateCount: 0,
    });
    const sampleBacktest = {
      totalTested: 30, drawsTested: 30, hits: 3, modelHitRate: 0.1, randomBaselineRate: 0.1,
      modelVsRandomDifference: 0, top1Matches: 2, top5Matches: 6, top10Matches: 10,
      top1MatchRate: 0.067, top5MatchRate: 0.2, top10MatchRate: 0.333,
      draws: [], disclaimer: "Historical Match Rate only — not a winning probability.",
    };
    mockedAnalysis.backtest.mockResolvedValue(sampleBacktest);
    mockedAnalysis.backtestMulti.mockResolvedValue({
      exact: sampleBacktest, last2: sampleBacktest, last3: sampleBacktest,
      disclaimer: "Historical Match Rate only — not a winning probability.",
    });

    render(<MemoryRouter><Analysis /></MemoryRouter>);
    await userEvent.click(screen.getByRole("button", { name: "Backtesting" }));

    expect(await screen.findByText("Historical Model Performance")).toBeInTheDocument();
    expect(screen.getByText("Top-N Match Rates")).toBeInTheDocument();
    expect(screen.getByText("6.7%")).toBeInTheDocument(); // Top 1
    expect(mockedAnalysis.backtest).toHaveBeenCalledWith(expect.objectContaining({ drawCount: 30 }));

    await userEvent.click(screen.getByRole("button", { name: "60 draws" }));

    await waitFor(() => expect(mockedAnalysis.backtest).toHaveBeenCalledWith(expect.objectContaining({ drawCount: 60 })));
  });
});
