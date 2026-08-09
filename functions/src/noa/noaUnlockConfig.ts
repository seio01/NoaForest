const NOA_ELEMENTS = [
  "water",
  "fire",
  "wind",
  "earth",
] as const;

export const DEFAULT_UNLOCKED_NOA_IDS = NOA_ELEMENTS.map(
  (element) => `noa_${element}_t1`
);

const VALID_NOA_IDS = new Set(
  NOA_ELEMENTS.flatMap((element) => [
    `noa_${element}_t1`,
    `noa_${element}_t2`,
    `noa_${element}_t3`,
  ])
);

export interface NoaUnlockConfig {
  itemId: string;
  prerequisiteItemId: string;
  unlockCost: number;
}

export function getNoaUnlockConfig(
  itemId: string
): NoaUnlockConfig | null {
  const match = /^noa_(water|fire|wind|earth)_t([23])$/.exec(itemId);
  if (!match) {
    return null;
  }

  const element = match[1];
  const tier = Number.parseInt(match[2], 10);
  return {
    itemId,
    prerequisiteItemId: `noa_${element}_t${tier - 1}`,
    unlockCost: tier === 2 ? 10 : 20,
  };
}

export function normalizeUnlockedNoaIds(value: unknown): string[] {
  const requestedIds = new Set<string>(DEFAULT_UNLOCKED_NOA_IDS);
  if (Array.isArray(value)) {
    for (const id of value) {
      if (typeof id === "string" && VALID_NOA_IDS.has(id)) {
        requestedIds.add(id);
      }
    }
  }

  const unlockedIds: string[] = [];
  for (const element of NOA_ELEMENTS) {
    const tier1Id = `noa_${element}_t1`;
    const tier2Id = `noa_${element}_t2`;
    const tier3Id = `noa_${element}_t3`;
    unlockedIds.push(tier1Id);
    if (!requestedIds.has(tier2Id)) {
      continue;
    }

    unlockedIds.push(tier2Id);
    if (requestedIds.has(tier3Id)) {
      unlockedIds.push(tier3Id);
    }
  }

  return unlockedIds;
}

export function isNoaUnlocked(
  value: unknown,
  noaId: string
): boolean {
  return normalizeUnlockedNoaIds(value).includes(noaId);
}
