export const REWARD_TYPES = [
  "seed",
  "elementCore",
  "noaMemory",
  "blessingTicket",
] as const;
export const RANDOM_REWARD_TYPES = [
  "noaMemory",
  "blessingTicket",
] as const;
export const RESULT_TYPES = ["clear", "fail"] as const;

export type RewardType = typeof REWARD_TYPES[number];
export type RandomRewardType = typeof RANDOM_REWARD_TYPES[number];
export type PurifyResultType = typeof RESULT_TYPES[number];

export interface RewardEntry {
  rewardType: RewardType;
  amount: number;
}

export interface FlowReward {
  rewardType: RewardType;
  amountPerCompletedFlow: number;
}

export interface RewardChanceThreshold {
  minimum: number;
  chancePercent: number;
}

export interface RewardAmountThreshold {
  minimum: number;
  amount: number;
}

export interface ResultRewardConfig {
  rewardType: RewardType;
  failCompletedFlowAmounts: RewardAmountThreshold[];
  clearAmount: number;
}

export interface RandomRewardConfig {
  rewardType: RandomRewardType;
  amount: number;
  failCompletedFlowChances: RewardChanceThreshold[];
  clearForestHpChances: RewardChanceThreshold[];
}

export type RewardRolls = Partial<Record<RandomRewardType, number>>;

export interface PurifyRewardConfig {
  stageId: string;
  enabled: boolean;
  configVersion: number;
  maxFlow: number;
  flowReward: FlowReward;
  clearRewards: RewardEntry[];
  firstClearRewards: RewardEntry[];
  resultRewards: ResultRewardConfig[];
  randomRewards: RandomRewardConfig[];
  forestHpBonusEnabled: boolean;
}

export interface StartPurifyRequest {
  stageId: string;
}

export interface StartPurifyResponse {
  runId: string;
  stageId: string;
  configVersion: number;
  expiresAtUtcMillis: number;
  availableNoaIds: string[];
}

export interface SettlePurifyRequest {
  runId: string;
  resultType: PurifyResultType;
  completedFlow: number;
  forestHp: number;
}

export interface SettlePurifyResponse {
  runId: string;
  stageId: string;
  resultType: PurifyResultType;
  completedFlow: number;
  forestHp: number;
  isNewBest: boolean;
  bestFlow: number;
  isFirstClear: boolean;
  rewards: RewardEntry[];
}
