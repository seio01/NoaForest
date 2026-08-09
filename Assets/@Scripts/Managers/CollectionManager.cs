using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class CollectionManager
{
    private const string FUNCTION_UPGRADE_COLLECTION = "upgradeCollection";
    private const string FUNCTION_UNLOCK_NOA = "unlockNoa";
    private const string FUNCTION_SUMMON_BLESSING = "summonBlessing";
    private const string SERVER_TYPE_NOA = "noa";
    private const string SERVER_TYPE_BLESSING = "blessing";

    public event Action<Define.CollectionType, string, int> LevelChanged;
    public event Action<string> NoaUnlocked;

    public bool IsUpgradeRequestPending { get; private set; }
    public bool IsUnlockRequestPending { get; private set; }
    public bool IsSummonRequestPending { get; private set; }

    public int GetLevel(ICollectionItem item)
    {
        if (item == null)
            return 1;

        return Managers.Data.CurrentSaveData.GetCollectionLevel(item.CollectionType, item.Id);
    }

    public bool CanUpgrade(ICollectionItem item)
    {
        if (item == null || !IsUnlocked(item) || IsUpgradeRequestPending || !Managers.Currency.IsReady)
            return false;

        int currentLevel = GetLevel(item);
        int maximumLevel = GetMaximumLevel(item);
        int cost = GetUpgradeCost(item, currentLevel);
        Define.CurrencyType? currencyType = GetCurrencyType(item);
        if (!currencyType.HasValue ||
            currentLevel >= maximumLevel ||
            cost <= 0 ||
            Managers.Currency.GetCurrency(currencyType.Value) < cost)
            return false;

        if (item is not BlessingSO blessingData)
            return true;

        int pieceCost = GetUpgradePieceCost(blessingData, currentLevel);
        return pieceCost > 0 && GetBlessingPieceCount(blessingData) >= pieceCost;
    }

    public bool IsUnlocked(ICollectionItem item)
    {
        switch (item)
        {
            case NoaSO noaData:
                return Managers.Data.CurrentSaveData.IsNoaUnlocked(noaData.Id);
            case BlessingSO blessingData:
                return Managers.Data.CurrentSaveData.BlessingLevels.ContainsKey(blessingData.Id);
            default:
                return false;
        }
    }

    public bool CanUnlock(NoaSO noaData)
    {
        if (!noaData || IsUnlocked(noaData) || IsUnlockRequestPending || !Managers.Currency.IsReady || noaData.UnlockCost <= 0)
            return false;

        return IsPreviousTierUnlocked(noaData) &&
               Managers.Currency.GetCurrency(Define.CurrencyType.NoaMemory) >= noaData.UnlockCost;
    }

    public bool CanSummonBlessing()
    {
        return !IsSummonRequestPending &&
               Managers.Currency.IsReady &&
               Managers.Currency.GetCurrency(Define.CurrencyType.BlessingTicket) > 0 &&
               HasSummonableBlessing();
    }

    public bool HasSummonableBlessing()
    {
        BlessingGroupSO blessingGroup = Managers.GameData.Blessings;
        if (!blessingGroup)
            return false;

        Dictionary<string, int> blessingLevels = Managers.Data.CurrentSaveData.BlessingLevels;
        foreach (BlessingSO blessingData in blessingGroup.Blessings)
        {
            if (!blessingData)
                continue;

            if (!blessingLevels.TryGetValue(blessingData.Id, out int level) || level < BlessingSO.MAX_LEVEL)
                return true;
        }

        return false;
    }

    public bool IsPreviousTierUnlocked(NoaSO noaData)
    {
        if (!noaData)
            return false;
        if (noaData.Tier <= Define.NoaTier.Tier1)
            return true;

        NoaSO previousTierNoa = GetPreviousTierNoa(noaData);
        return previousTierNoa && IsUnlocked(previousTierNoa);
    }

    public async Task<bool> UnlockNoaAsync(NoaSO noaData)
    {
        if (!CanUnlock(noaData))
            return false;

        IsUnlockRequestPending = true;
        try
        {
            Dictionary<string, object> request = new()
            {
                { "itemId", noaData.Id }
            };
            NoaUnlockResponse response = await Managers.Data.SaveAsync<NoaUnlockResponse>(
                FUNCTION_UNLOCK_NOA,
                request,
                serverResponse => new SavePatch().Set(
                    SaveField.UnlockedNoaIds,
                    new List<string>(serverResponse.UnlockedNoaIds)));
            if (response == null)
                return false;

            NoaUnlocked?.Invoke(response.ItemId);
            await Managers.Currency.RefreshFromServerAsync();
            return true;
        }
        finally
        {
            IsUnlockRequestPending = false;
        }
    }

    public async Task<BlessingSummonResponse> SummonBlessingAsync()
    {
        if (!CanSummonBlessing())
            return null;

        IsSummonRequestPending = true;
        try
        {
            BlessingSummonResponse response = await Managers.Data.SaveAsync<BlessingSummonResponse>(
                FUNCTION_SUMMON_BLESSING,
                new Dictionary<string, object>(),
                serverResponse =>
                {
                    Dictionary<string, int> blessingLevels =
                        new(Managers.Data.CurrentSaveData.BlessingLevels)
                        {
                            [serverResponse.ItemId] = Mathf.Max(1, serverResponse.Level)
                        };
                    Dictionary<string, int> blessingPieceCounts =
                        new(Managers.Data.CurrentSaveData.BlessingPieceCounts)
                        {
                            [serverResponse.ItemId] = Mathf.Max(0, serverResponse.PieceBalance)
                        };
                    return new SavePatch()
                        .Set(SaveField.BlessingLevels, blessingLevels)
                        .Set(SaveField.BlessingPieceCounts, blessingPieceCounts);
                });
            if (response == null)
                return null;

            await Managers.Currency.RefreshFromServerAsync();
            return response;
        }
        finally
        {
            IsSummonRequestPending = false;
        }
    }

    public async Task<bool> UpgradeAsync(ICollectionItem item)
    {
        if (!CanUpgrade(item))
            return false;

        IsUpgradeRequestPending = true;
        try
        {
            Dictionary<string, object> request = new()
            {
                { "collectionType", GetServerCollectionType(item.CollectionType) },
                { "itemId", item.Id }
            };
            CollectionUpgradeResponse response = await Managers.Data.SaveAsync<CollectionUpgradeResponse>(
                FUNCTION_UPGRADE_COLLECTION,
                request,
                serverResponse =>
                {
                    Dictionary<string, int> levels = GetUpdatedLevels(item, serverResponse.Level);
                    SavePatch patch = new SavePatch().Set(GetSaveField(item.CollectionType), levels);
                    if (item is not BlessingSO blessingData || !serverResponse.PieceBalance.HasValue)
                        return patch;

                    Dictionary<string, int> pieceCounts =
                        new(Managers.Data.CurrentSaveData.BlessingPieceCounts)
                        {
                            [blessingData.Id] = Mathf.Max(0, serverResponse.PieceBalance.Value)
                        };
                    return patch.Set(SaveField.BlessingPieceCounts, pieceCounts);
                });
            if (response == null)
                return false;

            LevelChanged?.Invoke(item.CollectionType, response.ItemId, response.Level);
            await Managers.Currency.RefreshFromServerAsync();
            return true;
        }
        finally
        {
            IsUpgradeRequestPending = false;
        }
    }

    public int GetMaximumLevel(ICollectionItem item)
    {
        switch (item)
        {
            case NoaSO:
                return Managers.GameData.Noas?.Stats?.MaximumLevel ?? 0;
            case BlessingSO:
                return BlessingSO.MAX_LEVEL;
            default:
                return 0;
        }
    }

    public int GetUpgradeCost(ICollectionItem item, int currentLevel)
    {
        switch (item)
        {
            case NoaSO:
                return Managers.GameData.Noas?.Stats?.GetUpgradeCost(currentLevel) ?? 0;
            case BlessingSO blessingData when currentLevel < BlessingSO.MAX_LEVEL:
                return blessingData.GetUpgradeCost(currentLevel);
            default:
                return 0;
        }
    }

    public int GetUpgradePieceCost(BlessingSO blessingData, int currentLevel)
    {
        return Managers.GameData.Blessings?.GetUpgradePieceCost(blessingData, currentLevel) ?? 0;
    }

    public int GetBlessingPieceCount(BlessingSO blessingData)
    {
        return blessingData ? Managers.Data.CurrentSaveData.GetBlessingPieceCount(blessingData.Id) : 0;
    }

    private static NoaSO GetPreviousTierNoa(NoaSO noaData)
    {
        if (!noaData || noaData.Tier <= Define.NoaTier.Tier1)
            return null;

        Define.NoaTier previousTier = (Define.NoaTier)((int)noaData.Tier - 1);
        return Managers.GameData.Noas?.GetNoa(noaData.Element, previousTier);
    }

    private static Define.CurrencyType? GetCurrencyType(ICollectionItem item)
    {
        switch (item)
        {
            case NoaSO:
                return Define.CurrencyType.Seed;
            case BlessingSO:
                return Define.CurrencyType.ElementCore;
            default:
                return null;
        }
    }

    private static Dictionary<string, int> GetUpdatedLevels(ICollectionItem item, int level)
    {
        Dictionary<string, int> levels = item.CollectionType == Define.CollectionType.Noa
            ? new Dictionary<string, int>(Managers.Data.CurrentSaveData.NoaLevels)
            : new Dictionary<string, int>(Managers.Data.CurrentSaveData.BlessingLevels);
        levels[item.Id] = level;
        return levels;
    }

    private static SaveField GetSaveField(Define.CollectionType collectionType)
    {
        return collectionType == Define.CollectionType.Noa ? SaveField.NoaLevels : SaveField.BlessingLevels;
    }

    private static string GetServerCollectionType(Define.CollectionType collectionType)
    {
        return collectionType == Define.CollectionType.Noa ? SERVER_TYPE_NOA : SERVER_TYPE_BLESSING;
    }
}
