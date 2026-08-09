export type CollectionType = "noa" | "blessing";
export type GrowthCurrencyType = "seed" | "elementCore";

export interface CollectionUpgradeConfig {
  collectionType: CollectionType;
  itemId: string;
  maximumLevel: number;
  upgradeCosts: readonly number[];
  currencyType: GrowthCurrencyType;
  levelField: "noaLevels" | "blessingLevels";
  pieceCosts?: readonly number[];
  pieceField?: "blessingPieceCounts";
  legacyItemId?: string;
}

const NOA_ID_PATTERN = /^noa_(water|fire|wind|earth)_t[1-3]$/;
const NOA_UPGRADE_COSTS = [
  100,
  200,
  300,
  500,
  700,
  1000,
  1400,
  1900,
  2500,
] as const;

interface BlessingUpgradeCosts {
  elementCore: readonly number[];
  pieces: readonly number[];
  legacyItemId?: string;
}

const BLESSING_UPGRADE_COSTS: Readonly<
  Record<string, BlessingUpgradeCosts>
> = {
  blessing_branch_of_cycle: {
    elementCore: [4, 8, 13],
    pieces: [2, 4, 7],
  },
  blessing_crown_of_breath: {
    elementCore: [6, 12, 20],
    pieces: [1, 3, 5],
    legacyItemId: "blessing_crown_of_four_seasons",
  },
  blessing_cycle_stone: {
    elementCore: [3, 6, 10],
    pieces: [2, 5, 9],
  },
  blessing_guardian_stone_of_earth: {
    elementCore: [6, 12, 20],
    pieces: [1, 3, 5],
    legacyItemId: "blessing_echo_of_earth",
  },
  blessing_oath_of_stars: {
    elementCore: [6, 12, 20],
    pieces: [1, 3, 5],
    legacyItemId: "blessing_elemental_oath",
  },
  blessing_purification_heart: {
    elementCore: [4, 8, 13],
    pieces: [2, 4, 7],
  },
  blessing_seed_of_life: {
    elementCore: [4, 8, 13],
    pieces: [2, 4, 7],
  },
  blessing_whispering_wind: {
    elementCore: [3, 6, 10],
    pieces: [2, 5, 9],
  },
};

export function getCollectionUpgradeConfig(
  collectionType: string,
  itemId: string
): CollectionUpgradeConfig | null {
  if (collectionType === "noa" && NOA_ID_PATTERN.test(itemId)) {
    return {
      collectionType,
      itemId,
      maximumLevel: NOA_UPGRADE_COSTS.length + 1,
      upgradeCosts: NOA_UPGRADE_COSTS,
      currencyType: "seed",
      levelField: "noaLevels",
    };
  }

  if (collectionType !== "blessing") {
    return null;
  }

  const upgradeCosts = BLESSING_UPGRADE_COSTS[itemId];
  if (!upgradeCosts) {
    return null;
  }

  return {
    collectionType,
    itemId,
    maximumLevel: upgradeCosts.elementCore.length + 1,
    upgradeCosts: upgradeCosts.elementCore,
    currencyType: "elementCore",
    levelField: "blessingLevels",
    pieceCosts: upgradeCosts.pieces,
    pieceField: "blessingPieceCounts",
    legacyItemId: upgradeCosts.legacyItemId,
  };
}
