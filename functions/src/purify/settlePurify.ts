import {
  FieldValue,
  Timestamp,
} from "firebase-admin/firestore";
import {randomInt} from "node:crypto";
import {logger} from "firebase-functions";
import {
  HttpsError,
  onCall,
} from "firebase-functions/v2/https";
import {firestore} from "../firebase";
import {MAX_FOREST_HP} from "./constants";
import {throwCallableError} from "./errors";
import {
  getPrivateSaveReference,
  getPurifyProgressReference,
  getPurifyRunReference,
  getPurifyRuntimeReference,
  getWalletReference,
} from "./firestorePaths";
import {calculateRewards} from "./rewardCalculator";
import type {
  PurifyRewardConfig,
  RewardEntry,
  RewardRolls,
  SettlePurifyRequest,
  SettlePurifyResponse,
} from "./types";
import {
  getStageNumber,
  parseRewardConfig,
  parseSettlePurifyRequest,
  parseStoredSettlement,
  requireAuthenticatedUserId,
} from "./validation";

export const settlePurify = onCall<
  unknown,
  Promise<SettlePurifyResponse>
>(async (request) => {
  try {
    const userId = requireAuthenticatedUserId(request.auth?.uid);
    const input = parseSettlePurifyRequest(request.data);
    const response = await settleRun(userId, input, createRewardRolls());

    logger.info("Purify run settled.", {
      runId: response.runId,
      stageId: response.stageId,
      resultType: response.resultType,
      completedFlow: response.completedFlow,
      isAbandoned: response.resultType === "fail" && response.forestHp > 0,
      isNewBest: response.isNewBest,
      isFirstClear: response.isFirstClear,
    });

    return response;
  } catch (error) {
    return throwCallableError("settlePurify", error);
  }
});

async function settleRun(
  userId: string,
  input: SettlePurifyRequest,
  rewardRolls: RewardRolls
): Promise<SettlePurifyResponse> {
  const runReference = getPurifyRunReference(
    firestore,
    userId,
    input.runId
  );
  const runtimeReference = getPurifyRuntimeReference(firestore, userId);
  const walletReference = getWalletReference(firestore, userId);
  const progressReference = getPurifyProgressReference(
    firestore,
    userId
  );
  const privateSaveReference = getPrivateSaveReference(firestore, userId);

  return firestore.runTransaction(async (transaction) => {
    const [
      runSnapshot,
      runtimeSnapshot,
      progressSnapshot,
      privateSaveSnapshot,
    ] = await Promise.all([
      transaction.get(runReference),
      transaction.get(runtimeReference),
      transaction.get(progressReference),
      transaction.get(privateSaveReference),
    ]);

    if (!runSnapshot.exists) {
      throw new HttpsError("not-found", "Purify run does not exist.");
    }

    const runData = runSnapshot.data();
    if (!runData || runData.userId !== userId) {
      throw new HttpsError(
        "permission-denied",
        "Purify run ownership is invalid."
      );
    }

    if (runData.status === "settled") {
      return parseStoredSettlement(runData.settlement);
    }

    if (runData.status !== "active") {
      throw new HttpsError(
        "failed-precondition",
        "Purify run is not active."
      );
    }

    const expiresAt = requireTimestamp(runData.expiresAt, "expiresAt");
    if (expiresAt.toMillis() <= Date.now()) {
      throw new HttpsError(
        "deadline-exceeded",
        "Purify run has expired."
      );
    }

    const stageId = requireString(runData.stageId, "stageId");
    const config = parseRewardConfig(
      runData.rewardConfigSnapshot,
      stageId
    );
    validateResult(input, config);

    const progressData = progressSnapshot.data();
    const privateSaveData = privateSaveSnapshot.data();
    const stageNumber = getStageNumber(stageId);
    const wasCleared = hasStageBeenCleared(
      progressData?.clearedStageIds,
      privateSaveData?.clearedStageIds,
      stageId,
      stageNumber
    );
    const isClear = input.resultType === "clear";
    const isAbandoned = input.resultType === "fail" && input.forestHp > 0;
    const isFirstClear = isClear && !wasCleared;
    const recordFlow = isClear ?
      input.completedFlow :
      Math.min(input.completedFlow + 1, config.maxFlow);
    const previousBestFlow = getBestFlow(
      progressData?.bestFlows,
      stageId
    );
    const bestFlow = isAbandoned ?
      previousBestFlow :
      Math.max(previousBestFlow, recordFlow);
    const isNewBest = !isAbandoned && recordFlow > previousBestFlow;
    const rewards = isAbandoned ? [] : calculateRewards(
      config,
      input.completedFlow,
      isClear,
      isFirstClear,
      input.forestHp,
      rewardRolls
    );
    const settlement = createSettlement(
      input,
      stageId,
      isNewBest,
      bestFlow,
      isFirstClear,
      rewards
    );

    if (rewards.length > 0) {
      transaction.set(walletReference, {
        currencies: createCurrencyIncrements(rewards),
        updatedAt: FieldValue.serverTimestamp(),
      }, {merge: true});
    }

    if (!isAbandoned) {
      const progressUpdate: Record<string, unknown> = {
        bestFlows: {[stageId]: bestFlow},
        updatedAt: FieldValue.serverTimestamp(),
      };
      if (isClear) {
        progressUpdate.clearedStageIds = FieldValue.arrayUnion(stageId);
        transaction.set(privateSaveReference, {
          clearedStageIds: FieldValue.arrayUnion(stageNumber),
          serverUpdatedAt: FieldValue.serverTimestamp(),
        }, {merge: true});
      }

      transaction.set(progressReference, progressUpdate, {merge: true});
    }

    transaction.update(runReference, {
      status: "settled",
      settledAt: FieldValue.serverTimestamp(),
      settlement,
    });

    const runtimeData = runtimeSnapshot.data();
    if (runtimeData?.activeRunId === input.runId) {
      transaction.set(runtimeReference, {
        activeRunId: null,
        status: "idle",
        updatedAt: FieldValue.serverTimestamp(),
      }, {merge: true});
    }

    return settlement;
  });
}

