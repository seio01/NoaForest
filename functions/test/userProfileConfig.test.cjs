const assert = require("node:assert/strict");
const test = require("node:test");
const {
  createDefaultName,
  createPublicId,
  isValidProfileName,
  isValidPublicId,
  normalizeProfileName,
} = require("../lib/user/profileConfig.js");
const {
  containsBadWord,
} = require("../lib/user/badWordFilter.js");

test("public IDs use the server-owned eight-character format", () => {
  for (let index = 0; index < 100; index++) {
    const publicId = createPublicId();
    assert.equal(publicId.length, 8);
    assert.equal(isValidPublicId(publicId), true);
  }
});

test("default profile names fit the twelve-character limit", () => {
  const name = createDefaultName("BCDF2345");

  assert.equal(name, "숲의 수호자_BCDF2");
  assert.equal(name.length, 12);
  assert.equal(isValidProfileName(name), true);
});

test("profile names support only the configured character set", () => {
  assert.equal(isValidProfileName("숲지기_A2"), true);
  assert.equal(isValidProfileName("forest one"), true);
  assert.equal(isValidProfileName("emoji🌲"), false);
  assert.equal(isValidProfileName("angle<bracket"), false);
});

test("profile names normalize compatibility characters and whitespace", () => {
  assert.equal(normalizeProfileName("  Ｆorest  "), "Forest");
});

test("profile names use the synchronized bad-word rules", () => {
  assert.equal(containsBadWord("forest one"), false);
  assert.equal(containsBadWord("f_u_c_k"), true);
  assert.equal(containsBadWord("씨 발"), true);
});
