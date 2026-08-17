import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, it, expect, vi, beforeEach } from "vitest";
import AdminImport from "./AdminImport";
import { adminAuth } from "../../api/admin";
import { importApi } from "../../api/import";

vi.mock("../../api/import", () => ({
  importApi: { csv: vi.fn(), json: vi.fn() },
}));

function renderPage() {
  return render(<MemoryRouter><AdminImport /></MemoryRouter>);
}

describe("AdminImport page", () => {
  beforeEach(() => {
    vi.spyOn(adminAuth, "isLoggedIn").mockReturnValue(true);
  });

  it("renders the CSV and JSON upload controls", () => {
    renderPage();
    expect(screen.getByText("CSV Upload")).toBeInTheDocument();
    expect(screen.getByText("JSON Upload")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Import CSV" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Import JSON" })).toBeInTheDocument();
  });

  it("renders the import summary after a successful import", async () => {
    const file = new File(["DrawDate,DrawTime,ResultValue\n2026-08-01,1 PM,42\n"], "data.csv", { type: "text/csv" });
    vi.mocked(importApi.csv).mockResolvedValue({
      totalRows: 1, imported: 1, skipped: 0, duplicates: 0, invalid: 0, errors: [],
    });

    renderPage();
    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
    Object.defineProperty(fileInput, "files", { value: [file] });

    const { default: userEvent } = await import("@testing-library/user-event");
    await userEvent.click(screen.getByRole("button", { name: "Import CSV" }));

    expect(await screen.findByText("Import Summary")).toBeInTheDocument();
    expect(screen.getByText("Total Rows")).toBeInTheDocument();
  });
});