function createRewardRolls(): RewardRolls {
  return {
    noaMemory: randomInt(100),
    blessingTicket: randomInt(100),
  };
}

function validateResult(
  input: SettlePurifyRequest,
  config: PurifyRewardConfig
): void {
  if (input.completedFlow > config.maxFlow) {
    throw new HttpsError(
      "invalid-argument",
      "completedFlow exceeds the stage maximum."
    );
  }

  if (input.forestHp > MAX_FOREST_HP) {
    throw new HttpsError(
      "invalid-argument",
      "forestHp exceeds the maximum."
    );
  }

  if (
    input.resultType === "clear" &&
    (
      input.completedFlow !== config.maxFlow ||
      input.forestHp <= 0
    )
  ) {
    throw new HttpsError(
      "failed-precondition",
      "Clear result values are inconsistent."
    );
  }

  if (
    input.resultType === "fail" &&
    input.completedFlow >= config.maxFlow
  ) {
    throw new HttpsError(
      "failed-precondition",
      "Fail result values are inconsistent."
    );
  }
}

function hasStageBeenCleared(
  progressClearedStageIds: unknown,
  privateClearedStageIds: unknown,
  stageId: string,
  stageNumber: number
): boolean {
  const progressIds = Array.isArray(progressClearedStageIds) ?
    progressClearedStageIds :
    [];
  const privateIds = Array.isArray(privateClearedStageIds) ?
    privateClearedStageIds :
    [];

  return progressIds.includes(stageId) || privateIds.includes(stageNumber);
}

function getBestFlow(value: unknown, stageId: string): number {
  if (!value || typeof value !== "object" || Array.isArray(value)) {
    return 0;
  }

  const bestFlow = (value as Record<string, unknown>)[stageId];
  return Number.isInteger(bestFlow) && (bestFlow as number) >= 0 ?
    bestFlow as number :
    0;
}

function createCurrencyIncrements(
  rewards: RewardEntry[]
): Record<string, FieldValue> {
  const increments: Record<string, FieldValue> = {};

  for (const reward of rewards) {
    increments[reward.rewardType] = FieldValue.increment(reward.amount);
  }

  return increments;
}

function createSettlement(
  input: SettlePurifyRequest,
  stageId: string,
  isNewBest: boolean,
  bestFlow: number,
  isFirstClear: boolean,
  rewards: RewardEntry[]
): SettlePurifyResponse {
  return {
    runId: input.runId,
    stageId,
    resultType: input.resultType,
    completedFlow: input.completedFlow,
    forestHp: input.forestHp,
    isNewBest,
    bestFlow,
    isFirstClear,
    rewards,
  };
}

function requireTimestamp(value: unknown, fieldName: string): Timestamp {
  if (!(value instanceof Timestamp)) {
    throw new Error(`${fieldName} must be a Firestore timestamp.`);
  }

  return value;
}

function requireString(value: unknown, fieldName: string): string {
  if (typeof value !== "string" || !value) {
    throw new Error(`${fieldName} must be a non-empty string.`);
  }

  return value;
}
