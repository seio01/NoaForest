const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const test = require("node:test");
const {
  calculateRewards,
} = require("../lib/purify/rewardCalculator.js");

const stage1Config = createConfig("stage_001", 2, 10, 1, 30, 2);
const stage2Config = createConfig("stage_002", 3, 15, 2, 45, 3);
const fixedMemoryConfig = createFixedMemoryConfig();
const publishedConfig = JSON.parse(fs.readFileSync(path.resolve(
  __dirname,
  "../../Docs/Firebase/purify-reward-config.json"
), "utf8"));
const stage1Version4Config = getPublishedStageConfig("stage_001");
const stage2Version4Config = getPublishedStageConfig("stage_002");

test("Version 4 failure rewards match every progression boundary", () => {
  const cases = [
    {completedFlow: 0, memory: 3, stage1Core: 0, stage2Core: 0, ticket: 0},
    {completedFlow: 5, memory: 3, stage1Core: 0, stage2Core: 0, ticket: 0},
    {completedFlow: 6, memory: 3, stage1Core: 1, stage2Core: 2, ticket: 0},
    {completedFlow: 10, memory: 4, stage1Core: 1, stage2Core: 2, ticket: 0},
    {completedFlow: 11, memory: 4, stage1Core: 2, stage2Core: 3, ticket: 1},
    {completedFlow: 15, memory: 5, stage1Core: 2, stage2Core: 3, ticket: 1},
    {completedFlow: 16, memory: 5, stage1Core: 3, stage2Core: 4, ticket: 2},
    {completedFlow: 19, memory: 5, stage1Core: 3, stage2Core: 4, ticket: 2},
  ];

  for (const testCase of cases) {
    const stage1Rewards = calculateRewards(
      stage1Version4Config,
      testCase.completedFlow,
      false,
      false
    );
    const stage2Rewards = calculateRewards(
      stage2Version4Config,
      testCase.completedFlow,
      false,
      false
    );
    assert.equal(getRewardAmount(stage1Rewards, "seed"), testCase.completedFlow * 2);
    assert.equal(getRewardAmount(stage2Rewards, "seed"), testCase.completedFlow * 3);
    assert.equal(getRewardAmount(stage1Rewards, "noaMemory"), testCase.memory);
    assert.equal(getRewardAmount(stage2Rewards, "noaMemory"), testCase.memory);
    assert.equal(getRewardAmount(stage1Rewards, "elementCore"), testCase.stage1Core);
    assert.equal(getRewardAmount(stage2Rewards, "elementCore"), testCase.stage2Core);
    assert.equal(getRewardAmount(stage1Rewards, "blessingTicket"), testCase.ticket);
    assert.equal(getRewardAmount(stage2Rewards, "blessingTicket"), testCase.ticket);
  }
});

test("Version 4 clear and first-clear totals match the final table", () => {
  assert.deepEqual(
    calculateRewards(stage1Version4Config, 20, true, false, 1),
    [
      {rewardType: "seed", amount: 50},
      {rewardType: "elementCore", amount: 4},
      {rewardType: "noaMemory", amount: 6},
      {rewardType: "blessingTicket", amount: 2},
    ]
  );
  assert.deepEqual(
    calculateRewards(stage1Version4Config, 20, true, true, 100),
    [
      {rewardType: "seed", amount: 80},
      {rewardType: "elementCore", amount: 6},
      {rewardType: "noaMemory", amount: 6},
      {rewardType: "blessingTicket", amount: 2},
    ]
  );
  assert.deepEqual(
    calculateRewards(stage2Version4Config, 20, true, false, 1),
    [
      {rewardType: "seed", amount: 75},
      {rewardType: "elementCore", amount: 6},
      {rewardType: "noaMemory", amount: 6},
      {rewardType: "blessingTicket", amount: 2},
    ]
  );
  assert.deepEqual(
    calculateRewards(stage2Version4Config, 20, true, true, 100),
    [
      {rewardType: "seed", amount: 120},
      {rewardType: "elementCore", amount: 9},
      {rewardType: "noaMemory", amount: 6},
      {rewardType: "blessingTicket", amount: 2},
    ]
  );
});

test("Fixed Noa Memory uses completed FLOW thresholds on failure", () => {
  const cases = [
    {completedFlow: 0, expectedMemory: 3},
    {completedFlow: 9, expectedMemory: 3},
    {completedFlow: 10, expectedMemory: 4},
    {completedFlow: 14, expectedMemory: 4},
    {completedFlow: 15, expectedMemory: 5},
    {completedFlow: 19, expectedMemory: 5},
  ];

  for (const testCase of cases) {
    const rewards = calculateRewards(
      fixedMemoryConfig,
      testCase.completedFlow,
      false,
      false
    );
    assert.equal(
      getRewardAmount(rewards, "noaMemory"),
      testCase.expectedMemory
    );
  }
});

test("Clear grants 6 Noa Memory regardless of forest HP", () => {
  for (const forestHp of [1, 50, 80, 100]) {
    const rewards = calculateRewards(
      fixedMemoryConfig,
      20,
      true,
      false,
      forestHp,
      {noaMemory: 99, blessingTicket: 99}
    );
    assert.equal(getRewardAmount(rewards, "noaMemory"), 6);
  }
});

test("Stage 1 failure grants only completed-flow Seed", () => {
  assert.deepEqual(
    calculateRewards(stage1Config, 5, false, false),
    [{rewardType: "seed", amount: 10}]
  );
});

test("Failure before completing a FLOW grants no rewards", () => {
  assert.deepEqual(
    calculateRewards(stage1Config, 0, false, false),
    []
  );
});

