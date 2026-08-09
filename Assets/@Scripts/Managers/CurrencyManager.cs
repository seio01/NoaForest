using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

public class CurrencyManager
{
    private const string FUNCTION_INITIALIZE_WALLET = "initializeWallet";
    private const string USER_WALLET_PATH = "users/{0}/wallet/main";
    private const string FIELD_CURRENCIES = "currencies";
    private const string FIELD_SEED = "seed";
    private const string FIELD_ELEMENT_CORE = "elementCore";
    private const string FIELD_NOA_MEMORY = "noaMemory";
    private const string FIELD_BLESSING_TICKET = "blessingTicket";

    private readonly Dictionary<Define.CurrencyType, int> _currencyByType = new();
    private FirebaseFirestore _firestore;
    private ListenerRegistration _listener;
    private Task _initializeTask;

    public event Action<Define.CurrencyType, int> CurrencyChanged;

    public bool IsReady { get; private set; }

    public Task InitializeAsync()
    {
        _initializeTask ??= InitializeInternalAsync();
        return _initializeTask;
    }

    public int GetCurrency(Define.CurrencyType currencyType)
    {
        return _currencyByType.TryGetValue(currencyType, out int amount) ? amount : 0;
    }

    public async Task<bool> RefreshFromServerAsync()
    {
        if (!await EnsureReadyForRequestAsync())
            return false;

        try
        {
            DocumentSnapshot snapshot = await GetWalletReference().GetSnapshotAsync(Source.Server);
            ApplySnapshot(snapshot);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[CurrencyManager] Server refresh failed: {exception.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        _listener?.Stop();
        _listener = null;
        _currencyByType.Clear();
        IsReady = false;
    }

    public void ResetForAccountChange()
    {
        Dispose();
        _firestore = null;
        _initializeTask = null;
    }

    private async Task InitializeInternalAsync()
    {
        await Managers.Firebase.InitializeAsync();
        await Managers.Auth.InitializeAsync();

        if (!Managers.Firebase.IsInitialized || !Managers.Auth.IsLoggedIn)
            throw new InvalidOperationException("[CurrencyManager] Firebase sign-in is required.");

        object initializeResponse = await Managers.Data.RequestAsync(
            FUNCTION_INITIALIZE_WALLET,
            new Dictionary<string, object>());
        if (initializeResponse == null)
            throw new InvalidOperationException("[CurrencyManager] Server wallet initialization failed.");

        _firestore = FirebaseFirestore.DefaultInstance;
        _listener = GetWalletReference().Listen(ApplySnapshot);
        await RefreshFromServerAsync();
        Debug.Log("[CurrencyManager] Realtime wallet listener started.");
    }

    private async Task<bool> EnsureReadyForRequestAsync()
    {
        if (_firestore != null && Managers.Auth.IsLoggedIn)
            return true;

        await InitializeAsync();
        return _firestore != null && Managers.Auth.IsLoggedIn;
    }

    private DocumentReference GetWalletReference()
    {
        return _firestore.Document(string.Format(USER_WALLET_PATH, Managers.Auth.UserId));
    }

    private void ApplySnapshot(DocumentSnapshot snapshot)
    {
        Dictionary<string, object> currencies = GetCurrencies(snapshot);
        bool isFirstSnapshot = !IsReady;
        IsReady = true;
        ApplyCurrency(Define.CurrencyType.Seed, GetAmount(currencies, FIELD_SEED), isFirstSnapshot);
        ApplyCurrency(Define.CurrencyType.ElementCore, GetAmount(currencies, FIELD_ELEMENT_CORE), isFirstSnapshot);
        ApplyCurrency(Define.CurrencyType.NoaMemory, GetAmount(currencies, FIELD_NOA_MEMORY), isFirstSnapshot);
        ApplyCurrency(Define.CurrencyType.BlessingTicket, GetAmount(currencies, FIELD_BLESSING_TICKET), isFirstSnapshot);
        ApplyCurrency(Define.CurrencyType.Energy, 0, isFirstSnapshot);
    }

    private void ApplyCurrency(Define.CurrencyType currencyType, int amount, bool forceNotification)
    {
        int previousAmount = GetCurrency(currencyType);
        _currencyByType[currencyType] = Mathf.Max(0, amount);
        if (forceNotification || previousAmount != _currencyByType[currencyType])
            CurrencyChanged?.Invoke(currencyType, _currencyByType[currencyType]);
    }

    private Dictionary<string, object> GetCurrencies(DocumentSnapshot snapshot)
    {
        if (snapshot == null || !snapshot.Exists)
            return new Dictionary<string, object>();

        Dictionary<string, object> data = snapshot.ToDictionary();
        return data.TryGetValue(FIELD_CURRENCIES, out object value) && value is IDictionary<string, object> currencies
            ? new Dictionary<string, object>(currencies)
            : new Dictionary<string, object>();
    }

    private int GetAmount(Dictionary<string, object> currencies, string fieldName)
    {
        return currencies.TryGetValue(fieldName, out object value) && value != null ? Convert.ToInt32(value) : 0;
    }
}
