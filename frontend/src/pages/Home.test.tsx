import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, it, expect, vi } from "vitest";
import Home from "./Home";
import { resultsApi } from "../api/results";
import { analysisApi } from "../api/analysis";

vi.mock("../api/results");
vi.mock("../api/analysis");

const mockedResults = vi.mocked(resultsApi);
const mockedAnalysis = vi.mocked(analysisApi);

function renderHome() {
  return render(<MemoryRouter><Home /></MemoryRouter>);
}

describe("Home", () => {
  it("shows the latest available date's results, quick analysis, and the disclaimer once data loads", async () => {
    mockedResults.list.mockResolvedValue({
      items: [
        { id: 1, drawDate: "2026-08-16", drawTime: "1 PM", resultValue: "27", status: "Published", lastUpdated: "2026-08-16T13:05:00Z" },
      ],
      totalCount: 1, page: 1, pageSize: 15,
    });
    mockedAnalysis.overview.mockResolvedValue({
      frequency: {
        fullNumberFrequency: [], lastDigitFrequency: [], last2DigitFrequency: [], last3DigitFrequency: [],
        hotNumbers: [{ value: "27", count: 3 }], coldNumbers: [{ value: "05", count: 1 }], sampleSize: 10,
      },
      recency: [],
      patterns: { oddCount: 5, evenCount: 5, digitSumDistribution: {}, repeatedDigitCount: 0, recentRepeats: [] },
    });
    mockedAnalysis.seasonal.mockResolvedValue({
      targetDate: "2026-08-16",
      draws: [
        { drawTime: "1 PM", sameDateLastYear: "2025-08-16", sameDateLastYearValue: null, currentMonthFrequency: [], currentMonthSampleSize: 0 },
        { drawTime: "6 PM", sameDateLastYear: "2025-08-16", sameDateLastYearValue: null, currentMonthFrequency: [], currentMonthSampleSize: 0 },
        { drawTime: "8 PM", sameDateLastYear: "2025-08-16", sameDateLastYearValue: null, currentMonthFrequency: [], currentMonthSampleSize: 0 },
      ],
      disclaimer: "Statistical pattern only.",
    });
    mockedAnalysis.dataQuality.mockResolvedValue({
      totalDraws: 609, earliestDate: "2025-01-01", latestDate: "2025-08-01",
      countsByDrawTime: [], missingSlotCount: 30, sampleMissingDates: [], duplicateCount: 0,
    });

    renderHome();

    expect(await screen.findAllByText("1 PM")).toHaveLength(2); // one Latest Available Results card, one Last Year This Date tile
    expect(screen.getAllByText("Waiting for result")).toHaveLength(2);
    await waitFor(() => expect(screen.getByText("Hot Numbers")).toBeInTheDocument());
    expect(screen.getByText(/Statistical analysis only/)).toBeInTheDocument();
  });

  it("shows an error state with retry when the API call fails", async () => {
    mockedResults.list.mockRejectedValue(new Error("network down"));
    mockedAnalysis.overview.mockResolvedValue({
      frequency: { fullNumberFrequency: [], lastDigitFrequency: [], last2DigitFrequency: [], last3DigitFrequency: [], hotNumbers: [], coldNumbers: [], sampleSize: 0 },
      recency: [],
      patterns: { oddCount: 0, evenCount: 0, digitSumDistribution: {}, repeatedDigitCount: 0, recentRepeats: [] },
    });
    mockedAnalysis.seasonal.mockResolvedValue({
      targetDate: "2026-08-16",
      draws: [
        { drawTime: "1 PM", sameDateLastYear: "2025-08-16", sameDateLastYearValue: null, currentMonthFrequency: [], currentMonthSampleSize: 0 },
        { drawTime: "6 PM", sameDateLastYear: "2025-08-16", sameDateLastYearValue: null, currentMonthFrequency: [], currentMonthSampleSize: 0 },
        { drawTime: "8 PM", sameDateLastYear: "2025-08-16", sameDateLastYearValue: null, currentMonthFrequency: [], currentMonthSampleSize: 0 },
      ],
      disclaimer: "Statistical pattern only.",
    });
    mockedAnalysis.dataQuality.mockResolvedValue({
      totalDraws: 609, earliestDate: "2025-01-01", latestDate: "2025-08-01",
      countsByDrawTime: [], missingSlotCount: 30, sampleMissingDates: [], duplicateCount: 0,
    });

    renderHome();

    // Both the hero and the Recent Results section read from the same failed `list` call.
    expect((await screen.findAllByRole("button", { name: /retry/i })).length).toBeGreaterThan(0);
  });
});
