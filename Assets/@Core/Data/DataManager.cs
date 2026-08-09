using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public readonly struct SaveResult
{
    public bool IsSuccess { get; }

    public SaveResult(bool isSuccess)
    {
        IsSuccess = isSuccess;
    }
}

public class DataManager
{
    private readonly LocalSaveRepository _localRepo = new();
    private readonly FirebaseSaveRepository _remoteRepo = new();
    private readonly FirebaseFunctionClient _functionClient = new();
    private bool _isInitialized;

    public SaveData CurrentSaveData { get; private set; } = new();

    public void Initialize()
    {
        if (_isInitialized)
            return;

        _localRepo.Initialize();
        _isInitialized = true;
        Debug.Log("[DataManager] Initialized.");
    }

    public async Task LoadAsync()
    {
        Initialize();
        await LoadServerFirstAsync(CreateContext());
    }

    public bool DeleteLocalSave(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        CurrentSaveData = new SaveData();
        try
        {
            Initialize();
            _localRepo.Delete(new SaveContext(userId));
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[DataManager] Local account data deletion failed: {exception}");
            return false;
        }
    }

    public SaveResult Save(SavePatch patch)
    {
        SavePatch snapshot = CreateValidatedSnapshot(patch);
        if (snapshot == null)
            return new SaveResult(false);

        try
        {
            Initialize();
            SaveData candidate = CurrentSaveData.CreateSnapshot();
            SaveFieldSchema.Apply(candidate, snapshot);
            candidate.UpdatedAtUtcTicks = DateTime.UtcNow.Ticks;

            bool isSaved = TrySaveLocal(candidate, CreateContext(), snapshot.GetDebugSummary());
            if (isSaved)
                CurrentSaveData = candidate;

            return new SaveResult(isSaved);
        }
        catch (Exception exception)
        {
            LogSaveFailure(snapshot.GetDebugSummary(), exception);
            return new SaveResult(false);
        }
    }

    public async Task<TResponse> SaveAsync<TResponse>(
        string functionName,
        Dictionary<string, object> request,
        Func<TResponse, SavePatch> createLocalPatch)
        where TResponse : class
    {
        TResponse response = await RequestAsync<TResponse>(functionName, request);
        if (response == null)
            return null;

        if (createLocalPatch == null)
            return response;

        SavePatch patch;
        try
        {
            patch = createLocalPatch.Invoke(response);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[DataManager] Failed to create a local cache patch after {functionName}: {exception}");
            return response;
        }

        SavePatch snapshot = CreateValidatedSnapshot(patch, false);
        if (snapshot == null)
            return response;

        try
        {
            Initialize();
            SaveData candidate = CurrentSaveData.CreateSnapshot();
            SaveFieldSchema.Apply(candidate, snapshot);
            candidate.UpdatedAtUtcTicks = DateTime.UtcNow.Ticks;
            CurrentSaveData = candidate;

            if (!TrySaveLocal(candidate, CreateContext(), snapshot.GetDebugSummary()))
                Debug.LogWarning($"[DataManager] Server save succeeded, but local cache failed. Function: {functionName}");
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[DataManager] Server save succeeded, but local cache failed. Function: {functionName}, Exception: {exception}");
        }

        return response;
    }

    public Task<TResponse> RequestAsync<TResponse>(string functionName, Dictionary<string, object> request)
        where TResponse : class
    {
        return _functionClient.CallAsync<TResponse>(functionName, request);
    }

    public Task<object> RequestAsync(string functionName, Dictionary<string, object> request)
    {
        return _functionClient.CallAsync(functionName, request);
    }

    private SavePatch CreateValidatedSnapshot(SavePatch patch, bool logEmptyPatch = true)
    {
        if (patch == null || patch.IsEmpty)
        {
            if (logEmptyPatch)
                Debug.LogWarning("[DataManager] Save patch is empty.");
            return null;
        }

        SavePatch snapshot = patch.CreateSnapshot();
        string validationError = SaveFieldSchema.GetValidationError(snapshot);
        if (validationError == null)
            return snapshot;

        Debug.LogError($"[DataManager] Save rejected. Patch: {snapshot.GetDebugSummary()}, Reason: {validationError}");
        return null;
    }

    private bool TrySaveLocal(SaveData saveData, SaveContext context, string debugSummary)
    {
        try
        {
            _localRepo.Save(saveData, context);
            LogSaveSuccess(debugSummary);
            return true;
        }
        catch (Exception exception)
        {
            LogSaveFailure(debugSummary, exception);
            return false;
        }
    }

    private void LogSaveFailure(string debugSummary, Exception exception)
    {
        Debug.LogError($"[DataManager] Local save failed. Patch: {debugSummary}, Exception: {exception}");
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void LogSaveSuccess(string debugSummary)
    {
        Debug.Log($"[DataManager] Local save succeeded. Patch: {debugSummary}");
    }

    private async Task LoadServerFirstAsync(SaveContext context)
    {
        SaveData localData = _localRepo.Load(context);
        SaveData remoteData = null;

        try
        {
            remoteData = await _remoteRepo.LoadAsync(context);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[DataManager] Remote load failed. Using local save: {exception.Message}");
        }

        if (remoteData == null)
        {
            CurrentSaveData = localData ?? new SaveData();
            return;
        }

        CurrentSaveData = MergeRemoteOwnedData(localData, remoteData);
        if (!TrySaveLocal(CurrentSaveData, context, "<remote-load-cache>"))
            Debug.LogWarning("[DataManager] Failed to cache the remote save locally.");
    }

    private SaveData MergeRemoteOwnedData(SaveData localData, SaveData remoteData)
    {
        SaveData mergedData = localData?.CreateSnapshot() ?? new SaveData();
        mergedData.UpdatedAtUtcTicks = remoteData.UpdatedAtUtcTicks;
        mergedData.ClearedStageIds = new List<int>(remoteData.ClearedStageIds);
        mergedData.BestFlows = new Dictionary<string, int>(remoteData.BestFlows);
        mergedData.NoaLevels = new Dictionary<string, int>(remoteData.NoaLevels);
        mergedData.BlessingLevels = new Dictionary<string, int>(remoteData.BlessingLevels);
        mergedData.BlessingPieceCounts = new Dictionary<string, int>(remoteData.BlessingPieceCounts);
        mergedData.UnlockedNoaIds = new List<string>(remoteData.UnlockedNoaIds);
        mergedData.ApplyMigrations();
        return mergedData;
    }

    private SaveContext CreateContext()
    {
        if (!Managers.Auth.IsLoggedIn)
            throw new InvalidOperationException("Data operation requires a signed-in user.");

        return new SaveContext(Managers.Auth.UserId);
    }
}
