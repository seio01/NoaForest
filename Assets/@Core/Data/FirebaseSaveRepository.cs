using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

public class FirebaseSaveRepository
{
    private const string USER_SAVE_PATH = "users/{0}/privateSave/main";
    private const string USER_PURIFY_PROGRESS_PATH = "users/{0}/purifyProgress/main";

    private FirebaseFirestore _firestore;
    private Task _initializeTask;

    public Task InitializeAsync()
    {
        _initializeTask ??= InitializeInternalAsync();
        return _initializeTask;
    }

    public async Task<SaveData> LoadAsync(SaveContext context)
    {
        ValidateContext(context);

        SaveData saveData = await LoadDocumentAsync(
            GetUserSavePath(context.UserId),
            snapshot =>
            {
                Dictionary<string, object> data = snapshot.ToDictionary();
                return new SaveData
                {
                    SaveVersion = (int)GetLong(data, "saveVersion", 1),
                    UpdatedAtUtcTicks = GetLong(data, "updatedAtUtcTicks", 0),
                    ClearedStageIds = GetIntList(data, "clearedStageIds"),
                    NoaLevels = GetStringIntDictionary(data, "noaLevels"),
                    BlessingLevels = GetStringIntDictionary(data, "blessingLevels"),
                    BlessingPieceCounts = GetStringIntDictionary(data, "blessingPieceCounts"),
                    UnlockedNoaIds = GetStringList(data, "unlockedNoaIds")
                };
            });

        if (saveData == null)
            return null;

        saveData.BestFlows = await LoadDocumentAsync(
            GetUserPurifyProgressPath(context.UserId),
            snapshot => GetStringIntDictionary(snapshot.ToDictionary(), "bestFlows")) ?? new Dictionary<string, int>();
        saveData.ApplyMigrations();
        return saveData;
    }

    private async Task InitializeInternalAsync()
    {
        await Managers.Firebase.InitializeAsync();
        if (!Managers.Firebase.IsInitialized)
            throw new InvalidOperationException("Firebase is not initialized.");

        _firestore = FirebaseFirestore.DefaultInstance;
        Debug.Log("[FirebaseSaveRepository] Initialized.");
    }

    private async Task<T> LoadDocumentAsync<T>(
        string documentPath,
        Func<DocumentSnapshot, T> converter,
        Source source = Source.Server)
    {
        ValidateDocumentPath(documentPath);
        if (converter == null)
            throw new ArgumentNullException(nameof(converter));

        await InitializeAsync();
        DocumentSnapshot snapshot = await _firestore.Document(documentPath).GetSnapshotAsync(source);
        if (!snapshot.Exists)
        {
            Debug.Log($"[FirebaseSaveRepository] Document does not exist: {documentPath}");
            return default;
        }

        return converter.Invoke(snapshot);
    }

    private static string GetUserSavePath(string userId)
    {
        return string.Format(USER_SAVE_PATH, userId);
    }

    private static string GetUserPurifyProgressPath(string userId)
    {
        return string.Format(USER_PURIFY_PROGRESS_PATH, userId);
    }

    private static long GetLong(Dictionary<string, object> data, string fieldName, long defaultValue)
    {
        return data.TryGetValue(fieldName, out object value) && value != null ? Convert.ToInt64(value) : defaultValue;
    }

    private static List<int> GetIntList(Dictionary<string, object> data, string fieldName)
    {
        List<int> values = new();
        if (!data.ContainsKey(fieldName) || data[fieldName] is not IEnumerable<object> rawValues)
            return values;

        foreach (object rawValue in rawValues)
        {
            int value = Convert.ToInt32(rawValue);
            if (!values.Contains(value))
                values.Add(value);
        }

        return values;
    }

    private static Dictionary<string, int> GetStringIntDictionary(
        Dictionary<string, object> data,
        string fieldName)
    {
        Dictionary<string, int> values = new();
        if (!data.TryGetValue(fieldName, out object rawValue) ||
            rawValue is not IDictionary<string, object> rawValues)
            return values;

        foreach (KeyValuePair<string, object> entry in rawValues)
            values[entry.Key] = Convert.ToInt32(entry.Value);

        return values;
    }

    private static List<string> GetStringList(Dictionary<string, object> data, string fieldName)
    {
        List<string> values = new();
        if (!data.ContainsKey(fieldName) || data[fieldName] is not IEnumerable<object> rawValues)
            return values;

        foreach (object rawValue in rawValues)
        {
            string value = rawValue as string;
            if (!string.IsNullOrWhiteSpace(value) && !values.Contains(value))
                values.Add(value);
        }

        return values;
    }

    private static void ValidateDocumentPath(string documentPath)
    {
        if (string.IsNullOrWhiteSpace(documentPath))
            throw new ArgumentException("Document path is empty.", nameof(documentPath));
    }

    private static void ValidateContext(SaveContext context)
    {
        if (context == null || !context.HasUserId)
            throw new InvalidOperationException("Firebase load requires a signed-in user.");
    }
}
