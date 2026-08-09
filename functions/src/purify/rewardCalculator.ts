import type {
  PurifyRewardConfig,
  RandomRewardConfig,
  RewardEntry,
  RewardRolls,
  RewardType,
  ResultRewardConfig,
} from "./types";

export function calculateRewards(
  config: PurifyRewardConfig,
  completedFlow: number,
  isClear: boolean,
  isFirstClear: boolean,
  forestHp = 0,
  rewardRolls: RewardRolls = {}
): RewardEntry[] {
  const amountByType = new Map<RewardType, number>();
  addReward(
    amountByType,
    config.flowReward.rewardType,
    completedFlow * config.flowReward.amountPerCompletedFlow
  );

  if (isClear) {
    addRewards(amountByType, config.clearRewards);
  }

  if (isClear && isFirstClear) {
    addRewards(amountByType, config.firstClearRewards);
  }

  addResultRewards(
    amountByType,
    config.resultRewards ?? [],
    completedFlow,
    isClear
  );

  addRandomRewards(
    amountByType,
    config.randomRewards ?? [],
    completedFlow,
    forestHp,
    isClear,
    rewardRolls
  );

  return Array.from(amountByType.entries())
    .filter(([, amount]) => amount > 0)
    .map(([rewardType, amount]) => ({rewardType, amount}));
}

function addResultRewards(
  amountByType: Map<RewardType, number>,
  resultRewards: ResultRewardConfig[],
  completedFlow: number,
  isClear: boolean
): void {
  for (const reward of resultRewards) {
    const amount = isClear ?
      reward.clearAmount :
      getAmount(reward.failCompletedFlowAmounts, completedFlow);
    addReward(amountByType, reward.rewardType, amount);
  }
}

function getAmount(
  thresholds: {minimum: number; amount: number}[],
  value: number
): number {
  let amount = 0;
  for (const threshold of thresholds) {
    if (value < threshold.minimum) {
      break;
    }

    amount = threshold.amount;
  }

  return amount;
}

function addRandomRewards(
  amountByType: Map<RewardType, number>,
  randomRewards: RandomRewardConfig[],
  completedFlow: number,
  forestHp: number,
  isClear: boolean,
  rewardRolls: RewardRolls
): void {
  for (const reward of randomRewards) {
    const thresholds = isClear ?
      reward.clearForestHpChances :
      reward.failCompletedFlowChances;
    const value = isClear ? forestHp : completedFlow;
    const chancePercent = getChancePercent(thresholds, value);
    const roll = rewardRolls[reward.rewardType];
    if (roll === undefined || roll < 0 || roll >= chancePercent) {
      continue;
    }

    addReward(amountByType, reward.rewardType, reward.amount);
  }
}

function getChancePercent(
  thresholds: {minimum: number; chancePercent: number}[],
  value: number
): number {
  let chancePercent = 0;
  for (const threshold of thresholds) {
    if (value < threshold.minimum) {
      break;
    }

    chancePercent = threshold.chancePercent;
  }

  return chancePercent;
}

function addRewards(
  amountByType: Map<RewardType, number>,
  rewards: RewardEntry[]
): void {
  for (const reward of rewards) {
    addReward(amountByType, reward.rewardType, reward.amount);
  }
}

function addReward(
  amountByType: Map<RewardType, number>,
  rewardType: RewardType,
  amount: number
): void {
  amountByType.set(
    rewardType,
    (amountByType.get(rewardType) ?? 0) + amount
  );
}
