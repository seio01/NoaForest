import {HttpsError} from "firebase-functions/v2/https";
import {
  PurifyResultType,
  PurifyRewardConfig,
  RANDOM_REWARD_TYPES,
  RandomRewardConfig,
  RandomRewardType,
  REWARD_TYPES,
  RESULT_TYPES,
  RewardEntry,
  RewardType,
  ResultRewardConfig,
  SettlePurifyRequest,
  SettlePurifyResponse,
  StartPurifyRequest,
} from "./types";

const STAGE_ID_PATTERN = /^stage_\d{3}$/;
const RUN_ID_PATTERN = /^[A-Za-z0-9_-]{1,128}$/;

export function requireAuthenticatedUserId(
  userId: string | undefined
): string {
  if (!userId) {
    throw new HttpsError(
      "unauthenticated",
      "Firebase Authentication is required."
    );
  }

  return userId;
}

export function parseStartPurifyRequest(
  value: unknown
): StartPurifyRequest {
  const data = requireRecord(value, "Request data");
  const stageId = requireString(data.stageId, "stageId");

  if (!STAGE_ID_PATTERN.test(stageId)) {
    throw new HttpsError("invalid-argument", "stageId is invalid.");
  }

  return {stageId};
}

export function parseSettlePurifyRequest(
  value: unknown
): SettlePurifyRequest {
  const data = requireRecord(value, "Request data");
  const runId = requireString(data.runId, "runId");
  const resultType = requireResultType(data.resultType);
  const completedFlow = requireNonNegativeInteger(
    data.completedFlow,
    "completedFlow"
  );
  const forestHp = requireNonNegativeInteger(data.forestHp, "forestHp");

  if (!RUN_ID_PATTERN.test(runId)) {
    throw new HttpsError("invalid-argument", "runId is invalid.");
  }

  return {runId, resultType, completedFlow, forestHp};
}

export function parseRewardConfig(
  value: unknown,
  expectedStageId: string
): PurifyRewardConfig {
  const data = requireConfigRecord(value, "Reward config");
  const stageId = requireConfigString(data.stageId, "stageId");
  const enabled = requireConfigBoolean(data.enabled, "enabled");
  const configVersion = requireConfigPositiveInteger(
    data.configVersion,
    "configVersion"
  );
  const maxFlow = requireConfigPositiveInteger(data.maxFlow, "maxFlow");
  const flowRewardData = requireConfigRecord(
    data.flowReward,
    "flowReward"
  );
  const flowReward = {
    rewardType: requireRewardType(flowRewardData.rewardType),
    amountPerCompletedFlow: requireConfigNonNegativeInteger(
      flowRewardData.amountPerCompletedFlow,
      "flowReward.amountPerCompletedFlow"
    ),
  };
  const clearRewards = parseRewardEntries(
    data.clearRewards,
    "clearRewards"
  );
  const firstClearRewards = parseRewardEntries(
    data.firstClearRewards,
    "firstClearRewards"
  );
  const resultRewards = data.resultRewards === undefined ?
    [] :
    parseResultRewardConfigs(data.resultRewards);
  const randomRewards = data.randomRewards === undefined ?
    [] :
    parseRandomRewardConfigs(data.randomRewards);
  const forestHpBonusEnabled = requireConfigBoolean(
    data.forestHpBonusEnabled,
    "forestHpBonusEnabled"
  );

  if (stageId !== expectedStageId) {
    throw new Error(
      `Reward config stage mismatch: ${stageId}, expected ${expectedStageId}`
    );
  }

  if (forestHpBonusEnabled) {
    throw new Error("Forest HP reward bonus is not implemented.");
  }

  validateUniqueConfiguredRewardTypes(resultRewards, randomRewards);

  return {
    stageId,
    enabled,
    configVersion,
    maxFlow,
    flowReward,
    clearRewards,
    firstClearRewards,
    resultRewards,
    randomRewards,
    forestHpBonusEnabled,
  };
}

export function parseStoredSettlement(
  value: unknown
): SettlePurifyResponse {
  const data = requireConfigRecord(value, "Stored settlement");
  const rewards = parseRewardEntries(data.rewards, "rewards");

  return {
    runId: requireConfigString(data.runId, "runId"),
    stageId: requireConfigString(data.stageId, "stageId"),
    resultType: requireStoredResultType(data.resultType),
    completedFlow: requireConfigNonNegativeInteger(
      data.completedFlow,
      "completedFlow"
    ),
    forestHp: requireConfigNonNegativeInteger(data.forestHp, "forestHp"),
    isNewBest: requireConfigBoolean(data.isNewBest, "isNewBest"),
    bestFlow: requireConfigNonNegativeInteger(data.bestFlow, "bestFlow"),
    isFirstClear: requireConfigBoolean(
      data.isFirstClear,
      "isFirstClear"
    ),
    rewards,
  };
}

