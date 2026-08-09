const assert = require("node:assert/strict");
const test = require("node:test");
const {
  createWalletInitialization,
} = require("../lib/wallet/walletConfig.js");

test("missing wallet initializes every currency to zero", () => {
  const initialization = createWalletInitialization(undefined);

  assert.deepEqual(initialization.currencies, {
    seed: 0,
    elementCore: 0,
    noaMemory: 0,
    blessingTicket: 0,
  });
  assert.deepEqual(initialization.patch, {
    seed: 0,
    elementCore: 0,
    noaMemory: 0,
    blessingTicket: 0,
  });
});

test("valid currency balances are preserved", () => {
  const initialization = createWalletInitialization({
    seed: 120,
    elementCore: 8,
    noaMemory: 15,
    blessingTicket: 3,
  });

  assert.deepEqual(initialization.currencies, {
    seed: 120,
    elementCore: 8,
    noaMemory: 15,
    blessingTicket: 3,
  });
  assert.deepEqual(initialization.patch, {});
});

test("only missing or invalid currency fields are initialized", () => {
  const initialization = createWalletInitialization({
    seed: 120,
    elementCore: -1,
    legacyCurrency: 99,
  });

  assert.deepEqual(initialization.currencies, {
    seed: 120,
    elementCore: 0,
    noaMemory: 0,
    blessingTicket: 0,
  });
  assert.deepEqual(initialization.patch, {
    elementCore: 0,
    noaMemory: 0,
    blessingTicket: 0,
  });
});
