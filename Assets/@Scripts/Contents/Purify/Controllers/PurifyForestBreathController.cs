using System;
using System.Collections;
using UnityEngine;

public class PurifyForestBreathController : MonoBehaviour
{
    [Header("Effects")]
    [SerializeField] private NoaParticleEffect particleEffectForestBreath;

    private PurifyGameManager _purifyManager;
    private PurifyWaveController _waveController;
    private PurifyBalanceSO _balance;
    private Coroutine _chargeCoroutine;
    private float _elapsedChargeTime;
    private bool _isCharged;

    public event Action<float> ChargeProgressChanged;
    public event Action ReadyChanged;
    public event Action<int> LevelChanged;

    public int CurrentLevel { get; private set; } = 1;
    public bool IsMaximumLevel => CurrentLevel >= _balance.ForestBreathLevels.Length;
    public bool CanTrigger => _purifyManager.IsRunning && _isCharged;
    public bool CanUpgrade => _purifyManager.IsRunning && !IsMaximumLevel && HasEnoughUpgradeEnergy;
    public bool HasEnoughUpgradeEnergy => _purifyManager && _purifyManager.CurrentEnergy >= UpgradeCost;
    public float ChargeProgress => Mathf.Clamp01(_elapsedChargeTime / ChargeDuration);
    public int CurrentPower => GetLevelData(CurrentLevel)?.PurificationPower ?? 0;
    public int NextPower => GetLevelData(CurrentLevel + 1)?.PurificationPower ?? CurrentPower;
    public int NextCostMultiplier => GetLevelData(CurrentLevel + 1)?.SummonCostMultiplier ?? 0;
    public int UpgradeCost => _balance.BaseSummonCost * NextCostMultiplier;
    private float ChargeDuration => _purifyManager.BlessingEffects.CalculateForestBreathChargeDuration(_balance.ForestBreathChargeDuration);

    public void Initialize(PurifyGameManager purifyManager, PurifyWaveController waveController, PurifyBalanceSO balance)
    {
        _purifyManager = purifyManager;
        _waveController = waveController;
        _balance = balance;
        ResetBreath();
    }

    public void StartCharging()
    {
        if (!_purifyManager.IsRunning || _isCharged || _chargeCoroutine != null) return;

        _chargeCoroutine = StartCoroutine(ChargeRoutine());
    }

    public void StopCharging()
    {
        if (_chargeCoroutine == null) return;

        StopCoroutine(_chargeCoroutine);
        _chargeCoroutine = null;
    }

    public void ResetBreath()
    {
        StopCharging();
        CurrentLevel = 1;
        _elapsedChargeTime = 0f;
        SetIsCharged(false);
        LevelChanged?.Invoke(CurrentLevel);
        ChargeProgressChanged?.Invoke(0f);
    }

    public void Trigger()
    {
        if (!CanTrigger) return;

        Managers.Sound.Play(Define.AudioClip.ForestBreath, Define.AudioSourceType.Sfx, Define.AudioPath.Purify, 3f);

        PlayForestBreathEffect();
        _waveController.DamageAllActiveMotes(CurrentPower);

        _elapsedChargeTime = 0f;
        SetIsCharged(false);
        ChargeProgressChanged?.Invoke(0f);
        StartCharging();
    }

    private void PlayForestBreathEffect()
    {
        if (!particleEffectForestBreath) return;

        particleEffectForestBreath.PlayStandalone();
    }

    public void Upgrade()
    {
        if (!CanUpgrade || !_purifyManager.TrySpendEnergy(UpgradeCost)) return;

        CurrentLevel++;
        LevelChanged?.Invoke(CurrentLevel);
    }

    private IEnumerator ChargeRoutine()
    {
        while (_purifyManager.IsRunning && !_isCharged)
        {
            _elapsedChargeTime = Mathf.Min(ChargeDuration, _elapsedChargeTime + Time.deltaTime);
            ChargeProgressChanged?.Invoke(ChargeProgress);

            if (_elapsedChargeTime >= ChargeDuration)
            {
                _chargeCoroutine = null;
                SetIsCharged(true);
                yield break;
            }

            yield return null;
        }

        _chargeCoroutine = null;
    }

    private PurifyForestBreathLevelData GetLevelData(int level)
    {
        if (level < 1 || level > _balance.ForestBreathLevels.Length) return null;

        return _balance.ForestBreathLevels[level - 1];
    }

    private void SetIsCharged(bool isCharged)
    {
        if (_isCharged == isCharged) return;

        _isCharged = isCharged;
        ReadyChanged?.Invoke();
    }
}