export function getStageNumber(stageId: string): number {
  const stageNumber = Number.parseInt(stageId.slice("stage_".length), 10);
  if (!Number.isInteger(stageNumber) || stageNumber <= 0) {
    throw new Error(`Cannot parse stage number: ${stageId}`);
  }

  return stageNumber;
}

function parseRewardEntries(
  value: unknown,
  fieldName: string
): RewardEntry[] {
  if (!Array.isArray(value)) {
    throw new Error(`${fieldName} must be an array.`);
  }

  return value.map((entry, index) => {
    const data = requireConfigRecord(
      entry,
      `${fieldName}[${index}]`
    );

    return {
      rewardType: requireRewardType(data.rewardType),
      amount: requireConfigNonNegativeInteger(
        data.amount,
        `${fieldName}[${index}].amount`
      ),
    };
  });
}

function parseResultRewardConfigs(value: unknown): ResultRewardConfig[] {
  if (!Array.isArray(value)) {
    throw new Error("resultRewards must be an array.");
  }

  const rewardTypes = new Set<RewardType>();
  return value.map((entry, index) => {
    const fieldName = `resultRewards[${index}]`;
    const data = requireConfigRecord(entry, fieldName);
    const rewardType = requireRewardType(data.rewardType);
    if (rewardTypes.has(rewardType)) {
      throw new Error(`Duplicate result rewardType: ${rewardType}`);
    }

    rewardTypes.add(rewardType);
    return {
      rewardType,
      failCompletedFlowAmounts: parseAmountThresholds(
        data.failCompletedFlowAmounts,
        `${fieldName}.failCompletedFlowAmounts`
      ),
      clearAmount: requireConfigPositiveInteger(
        data.clearAmount,
        `${fieldName}.clearAmount`
      ),
    };
  });
}

function parseAmountThresholds(
  value: unknown,
  fieldName: string
): {minimum: number; amount: number}[] {
  if (!Array.isArray(value) || value.length === 0) {
    throw new Error(`${fieldName} must be a non-empty array.`);
  }

  let previousMinimum = -1;
  return value.map((entry, index) => {
    const entryFieldName = `${fieldName}[${index}]`;
    const data = requireConfigRecord(entry, entryFieldName);
    const minimum = requireConfigNonNegativeInteger(
      data.minimum,
      `${entryFieldName}.minimum`
    );
    if (minimum <= previousMinimum) {
      throw new Error(`${fieldName} minimum values must be ascending.`);
    }

    previousMinimum = minimum;
    return {
      minimum,
      amount: requireConfigPositiveInteger(
        data.amount,
        `${entryFieldName}.amount`
      ),
    };
  });
}

function validateUniqueConfiguredRewardTypes(
  resultRewards: ResultRewardConfig[],
  randomRewards: RandomRewardConfig[]
): void {
  const resultRewardTypes = new Set(
    resultRewards.map((reward) => reward.rewardType)
  );
  for (const reward of randomRewards) {
    if (resultRewardTypes.has(reward.rewardType)) {
      throw new Error(
        `rewardType cannot be both result and random: ${reward.rewardType}`
      );
    }
  }
}

function parseRandomRewardConfigs(value: unknown): RandomRewardConfig[] {
  if (!Array.isArray(value)) {
    throw new Error("randomRewards must be an array.");
  }

  const rewardTypes = new Set<RandomRewardType>();
  return value.map((entry, index) => {
    const fieldName = `randomRewards[${index}]`;
    const data = requireConfigRecord(entry, fieldName);
    const rewardType = requireRandomRewardType(data.rewardType);
    if (rewardTypes.has(rewardType)) {
      throw new Error(`Duplicate random rewardType: ${rewardType}`);
    }

    rewardTypes.add(rewardType);
    return {
      rewardType,
      amount: requireConfigPositiveInteger(
        data.amount,
        `${fieldName}.amount`
      ),
      failCompletedFlowChances: parseChanceThresholds(
        data.failCompletedFlowChances,
        `${fieldName}.failCompletedFlowChances`
      ),
      clearForestHpChances: parseChanceThresholds(
        data.clearForestHpChances,
        `${fieldName}.clearForestHpChances`
      ),
    };
  });
}

