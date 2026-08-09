import {readFileSync} from "node:fs";
import {resolve} from "node:path";

const BAD_WORDS_PATH = resolve(
  __dirname,
  "../../data/BadWords.txt"
);

interface BadWordLookup {
  exactWords: Set<string>;
  containsWords: string[];
}

const badWordLookup = createLookup(
  readFileSync(BAD_WORDS_PATH, "utf8")
);

export function containsBadWord(value: string): boolean {
  const comparisonKey = createComparisonKey(value);
  if (!comparisonKey) {
    return false;
  }

  if (badWordLookup.exactWords.has(comparisonKey)) {
    return true;
  }

  return badWordLookup.containsWords.some(
    (badWord) => comparisonKey.includes(badWord)
  );
}

function createLookup(content: string): BadWordLookup {
  const exactWords = new Set<string>();
  const containsWords: string[] = [];
  const uniqueWords = new Set<string>();

  for (const line of content.split(/\r\n|\n|\r/u)) {
    const comparisonKey = createComparisonKey(line);
    if (!comparisonKey || uniqueWords.has(comparisonKey)) {
      continue;
    }

    uniqueWords.add(comparisonKey);
    if (isShortAsciiWord(comparisonKey)) {
      exactWords.add(comparisonKey);
    } else {
      containsWords.push(comparisonKey);
    }
  }

  containsWords.sort(
    (left, right) => right.length - left.length
  );
  return {exactWords, containsWords};
}

function createComparisonKey(value: string): string {
  return value
    .normalize("NFKC")
    .trim()
    .toLocaleLowerCase("en-US")
    .replace(/[^\p{L}\p{N}]/gu, "");
}

function isShortAsciiWord(value: string): boolean {
  return value.length <= 3 && /^[a-z0-9]+$/u.test(value);
}
