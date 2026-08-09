import {FieldValue} from "firebase-admin/firestore";
import {logger} from "firebase-functions";
import {onCall} from "firebase-functions/v2/https";
import {firestore} from "../firebase";
import {throwCallableError} from "../purify/errors";
import {getWalletReference} from "../purify/firestorePaths";
import {requireAuthenticatedUserId} from "../purify/validation";
import {
  createWalletInitialization,
  WalletCurrencies,
  WalletCurrencyType,
} from "./walletConfig";

interface InitializeWalletResponse {
  currencies: WalletCurrencies;
  initializedFields: WalletCurrencyType[];
}

export const initializeWallet = onCall<
  unknown,
  Promise<InitializeWalletResponse>
>(async (request) => {
  try {
    const userId = requireAuthenticatedUserId(request.auth?.uid);
    const response = await initialize(userId);

    logger.info("User wallet initialized.", {
      userId,
      initializedFields: response.initializedFields,
    });
    return response;
  } catch (error) {
    return throwCallableError("initializeWallet", error);
  }
});

async function initialize(
  userId: string
): Promise<InitializeWalletResponse> {
  const walletReference = getWalletReference(firestore, userId);

  return firestore.runTransaction(async (transaction) => {
    const walletSnapshot = await transaction.get(walletReference);
    const initialization = createWalletInitialization(
      walletSnapshot.data()?.currencies
    );
    const initializedFields = Object.keys(
      initialization.patch
    ) as WalletCurrencyType[];

    if (initializedFields.length > 0) {
      const timestamp = FieldValue.serverTimestamp();
      const walletUpdate: Record<string, unknown> = {
        currencies: initialization.patch,
        updatedAt: timestamp,
      };
      if (!walletSnapshot.exists) {
        walletUpdate.createdAt = timestamp;
      }

      transaction.set(walletReference, walletUpdate, {merge: true});
    }

    return {
      currencies: initialization.currencies,
      initializedFields,
    };
  });
}