test("Stage 1 repeat clear grants 50 Seed and 1 ElementCore", () => {
  assert.deepEqual(
    calculateRewards(stage1Config, 20, true, false),
    [
      {rewardType: "seed", amount: 50},
      {rewardType: "elementCore", amount: 1},
    ]
  );
});

test("Stage 1 first clear grants 80 Seed and 3 ElementCore", () => {
  assert.deepEqual(
    calculateRewards(stage1Config, 20, true, true),
    [
      {rewardType: "seed", amount: 80},
      {rewardType: "elementCore", amount: 3},
    ]
  );
});

test("Stage 2 repeat clear grants 75 Seed and 2 ElementCore", () => {
  assert.deepEqual(
    calculateRewards(stage2Config, 20, true, false),
    [
      {rewardType: "seed", amount: 75},
      {rewardType: "elementCore", amount: 2},
    ]
  );
});

test("Stage 2 first clear grants 120 Seed and 5 ElementCore", () => {
  assert.deepEqual(
    calculateRewards(stage2Config, 20, true, true),
    [
      {rewardType: "seed", amount: 120},
      {rewardType: "elementCore", amount: 5},
    ]
  );
});

test("Stage 1 late failure can grant Noa Memory but never a ticket", () => {
  assert.deepEqual(
    calculateRewards(
      stage1Config,
      15,
      false,
      false,
      0,
      {noaMemory: 14, blessingTicket: 0}
    ),
    [
      {rewardType: "seed", amount: 30},
      {rewardType: "noaMemory", amount: 1},
    ]
  );
});

test("Stage 2 high-HP clear can independently grant both random rewards", () => {
  assert.deepEqual(
    calculateRewards(
      stage2Config,
      20,
      true,
      false,
      80,
      {noaMemory: 84, blessingTicket: 29}
    ),
    [
      {rewardType: "seed", amount: 75},
      {rewardType: "elementCore", amount: 2},
      {rewardType: "noaMemory", amount: 1},
      {rewardType: "blessingTicket", amount: 1},
    ]
  );
});

test("Random reward chance boundaries are exclusive", () => {
  assert.deepEqual(
    calculateRewards(
      stage2Config,
      20,
      true,
      false,
      80,
      {noaMemory: 85, blessingTicket: 30}
    ),
    [
      {rewardType: "seed", amount: 75},
      {rewardType: "elementCore", amount: 2},
    ]
  );
});

test("First clear does not increase random reward chances", () => {
  const rolls = {noaMemory: 65, blessingTicket: 12};
  const repeatRewards = calculateRewards(
    stage1Config,
    20,
    true,
    false,
    80,
    rolls
  );
  const firstRewards = calculateRewards(
    stage1Config,
    20,
    true,
    true,
    80,
    rolls
  );

  assert.equal(hasRandomReward(repeatRewards), false);
  assert.equal(hasRandomReward(firstRewards), false);
});

function createConfig(
  stageId,
  seedPerFlow,
  clearSeed,
  clearCore,
  firstClearSeed,
  firstClearCore
) {
  const isStage1 = stageId === "stage_001";
  return {
    stageId,
    enabled: true,
    configVersion: 2,
    maxFlow: 20,
    flowReward: {
      rewardType: "seed",
      amountPerCompletedFlow: seedPerFlow,
    },
    clearRewards: [
      {rewardType: "seed", amount: clearSeed},
      {rewardType: "elementCore", amount: clearCore},
    ],
    firstClearRewards: [
      {rewardType: "seed", amount: firstClearSeed},
      {rewardType: "elementCore", amount: firstClearCore},
    ],
    randomRewards: [
      {
        rewardType: "noaMemory",
        amount: 1,
        failCompletedFlowChances: [
          {minimum: 10, chancePercent: isStage1 ? 5 : 10},
          {minimum: 15, chancePercent: isStage1 ? 15 : 25},
        ],
        clearForestHpChances: [
          {minimum: 1, chancePercent: isStage1 ? 45 : 65},
          {minimum: 50, chancePercent: isStage1 ? 55 : 75},
          {minimum: 80, chancePercent: isStage1 ? 65 : 85},
        ],
      },
      {
        rewardType: "blessingTicket",
        amount: 1,
        failCompletedFlowChances: [],
        clearForestHpChances: [
          {minimum: 1, chancePercent: isStage1 ? 5 : 12},
          {minimum: 50, chancePercent: isStage1 ? 8 : 20},
          {minimum: 80, chancePercent: isStage1 ? 12 : 30},
        ],
      },
    ],
    forestHpBonusEnabled: false,
  };
}

function createFixedMemoryConfig() {
  return {
    stageId: "stage_001",
    enabled: true,
    configVersion: 3,
    maxFlow: 20,
    flowReward: {
      rewardType: "seed",
      amountPerCompletedFlow: 2,
    },
    clearRewards: [
      {rewardType: "seed", amount: 10},
      {rewardType: "elementCore", amount: 1},
    ],
    firstClearRewards: [],
    resultRewards: [
      {
        rewardType: "noaMemory",
        failCompletedFlowAmounts: [
          {minimum: 0, amount: 3},
          {minimum: 10, amount: 4},
          {minimum: 15, amount: 5},
        ],
        clearAmount: 6,
      },
    ],
    randomRewards: [],
    forestHpBonusEnabled: false,
  };
}

function getPublishedStageConfig(stageId) {
  return publishedConfig.documents.find(
    (document) => document.documentId === stageId
  ).data;
}

function getRewardAmount(rewards, rewardType) {
  return rewards.find((reward) => reward.rewardType === rewardType)?.amount ?? 0;
}

function hasRandomReward(rewards) {
  return rewards.some((reward) =>
    reward.rewardType === "noaMemory" ||
    reward.rewardType === "blessingTicket"
  );
}
