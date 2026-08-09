const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const test = require("node:test");
const {
  parseRewardConfig,
} = require("../lib/purify/validation.js");

test("published version 4 reward documents pass server validation", () => {
  const configPath = path.resolve(
    __dirname,
    "../../Docs/Firebase/purify-reward-config.json"
  );
  const publishedConfig = JSON.parse(fs.readFileSync(configPath, "utf8"));

  assert.equal(publishedConfig.schemaVersion, 4);
  for (const document of publishedConfig.documents) {
    assert.equal(
      parseRewardConfig(document.data, document.documentId).configVersion,
      4
    );
  }
});

test("version 1 reward config remains compatible without random rewards", () => {
  const config = createBaseConfig();

  assert.deepEqual(
    parseRewardConfig(config, "stage_001").randomRewards,
    []
  );
  assert.deepEqual(
    parseRewardConfig(config, "stage_001").resultRewards,
    []
  );
});

test("version 3 reward config parses fixed result rewards", () => {
  const config = createBaseConfig();
  config.configVersion = 3;
  config.resultRewards = [
    {
      rewardType: "noaMemory",
      failCompletedFlowAmounts: [
        {minimum: 0, amount: 3},
        {minimum: 10, amount: 4},
        {minimum: 15, amount: 5},
      ],
      clearAmount: 6,
    },
  ];
  config.randomRewards = [];

  assert.deepEqual(
    parseRewardConfig(config, "stage_001").resultRewards,
    config.resultRewards
  );
});

test("version 2 reward config parses independent random rewards", () => {
  const config = createBaseConfig();
  config.configVersion = 2;
  config.randomRewards = [
    {
      rewardType: "noaMemory",
      amount: 1,
      failCompletedFlowChances: [
        {minimum: 10, chancePercent: 5},
        {minimum: 15, chancePercent: 15},
      ],
      clearForestHpChances: [
        {minimum: 1, chancePercent: 45},
        {minimum: 50, chancePercent: 55},
        {minimum: 80, chancePercent: 65},
      ],
    },
    {
      rewardType: "blessingTicket",
      amount: 1,
      failCompletedFlowChances: [],
      clearForestHpChances: [
        {minimum: 1, chancePercent: 5},
        {minimum: 50, chancePercent: 8},
        {minimum: 80, chancePercent: 12},
      ],
    },
  ];

  assert.deepEqual(
    parseRewardConfig(config, "stage_001").randomRewards,
    config.randomRewards
  );
});

test("random reward thresholds must be in ascending order", () => {
  const config = createBaseConfig();
  config.configVersion = 2;
  config.randomRewards = [
    {
      rewardType: "noaMemory",
      amount: 1,
      failCompletedFlowChances: [
        {minimum: 15, chancePercent: 15},
        {minimum: 10, chancePercent: 5},
      ],
      clearForestHpChances: [],
    },
  ];

  assert.throws(
    () => parseRewardConfig(config, "stage_001"),
    /minimum values must be ascending/
  );
});

test("result reward thresholds must be in ascending order", () => {
  const config = createBaseConfig();
  config.configVersion = 3;
  config.resultRewards = [
    {
      rewardType: "noaMemory",
      failCompletedFlowAmounts: [
        {minimum: 10, amount: 4},
        {minimum: 0, amount: 3},
      ],
      clearAmount: 6,
    },
  ];

  assert.throws(
    () => parseRewardConfig(config, "stage_001"),
    /minimum values must be ascending/
  );
});

test("reward type cannot be both fixed and random", () => {
  const config = createBaseConfig();
  config.configVersion = 3;
  config.resultRewards = [
    {
      rewardType: "noaMemory",
      failCompletedFlowAmounts: [{minimum: 0, amount: 3}],
      clearAmount: 6,
    },
  ];
  config.randomRewards = [
    {
      rewardType: "noaMemory",
      amount: 1,
      failCompletedFlowChances: [],
      clearForestHpChances: [],
    },
  ];

  assert.throws(
    () => parseRewardConfig(config, "stage_001"),
    /cannot be both result and random/
  );
});

function createBaseConfig() {
  return {
    stageId: "stage_001",
    enabled: true,
    configVersion: 1,
    maxFlow: 20,
    flowReward: {
      rewardType: "seed",
      amountPerCompletedFlow: 2,
    },
    clearRewards: [
      {rewardType: "seed", amount: 10},
      {rewardType: "elementCore", amount: 1},
    ],
    firstClearRewards: [
      {rewardType: "seed", amount: 30},
      {rewardType: "elementCore", amount: 2},
    ],
    forestHpBonusEnabled: false,
  };
}
