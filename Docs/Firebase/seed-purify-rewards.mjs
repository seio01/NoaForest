import { readFile } from "node:fs/promises";
import process from "node:process";

const APPLY_OPTION = "--apply";
const PROJECT_OPTION_PREFIX = "--project=";
const configPath = new URL("./purify-reward-config.json", import.meta.url);
const config = JSON.parse(await readFile(configPath, "utf8"));
const shouldApply = process.argv.includes(APPLY_OPTION);
const projectOption = process.argv.find(argument => argument.startsWith(PROJECT_OPTION_PREFIX));
const projectId = projectOption?.slice(PROJECT_OPTION_PREFIX.length)
    || process.env.GOOGLE_CLOUD_PROJECT
    || process.env.GCLOUD_PROJECT;

validateConfig(config);
printSummary(config, shouldApply, projectId);

if (!shouldApply)
{
    console.log(`Dry run completed. Use "${APPLY_OPTION}" to write these documents.`);
    process.exit(0);
}

const { applicationDefault, initializeApp } = await import("firebase-admin/app");
const { FieldValue, getFirestore } = await import("firebase-admin/firestore");
const appOptions = { credential: applicationDefault() };
if (projectId)
{
    appOptions.projectId = projectId;
}

initializeApp(appOptions);

const firestore = getFirestore();
const batch = firestore.batch();
const parentDocumentReference = firestore.doc(config.parentDocument.documentPath);
batch.set(parentDocumentReference, {
    ...config.parentDocument.data,
    updatedAt: FieldValue.serverTimestamp()
}, { merge: true });

for (const document of config.documents)
{
    const documentPath = `${config.collectionPath}/${document.documentId}`;
    const documentReference = firestore.doc(documentPath);
    batch.set(documentReference, {
        ...document.data,
        updatedAt: FieldValue.serverTimestamp()
    }, { merge: true });
}

await batch.commit();
console.log(`Applied the parent config and ${config.documents.length} purify reward documents.`);

function validateConfig(value)
{
    if (!value || typeof value !== "object")
    {
        throw new Error("Reward config must be an object.");
    }

    if (!Number.isInteger(value.schemaVersion) || value.schemaVersion <= 0)
    {
        throw new Error("schemaVersion must be a positive integer.");
    }

    if (!value.parentDocument || typeof value.parentDocument !== "object")
    {
        throw new Error("parentDocument is required.");
    }

    const parentDocumentPath = value.parentDocument.documentPath;
    if (typeof parentDocumentPath !== "string" || !parentDocumentPath.trim())
    {
        throw new Error("parentDocument.documentPath is required.");
    }

    const parentSegments = parentDocumentPath.split("/").filter(Boolean);
    if (parentSegments.length % 2 !== 0)
    {
        throw new Error("parentDocument.documentPath must point to a Firestore document.");
    }

    if (!value.parentDocument.data || typeof value.parentDocument.data !== "object" || Array.isArray(value.parentDocument.data))
    {
        throw new Error("parentDocument.data must be an object.");
    }

    if (typeof value.collectionPath !== "string" || !value.collectionPath.trim())
    {
        throw new Error("collectionPath is required.");
    }

    const collectionSegments = value.collectionPath.split("/").filter(Boolean);
    if (collectionSegments.length % 2 === 0)
    {
        throw new Error("collectionPath must point to a Firestore collection.");
    }

    if (!Array.isArray(value.documents) || value.documents.length === 0)
    {
        throw new Error("At least one reward document is required.");
    }

    const documentIds = new Set();
    for (const document of value.documents)
    {
        if (!document || typeof document.documentId !== "string" || !document.documentId.trim())
        {
            throw new Error("Every reward document requires a documentId.");
        }

        if (document.documentId.includes("/"))
        {
            throw new Error(`documentId cannot contain '/': ${document.documentId}`);
        }

        if (documentIds.has(document.documentId))
        {
            throw new Error(`Duplicate documentId: ${document.documentId}`);
        }

        if (!document.data || typeof document.data !== "object" || Array.isArray(document.data))
        {
            throw new Error(`Document data must be an object: ${document.documentId}`);
        }

        if (value.schemaVersion >= 2)
        {
            validateRandomRewards(document.data.randomRewards, document.documentId);
        }

        if (value.schemaVersion >= 3)
        {
            validateResultRewards(document.data.resultRewards, document.documentId);
            validateDistinctRewardTypes(document.data.resultRewards, document.data.randomRewards, document.documentId);
        }

        documentIds.add(document.documentId);
    }
}

