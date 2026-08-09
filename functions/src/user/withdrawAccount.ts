import {getAuth} from "firebase-admin/auth";
import {DocumentReference} from "firebase-admin/firestore";
import {logger} from "firebase-functions";
import {onCall} from "firebase-functions/v2/https";
import {firestore} from "../firebase";
import {throwCallableError} from "../purify/errors";
import {requireAuthenticatedUserId} from "../purify/validation";
import {isValidPublicId} from "./profileConfig";

interface WithdrawAccountResponse {
  success: true;
}

export const withdrawAccount = onCall<
  unknown,
  Promise<WithdrawAccountResponse>
>(async (request) => {
  try {
    const userId = requireAuthenticatedUserId(request.auth?.uid);
    await deleteAccount(userId);
    logger.info("User account permanently deleted.", {userId});
    return {success: true};
  } catch (error) {
    return throwCallableError("withdrawAccount", error);
  }
});

async function deleteAccount(userId: string): Promise<void> {
  const userReference = getUserReference(userId);
  const userSnapshot = await userReference.get();
  const publicId = userSnapshot.data()?.id;

  if (isValidPublicId(publicId)) {
    await deleteOwnedPublicId(publicId, userId);
  }

  await firestore.recursiveDelete(userReference);
  await getAuth().deleteUser(userId);
}

async function deleteOwnedPublicId(
  publicId: string,
  userId: string
): Promise<void> {
  const publicIdReference = firestore
    .collection("publicUserIds")
    .doc(publicId);

  await firestore.runTransaction(async (transaction) => {
    const publicIdSnapshot = await transaction.get(publicIdReference);
    if (publicIdSnapshot.data()?.uuid === userId) {
      transaction.delete(publicIdReference);
    }
  });
}

function getUserReference(userId: string): DocumentReference {
  return firestore.collection("users").doc(userId);
}
