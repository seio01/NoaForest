const assert = require("node:assert/strict");
const test = require("node:test");
const {
  DEFAULT_UNLOCKED_NOA_IDS,
  getNoaUnlockConfig,
  isNoaUnlocked,
  normalizeUnlockedNoaIds,
} = require("../lib/noa/noaUnlockConfig.js");

test("tier unlock configs use sequential prerequisites and costs", () => {
  assert.deepEqual(getNoaUnlockConfig("noa_water_t2"), {
    itemId: "noa_water_t2",
    prerequisiteItemId: "noa_water_t1",
    unlockCost: 10,
  });
  assert.deepEqual(getNoaUnlockConfig("noa_fire_t3"), {
    itemId: "noa_fire_t3",
    prerequisiteItemId: "noa_fire_t2",
    unlockCost: 20,
  });
});

test("tier 1 and invalid Noa IDs cannot be unlocked", () => {
  assert.equal(getNoaUnlockConfig("noa_water_t1"), null);
  assert.equal(getNoaUnlockConfig("invalid_noa"), null);
});

test("missing unlock data defaults to every tier 1 Noa", () => {
  assert.deepEqual(
    normalizeUnlockedNoaIds(undefined),
    DEFAULT_UNLOCKED_NOA_IDS
  );
});

test("tier 3 is excluded until its tier 2 prerequisite is unlocked", () => {
  const unlockedIds = normalizeUnlockedNoaIds([
    "noa_water_t3",
    "noa_fire_t2",
    "noa_fire_t3",
  ]);

  assert.equal(unlockedIds.includes("noa_water_t3"), false);
  assert.equal(unlockedIds.includes("noa_fire_t2"), true);
  assert.equal(unlockedIds.includes("noa_fire_t3"), true);
});

test("invalid and duplicate Noa IDs are removed", () => {
  const unlockedIds = normalizeUnlockedNoaIds([
    "noa_water_t2",
    "noa_water_t2",
    "invalid_noa",
  ]);

  assert.equal(
    unlockedIds.filter((id) => id === "noa_water_t2").length,
    1
  );
  assert.equal(unlockedIds.includes("invalid_noa"), false);
});

test("unlock checks use normalized sequential data", () => {
  assert.equal(
    isNoaUnlocked(["noa_earth_t3"], "noa_earth_t3"),
    false
  );
  assert.equal(
    isNoaUnlocked(
      ["noa_earth_t2", "noa_earth_t3"],
      "noa_earth_t3"
    ),
    true
  );
});
