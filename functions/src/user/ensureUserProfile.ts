import {
  DocumentReference,
  Transaction,
} from "firebase-admin/firestore";
import {logger} from "firebase-functions";
import {HttpsError, onCall} from "firebase-functions/v2/https";
import {firestore} from "../firebase";
import {throwCallableError} from "../purify/errors";
import {requireAuthenticatedUserId} from "../purify/validation";
import {
  createDefaultName,
  createPublicId,
  getStoredLevel,
  getStoredName,
  isValidPublicId,
  UserProfileData,
} from "./profileConfig";

const MAX_PUBLIC_ID_ATTEMPTS = 10;

export const ensureUserProfile = onCall<
  unknown,
  Promise<UserProfileData>
>(async (request) => {
  try {
    const userId = requireAuthenticatedUserId(request.auth?.uid);
    const profile = await ensureProfile(userId);

    logger.info("User profile ensured.", {
      userId,
      publicId: profile.id,
    });
    return profile;
  } catch (error) {
    return throwCallableError("ensureUserProfile", error);
  }
});

async function ensureProfile(userId: string): Promise<UserProfileData> {
  const profileReference = getUserProfileReference(userId);

  return firestore.runTransaction(async (transaction) => {
    const profileSnapshot = await transaction.get(profileReference);
    const profileData = profileSnapshot.data();
    const publicId = await resolvePublicId(
      transaction,
      userId,
      profileData?.id
    );
    const name = getStoredName(profileData?.name) ??
      createDefaultName(publicId);
    const profile: UserProfileData = {
      name,
      id: publicId,
      uuid: userId,
      level: getStoredLevel(profileData?.level),
    };

    transaction.set(profileReference, profile, {merge: true});
    transaction.set(getPublicIdReference(publicId), {
      uuid: userId,
    });

    return profile;
  });
}

async function resolvePublicId(
  transaction: Transaction,
  userId: string,
  storedPublicId: unknown
): Promise<string> {
  if (isValidPublicId(storedPublicId)) {
    const storedIdSnapshot = await transaction.get(
      getPublicIdReference(storedPublicId)
    );
    const ownerUuid = storedIdSnapshot.data()?.uuid;
    if (!storedIdSnapshot.exists || ownerUuid === userId) {
      return storedPublicId;
    }
  }

  for (let attempt = 0; attempt < MAX_PUBLIC_ID_ATTEMPTS; attempt++) {
    const publicId = createPublicId();
    const publicIdSnapshot = await transaction.get(
      getPublicIdReference(publicId)
    );
    if (!publicIdSnapshot.exists) {
      return publicId;
    }
  }

  throw new HttpsError(
    "resource-exhausted",
    "A unique public user ID could not be generated."
  );
}

function getUserProfileReference(
  userId: string
): DocumentReference {
  return firestore.collection("users").doc(userId);
}

function getPublicIdReference(
  publicId: string
): DocumentReference {
  return firestore.collection("publicUserIds").doc(publicId);
}
