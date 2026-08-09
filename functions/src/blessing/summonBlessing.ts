import {randomInt} from "node:crypto";
import {FieldValue} from "firebase-admin/firestore";
import {logger} from "firebase-functions";
import {HttpsError, onCall} from "firebase-functions/v2/https";
import {firestore} from "../firebase";
import {throwCallableError} from "../purify/errors";
import {
  getPrivateSaveReference,
  getWalletReference,
} from "../purify/firestorePaths";
import {requireAuthenticatedUserId} from "../purify/validation";
import {
  BlessingRarity,
  BlessingSummonConfig,
  getEligibleBlessingConfigs,
  readBlessingLevel,
  readBlessingPieceCount,
  selectBlessingConfig,
} from "./blessingSummonConfig";

const RANDOM_RANGE = 1_000_000;

interface SummonBlessingResponse {
  itemId: string;
  rarity: BlessingRarity;
  isNew: boolean;
  level: number;
  acquiredPieceCount: number;
  pieceBalance: number;
  currencyType: "blessingTicket";
  currencyBalance: number;
}

export const summonBlessing = onCall<
  unknown,
  Promise<SummonBlessingResponse>
>(async (request) => {
  try {
    const userId = requireAuthenticatedUserId(request.auth?.uid);
    const rarityRoll = createRandomRoll();
    const itemRoll = createRandomRoll();
    const response = await summon(userId, rarityRoll, itemRoll);

    logger.info("Blessing summoned.", {userId, ...response});
    return response;
  } catch (error) {
    return throwCallableError("summonBlessing", error);
  }
});

async function summon(
  userId: string,
  rarityRoll: number,
  itemRoll: number
): Promise<SummonBlessingResponse> {
  const walletReference = getWalletReference(firestore, userId);
  const privateSaveReference = getPrivateSaveReference(firestore, userId);

  return firestore.runTransaction(async (transaction) => {
    const [walletSnapshot, privateSaveSnapshot] = await Promise.all([
      transaction.get(walletReference),
      transaction.get(privateSaveReference),
    ]);
    const ticketBalance = readBlessingTicket(
      walletSnapshot.data()?.currencies
    );
    if (ticketBalance < 1) {
      throw new HttpsError(
        "failed-precondition",
        "Blessing Ticket balance is insufficient."
      );
    }

    const privateSave = privateSaveSnapshot.data();
    const blessingLevels = privateSave?.blessingLevels;
    const candidates = getEligibleBlessingConfigs(blessingLevels);
    const selectedConfig = selectBlessingConfig(
      candidates,
      rarityRoll,
      itemRoll
    );
    if (!selectedConfig) {
      throw new HttpsError(
        "failed-precondition",
        "There are no summonable Blessings."
      );
    }

    const currentLevel = readBlessingLevel(
      blessingLevels,
      selectedConfig
    );
    const isNew = currentLevel === null;
    const currentPieceBalance = readBlessingPieceCount(
      privateSave?.blessingPieceCounts,
      selectedConfig
    ) ?? 0;
    const nextLevel = currentLevel ?? 1;
    const acquiredPieceCount = isNew ? 0 : 1;
    const nextPieceBalance = currentPieceBalance + acquiredPieceCount;
    const nextTicketBalance = ticketBalance - 1;

    transaction.set(walletReference, {
      currencies: {blessingTicket: nextTicketBalance},
      updatedAt: FieldValue.serverTimestamp(),
    }, {merge: true});
    transaction.set(privateSaveReference, createPrivateSavePatch(
      selectedConfig,
      nextLevel,
      nextPieceBalance,
      !isNew
    ), {merge: true});

    return {
      itemId: selectedConfig.itemId,
      rarity: selectedConfig.rarity,
      isNew,
      level: nextLevel,
      acquiredPieceCount,
      pieceBalance: nextPieceBalance,
      currencyType: "blessingTicket",
      currencyBalance: nextTicketBalance,
    };
  });
}

function createPrivateSavePatch(
  config: BlessingSummonConfig,
  level: number,
  pieceBalance: number,
  shouldUpdatePieces: boolean
): Record<string, unknown> {
  const levelPatch: Record<string, unknown> = {
    [config.itemId]: level,
  };
  if (config.legacyItemId) {
    levelPatch[config.legacyItemId] = FieldValue.delete();
  }

  const privateSavePatch: Record<string, unknown> = {
    blessingLevels: levelPatch,
    serverUpdatedAt: FieldValue.serverTimestamp(),
  };
  if (!shouldUpdatePieces) {
    return privateSavePatch;
  }

  const piecePatch: Record<string, unknown> = {
    [config.itemId]: pieceBalance,
  };
  if (config.legacyItemId) {
    piecePatch[config.legacyItemId] = FieldValue.delete();
  }
  privateSavePatch.blessingPieceCounts = piecePatch;
  return privateSavePatch;
}

function readBlessingTicket(value: unknown): number {
  if (typeof value !== "object" || value === null) {
    return 0;
  }

  const amount = (value as Record<string, unknown>).blessingTicket;
  return typeof amount === "number" &&
    Number.isInteger(amount) &&
    amount >= 0 ?
    amount :
    0;
}

function createRandomRoll(): number {
  return randomInt(0, RANDOM_RANGE) / RANDOM_RANGE;
}