function parseChanceThresholds(
  value: unknown,
  fieldName: string
): {minimum: number; chancePercent: number}[] {
  if (!Array.isArray(value)) {
    throw new Error(`${fieldName} must be an array.`);
  }

  let previousMinimum = -1;
  return value.map((entry, index) => {
    const entryFieldName = `${fieldName}[${index}]`;
    const data = requireConfigRecord(entry, entryFieldName);
    const minimum = requireConfigNonNegativeInteger(
      data.minimum,
      `${entryFieldName}.minimum`
    );
    if (minimum <= previousMinimum) {
      throw new Error(`${fieldName} minimum values must be ascending.`);
    }

    previousMinimum = minimum;
    return {
      minimum,
      chancePercent: requireConfigPercentageInteger(
        data.chancePercent,
        `${entryFieldName}.chancePercent`
      ),
    };
  });
}

function requireRewardType(value: unknown): RewardType {
  if (
    typeof value !== "string" ||
    !REWARD_TYPES.includes(value as RewardType)
  ) {
    throw new Error(`Unsupported rewardType: ${String(value)}`);
  }

  return value as RewardType;
}

function requireRandomRewardType(value: unknown): RandomRewardType {
  if (
    typeof value !== "string" ||
    !RANDOM_REWARD_TYPES.includes(value as RandomRewardType)
  ) {
    throw new Error(`Unsupported random rewardType: ${String(value)}`);
  }

  return value as RandomRewardType;
}

function requireResultType(value: unknown): PurifyResultType {
  if (
    typeof value !== "string" ||
    !RESULT_TYPES.includes(value as PurifyResultType)
  ) {
    throw new HttpsError("invalid-argument", "resultType is invalid.");
  }

  return value as PurifyResultType;
}

function requireStoredResultType(value: unknown): PurifyResultType {
  if (
    typeof value !== "string" ||
    !RESULT_TYPES.includes(value as PurifyResultType)
  ) {
    throw new Error(`Stored resultType is invalid: ${String(value)}`);
  }

  return value as PurifyResultType;
}

function requireRecord(
  value: unknown,
  fieldName: string
): Record<string, unknown> {
  if (!value || typeof value !== "object" || Array.isArray(value)) {
    throw new HttpsError(
      "invalid-argument",
      `${fieldName} must be an object.`
    );
  }

  return value as Record<string, unknown>;
}

function requireConfigRecord(
  value: unknown,
  fieldName: string
): Record<string, unknown> {
  if (!value || typeof value !== "object" || Array.isArray(value)) {
    throw new Error(`${fieldName} must be an object.`);
  }

  return value as Record<string, unknown>;
}

function requireString(value: unknown, fieldName: string): string {
  if (typeof value !== "string" || !value.trim()) {
    throw new HttpsError(
      "invalid-argument",
      `${fieldName} must be a non-empty string.`
    );
  }

  return value.trim();
}

function requireConfigString(value: unknown, fieldName: string): string {
  if (typeof value !== "string" || !value.trim()) {
    throw new Error(`${fieldName} must be a non-empty string.`);
  }

  return value.trim();
}

function requireConfigBoolean(
  value: unknown,
  fieldName: string
): boolean {
  if (typeof value !== "boolean") {
    throw new Error(`${fieldName} must be a boolean.`);
  }

  return value;
}

function requireNonNegativeInteger(
  value: unknown,
  fieldName: string
): number {
  if (!Number.isInteger(value) || (value as number) < 0) {
    throw new HttpsError(
      "invalid-argument",
      `${fieldName} must be a non-negative integer.`
    );
  }

  return value as number;
}

function requireConfigPositiveInteger(
  value: unknown,
  fieldName: string
): number {
  if (!Number.isInteger(value) || (value as number) <= 0) {
    throw new Error(`${fieldName} must be a positive integer.`);
  }

  return value as number;
}

function requireConfigNonNegativeInteger(
  value: unknown,
  fieldName: string
): number {
  if (!Number.isInteger(value) || (value as number) < 0) {
    throw new Error(`${fieldName} must be a non-negative integer.`);
  }

  return value as number;
}

function requireConfigPercentageInteger(
  value: unknown,
  fieldName: string
): number {
  if (
    !Number.isInteger(value) ||
    (value as number) < 0 ||
    (value as number) > 100
  ) {
    throw new Error(`${fieldName} must be an integer from 0 to 100.`);
  }

  return value as number;
}
