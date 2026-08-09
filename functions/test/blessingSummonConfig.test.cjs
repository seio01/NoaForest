const assert = require("node:assert/strict");
const test = require("node:test");
const {
  BLESSING_RARITY_WEIGHTS,
  BLESSING_SUMMON_CONFIGS,
  getEligibleBlessingConfigs,
  selectBlessingConfig,
} = require("../lib/blessing/blessingSummonConfig.js");
const {
  getCollectionUpgradeConfig,
} = require("../lib/collection/collectionUpgradeConfig.js");

test("Blessing rarity weights use 65/25/10", () => {
  assert.deepEqual(BLESSING_RARITY_WEIGHTS, {
    common: 65,
    rare: 25,
    epic: 10,
  });
});

test("Every current Blessing is registered for summoning", () => {
  assert.equal(BLESSING_SUMMON_CONFIGS.length, 8);
  assert.equal(
    BLESSING_SUMMON_CONFIGS.filter((config) =>
      config.rarity === "common").length,
    2
  );
  assert.equal(
    BLESSING_SUMMON_CONFIGS.filter((config) =>
      config.rarity === "rare").length,
    3
  );
  assert.equal(
    BLESSING_SUMMON_CONFIGS.filter((config) =>
      config.rarity === "epic").length,
    3
  );
  for (const config of BLESSING_SUMMON_CONFIGS) {
    assert.notEqual(
      getCollectionUpgradeConfig("blessing", config.itemId),
      null
    );
  }
});

test("Maximum-level Blessings are excluded from candidates", () => {
  const candidates = getEligibleBlessingConfigs({
    blessing_cycle_stone: 4,
    blessing_seed_of_life: 3,
  });

  assert.equal(
    candidates.some((config) =>
      config.itemId === "blessing_cycle_stone"),
    false
  );
  assert.equal(
    candidates.some((config) =>
      config.itemId === "blessing_seed_of_life"),
    true
  );
});

test("Legacy maximum-level Blessings are excluded from candidates", () => {
  const candidates = getEligibleBlessingConfigs({
    blessing_echo_of_earth: 4,
  });

  assert.equal(
    candidates.some((config) =>
      config.itemId === "blessing_guardian_stone_of_earth"),
    false
  );
});

test("All maximum-level Blessings leave no summon candidate", () => {
  const levels = Object.fromEntries(
    BLESSING_SUMMON_CONFIGS.map((config) => [
      config.itemId,
      config.maximumLevel,
    ])
  );
  const candidates = getEligibleBlessingConfigs(levels);

  assert.equal(candidates.length, 0);
  assert.equal(selectBlessingConfig(candidates, 0, 0), null);
});

test("Rarity roll selects Common, Rare, and Epic boundaries", () => {
  const candidates = getEligibleBlessingConfigs({});

  assert.equal(
    selectBlessingConfig(candidates, 0.6499, 0)?.rarity,
    "common"
  );
  assert.equal(
    selectBlessingConfig(candidates, 0.65, 0)?.rarity,
    "rare"
  );
  assert.equal(
    selectBlessingConfig(candidates, 0.9, 0)?.rarity,
    "epic"
  );
});

test("Unavailable rarity weight is redistributed proportionally", () => {
  const levels = Object.fromEntries(
    BLESSING_SUMMON_CONFIGS
      .filter((config) => config.rarity === "common")
      .map((config) => [config.itemId, config.maximumLevel])
  );
  const candidates = getEligibleBlessingConfigs(levels);

  assert.equal(
    selectBlessingConfig(candidates, 0.7142, 0)?.rarity,
    "rare"
  );
  assert.equal(
    selectBlessingConfig(candidates, 0.7143, 0)?.rarity,
    "epic"
  );
});

test("Item roll is uniform within the selected rarity", () => {
  const candidates = getEligibleBlessingConfigs({});
  const firstRare = selectBlessingConfig(candidates, 0.7, 0);
  const lastRare = selectBlessingConfig(candidates, 0.7, 0.9999);

  assert.equal(firstRare?.itemId, "blessing_branch_of_cycle");
  assert.equal(lastRare?.itemId, "blessing_seed_of_life");
});
