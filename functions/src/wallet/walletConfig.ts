export const WALLET_CURRENCY_TYPES = [
  "seed",
  "elementCore",
  "noaMemory",
  "blessingTicket",
] as const;

export type WalletCurrencyType = typeof WALLET_CURRENCY_TYPES[number];
export type WalletCurrencies = Record<WalletCurrencyType, number>;

export interface WalletInitialization {
  currencies: WalletCurrencies;
  patch: Partial<WalletCurrencies>;
}

export function createWalletInitialization(
  value: unknown
): WalletInitialization {
  const source = isRecord(value) ? value : {};
  const currencies = createEmptyWalletCurrencies();
  const patch: Partial<WalletCurrencies> = {};

  for (const currencyType of WALLET_CURRENCY_TYPES) {
    const amount = source[currencyType];
    if (isValidCurrencyAmount(amount)) {
      currencies[currencyType] = amount;
      continue;
    }

    currencies[currencyType] = 0;
    patch[currencyType] = 0;
  }

  return {currencies, patch};
}

function createEmptyWalletCurrencies(): WalletCurrencies {
  return {
    seed: 0,
    elementCore: 0,
    noaMemory: 0,
    blessingTicket: 0,
  };
}

function isValidCurrencyAmount(value: unknown): value is number {
  return typeof value === "number" &&
    Number.isInteger(value) &&
    value >= 0;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" &&
    value !== null &&
    !Array.isArray(value);
}
