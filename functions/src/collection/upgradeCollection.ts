import {FieldValue} from "firebase-admin/firestore";
import {logger} from "firebase-functions";
import {HttpsError, onCall} from "firebase-functions/v2/https";
import {firestore} from "../firebase";
import {isNoaUnlocked} from "../noa/noaUnlockConfig";
import {throwCallableError} from "../purify/errors";
import {
  getPrivateSaveReference,
  getWalletReference,
} from "../purify/firestorePaths";
import {requireAuthenticatedUserId} from "../purify/validation";
import {
  CollectionUpgradeConfig,
  getCollectionUpgradeConfig,
} from "./collectionUpgradeConfig";

interface UpgradeCollectionResponse {
  collectionType: string;
  itemId: string;
  level: number;
  currencyType: string;
  currencyBalance: number;
  pieceBalance: number | null;
}

export const upgradeCollection = onCall<
  unknown,
  Promise<UpgradeCollectionResponse>
>(async (request) => {
  try {
    const userId = requireAuthenticatedUserId(request.auth?.uid);
    const config = parseCollection(request.data);
    const response = await upgrade(userId, config);

    logger.info("Collection item upgraded.", response);
    return response;
  } catch (error) {
    return throwCallableError("upgradeCollection", error);
  }
});

async function upgrade(
  userId: string,
  config: CollectionUpgradeConfig
): Promise<UpgradeCollectionResponse> {
  const walletReference = getWalletReference(firestore, userId);
  const privateSaveReference = getPrivateSaveReference(firestore, userId);

  return firestore.runTransaction(async (transaction) => {
    const [walletSnapshot, privateSaveSnapshot] = await Promise.all([
      transaction.get(walletReference),
      transaction.get(privateSaveReference),
    ]);

    const storedLevels = privateSaveSnapshot.data()?.[config.levelField];
    const storedLevel = readStoredLevel(
      storedLevels,
      config.itemId
    ) ?? (
      config.legacyItemId ?
        readStoredLevel(storedLevels, config.legacyItemId) :
        null
    );
    const currentLevel = storedLevel ?? 1;
    if (config.collectionType === "blessing" && storedLevel === null) {
      throw new HttpsError(
        "failed-precondition",
        "Blessing is locked."
      );
    }
    if (
      config.collectionType === "noa" &&
      !isNoaUnlocked(
        privateSaveSnapshot.data()?.unlockedNoaIds,
        config.itemId
      )
    ) {
      throw new HttpsError(
        "failed-precondition",
        "Noa is locked."
      );
    }

    if (currentLevel >= config.maximumLevel) {
      throw new HttpsError(
        "failed-precondition",
        "Collection item has already reached the maximum level."
      );
    }

    const cost = config.upgradeCosts[currentLevel - 1];
    const currencyBalance = readCurrencyBalance(
      walletSnapshot.data()?.currencies,
      config.currencyType
    );
    if (currencyBalance < cost) {
      throw new HttpsError(
        "failed-precondition",
        "Currency balance is insufficient."
      );
    }

    const pieceCost = config.pieceCosts?.[currentLevel - 1] ?? 0;
    const pieceBalance = config.pieceField ?
      readItemBalance(
        privateSaveSnapshot.data()?.[config.pieceField],
        config.itemId,
        config.legacyItemId
      ) :
      null;
    if (pieceBalance !== null && pieceBalance < pieceCost) {
      throw new HttpsError(
        "failed-precondition",
        "Blessing piece balance is insufficient."
      );
    }

    const nextLevel = currentLevel + 1;
    const nextCurrencyBalance = currencyBalance - cost;
    const nextPieceBalance = pieceBalance === null ?
      null :
      pieceBalance - pieceCost;
    transaction.set(walletReference, {
      currencies: {[config.currencyType]: nextCurrencyBalance},
      updatedAt: FieldValue.serverTimestamp(),
    }, {merge: true});
    const levelPatch: Record<string, unknown> = {
      [config.itemId]: nextLevel,
    };
    if (config.legacyItemId) {
      levelPatch[config.legacyItemId] = FieldValue.delete();
    }
    const privateSavePatch: Record<string, unknown> = {
      [config.levelField]: levelPatch,
      serverUpdatedAt: FieldValue.serverTimestamp(),
    };
    if (config.pieceField && nextPieceBalance !== null) {
      const piecePatch: Record<string, unknown> = {
        [config.itemId]: nextPieceBalance,
      };
      if (config.legacyItemId) {
        piecePatch[config.legacyItemId] = FieldValue.delete();
      }
      privateSavePatch[config.pieceField] = piecePatch;
    }
    transaction.set(privateSaveReference, privateSavePatch, {merge: true});

    return {
      collectionType: config.collectionType,
      itemId: config.itemId,
      level: nextLevel,
      currencyType: config.currencyType,
      currencyBalance: nextCurrencyBalance,
      pieceBalance: nextPieceBalance,
    };
  });
}

function parseCollection(value: unknown): CollectionUpgradeConfig {
  if (
    typeof value !== "object" ||
    value === null ||
    !("collectionType" in value) ||
    !("itemId" in value)
  ) {
    throw new HttpsError(
      "invalid-argument",
      "collectionType and itemId are required."
    );
  }

  const data = value as Record<string, unknown>;
  if (
    typeof data.collectionType !== "string" ||
    typeof data.itemId !== "string"
  ) {
    throw new HttpsError("invalid-argument", "Collection request is invalid.");
  }

  const config = getCollectionUpgradeConfig(
    data.collectionType,
    data.itemId
  );
  if (!config) {
    throw new HttpsError("invalid-argument", "Collection item is invalid.");
  }

  return config;
}

function readStoredLevel(value: unknown, itemId: string): number | null {
  if (typeof value !== "object" || value === null) {
    return null;
  }

  const level = (value as Record<string, unknown>)[itemId];
  return typeof level === "number" &&
    Number.isInteger(level) &&
    level >= 1 ?
    level :
    null;
}

function readCurrencyBalance(
  value: unknown,
  currencyType: string
): number {
  if (typeof value !== "object" || value === null) {
    return 0;
  }

  const balance = (value as Record<string, unknown>)[currencyType];
  return typeof balance === "number" &&
    Number.isInteger(balance) &&
    balance >= 0 ?
    balance :
    0;
}

function readItemBalance(
  value: unknown,
  itemId: string,
  legacyItemId?: string
): number {
  if (typeof value !== "object" || value === null) {
    return 0;
  }

  const data = value as Record<string, unknown>;
  const balance = data[itemId] ?? (
    legacyItemId ? data[legacyItemId] : undefined
  );
  return typeof balance === "number" &&
    Number.isInteger(balance) &&
    balance >= 0 ?
    balance :
    0;
}
