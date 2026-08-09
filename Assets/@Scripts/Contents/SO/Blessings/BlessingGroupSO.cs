using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BlessingGroupSO", menuName = "Noa Forest/Purify/Blessing Group")]
public class BlessingGroupSO : ScriptableObject
{
    [SerializeField] private BlessingSO[] blessings = Array.Empty<BlessingSO>();

    [Header("Upgrade Piece Costs")]
    [SerializeField] private int[] pieceCostsCommon = { 2, 5, 9 };
    [SerializeField] private int[] pieceCostsRare = { 2, 4, 7 };
    [SerializeField] private int[] pieceCostsEpic = { 1, 3, 5 };
    [SerializeField] private int[] pieceCostsLegendary = { 1, 3, 5 };

    public BlessingSO[] Blessings => blessings;

    public BlessingSO GetBlessing(string id)
    {
        foreach (BlessingSO data in blessings)
        {
            if (!data ||
                !string.Equals(data.Id, id, StringComparison.Ordinal))
            {
                continue;
            }

            return data;
        }

        return null;
    }

    public int GetUpgradePieceCost(BlessingSO blessing, int currentLevel)
    {
        if (!blessing || currentLevel < 1 || currentLevel >= BlessingSO.MAX_LEVEL)
            return 0;

        int[] pieceCosts = GetUpgradePieceCosts(blessing.Rarity);
        int costIndex = currentLevel - 1;
        return pieceCosts != null && costIndex < pieceCosts.Length ? pieceCosts[costIndex] : 0;
    }

    private void OnValidate()
    {
        HashSet<string> registeredIds = new(StringComparer.Ordinal);
        foreach (BlessingSO blessing in blessings)
        {
            if (!blessing)
            {
                Debug.LogError(
                    "[BlessingGroupSO] Null BlessingSO is registered.");
                continue;
            }

            if (!registeredIds.Add(blessing.Id))
            {
                Debug.Log(
                    $"[BlessingGroupSO] Duplicate Id: {blessing.Id}");
            }
        }

        ValidateUpgradePieceCosts(pieceCostsCommon, Define.BlessingRarity.Common);
        ValidateUpgradePieceCosts(pieceCostsRare, Define.BlessingRarity.Rare);
        ValidateUpgradePieceCosts(pieceCostsEpic, Define.BlessingRarity.Epic);
        ValidateUpgradePieceCosts(pieceCostsLegendary, Define.BlessingRarity.Legendary);
    }

    private int[] GetUpgradePieceCosts(Define.BlessingRarity rarity)
    {
        switch (rarity)
        {
            case Define.BlessingRarity.Common:
                return pieceCostsCommon;
            case Define.BlessingRarity.Rare:
                return pieceCostsRare;
            case Define.BlessingRarity.Epic:
                return pieceCostsEpic;
            case Define.BlessingRarity.Legendary:
                return pieceCostsLegendary;
            default:
                return null;
        }
    }

    private void ValidateUpgradePieceCosts(int[] pieceCosts, Define.BlessingRarity rarity)
    {
        if (pieceCosts == null || pieceCosts.Length != BlessingSO.MAX_LEVEL - 1)
        {
            Debug.LogError($"[BlessingGroupSO] {rarity} piece cost count must be {BlessingSO.MAX_LEVEL - 1}.");
            return;
        }

        foreach (int pieceCost in pieceCosts)
        {
            if (pieceCost <= 0)
                Debug.LogError($"[BlessingGroupSO] {rarity} piece cost must be positive.");
        }
    }
}
