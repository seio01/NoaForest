export type BlessingRarity = "common" | "rare" | "epic";

export interface BlessingSummonConfig {
  itemId: string;
  rarity: BlessingRarity;
  maximumLevel: number;
  legacyItemId?: string;
}

export const BLESSING_RARITY_WEIGHTS: Readonly<
  Record<BlessingRarity, number>
> = {
  common: 65,
  rare: 25,
  epic: 10,
};

export const BLESSING_SUMMON_CONFIGS: readonly BlessingSummonConfig[] = [
  {
    itemId: "blessing_cycle_stone",
    rarity: "common",
    maximumLevel: 4,
  },
  {
    itemId: "blessing_whispering_wind",
    rarity: "common",
    maximumLevel: 4,
  },
  {
    itemId: "blessing_branch_of_cycle",
    rarity: "rare",
    maximumLevel: 4,
  },
  {
    itemId: "blessing_purification_heart",
    rarity: "rare",
    maximumLevel: 4,
  },
  {
    itemId: "blessing_seed_of_life",
    rarity: "rare",
    maximumLevel: 4,
  },
  {
    itemId: "blessing_crown_of_breath",
    rarity: "epic",
    maximumLevel: 4,
    legacyItemId: "blessing_crown_of_four_seasons",
  },
  {
    itemId: "blessing_guardian_stone_of_earth",
    rarity: "epic",
    maximumLevel: 4,
    legacyItemId: "blessing_echo_of_earth",
  },
  {
    itemId: "blessing_oath_of_stars",
    rarity: "epic",
    maximumLevel: 4,
    legacyItemId: "blessing_elemental_oath",
  },
];

const BLESSING_RARITIES: readonly BlessingRarity[] = [
  "common",
  "rare",
  "epic",
];

export function getEligibleBlessingConfigs(
  blessingLevels: unknown
): BlessingSummonConfig[] {
  return BLESSING_SUMMON_CONFIGS.filter((config) => {
    const level = readBlessingLevel(blessingLevels, config);
    return level === null || level < config.maximumLevel;
  });
}

export function selectBlessingConfig(
  candidates: readonly BlessingSummonConfig[],
  rarityRoll: number,
  itemRoll: number
): BlessingSummonConfig | null {
  const candidatesByRarity = BLESSING_RARITIES.map((rarity) => ({
    rarity,
    candidates: candidates.filter((candidate) => candidate.rarity === rarity),
  })).filter((group) => group.candidates.length > 0);
  if (candidatesByRarity.length === 0) {
    return null;
  }

  const totalWeight = candidatesByRarity.reduce(
    (total, group) => total + BLESSING_RARITY_WEIGHTS[group.rarity],
    0
  );
  const rarityTarget = normalizeRoll(rarityRoll) * totalWeight;
  let accumulatedWeight = 0;
  let selectedGroup = candidatesByRarity[candidatesByRarity.length - 1];
  for (const group of candidatesByRarity) {
    accumulatedWeight += BLESSING_RARITY_WEIGHTS[group.rarity];
    if (rarityTarget < accumulatedWeight) {
      selectedGroup = group;
      break;
    }
  }

  const candidateIndex = Math.min(
    Math.floor(normalizeRoll(itemRoll) * selectedGroup.candidates.length),
    selectedGroup.candidates.length - 1
  );
  return selectedGroup.candidates[candidateIndex];
}

export function readBlessingLevel(
  value: unknown,
  config: BlessingSummonConfig
): number | null {
  return readBlessingValue(value, config, 1);
}

export function readBlessingPieceCount(
  value: unknown,
  config: BlessingSummonConfig
): number | null {
  return readBlessingValue(value, config, 0);
}

function readBlessingValue(
  value: unknown,
  config: BlessingSummonConfig,
  minimumValue: number
): number | null {
  if (typeof value !== "object" || value === null) {
    return null;
  }

  const data = value as Record<string, unknown>;
  const storedValue = data[config.itemId] ?? (
    config.legacyItemId ? data[config.legacyItemId] : undefined
  );
  return typeof storedValue === "number" &&
    Number.isInteger(storedValue) &&
    storedValue >= minimumValue ?
    storedValue :
    null;
}

function normalizeRoll(value: number): number {
  if (!Number.isFinite(value)) {
    return 0;
  }

  return Math.max(0, Math.min(value, 1 - Number.EPSILON));
}
