using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public const int CURRENT_VERSION = 10;

    private static readonly string[] _defaultUnlockedNoaIds =
    {
        "noa_water_t1",
        "noa_fire_t1",
        "noa_wind_t1",
        "noa_earth_t1"
    };
    private static readonly Dictionary<string, string> _legacyBlessingIdMappings =
        new Dictionary<string, string>
        {
            { "blessing_echo_of_earth", "blessing_guardian_stone_of_earth" },
            { "blessing_elemental_oath", "blessing_oath_of_stars" },
            { "blessing_crown_of_four_seasons", "blessing_crown_of_breath" }
        };

    public int SaveVersion = CURRENT_VERSION;
    public long UpdatedAtUtcTicks;
    public UserData User = new();

    public int SelectedStageId = (int)Define.StageId.None;
    public bool IsBgmEnabled = true;
    public bool IsSfxEnabled;
    public bool IsVibrationEnabled;
    public List<int> ClearedStageIds = new();
    public Dictionary<string, int> BestFlows = new();
    public Dictionary<string, int> NoaLevels = new();
    public Dictionary<string, int> BlessingLevels = new();
    public Dictionary<string, int> BlessingPieceCounts = new();
    public List<string> UnlockedNoaIds = new(_defaultUnlockedNoaIds);

    public void ApplyMigrations()
    {
        User ??= new UserData();
        if (User.Level < 1)
            User.Level = 1;
        ClearedStageIds ??= new List<int>();
        BestFlows ??= new Dictionary<string, int>();
        NoaLevels ??= new Dictionary<string, int>();
        BlessingLevels ??= new Dictionary<string, int>();
        BlessingPieceCounts ??= new Dictionary<string, int>();
        MigrateBlessingIds(BlessingLevels);
        MigrateBlessingIds(BlessingPieceCounts);
        UnlockedNoaIds ??= new List<string>();
        foreach (string noaId in _defaultUnlockedNoaIds)
        {
            if (!UnlockedNoaIds.Contains(noaId))
                UnlockedNoaIds.Add(noaId);
        }
        SaveVersion = CURRENT_VERSION;
    }

    private static void MigrateBlessingIds(Dictionary<string, int> values)
    {
        foreach (KeyValuePair<string, string> mapping in _legacyBlessingIdMappings)
        {
            if (!values.Remove(mapping.Key, out int legacyValue))
                continue;

            if (!values.TryGetValue(mapping.Value, out int currentValue) || legacyValue > currentValue)
                values[mapping.Value] = legacyValue;
        }
    }

    public SaveData CreateSnapshot()
    {
        return new SaveData
        {
            SaveVersion = SaveVersion,
            UpdatedAtUtcTicks = UpdatedAtUtcTicks,
            User = User?.CreateSnapshot() ?? new UserData(),
            SelectedStageId = SelectedStageId,
            IsBgmEnabled = IsBgmEnabled,
            IsSfxEnabled = IsSfxEnabled,
            IsVibrationEnabled = IsVibrationEnabled,
            ClearedStageIds = new List<int>(ClearedStageIds),
            BestFlows = new Dictionary<string, int>(BestFlows),
            NoaLevels = new Dictionary<string, int>(NoaLevels),
            BlessingLevels = new Dictionary<string, int>(BlessingLevels),
            BlessingPieceCounts = new Dictionary<string, int>(BlessingPieceCounts),
            UnlockedNoaIds = new List<string>(UnlockedNoaIds)
        };
    }

    public int GetCollectionLevel(Define.CollectionType collectionType, string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return 1;

        Dictionary<string, int> levels = collectionType == Define.CollectionType.Noa ? NoaLevels : BlessingLevels;
        return levels.TryGetValue(itemId, out int level) ? Math.Max(1, level) : 1;
    }

    public int GetBlessingPieceCount(string blessingId)
    {
        if (string.IsNullOrWhiteSpace(blessingId))
            return 0;

        return BlessingPieceCounts.TryGetValue(blessingId, out int count) ? Math.Max(0, count) : 0;
    }

    public bool IsNoaUnlocked(string noaId)
    {
        return !string.IsNullOrWhiteSpace(noaId) && UnlockedNoaIds.Contains(noaId);
    }
}
