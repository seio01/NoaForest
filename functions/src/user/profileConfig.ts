import {randomInt} from "node:crypto";

export const MAX_PROFILE_NAME_LENGTH = 12;
export const PUBLIC_ID_LENGTH = 8;

const DEFAULT_NAME_PREFIX = "숲의 수호자_";
const DEFAULT_NAME_SUFFIX_LENGTH = 5;
const PUBLIC_ID_CHARACTERS = "BCDFGHJKLMNPQRSTVWXYZ23456789";
const PUBLIC_ID_PATTERN = /^[BCDFGHJKLMNPQRSTVWXYZ2-9]{8}$/;
const HANGUL_CHARACTER_RANGES =
  "\\u1100-\\u11FF\\u3130-\\u318F\\uA960-\\uA97F" +
  "\\uAC00-\\uD7A3\\uD7B0-\\uD7FF";
const PROFILE_NAME_PATTERN = new RegExp(
  `^[A-Za-z0-9 _${HANGUL_CHARACTER_RANGES}]+$`,
  "u"
);

export interface UserProfileData {
  name: string;
  id: string;
  uuid: string;
  level: number;
}

export function createPublicId(): string {
  let publicId = "";
  for (let index = 0; index < PUBLIC_ID_LENGTH; index++) {
    publicId += PUBLIC_ID_CHARACTERS[
      randomInt(0, PUBLIC_ID_CHARACTERS.length)
    ];
  }

  return publicId;
}

export function createDefaultName(publicId: string): string {
  return DEFAULT_NAME_PREFIX +
    publicId.slice(0, DEFAULT_NAME_SUFFIX_LENGTH);
}

export function normalizeProfileName(value: string): string {
  return value.normalize("NFKC").trim();
}

export function isValidProfileName(value: string): boolean {
  return value.length > 0 &&
    value.length <= MAX_PROFILE_NAME_LENGTH &&
    PROFILE_NAME_PATTERN.test(value);
}

export function isValidPublicId(value: unknown): value is string {
  return typeof value === "string" && PUBLIC_ID_PATTERN.test(value);
}

export function getStoredName(value: unknown): string | null {
  if (typeof value !== "string") {
    return null;
  }

  const normalizedName = normalizeProfileName(value);
  return isValidProfileName(normalizedName) ? normalizedName : null;
}

export function getStoredLevel(value: unknown): number {
  return Number.isInteger(value) && (value as number) >= 1 ?
    value as number :
    1;
}
