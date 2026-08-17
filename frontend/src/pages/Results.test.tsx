import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { describe, it, expect, vi } from "vitest";
import Results from "./Results";
import { resultsApi } from "../api/results";

vi.mock("../api/results");
const mockedResults = vi.mocked(resultsApi);

describe("Results page", () => {
  it("shows an empty state when there are no results", async () => {
    mockedResults.list.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });

    render(<MemoryRouter><Results /></MemoryRouter>);

    expect(await screen.findByText("No results found.")).toBeInTheDocument();
  });

  it("re-fetches with the selected draw filter", async () => {
    mockedResults.list.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    render(<MemoryRouter><Results /></MemoryRouter>);

    await waitFor(() => expect(mockedResults.list).toHaveBeenCalledWith(
      expect.objectContaining({ drawTime: undefined })
    ));

    const select = screen.getByRole("combobox");
    await userEvent.selectOptions(select, "6 PM");

    await waitFor(() => expect(mockedResults.list).toHaveBeenCalledWith(
      expect.objectContaining({ drawTime: "6 PM" })
    ));
  });
});
