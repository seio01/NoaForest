import {
  CollectionReference,
  DocumentReference,
  Firestore,
} from "firebase-admin/firestore";

export function getRewardConfigReference(
  firestore: Firestore,
  stageId: string
): DocumentReference {
  return firestore
    .collection("gameConfigs")
    .doc("purifyRewards")
    .collection("stages")
    .doc(stageId);
}

export function getPurifyRunCollection(
  firestore: Firestore,
  userId: string
): CollectionReference {
  return firestore
    .collection("users")
    .doc(userId)
    .collection("purifyRuns");
}

export function getPurifyRunReference(
  firestore: Firestore,
  userId: string,
  runId: string
): DocumentReference {
  return getPurifyRunCollection(firestore, userId).doc(runId);
}

export function getPurifyRuntimeReference(
  firestore: Firestore,
  userId: string
): DocumentReference {
  return firestore
    .collection("users")
    .doc(userId)
    .collection("purifyRuntime")
    .doc("current");
}

export function getWalletReference(
  firestore: Firestore,
  userId: string
): DocumentReference {
  return firestore
    .collection("users")
    .doc(userId)
    .collection("wallet")
    .doc("main");
}

export function getPurifyProgressReference(
  firestore: Firestore,
  userId: string
): DocumentReference {
  return firestore
    .collection("users")
    .doc(userId)
    .collection("purifyProgress")
    .doc("main");
}

export function getPrivateSaveReference(
  firestore: Firestore,
  userId: string
): DocumentReference {
  return firestore
    .collection("users")
    .doc(userId)
    .collection("privateSave")
    .doc("main");
}
