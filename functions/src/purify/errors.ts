import {logger} from "firebase-functions";
import {HttpsError} from "firebase-functions/v2/https";

export function throwCallableError(
  operation: string,
  error: unknown
): never {
  if (error instanceof HttpsError) {
    throw error;
  }

  logger.error(`${operation} failed.`, error);
  throw new HttpsError("internal", `${operation} failed.`);
}
