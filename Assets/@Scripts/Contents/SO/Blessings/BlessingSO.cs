using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct BlessingEffectData
{
    [SerializeField] private Define.BlessingEffectType type;
    [SerializeField] private float[] levelValues;

    public Define.BlessingEffectType Type => type;
    public float[] LevelValues => levelValues;

    public float GetValue(int level)
    {
        return levelValues[level - 1];
    }

    public string GetDisplayValue(int currentLevel, int? nextLevel)
    {
        if (levelValues == null || currentLevel < 1 || currentLevel > levelValues.Length)
            return "-";

        float multiplier = GetDisplayMultiplier();
        string unit = GetDisplayUnit();
        float currentValue = GetValue(currentLevel) * multiplier;
        float? nextValue = nextLevel.HasValue && nextLevel.Value <= levelValues.Length
            ? GetValue(nextLevel.Value) * multiplier
            : null;
        return $"{FormatNumber(currentValue)}{unit}{FormatDelta(currentValue, nextValue, unit)}";
    }

    private float GetDisplayMultiplier()
    {
        switch (type)
        {
            case Define.BlessingEffectType.SummonCostReductionRate:
            case Define.BlessingEffectType.PurifySpeedBonusRate:
            case Define.BlessingEffectType.PurifyPowerBonusRate:
            case Define.BlessingEffectType.MoteKillRewardBonusRate:
            case Define.BlessingEffectType.MoteEscapeDamageReductionRate:
            case Define.BlessingEffectType.HigherTierSummonProbabilityBonusRate:
            case Define.BlessingEffectType.ForestBreathChargeTimeReductionRate:
                return 100f;
            default:
                return 1f;
        }
    }

    private string GetDisplayUnit()
    {
        switch (type)
        {
            case Define.BlessingEffectType.SummonCostReductionRate:
            case Define.BlessingEffectType.PurifySpeedBonusRate:
            case Define.BlessingEffectType.PurifyPowerBonusRate:
            case Define.BlessingEffectType.MoteKillRewardBonusRate:
            case Define.BlessingEffectType.MoteEscapeDamageReductionRate:
            case Define.BlessingEffectType.HigherTierSummonProbabilityBonusRate:
            case Define.BlessingEffectType.ForestBreathChargeTimeReductionRate:
                return "%";
            default:
                return string.Empty;
        }
    }

    private string FormatDelta(float currentValue, float? nextValue, string unit)
    {
        if (!nextValue.HasValue)
            return string.Empty;

        float delta = nextValue.Value - currentValue;
        if (Mathf.Approximately(delta, 0f))
            return string.Empty;

        string sign = delta > 0f ? "+" : string.Empty;
        return $"<color={Constants.Olive2}>({sign}{FormatNumber(delta)}{unit})</color>";
    }

    private string FormatNumber(float value)
    {
        return value.ToString("0.##");
    }
}

[CreateAssetMenu(fileName = "BlessingSO", menuName = "Noa Forest/Purify/Blessing")]
public class BlessingSO : ScriptableObject, ICollectionItem
{
    public const int MAX_LEVEL = 4;

    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [TextArea(1, 3)]
    [SerializeField] private string simplifiedDescriptionTemplate;
    [TextArea(2, 5)]
    [SerializeField] private string description;
    [TextArea(2, 5)]
    [SerializeField] private string effectDescriptionTemplate;
    [SerializeField] private Define.BlessingRarity rarity;
    [SerializeField] private Define.BlessingCategory category;
    [SerializeField] private bool isUsable;
    [SerializeField] private int[] upgradeCosts = Array.Empty<int>();
    [SerializeField] private BlessingEffectData[] effects = Array.Empty<BlessingEffectData>();

    public string Id => id;
    public Define.CollectionType CollectionType => Define.CollectionType.Blessing;
    public string DisplayName => displayName;
    public string SimplifiedDescriptionTemplate => simplifiedDescriptionTemplate;
    public string Description => description;
    public string EffectDescriptionTemplate => effectDescriptionTemplate;
    public Define.BlessingRarity Rarity => rarity;
    public Define.BlessingCategory Category => category;
    public bool IsUsable => isUsable;
    public int[] UpgradeCosts => upgradeCosts;
    public BlessingEffectData[] Effects => effects;

