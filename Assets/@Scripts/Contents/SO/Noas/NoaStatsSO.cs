using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct NoaTierStats
{
    [SerializeField] private Define.NoaTier tier;
    [SerializeField] private float purifyPower;
    [SerializeField] private float purifyInterval;
    [SerializeField] private float purifyRange;

    public Define.NoaTier Tier => tier;
    public float PurifyPower => purifyPower;
    public float PurifyInterval => purifyInterval;
    public float PurifyRange => purifyRange;
}

[Serializable]
public struct NoaLevelStats
{
    [SerializeField] private int level;
    [SerializeField] private float purifyPowerBonus;
    [SerializeField] private float purifySpeedBonus;
    [SerializeField] private int upgradeCost;

    public int Level => level;
    public float PurifyPowerBonus => purifyPowerBonus;
    public float PurifySpeedBonus => purifySpeedBonus;
    public int UpgradeCost => upgradeCost;
}

public readonly struct NoaCalculatedStats
{
    public float PurifyPower { get; }
    public float PurifyInterval { get; }
    public float PurifyRange { get; }

    public NoaCalculatedStats(float purifyPower, float purifyInterval, float purifyRange)
    {
        PurifyPower = purifyPower;
        PurifyInterval = purifyInterval;
        PurifyRange = purifyRange;
    }
}

[CreateAssetMenu(fileName = "NoaStatsSO", menuName = "Noa Forest/Purify/Noa Stats")]
public class NoaStatsSO : ScriptableObject
{
    [SerializeField] private NoaTierStats[] tierStats = Array.Empty<NoaTierStats>();
    [SerializeField] private NoaLevelStats[] levelStats = Array.Empty<NoaLevelStats>();
    [Min(1f)]
    [SerializeField] private float elementAdvantageMultiplier = 1.25f;
    [Range(0.01f, 1f)]
    [SerializeField] private float elementDisadvantageMultiplier = 0.85f;
    [Min(0.01f)]
    [SerializeField] private float minimumPurifyInterval = 0.25f;

    public NoaTierStats[] TierStats => tierStats;
    public NoaLevelStats[] LevelStats => levelStats;
    public float ElementAdvantageMultiplier => elementAdvantageMultiplier;
    public float ElementDisadvantageMultiplier => elementDisadvantageMultiplier;
    public float MinimumPurifyInterval => minimumPurifyInterval;
    public int MaximumLevel => levelStats?.Length ?? 0;

    public NoaTierStats? GetTierStats(Define.NoaTier tier)
    {
        foreach (NoaTierStats tierStatsData in tierStats)
        {
            if (tierStatsData.Tier != tier)
            {
                continue;
            }

            return tierStatsData;
        }

        return null;
    }

    public NoaLevelStats? GetLevelStats(int level)
    {
        foreach (NoaLevelStats levelStatsData in levelStats)
        {
            if (levelStatsData.Level != level)
            {
                continue;
            }

            return levelStatsData;
        }

        return null;
    }

    public int GetUpgradeCost(int currentLevel)
    {
        if (currentLevel >= MaximumLevel)
            return 0;

        return GetLevelStats(currentLevel)?.UpgradeCost ?? 0;
    }

    public NoaCalculatedStats? GetCalculatedStats(Define.NoaTier tier, int level)
    {
        NoaTierStats? tierResult = GetTierStats(tier);
        NoaLevelStats? levelResult = GetLevelStats(level);
        if (tierResult == null || levelResult == null)
            return null;

        NoaTierStats tierStatsData = tierResult.Value;
        NoaLevelStats levelStatsData = levelResult.Value;
        float purifyPower = tierStatsData.PurifyPower * (1f + levelStatsData.PurifyPowerBonus);
        float purifyInterval = tierStatsData.PurifyInterval / (1f + levelStatsData.PurifySpeedBonus);

        return new NoaCalculatedStats(purifyPower, Mathf.Max(minimumPurifyInterval, purifyInterval), tierStatsData.PurifyRange);
    }

    private void OnValidate()
    {
        ValidateTierStats();
        ValidateLevelStats();
    }

    private void ValidateTierStats()
    {
        HashSet<Define.NoaTier> registeredTiers = new();

        foreach (NoaTierStats stats in tierStats)
        {
            if (!registeredTiers.Add(stats.Tier))
            {
                Debug.Log($"[NoaStatsSO] Duplicate tier: {stats.Tier}");
            }

            if (stats.PurifyPower <= 0f ||
                stats.PurifyInterval <= 0f ||
                stats.PurifyRange <= 0f)
            {
                Debug.Log($"[NoaStatsSO] Invalid tier stats: {stats.Tier}");
            }
        }

        foreach (Define.NoaTier tier in Enum.GetValues(typeof(Define.NoaTier)))
        {
            if (!registeredTiers.Contains(tier))
            {
                Debug.LogError($"[NoaStatsSO] Missing tier stats: {tier}");
            }
        }
    }

    private void ValidateLevelStats()
    {
        HashSet<int> registeredLevels = new();

        foreach (NoaLevelStats stats in levelStats)
        {
            if (!registeredLevels.Add(stats.Level))
            {
                Debug.Log($"[NoaStatsSO] Duplicate level: {stats.Level}");
            }

            if (stats.Level < 1 || stats.Level > 10 ||
                stats.PurifyPowerBonus < 0f ||
                stats.PurifySpeedBonus < 0f ||
                (stats.Level < levelStats.Length && stats.UpgradeCost <= 0) ||
                (stats.Level == levelStats.Length && stats.UpgradeCost != 0))
            {
                Debug.Log($"[NoaStatsSO] Invalid level stats: {stats.Level}");
            }
        }

        for (int level = 1; level <= 10; level++)
        {
            if (!registeredLevels.Contains(level))
            {
                Debug.LogError($"[NoaStatsSO] Missing level stats: {level}");
            }
        }
    }
}