function validateResultRewards(value, documentId)
{
    if (!Array.isArray(value))
    {
        throw new Error(`resultRewards must be an array: ${documentId}`);
    }

    const supportedRewardTypes = new Set(["seed", "elementCore", "noaMemory", "blessingTicket"]);
    const rewardTypes = new Set();
    for (const reward of value)
    {
        if (!reward || !supportedRewardTypes.has(reward.rewardType))
        {
            throw new Error(`Unsupported result rewardType: ${documentId}`);
        }

        if (rewardTypes.has(reward.rewardType))
        {
            throw new Error(`Duplicate result rewardType: ${documentId}/${reward.rewardType}`);
        }

        if (!Number.isInteger(reward.clearAmount) || reward.clearAmount <= 0)
        {
            throw new Error(`Result reward clearAmount must be positive: ${documentId}/${reward.rewardType}`);
        }

        validateAmountThresholds(reward.failCompletedFlowAmounts, documentId, reward.rewardType);
        rewardTypes.add(reward.rewardType);
    }
}

function validateAmountThresholds(value, documentId, rewardType)
{
    if (!Array.isArray(value) || value.length === 0)
    {
        throw new Error(`failCompletedFlowAmounts must be a non-empty array: ${documentId}/${rewardType}`);
    }

    let previousMinimum = -1;
    for (const threshold of value)
    {
        if (!threshold || !Number.isInteger(threshold.minimum) || threshold.minimum < 0 || threshold.minimum <= previousMinimum)
        {
            throw new Error(`failCompletedFlowAmounts minimum values must be ascending: ${documentId}/${rewardType}`);
        }

        if (!Number.isInteger(threshold.amount) || threshold.amount <= 0)
        {
            throw new Error(`failCompletedFlowAmounts amount must be positive: ${documentId}/${rewardType}`);
        }

        previousMinimum = threshold.minimum;
    }
}

function validateDistinctRewardTypes(resultRewards, randomRewards, documentId)
{
    const resultRewardTypes = new Set(resultRewards.map(reward => reward.rewardType));
    for (const reward of randomRewards)
    {
        if (resultRewardTypes.has(reward.rewardType))
        {
            throw new Error(`rewardType cannot be both result and random: ${documentId}/${reward.rewardType}`);
        }
    }
}

function validateRandomRewards(value, documentId)
{
    if (!Array.isArray(value))
    {
        throw new Error(`randomRewards must be an array: ${documentId}`);
    }

    const supportedRewardTypes = new Set(["noaMemory", "blessingTicket"]);
    const rewardTypes = new Set();
    for (const reward of value)
    {
        if (!reward || !supportedRewardTypes.has(reward.rewardType))
        {
            throw new Error(`Unsupported random rewardType: ${documentId}`);
        }

        if (rewardTypes.has(reward.rewardType))
        {
            throw new Error(`Duplicate random rewardType: ${documentId}/${reward.rewardType}`);
        }

        if (!Number.isInteger(reward.amount) || reward.amount <= 0)
        {
            throw new Error(`Random reward amount must be positive: ${documentId}/${reward.rewardType}`);
        }

        validateChanceThresholds(reward.failCompletedFlowChances, documentId, reward.rewardType, "failCompletedFlowChances");
        validateChanceThresholds(reward.clearForestHpChances, documentId, reward.rewardType, "clearForestHpChances");
        rewardTypes.add(reward.rewardType);
    }
}

function validateChanceThresholds(value, documentId, rewardType, fieldName)
{
    if (!Array.isArray(value))
    {
        throw new Error(`${fieldName} must be an array: ${documentId}/${rewardType}`);
    }

    let previousMinimum = -1;
    for (const threshold of value)
    {
        if (!threshold || !Number.isInteger(threshold.minimum) || threshold.minimum < 0 || threshold.minimum <= previousMinimum)
        {
            throw new Error(`${fieldName} minimum values must be ascending: ${documentId}/${rewardType}`);
        }

        if (!Number.isInteger(threshold.chancePercent) || threshold.chancePercent < 0 || threshold.chancePercent > 100)
        {
            throw new Error(`${fieldName} chancePercent must be from 0 to 100: ${documentId}/${rewardType}`);
        }

        previousMinimum = threshold.minimum;
    }
}

function printSummary(value, apply, selectedProjectId)
{
    console.log(`Mode: ${apply ? "APPLY" : "DRY RUN"}`);
    console.log(`Project: ${selectedProjectId || "(resolved from application credentials)"}`);
    console.log(`Parent: ${value.parentDocument.documentPath}`);
    console.log(JSON.stringify(value.parentDocument.data, null, 2));
    console.log(`Collection: ${value.collectionPath}`);

    for (const document of value.documents)
    {
        console.log(`- ${value.collectionPath}/${document.documentId}`);
        console.log(JSON.stringify(document.data, null, 2));
    }
}
