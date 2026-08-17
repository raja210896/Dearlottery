import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { describe, it, expect, vi } from "vitest";
import PredictionHistory from "./PredictionHistory";
import { predictionsApi } from "../api/predictions";

vi.mock("../api/predictions");
const mockedPredictions = vi.mocked(predictionsApi);

const candidate = {
  value: "27", modelScore: 82,
  breakdown: { frequencyScore: 90, recencyScore: 80, digitScore: 70, repeatScore: 60, patternScore: 50 },
  historicalFrequency: 5, recentFrequency: 2, reason: "appeared 5x historically",
};

function basePrediction(overrides: Record<string, unknown> = {}) {
  return {
    id: 1, drawDate: "2026-01-01", drawTime: "1 PM", digitLength: 2,
    candidates: [candidate], generatedAt: "2026-01-01T00:00:00Z",
    actualResult: null, isEvaluated: false, matchFound: null, matchPosition: null, evaluatedAt: null,
    exactMatch: null, last3Match: null, last2Match: null,
    ...overrides,
  };
}

function renderPage() {
  return render(<MemoryRouter><PredictionHistory /></MemoryRouter>);
}

describe("PredictionHistory page", () => {
  it("shows an empty state when there is no history", async () => {
    mockedPredictions.history.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    renderPage();
    expect(await screen.findByText("No predictions saved yet.")).toBeInTheDocument();
  });

  it("renders match status badges for evaluated and pending predictions", async () => {
    mockedPredictions.history.mockResolvedValue({
      items: [
        basePrediction({ id: 1, actualResult: "27", isEvaluated: true, matchFound: true, matchPosition: 1, evaluatedAt: "2026-01-01T13:05:00Z", exactMatch: false, last3Match: false, last2Match: true }),
        basePrediction({ id: 2, drawDate: "2026-01-02", drawTime: "6 PM" }),
      ],
      totalCount: 2, page: 1, pageSize: 20,
    });

    renderPage();

    expect(await screen.findByText("Match")).toBeInTheDocument();
    expect(screen.getAllByText("Pending").length).toBeGreaterThan(0);
    expect(screen.getByText("Exact: No match")).toBeInTheDocument();
    expect(screen.getByText("Last-2: Match")).toBeInTheDocument();
  });

  it("re-fetches when the match-status filter changes", async () => {
    mockedPredictions.history.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    renderPage();
    await screen.findByText("No predictions saved yet.");

    const selects = screen.getAllByRole("combobox");
    const matchSelect = selects[selects.length - 1];
    await userEvent.selectOptions(matchSelect, "matched");

    await waitFor(() => expect(mockedPredictions.history).toHaveBeenCalledWith(
      expect.objectContaining({ matchStatus: "matched" })
    ));
  });

  it("expands to show all candidates with historical/recent frequency and reason", async () => {
    const secondCandidate = { ...candidate, value: "43", modelScore: 70, historicalFrequency: 3, recentFrequency: 1, reason: "overdue" };
    mockedPredictions.history.mockResolvedValue({
      items: [basePrediction({ candidates: [candidate, secondCandidate] })],
      totalCount: 1, page: 1, pageSize: 20,
    });

    renderPage();
    await userEvent.click(await screen.findByRole("button", { name: "Show all 2 candidates" }));

    expect(screen.getByText("43")).toBeInTheDocument();
    expect(screen.getAllByText(/Historical Frequency/).length).toBe(2);
    expect(screen.getByText("overdue")).toBeInTheDocument();
  });
});
