const {
  copyFileSync,
  mkdirSync,
} = require("node:fs");
const {dirname, resolve} = require("node:path");

const sourcePath = resolve(
  __dirname,
  "../../Assets/Resources/Data/BadWords.txt"
);
const destinationPath = resolve(
  __dirname,
  "../data/BadWords.txt"
);

mkdirSync(dirname(destinationPath), {recursive: true});
copyFileSync(sourcePath, destinationPath);
