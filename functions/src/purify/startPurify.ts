import {
  FieldValue,
  Timestamp,
} from "firebase-admin/firestore";
import {logger} from "firebase-functions";
import {
  HttpsError,
  onCall,
} from "firebase-functions/v2/https";
import {firestore} from "../firebase";
import {normalizeUnlockedNoaIds} from "../noa/noaUnlockConfig";
import {RUN_DURATION_MILLISECONDS} from "./constants";
import {throwCallableError} from "./errors";
import {
  getPrivateSaveReference,
  getPurifyRunCollection,
  getPurifyRunReference,
  getPurifyRuntimeReference,
  getRewardConfigReference,
} from "./firestorePaths";
import type {StartPurifyResponse} from "./types";
import {
  parseRewardConfig,
  parseStartPurifyRequest,
  requireAuthenticatedUserId,
} from "./validation";

export const startPurify = onCall<
  unknown,
  Promise<StartPurifyResponse>
>(async (request) => {
  try {
    const userId = requireAuthenticatedUserId(request.auth?.uid);
    const input = parseStartPurifyRequest(request.data);
    const response = await createOrGetActiveRun(userId, input.stageId);

    logger.info("Purify run started.", {
      runId: response.runId,
      stageId: response.stageId,
      configVersion: response.configVersion,
    });

    return response;
  } catch (error) {
    return throwCallableError("startPurify", error);
  }
});

async function createOrGetActiveRun(
  userId: string,
  stageId: string
): Promise<StartPurifyResponse> {
  const configReference = getRewardConfigReference(firestore, stageId);
  const runtimeReference = getPurifyRuntimeReference(firestore, userId);
  const privateSaveReference = getPrivateSaveReference(firestore, userId);

  return firestore.runTransaction(async (transaction) => {
    const [
      configSnapshot,
      runtimeSnapshot,
      privateSaveSnapshot,
    ] = await Promise.all([
      transaction.get(configReference),
      transaction.get(runtimeReference),
      transaction.get(privateSaveReference),
    ]);

    if (!configSnapshot.exists) {
      throw new HttpsError(
        "not-found",
        `Reward config is missing: ${stageId}`
      );
    }

    const config = parseRewardConfig(configSnapshot.data(), stageId);
    if (!config.enabled) {
      throw new HttpsError(
        "failed-precondition",
        `Reward config is disabled: ${stageId}`
      );
    }

    const nowUtcMillis = Date.now();
    const runtimeData = runtimeSnapshot.data();
    const activeRunId = getOptionalString(runtimeData?.activeRunId);
    const activeStageId = getOptionalString(runtimeData?.stageId);
    const activeExpiresAt = getOptionalTimestamp(runtimeData?.expiresAt);
    const availableNoaIds = normalizeUnlockedNoaIds(
      privateSaveSnapshot.data()?.unlockedNoaIds
    );

    if (
      activeRunId &&
      activeExpiresAt &&
      activeExpiresAt.toMillis() > nowUtcMillis
    ) {
      if (activeStageId !== stageId) {
        throw new HttpsError(
          "failed-precondition",
          `Another purify run is active: ${activeStageId}`
        );
      }

      const activeRunReference = getPurifyRunReference(
        firestore,
        userId,
        activeRunId
      );
      const activeRunSnapshot = await transaction.get(activeRunReference);
      const activeRunData = activeRunSnapshot.data();

      if (activeRunSnapshot.exists && activeRunData?.status === "active") {
        const activeAvailableNoaIds = normalizeUnlockedNoaIds(
          activeRunData.availableNoaIds ?? availableNoaIds
        );
        transaction.set(privateSaveReference, {
          unlockedNoaIds: availableNoaIds,
          serverUpdatedAt: FieldValue.serverTimestamp(),
        }, {merge: true});
        return {
          runId: activeRunId,
          stageId,
          configVersion: getPositiveInteger(
            activeRunData.configVersion,
            "configVersion"
          ),
          expiresAtUtcMillis: activeExpiresAt.toMillis(),
          availableNoaIds: activeAvailableNoaIds,
        };
      }
    }

    transaction.set(privateSaveReference, {
      unlockedNoaIds: availableNoaIds,
      serverUpdatedAt: FieldValue.serverTimestamp(),
    }, {merge: true});

    const runReference = getPurifyRunCollection(
      firestore,
      userId
    ).doc();
    const expiresAt = Timestamp.fromMillis(
      nowUtcMillis + RUN_DURATION_MILLISECONDS
    );

    transaction.create(runReference, {
      userId,
      stageId,
      status: "active",
      configVersion: config.configVersion,
      rewardConfigSnapshot: config,
      availableNoaIds,
      createdAt: FieldValue.serverTimestamp(),
      expiresAt,
    });
    transaction.set(runtimeReference, {
      activeRunId: runReference.id,
      stageId,
      status: "active",
      expiresAt,
      updatedAt: FieldValue.serverTimestamp(),
    }, {merge: true});

    return {
      runId: runReference.id,
      stageId,
      configVersion: config.configVersion,
      expiresAtUtcMillis: expiresAt.toMillis(),
      availableNoaIds,
    };
  });
}

function getOptionalString(value: unknown): string | null {
  return typeof value === "string" && value ? value : null;
}

function getOptionalTimestamp(value: unknown): Timestamp | null {
  return value instanceof Timestamp ? value : null;
}

function getPositiveInteger(value: unknown, fieldName: string): number {
  if (!Number.isInteger(value) || (value as number) <= 0) {
    throw new Error(`${fieldName} must be a positive integer.`);
  }

  return value as number;
}
