export interface ApiResponse<T> {
  success: boolean;
  message?: string | null;
  data?: T;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ResultDto {
  id: number;
  drawDate: string;
  drawTime: string;
  resultValue: string | null;
  status: "Pending" | "Published";
  lastUpdated: string | null;
}

export interface FrequencyEntry {
  value: string;
  count: number;
}

export interface FrequencySnapshot {
  fullNumberFrequency: FrequencyEntry[];
  lastDigitFrequency: FrequencyEntry[];
  last2DigitFrequency: FrequencyEntry[];
  last3DigitFrequency: FrequencyEntry[];
  hotNumbers: FrequencyEntry[];
  coldNumbers: FrequencyEntry[];
  sampleSize: number;
}

export interface RecencyEntry {
  value: string;
  lastAppearance: string | null;
  drawsSinceAppearance: number;
  recentFrequency: number;
}

export interface RecentRepeat {
  value: string;
  firstDate: string;
  secondDate: string;
  drawsApart: number;
}

export interface PatternStats {
  oddCount: number;
  evenCount: number;
  digitSumDistribution: Record<string, number>;
  repeatedDigitCount: number;
  recentRepeats: RecentRepeat[];
}

export interface AnalysisOverview {
  frequency: FrequencySnapshot;
  recency: RecencyEntry[];
  patterns: PatternStats;
}

export interface ScoreBreakdown {
  frequencyScore: number;
  recencyScore: number;
  digitScore: number;
  repeatScore: number;
  patternScore: number;
}

export interface Candidate {
  value: string;
  modelScore: number;
  breakdown: ScoreBreakdown;
  historicalFrequency: number;
  recentFrequency: number;
  reason: string;
}

export interface CandidateResponse {
  drawTime: string;
  candidates: Candidate[];
  disclaimer: string;
}

export interface BacktestDrawResult {
  drawDate: string;
  actualValue: string;
  hit: boolean;
  topScore: number;
  top1: boolean;
  top5: boolean;
  top10: boolean;
}

export interface BacktestResponse {
  totalTested: number;
  drawsTested: number;
  hits: number;
  modelHitRate: number;
  randomBaselineRate: number;
  modelVsRandomDifference: number;
  top1Matches: number;
  top5Matches: number;
  top10Matches: number;
  top1MatchRate: number;
  top5MatchRate: number;
  top10MatchRate: number;
  draws: BacktestDrawResult[];
  disclaimer: string;
}

export interface DrawTimeCount {
  drawTime: string;
  count: number;
}

export interface DataQualitySummary {
  totalDraws: number;
  earliestDate: string | null;
  latestDate: string | null;
  countsByDrawTime: DrawTimeCount[];
  missingSlotCount: number;
  sampleMissingDates: string[];
  duplicateCount: number;
}

export interface ModelHitRateResult {
  drawsTested: number;
  hits: number;
  hitRate: number;
}

export interface ModelDigitResults {
  exact: ModelHitRateResult;
  last3: ModelHitRateResult;
  last2: ModelHitRateResult;
}

export interface DrawTimeModelComparison {
  drawTime: string;
  multiFactor: ModelDigitResults;
  frequencyOnly: ModelDigitResults;
  recencyOnly: ModelDigitResults;
  random: ModelDigitResults;
}

export interface ModelComparisonResponse {
  byDrawTime: DrawTimeModelComparison[];
  disclaimer: string;
}

export interface MultiDigitBacktestSummary {
  exact: BacktestResponse;
  last2: BacktestResponse;
  last3: BacktestResponse;
  disclaimer: string;
}

export interface PositionFrequency {
  position: number;
  digits: FrequencyEntry[];
}

export interface RecentVsHistoricalEntry {
  value: string;
  historicalCount: number;
  recentCount: number;
}

export interface DigitAnalysis {
  digitFrequency: FrequencyEntry[];
  hotDigits: FrequencyEntry[];
  coldDigits: FrequencyEntry[];
  positionFrequency: PositionFrequency[];
  digitPairFrequency: FrequencyEntry[];
  recentVsHistorical: RecentVsHistoricalEntry[];
  sampleSize: number;
}

export interface ImportRowError {
  rowNumber: number;
  reason: string;
}

export interface ImportSummary {
  totalRows: number;
  imported: number;
  skipped: number;
  duplicates: number;
  invalid: number;
  errors: ImportRowError[];
}

export const DRAW_TIMES = ["1 PM", "6 PM", "8 PM"] as const;
export type DrawTime = (typeof DRAW_TIMES)[number];

export interface PredictionHistoryDto {
  id: number;
  drawDate: string;
  drawTime: string;
  digitLength: number;
  candidates: Candidate[];
  generatedAt: string;
  actualResult: string | null;
  isEvaluated: boolean;
  matchFound: boolean | null;
  matchPosition: number | null;
  evaluatedAt: string | null;
  exactMatch: boolean | null;
  last3Match: boolean | null;
  last2Match: boolean | null;
}

export interface RecentPredictionOutcome {
  drawDate: string;
  drawTime: string;
  matchFound: boolean;
}

export interface PredictionPerformanceDto {
  totalPredictions: number;
  evaluatedPredictions: number;
  matches: number;
  matchRate: number;
  randomBaselineRate: number;
  recentPerformance: RecentPredictionOutcome[];
  disclaimer: string;
}
