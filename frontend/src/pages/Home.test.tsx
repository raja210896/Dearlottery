import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, it, expect, vi } from "vitest";
import Home from "./Home";
import { resultsApi } from "../api/results";
import { analysisApi } from "../api/analysis";
import { predictionsApi } from "../api/predictions";

vi.mock("../api/results");
vi.mock("../api/analysis");
vi.mock("../api/predictions");

const mockedResults = vi.mocked(resultsApi);
const mockedAnalysis = vi.mocked(analysisApi);
const mockedPredictions = vi.mocked(predictionsApi);

function renderHome() {
  return render(<MemoryRouter><Home /></MemoryRouter>);
}

describe("Home", () => {
  it("shows today's results, quick analysis, and the disclaimer once data loads", async () => {
    mockedResults.today.mockResolvedValue([
      { id: 1, drawDate: "2026-08-16", drawTime: "1 PM", resultValue: "27", status: "Published", lastUpdated: "2026-08-16T13:05:00Z" },
      { id: 0, drawDate: "2026-08-16", drawTime: "6 PM", resultValue: null, status: "Pending", lastUpdated: null },
      { id: 0, drawDate: "2026-08-16", drawTime: "8 PM", resultValue: null, status: "Pending", lastUpdated: null },
    ]);
    mockedResults.list.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 5 });
    mockedAnalysis.overview.mockResolvedValue({
      frequency: {
        fullNumberFrequency: [], lastDigitFrequency: [], last2DigitFrequency: [], last3DigitFrequency: [],
        hotNumbers: [{ value: "27", count: 3 }], coldNumbers: [{ value: "05", count: 1 }], sampleSize: 10,
      },
      recency: [],
      patterns: { oddCount: 5, evenCount: 5, digitSumDistribution: {}, repeatedDigitCount: 0, recentRepeats: [] },
    });
    mockedPredictions.history.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 1 });

    renderHome();

    expect(await screen.findByText("1 PM")).toBeInTheDocument();
    expect(screen.getAllByText("Waiting for result")).toHaveLength(2);
    await waitFor(() => expect(screen.getByText("Hot Numbers")).toBeInTheDocument());
    expect(screen.getByText(/Statistical analysis only/)).toBeInTheDocument();
  });

  it("shows an error state with retry when the API call fails", async () => {
    mockedResults.today.mockRejectedValue(new Error("network down"));
    mockedResults.list.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 5 });
    mockedAnalysis.overview.mockResolvedValue({
      frequency: { fullNumberFrequency: [], lastDigitFrequency: [], last2DigitFrequency: [], last3DigitFrequency: [], hotNumbers: [], coldNumbers: [], sampleSize: 0 },
      recency: [],
      patterns: { oddCount: 0, evenCount: 0, digitSumDistribution: {}, repeatedDigitCount: 0, recentRepeats: [] },
    });
    mockedPredictions.history.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 1 });

    renderHome();

    expect(await screen.findByRole("button", { name: /retry/i })).toBeInTheDocument();
  });

  it("shows the latest prediction with status, top candidate detail, and match badges when evaluated", async () => {
    mockedResults.today.mockResolvedValue([
      { id: 1, drawDate: "2026-08-16", drawTime: "1 PM", resultValue: "27", status: "Published", lastUpdated: "2026-08-16T13:05:00Z" },
      { id: 0, drawDate: "2026-08-16", drawTime: "6 PM", resultValue: null, status: "Pending", lastUpdated: null },
      { id: 0, drawDate: "2026-08-16", drawTime: "8 PM", resultValue: null, status: "Pending", lastUpdated: null },
    ]);
    mockedResults.list.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 15 });
    mockedAnalysis.overview.mockResolvedValue({
      frequency: { fullNumberFrequency: [], lastDigitFrequency: [], last2DigitFrequency: [], last3DigitFrequency: [], hotNumbers: [], coldNumbers: [], sampleSize: 0 },
      recency: [],
      patterns: { oddCount: 0, evenCount: 0, digitSumDistribution: {}, repeatedDigitCount: 0, recentRepeats: [] },
    });
    mockedPredictions.history.mockResolvedValue({
      items: [{
        id: 1, drawDate: "2026-08-16", drawTime: "1 PM", digitLength: 2,
        candidates: [{
          value: "27", modelScore: 82,
          breakdown: { frequencyScore: 90, recencyScore: 80, digitScore: 70, repeatScore: 60, patternScore: 50 },
          historicalFrequency: 5, recentFrequency: 2, reason: "appeared 5x historically",
        }],
        generatedAt: "2026-08-16T00:00:00Z", actualResult: "27", isEvaluated: true,
        matchFound: true, matchPosition: 1, evaluatedAt: "2026-08-16T13:05:00Z",
        exactMatch: false, last3Match: false, last2Match: true,
      }],
      totalCount: 1, page: 1, pageSize: 1,
    });

    renderHome();

    expect(await screen.findByText("Evaluated")).toBeInTheDocument();
    expect(screen.getByText(/Top candidate:/)).toBeInTheDocument();
    expect(screen.getByText("Last-2: Match")).toBeInTheDocument();
    expect(screen.getByText("Exact: No match")).toBeInTheDocument();
  });
});
