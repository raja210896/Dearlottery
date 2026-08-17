import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { describe, it, expect, vi, beforeEach } from "vitest";
import AdminResults from "./AdminResults";
import { adminApi, adminAuth } from "../../api/admin";

vi.mock("../../api/admin", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../../api/admin")>();
  return {
    ...actual,
    adminApi: {
      listResults: vi.fn(),
      createResult: vi.fn(),
      updateResult: vi.fn(),
      deleteResult: vi.fn(),
    },
  };
});

const mockedApi = vi.mocked(adminApi);

function renderPage() {
  return render(<MemoryRouter><AdminResults /></MemoryRouter>);
}

describe("AdminResults page", () => {
  beforeEach(() => {
    vi.spyOn(adminAuth, "isLoggedIn").mockReturnValue(true);
    mockedApi.listResults.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 15 });
  });

  it("shows a validation error when required fields are missing", async () => {
    renderPage();
    await screen.findByText("No results yet.");

    await userEvent.click(screen.getByRole("button", { name: "Save" }));

    expect(await screen.findByText(/required/i)).toBeInTheDocument();
    expect(mockedApi.createResult).not.toHaveBeenCalled();
  });

  it("submits a new result and shows the candidate comparison note", async () => {
    mockedApi.createResult.mockResolvedValue({
      result: { id: 1, drawDate: "2026-01-01", drawTime: "1 PM", resultValue: "27", status: "Published", lastUpdated: null },
      matchedCandidate: true,
    });

    renderPage();
    await screen.findByText("No results yet.");

    const dateInput = document.querySelector('input[type="date"]') as HTMLInputElement;
    await userEvent.type(dateInput, "2026-01-01");
    await userEvent.type(screen.getByPlaceholderText("e.g. 27"), "27");
    await userEvent.click(screen.getByRole("button", { name: "Save" }));

    expect(await screen.findByText(/matched one of the pre-draw Statistical Candidates/)).toBeInTheDocument();
    expect(mockedApi.createResult).toHaveBeenCalledWith({ drawDate: "2026-01-01", drawTime: "1 PM", resultValue: "27" });
  });
});
