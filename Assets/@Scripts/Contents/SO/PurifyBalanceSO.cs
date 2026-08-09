using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ElementWaveData
{
    [Min(1)]
    [SerializeField] private int waveNumber;
    [SerializeField] private Define.ElementType element;

    public int WaveNumber => waveNumber;
    public Define.ElementType Element => element;
}

[Serializable]
public class PurifyForestBreathLevelData
{
    [Min(1)]
    [SerializeField] private int purificationPower = 1;
    [Min(0)]
    [SerializeField] private int summonCostMultiplier;

    public int PurificationPower => purificationPower;
    public int SummonCostMultiplier => summonCostMultiplier;
}

[CreateAssetMenu(fileName = "PurifyBalanceSO", menuName = "Noa Forest/Purify/Balance")]
public class PurifyBalanceSO : ScriptableObject
{
    private const float SUMMON_TIER_PROBABILITY_TOTAL = 1f;

    [Header("Summon")]
    [Min(0f)]
    [SerializeField] private float startingEnergy = 40f;
    [Min(1)]
    // TODO: 밸런스 테스트가 끝나면 PurifyBalanceSO 에셋의 기본 소환 비용을 20으로 복구한다.
    [SerializeField] private int baseSummonCost = 20;
    [Range(0f, 1f)]
    [SerializeField] private float tier1SummonProbability = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float tier2SummonProbability = 0.15f;
    [Range(0f, 1f)]
    [SerializeField] private float tier3SummonProbability = 0.05f;
    [Range(0f, 1f)]
    [SerializeField] private float baseElementProbability = 0.25f;
    [Range(0f, 1f)]
    [SerializeField] private float selectedElementProbability = 0.55f;

    [Header("Spirit Tree")]
    [Min(1)]
    [SerializeField] private int startingTreeHealth = 100;
    [Min(1)]
    [SerializeField] private int maximumTreeHealth = 100;
    [Min(0)]
    [SerializeField] private int normalWaveRecovery = 5;
    [Min(0)]
    [SerializeField] private int elementWaveRecovery = 10;

    [Header("Forest Breath")]
    [Min(0.1f)]
    [SerializeField] private float forestBreathChargeDuration = 25f;
    [SerializeField] private PurifyForestBreathLevelData[] forestBreathLevels = Array.Empty<PurifyForestBreathLevelData>();

    public float StartingEnergy => startingEnergy;
    public int BaseSummonCost => baseSummonCost;
    public float Tier1SummonProbability => tier1SummonProbability;
    public float Tier2SummonProbability => tier2SummonProbability;
    public float Tier3SummonProbability => tier3SummonProbability;
    public float BaseElementProbability => baseElementProbability;
    public float SelectedElementProbability => selectedElementProbability;
    public int StartingTreeHealth => startingTreeHealth;
    public int MaximumTreeHealth => maximumTreeHealth;
    public int NormalWaveRecovery => normalWaveRecovery;
    public int ElementWaveRecovery => elementWaveRecovery;
    public float ForestBreathChargeDuration => forestBreathChargeDuration;
    public PurifyForestBreathLevelData[] ForestBreathLevels => forestBreathLevels;

    private void OnValidate()
    {
        if (startingTreeHealth > maximumTreeHealth)
        {
            Debug.Log("[PurifyBalanceSO] Starting tree health cannot exceed maximum health.");
        }

        float probabilityTotal = tier1SummonProbability +
                                 tier2SummonProbability +
                                 tier3SummonProbability;
        if (!Mathf.Approximately(probabilityTotal, SUMMON_TIER_PROBABILITY_TOTAL))
        {
            Debug.LogError(
                $"[PurifyBalanceSO] Summon tier probabilities must total " +
                $"{SUMMON_TIER_PROBABILITY_TOTAL:P0}. Current: {probabilityTotal:P0}");
        }

        if (forestBreathLevels == null || forestBreathLevels.Length == 0)
        {
            Debug.LogError("[PurifyBalanceSO] Forest Breath requires at least one level.");
        }
    }
}
