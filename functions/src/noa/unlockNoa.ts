import {FieldValue} from "firebase-admin/firestore";
import {logger} from "firebase-functions";
import {HttpsError, onCall} from "firebase-functions/v2/https";
import {firestore} from "../firebase";
import {
  getPrivateSaveReference,
  getWalletReference,
} from "../purify/firestorePaths";
import {throwCallableError} from "../purify/errors";
import {requireAuthenticatedUserId} from "../purify/validation";
import {
  getNoaUnlockConfig,
  normalizeUnlockedNoaIds,
} from "./noaUnlockConfig";

interface UnlockNoaResponse {
  itemId: string;
  currencyType: "noaMemory";
  currencyBalance: number;
  unlockedNoaIds: string[];
}

export const unlockNoa = onCall<
  unknown,
  Promise<UnlockNoaResponse>
>(async (request) => {
  try {
    const userId = requireAuthenticatedUserId(request.auth?.uid);
    const itemId = parseItemId(request.data);
    const response = await unlock(userId, itemId);

    logger.info("Noa unlocked.", response);
    return response;
  } catch (error) {
    return throwCallableError("unlockNoa", error);
  }
});

async function unlock(
  userId: string,
  itemId: string
): Promise<UnlockNoaResponse> {
  const config = getNoaUnlockConfig(itemId);
  if (!config) {
    throw new HttpsError("invalid-argument", "Noa item is invalid.");
  }

  const walletReference = getWalletReference(firestore, userId);
  const privateSaveReference = getPrivateSaveReference(firestore, userId);

  return firestore.runTransaction(async (transaction) => {
    const [walletSnapshot, privateSaveSnapshot] = await Promise.all([
      transaction.get(walletReference),
      transaction.get(privateSaveReference),
    ]);

    const unlockedNoaIds = normalizeUnlockedNoaIds(
      privateSaveSnapshot.data()?.unlockedNoaIds
    );
    const currentBalance = readNoaMemory(
      walletSnapshot.data()?.currencies
    );

    if (unlockedNoaIds.includes(itemId)) {
      return createResponse(itemId, currentBalance, unlockedNoaIds);
    }

    if (!unlockedNoaIds.includes(config.prerequisiteItemId)) {
      throw new HttpsError(
        "failed-precondition",
        "Previous tier Noa must be unlocked first."
      );
    }

    if (currentBalance < config.unlockCost) {
      throw new HttpsError(
        "failed-precondition",
        "Noa Memory balance is insufficient."
      );
    }

    const nextBalance = currentBalance - config.unlockCost;
    const nextUnlockedNoaIds = normalizeUnlockedNoaIds([
      ...unlockedNoaIds,
      itemId,
    ]);

    transaction.set(walletReference, {
      currencies: {noaMemory: nextBalance},
      updatedAt: FieldValue.serverTimestamp(),
    }, {merge: true});
    transaction.set(privateSaveReference, {
      unlockedNoaIds: nextUnlockedNoaIds,
      serverUpdatedAt: FieldValue.serverTimestamp(),
    }, {merge: true});

    return createResponse(itemId, nextBalance, nextUnlockedNoaIds);
  });
}

function parseItemId(value: unknown): string {
  if (
    typeof value !== "object" ||
    value === null ||
    !("itemId" in value)
  ) {
    throw new HttpsError("invalid-argument", "itemId is required.");
  }

  const itemId = (value as Record<string, unknown>).itemId;
  if (typeof itemId !== "string" || !itemId.trim()) {
    throw new HttpsError("invalid-argument", "itemId is invalid.");
  }

  return itemId.trim();
}

function readNoaMemory(value: unknown): number {
  if (typeof value !== "object" || value === null) {
    return 0;
  }

  const amount = (value as Record<string, unknown>).noaMemory;
  return typeof amount === "number" &&
    Number.isInteger(amount) &&
    amount >= 0 ?
    amount :
    0;
}

function createResponse(
  itemId: string,
  currencyBalance: number,
  unlockedNoaIds: string[]
): UnlockNoaResponse {
  return {
    itemId,
    currencyType: "noaMemory",
    currencyBalance,
    unlockedNoaIds,
  };
}
