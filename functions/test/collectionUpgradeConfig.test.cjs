const assert = require("node:assert/strict");
const test = require("node:test");
const {
  getCollectionUpgradeConfig,
} = require("../lib/collection/collectionUpgradeConfig.js");

test("Noa upgrade config uses Seed and ten levels", () => {
  const config = getCollectionUpgradeConfig("noa", "noa_water_t1");

  assert.equal(config.currencyType, "seed");
  assert.equal(config.levelField, "noaLevels");
  assert.equal(config.pieceCosts, undefined);
  assert.equal(config.pieceField, undefined);
  assert.equal(config.maximumLevel, 10);
  assert.deepEqual([...config.upgradeCosts], [
    100,
    200,
    300,
    500,
    700,
    1000,
    1400,
    1900,
    2500,
  ]);
});

test("Blessing configs match every rarity upgrade cost", () => {
  const expectedCosts = {
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

  for (const [itemId, upgradeCosts] of Object.entries(expectedCosts)) {
    const config = getCollectionUpgradeConfig("blessing", itemId);

    assert.equal(config.currencyType, "elementCore");
    assert.equal(config.levelField, "blessingLevels");
    assert.equal(config.pieceField, "blessingPieceCounts");
    assert.equal(config.maximumLevel, 4);
    assert.deepEqual([...config.upgradeCosts], upgradeCosts.elementCore);
    assert.deepEqual([...config.pieceCosts], upgradeCosts.pieces);
    assert.equal(config.legacyItemId, upgradeCosts.legacyItemId);
  }
});

test("Unknown collection item has no upgrade config", () => {
  assert.equal(
    getCollectionUpgradeConfig("blessing", "blessing_unknown"),
    null
  );
  assert.equal(
    getCollectionUpgradeConfig("unknown", "noa_water_t1"),
    null
  );
});
