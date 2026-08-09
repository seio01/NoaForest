using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct PurifySummonTierWeights
{
    public float Tier1 { get; }
    public float Tier2 { get; }
    public float Tier3 { get; }

    public PurifySummonTierWeights(float tier1, float tier2, float tier3)
    {
        Tier1 = tier1;
        Tier2 = tier2;
        Tier3 = tier3;
    }
}

public class PurifyBlessingEffects
{
    private readonly Dictionary<Define.BlessingEffectType, float> _values = new();

    public static PurifyBlessingEffects Empty { get; } = new(null, null);

    public float SummonCostReductionRate => GetValue(Define.BlessingEffectType.SummonCostReductionRate);
    public float PurifySpeedBonusRate => GetValue(Define.BlessingEffectType.PurifySpeedBonusRate);
    public float PurifyPowerBonusRate => GetValue(Define.BlessingEffectType.PurifyPowerBonusRate);
    public int WaveEndHealBonus => Mathf.RoundToInt(GetValue(Define.BlessingEffectType.WaveEndHealBonus));
    public float MoteKillRewardBonusRate => GetValue(Define.BlessingEffectType.MoteKillRewardBonusRate);
    public float MoteEscapeDamageReductionRate => GetValue(Define.BlessingEffectType.MoteEscapeDamageReductionRate);
    public float HigherTierSummonProbabilityBonusRate => GetValue(Define.BlessingEffectType.HigherTierSummonProbabilityBonusRate);
    public float ForestBreathChargeTimeReductionRate => GetValue(Define.BlessingEffectType.ForestBreathChargeTimeReductionRate);

    public PurifyBlessingEffects(List<BlessingSO> blessings, Func<BlessingSO, int> levelProvider)
    {
        if (blessings == null || levelProvider == null) return;

        foreach (var blessing in blessings)
        {
            if (!blessing) continue;

            int level = Mathf.Clamp(levelProvider(blessing), 1, BlessingSO.MAX_LEVEL);
            foreach (var effect in blessing.Effects)
            {
                float currentValue = GetValue(effect.Type);
                _values[effect.Type] = currentValue + effect.GetValue(level);
            }
        }
    }

    public int CalculateSummonCost(int baseCost)
    {
        float reductionRate = Mathf.Clamp01(SummonCostReductionRate);
        return Mathf.Max(1, Mathf.RoundToInt(baseCost * (1f - reductionRate)));
    }

    public float CalculatePurifyPower(float basePower)
    {
        return basePower * (1f + Mathf.Max(0f, PurifyPowerBonusRate));
    }

    public float CalculatePurifyInterval(float baseInterval, float minimumInterval)
    {
        float speedMultiplier = 1f + Mathf.Max(0f, PurifySpeedBonusRate);
        return Mathf.Max(minimumInterval, baseInterval / speedMultiplier);
    }

    public float CalculateMoteKillReward(float baseReward)
    {
        return baseReward * (1f + Mathf.Max(0f, MoteKillRewardBonusRate));
    }

    public int CalculateMoteEscapeDamage(int baseDamage)
    {
        float reductionRate = Mathf.Clamp01(MoteEscapeDamageReductionRate);
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * (1f - reductionRate)));
    }

    public float CalculateForestBreathChargeDuration(float baseDuration)
    {
        float reductionRate = Mathf.Clamp01(ForestBreathChargeTimeReductionRate);
        return baseDuration * (1f - reductionRate);
    }

    public PurifySummonTierWeights CalculateSummonTierWeights(float tier1Weight, float tier2Weight, float tier3Weight)
    {
        float totalWeight = tier1Weight + tier2Weight + tier3Weight;
        float higherTierWeight = tier2Weight + tier3Weight;
        if (totalWeight <= 0f || tier1Weight <= 0f || higherTierWeight <= 0f)
            return new PurifySummonTierWeights(tier1Weight, tier2Weight, tier3Weight);

        float transferWeight = Mathf.Min(tier1Weight, Mathf.Max(0f, HigherTierSummonProbabilityBonusRate) * totalWeight);
        float tier2Ratio = tier2Weight / higherTierWeight;
        return new PurifySummonTierWeights(
            tier1Weight - transferWeight,
            tier2Weight + transferWeight * tier2Ratio,
            tier3Weight + transferWeight * (1f - tier2Ratio));
    }

    private float GetValue(Define.BlessingEffectType type)
    {
        return _values.ContainsKey(type) ? _values[type] : 0f;
    }
}
