import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, it, expect, vi } from "vitest";
import { LoadingSkeleton, ErrorState, EmptyState } from "./StateViews";

describe("StateViews", () => {
  it("renders the requested number of skeleton rows", () => {
    const { container } = render(<LoadingSkeleton rows={4} />);
    expect(container.querySelectorAll(".skeleton")).toHaveLength(4);
  });

  it("renders an error message and calls onRetry when clicked", async () => {
    const onRetry = vi.fn();
    render(<ErrorState message="Unable to load." onRetry={onRetry} />);

    expect(screen.getByText("Unable to load.")).toBeInTheDocument();
    await userEvent.click(screen.getByRole("button", { name: /retry/i }));
    expect(onRetry).toHaveBeenCalledTimes(1);
  });

  it("renders an empty state message", () => {
    render(<EmptyState message="Nothing here." />);
    expect(screen.getByText("Nothing here.")).toBeInTheDocument();
  });
});
