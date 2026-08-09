import {logger} from "firebase-functions";
import {HttpsError, onCall} from "firebase-functions/v2/https";
import {firestore} from "../firebase";
import {throwCallableError} from "../purify/errors";
import {requireAuthenticatedUserId} from "../purify/validation";
import {containsBadWord} from "./badWordFilter";
import {
  getStoredLevel,
  isValidProfileName,
  isValidPublicId,
  normalizeProfileName,
  UserProfileData,
} from "./profileConfig";

interface UpdateUserNameRequest {
  name: string;
}

export const updateUserName = onCall<
  UpdateUserNameRequest,
  Promise<UserProfileData>
>(async (request) => {
  try {
    const userId = requireAuthenticatedUserId(request.auth?.uid);
    const name = parseName(request.data);
    const profile = await updateName(userId, name);

    logger.info("User profile name updated.", {userId});
    return profile;
  } catch (error) {
    return throwCallableError("updateUserName", error);
  }
});

async function updateName(
  userId: string,
  name: string
): Promise<UserProfileData> {
  const profileReference = firestore.collection("users").doc(userId);

  return firestore.runTransaction(async (transaction) => {
    const profileSnapshot = await transaction.get(profileReference);
    const profileData = profileSnapshot.data();
    if (!profileSnapshot.exists ||
      !isValidPublicId(profileData?.id) ||
      profileData?.uuid !== userId) {
      throw new HttpsError(
        "failed-precondition",
        "User profile must be initialized before changing its name."
      );
    }

    const profile: UserProfileData = {
      name,
      id: profileData.id,
      uuid: userId,
      level: getStoredLevel(profileData.level),
    };
    transaction.set(profileReference, {name}, {merge: true});
    return profile;
  });
}

function parseName(value: unknown): string {
  if (
    typeof value !== "object" ||
    value === null ||
    Array.isArray(value) ||
    !("name" in value) ||
    typeof (value as Record<string, unknown>).name !== "string"
  ) {
    throw new HttpsError("invalid-argument", "name is required.");
  }

  const name = normalizeProfileName(
    (value as Record<string, unknown>).name as string
  );
  if (!isValidProfileName(name)) {
    throw new HttpsError("invalid-argument", "name is invalid.");
  }
  if (containsBadWord(name)) {
    throw new HttpsError(
      "invalid-argument",
      "name contains a prohibited word."
    );
  }

  return name;
}
