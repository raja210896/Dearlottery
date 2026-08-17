// Minimal inline SVG icon set — no icon library dependency.
type IconProps = { size?: number };

const base = (size = 20) => ({
  width: size,
  height: size,
  viewBox: "0 0 24 24",
  fill: "none",
  stroke: "currentColor",
  strokeWidth: 2,
  strokeLinecap: "round" as const,
  strokeLinejoin: "round" as const,
});

export const HomeIcon = ({ size }: IconProps) => (
  <svg {...base(size)}><path d="M3 10.5 12 3l9 7.5" /><path d="M5 9.5V21h14V9.5" /></svg>
);
export const ResultsIcon = ({ size }: IconProps) => (
  <svg {...base(size)}><rect x="4" y="3" width="16" height="18" rx="2" /><path d="M8 8h8M8 12h8M8 16h5" /></svg>
);
export const AnalysisIcon = ({ size }: IconProps) => (
  <svg {...base(size)}><path d="M4 20V10M12 20V4M20 20v-7" /></svg>
);
export const HistoryIcon = ({ size }: IconProps) => (
  <svg {...base(size)}><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 3" /></svg>
);
export const SettingsIcon = ({ size }: IconProps) => (
  <svg {...base(size)}><circle cx="12" cy="12" r="3" /><path d="M19.4 15a1.7 1.7 0 0 0 .3 1.9l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.9-.3 1.7 1.7 0 0 0-1 1.6V21a2 2 0 1 1-4 0v-.2a1.7 1.7 0 0 0-1-1.6 1.7 1.7 0 0 0-1.9.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.9 1.7 1.7 0 0 0-1.6-1H3a2 2 0 1 1 0-4h.2a1.7 1.7 0 0 0 1.6-1 1.7 1.7 0 0 0-.3-1.9l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.9.3H9a1.7 1.7 0 0 0 1-1.6V3a2 2 0 1 1 4 0v.2a1.7 1.7 0 0 0 1 1.6 1.7 1.7 0 0 0 1.9-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.9V9a1.7 1.7 0 0 0 1.6 1H21a2 2 0 1 1 0 4h-.2a1.7 1.7 0 0 0-1.6 1Z" /></svg>
);
export const FlameIcon = ({ size }: IconProps) => (
  <svg {...base(size)}><path d="M12 2s-6 5.5-6 10.5A6 6 0 0 0 12 22a6 6 0 0 0 6-9.5C18 12.5 15 14 15 14c1-3-1-8-3-12Z" /></svg>
);
export const SnowflakeIcon = ({ size }: IconProps) => (
  <svg {...base(size)}><path d="M12 2v20M4.9 4.9l14.2 14.2M19.1 4.9 4.9 19.1M2 12h20M6 6l3 3M18 18l-3-3M18 6l-3 3M6 18l3-3" /></svg>
);
export const ChevronRightIcon = ({ size }: IconProps) => (
  <svg {...base(size)}><path d="m9 6 6 6-6 6" /></svg>
);
export const RefreshIcon = ({ size }: IconProps) => (
  <svg {...base(size)}><path d="M21 12a9 9 0 1 1-2.6-6.4M21 3v6h-6" /></svg>
);
