using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct PurifyWaveData
{
    [Min(1)]
    [SerializeField] private int waveNumber;
    [Min(1)]
    [SerializeField] private int moteCount;
    [Min(0.01f)]
    [SerializeField] private float spawnInterval;
    [Min(1f)]
    [SerializeField] private float duration;
    [Min(0.01f)]
    [SerializeField] private float healthMultiplier;
    [SerializeField] private Define.ElementType element;
    [SerializeField] private int clearRewardAmount;

    public int WaveNumber => waveNumber;
    public int MoteCount => moteCount;
    public float SpawnInterval => spawnInterval;
    public float Duration => duration;
    public float HealthMultiplier => healthMultiplier;
    public Define.ElementType Element => element;
    public int ClearRewardAmount => clearRewardAmount;
}

[CreateAssetMenu(fileName = "PurifyWaveSetSO", menuName = "Noa Forest/Purify/Wave Set")]
public class PurifyWaveSetSO : ScriptableObject
{
    public const int WAVE_COUNT = 20;
    public const int ELEMENT_WAVE_INTERVAL = 5;

    [SerializeField] private PurifyWaveData[] waves = Array.Empty<PurifyWaveData>();

    public PurifyWaveData[] Waves => waves;

    public PurifyWaveData GetWave(int waveNumber)
    {
        return waves[waveNumber - 1];
    }

    private void OnValidate()
    {
        if (waves == null || waves.Length != WAVE_COUNT)
        {
            Debug.Log($"[PurifyWaveSetSO] Wave count must be {WAVE_COUNT}: {name}");
            return;
        }

        for (int index = 0; index < waves.Length; index++)
        {
            ValidateWave(waves[index], index + 1);
        }
    }

    private void ValidateWave(PurifyWaveData waveData, int expectedWaveNumber)
    {
        if (waveData.WaveNumber != expectedWaveNumber)
        {
            Debug.Log($"[PurifyWaveSetSO] Invalid wave order: {waveData.WaveNumber}, expected {expectedWaveNumber}");
        }

        float finalSpawnTime = Mathf.Max(0, waveData.MoteCount - 1) * waveData.SpawnInterval;
        if (finalSpawnTime > waveData.Duration)
        {
            Debug.Log($"[PurifyWaveSetSO] Spawn time exceeds wave duration: Wave {waveData.WaveNumber}");
        }

        bool isElementWave = waveData.WaveNumber % ELEMENT_WAVE_INTERVAL == 0;
        if (isElementWave && waveData.Element == Define.ElementType.Neutral)
        {
            Debug.Log($"[PurifyWaveSetSO] Element wave cannot be Neutral: Wave {waveData.WaveNumber}");
        }
        else if (!isElementWave && waveData.Element != Define.ElementType.Neutral)
        {
            Debug.Log($"[PurifyWaveSetSO] Normal wave must be Neutral: Wave {waveData.WaveNumber}");
        }
    }
}