    public int GetUpgradeCost(int currentLevel)
    {
        return upgradeCosts[currentLevel - 1];
    }

    public string GetEffectDescription(int currentLevel, bool includeNextLevel = true)
    {
        return GetFormattedDescription(effectDescriptionTemplate, currentLevel, includeNextLevel);
    }

    public string GetSimplifiedDescription(int currentLevel)
    {
        return GetFormattedDescription(simplifiedDescriptionTemplate, currentLevel, false);
    }

    private string GetFormattedDescription(string descriptionTemplate, int currentLevel, bool includeNextLevel)
    {
        if (string.IsNullOrWhiteSpace(descriptionTemplate))
            return "-";

        int clampedLevel = Mathf.Clamp(currentLevel, 1, MAX_LEVEL);
        int? nextLevel = includeNextLevel && clampedLevel < MAX_LEVEL ? clampedLevel + 1 : null;
        string result = descriptionTemplate;
        foreach (BlessingEffectData effect in effects)
        {
            string token = $"{{{effect.Type}}}";
            result = result.Replace(token, effect.GetDisplayValue(clampedLevel, nextLevel));
        }

        return result;
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.Log($"[BlessingSO] Id is empty: {name}");
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            Debug.Log($"[BlessingSO] DisplayName is empty: {id}");
        }

        if (string.IsNullOrWhiteSpace(simplifiedDescriptionTemplate))
        {
            Debug.Log($"[BlessingSO] SimplifiedDescriptionTemplate is empty: {id}");
        }

        ValidateEffectDescription();
        ValidateUpgradeCosts();
        ValidateEffects();
    }

    private void ValidateEffectDescription()
    {
        if (string.IsNullOrWhiteSpace(effectDescriptionTemplate))
        {
            Debug.Log($"[BlessingSO] EffectDescriptionTemplate is empty: {id}");
            return;
        }

        foreach (BlessingEffectData effect in effects)
        {
            string token = $"{{{effect.Type}}}";
            if (effectDescriptionTemplate.IndexOf(token, StringComparison.Ordinal) < 0)
                Debug.Log($"[BlessingSO] Effect token is missing: {id}, {token}");
        }
    }

    private void ValidateUpgradeCosts()
    {
        int expectedCount = MAX_LEVEL - 1;
        if (upgradeCosts == null || upgradeCosts.Length != expectedCount)
        {
            Debug.Log(
                $"[BlessingSO] Upgrade cost count must be {expectedCount}: {id}");
            return;
        }

        foreach (int upgradeCost in upgradeCosts)
        {
            if (upgradeCost <= 0)
            {
                Debug.Log($"[BlessingSO] Invalid upgrade cost: {id}");
            }
        }
    }

    private void ValidateEffects()
    {
        if (effects == null || effects.Length == 0)
        {
            Debug.LogError($"[BlessingSO] Effect is missing: {id}");
            return;
        }

        HashSet<Define.BlessingEffectType> registeredEffects = new();
        foreach (BlessingEffectData effect in effects)
        {
            if (!registeredEffects.Add(effect.Type))
            {
                Debug.Log(
                    $"[BlessingSO] Duplicate effect: {id}, {effect.Type}");
            }

            if (effect.LevelValues == null ||
                effect.LevelValues.Length != MAX_LEVEL)
            {
                Debug.Log(
                    $"[BlessingSO] Effect value count must be {MAX_LEVEL}: " +
                    $"{id}, {effect.Type}");
                continue;
            }

            foreach (float levelValue in effect.LevelValues)
            {
                if (levelValue < 0f)
                {
                    Debug.Log(
                        $"[BlessingSO] Effect value cannot be negative: " +
                        $"{id}, {effect.Type}");
                }
            }
        }
    }
}
