using System;
using UnityEngine;

public enum PurifyState
{
    None,
    Ready,
    Playing,
    Result,
    Stopped
}

public enum PurifyResultType
{
    None,
    Clear,
    Fail
}

public class PurifyGameManager : MonoBehaviour, IMoteTargetProvider
{
    [Header("Configuration")]
    [SerializeField] private StageSO defaultStage;
    [SerializeField] private GameObject motePrefab;

    [Header("Scene Components")]
    [SerializeField] private PurifyWaveController waveController;
    [SerializeField] private PurifyPoolController poolController;
    [SerializeField] private PurifyMoteRoute moteRoute;
    [SerializeField] private PurifyForestBreathController forestBreathController;

    private bool _isInitialized;
    private StageSO _stage;
    private PurifyState _currentState;
    private PurifyResultType _resultType;
    private float _currentEnergy;
    private int _displayedEnergy;
    private int _currentTreeHealth;
    private int _completedFlow;
    private PurifyBlessingEffects _blessingEffects = PurifyBlessingEffects.Empty;

    public event Action<PurifyState> StateChanged;
    public event Action<int> WaveChanged;
    public event Action<int> WaveCompleted;
    public event Action<int> RemainingTimeChanged;
    public event Action<int, int> TreeHealthChanged;
    public event Action<Vector3, float> MoteDamaged;
    public event Action<int> TreeDamaged;
    public event Action<int> TreeHealed;
    public event Action<int> EnergyChanged;
    public event Action<int> EnergyGained;
    public event Action<PurifyResultType> PurifyCompleted;

    public PurifyBalanceSO Balance => Managers.GameData.PurifyBalance;
    public MoteGroupSO MoteGroup => Managers.GameData.Motes;
    public StageSO Stage => _stage;
    public PurifyState CurrentState => _currentState;
    public PurifyResultType ResultType => _resultType;
    public float CurrentEnergy => _currentEnergy;
    public int CurrentTreeHealth => _currentTreeHealth;
    public int CompletedFlow => _completedFlow;
    public bool IsRunning => _currentState == PurifyState.Playing;
    public PurifyForestBreathController ForestBreathController => forestBreathController;
    public PurifyPoolController PoolController => poolController;
    public PurifyBlessingEffects BlessingEffects => _blessingEffects;
    public int SummonCost => _blessingEffects.CalculateSummonCost(Balance.BaseSummonCost);

    public bool Initialize(PurifyBlessingEffects blessingEffects = null)
    {
        if (_isInitialized) return true;

        _blessingEffects = blessingEffects ?? PurifyBlessingEffects.Empty;
        _stage = GetStage();

        if (!Balance || !_stage || !MoteGroup || !motePrefab || !waveController || !poolController || !moteRoute || !forestBreathController)
        {
            Debug.LogError("[PurifyGameManager] Required dependency is missing.");
            return false;
        }

        if (!_stage.WaveSet)
        {
            Debug.LogError($"[PurifyGameManager] WaveSet is missing: {_stage.name}");
            return false;
        }

        if (!moteRoute.Initialize()) return false;
        if (!waveController.Initialize(_stage, MoteGroup, moteRoute, poolController, motePrefab)) return false;
        forestBreathController.Initialize(this, waveController, Balance);

        waveController.WaveStarted += HandleWaveStarted;
        waveController.RemainingTimeChanged += HandleRemainingTimeChanged;
        waveController.WaveCompleted += HandleWaveCompleted;
        waveController.MoteDamaged += HandleMoteDamaged;
        waveController.MoteEscaped += HandleMoteEscaped;
        waveController.MoteDefeated += HandleMoteDefeated;
        waveController.AllWavesCompleted += HandleAllWavesCompleted;

        _isInitialized = true;
        ChangeState(PurifyState.Ready);
        return true;
    }

    private StageSO GetStage()
    {
        StageSO selectedStage = Managers.Scene.GetParameter<StageSO>(Constants.STAGE_KEY);
        if (selectedStage)
        {
            Managers.Scene.RemoveParameter(Constants.STAGE_KEY);
            return selectedStage;
        }

        return defaultStage;
    }

    public void StartPurify()
    {
        if (!_isInitialized && !Initialize()) return;
        if (_currentState != PurifyState.Ready) return;

        ChangeState(PurifyState.Playing);
    }

    public void StopPurify()
    {
        if (_currentState == PurifyState.Stopped) return;

        ChangeState(PurifyState.Stopped);
    }

    public void GiveUp()
    {
        if (!_isInitialized || _currentState != PurifyState.Playing) return;

        _resultType = PurifyResultType.Fail;
        ChangeState(PurifyState.Result);
    }

    public bool TrySpendEnergy(int amount)
    {
        if (_currentState != PurifyState.Playing || amount <= 0 || _currentEnergy < amount) return false;

        SetEnergy(_currentEnergy - amount);
        return true;
    }

    public Mote FindClosestMote(Vector3 origin, float range)
    {
        if (!_isInitialized || _currentState != PurifyState.Playing)
        {
            return null;
        }

        return waveController.TryGetClosestMote(origin, range);
    }

    private void ChangeState(PurifyState nextState)
    {
        if (_currentState == nextState) return;

        ExitState(_currentState);
        _currentState = nextState;
        EnterState(_currentState);
        StateChanged?.Invoke(_currentState);
    }

    private void EnterState(PurifyState state)
    {
        switch (state)
        {
            case PurifyState.Ready:
                ResetPurifyValues();
                break;
            case PurifyState.Playing:
                waveController.StartWaves();
                forestBreathController.StartCharging();
                break;
            case PurifyState.Result:
                PurifyCompleted?.Invoke(_resultType);
                break;
        }
    }

    private void ExitState(PurifyState state)
    {
        if (state != PurifyState.Playing) return;

        forestBreathController.StopCharging();
        waveController.StopWaves();
    }

    private void ResetPurifyValues()
    {
        _resultType = PurifyResultType.None;
        _currentTreeHealth = Mathf.Min(Balance.StartingTreeHealth, Balance.MaximumTreeHealth);
        _currentEnergy = Balance.StartingEnergy;
        _displayedEnergy = -1;
        _completedFlow = 0;
        forestBreathController.ResetBreath();

        WaveChanged?.Invoke(0);
        RemainingTimeChanged?.Invoke(0);
        TreeHealthChanged?.Invoke(_currentTreeHealth, Balance.MaximumTreeHealth);
        NotifyEnergyChanged();
    }

    private void SetEnergy(float amount)
    {
        float nextEnergy = amount;
        if (Mathf.Approximately(_currentEnergy, nextEnergy)) return;

        int previousDisplayedEnergy = Mathf.FloorToInt(_currentEnergy);
        _currentEnergy = nextEnergy;
        NotifyEnergyChanged();

        int gainedEnergy = Mathf.FloorToInt(_currentEnergy) - previousDisplayedEnergy;
        if (_currentState == PurifyState.Playing && gainedEnergy > 0) EnergyGained?.Invoke(gainedEnergy);
    }

    private void NotifyEnergyChanged()
    {
        int displayedEnergy = Mathf.FloorToInt(_currentEnergy);
        if (_displayedEnergy == displayedEnergy) return;

        _displayedEnergy = displayedEnergy;
        EnergyChanged?.Invoke(_displayedEnergy);
    }

    private void HandleWaveStarted(int waveNumber)
    {
        WaveChanged?.Invoke(waveNumber);
    }

    private void HandleRemainingTimeChanged(int remainingSeconds)
    {
        RemainingTimeChanged?.Invoke(remainingSeconds);
    }

    private void HandleWaveCompleted(int waveNumber, int clearRewardAmount)
    {
        _completedFlow = Mathf.Max(_completedFlow, waveNumber);
        SetEnergy(_currentEnergy + clearRewardAmount);
        HealTree(_blessingEffects.WaveEndHealBonus);
        WaveCompleted?.Invoke(waveNumber);
    }

    private void HandleMoteDamaged(Vector3 worldPosition, float damage)
    {
        MoteDamaged?.Invoke(worldPosition, damage);
    }

    private void HandleMoteEscaped(int damage)
    {
        if (_currentState != PurifyState.Playing) return;

        int previousTreeHealth = _currentTreeHealth;
        int adjustedDamage = _blessingEffects.CalculateMoteEscapeDamage(damage);
        _currentTreeHealth = Mathf.Max(0, _currentTreeHealth - adjustedDamage);
        TreeHealthChanged?.Invoke(_currentTreeHealth, Balance.MaximumTreeHealth);
        int appliedDamage = previousTreeHealth - _currentTreeHealth;
        if (appliedDamage > 0)
        {
            TreeDamaged?.Invoke(appliedDamage);
            Managers.Sound.Play(Define.AudioClip.DamageTree, Define.AudioSourceType.Sfx, Define.AudioPath.Purify);
        }
        Haptic.Vibrate();

        if (_currentTreeHealth > 0) return;

        _resultType = PurifyResultType.Fail;
        ChangeState(PurifyState.Result);
    }

    private void HandleMoteDefeated(int rewardAmount)
    {
        if (_currentState != PurifyState.Playing || rewardAmount <= 0)
        {
            return;
        }

        SetEnergy(_currentEnergy + _blessingEffects.CalculateMoteKillReward(rewardAmount));
    }

    private void HealTree(int amount)
    {
        if (amount <= 0 || _currentTreeHealth >= Balance.MaximumTreeHealth) return;

        int previousTreeHealth = _currentTreeHealth;
        _currentTreeHealth = Mathf.Min(Balance.MaximumTreeHealth, _currentTreeHealth + amount);
        TreeHealthChanged?.Invoke(_currentTreeHealth, Balance.MaximumTreeHealth);

        int appliedHeal = _currentTreeHealth - previousTreeHealth;
        if (appliedHeal > 0) TreeHealed?.Invoke(appliedHeal);
    }

    private void HandleAllWavesCompleted()
    {
        if (_currentState != PurifyState.Playing) return;

        _resultType = PurifyResultType.Clear;
        ChangeState(PurifyState.Result);
    }

    private void OnDestroy()
    {
        StopPurify();

        if (!waveController) return;

        waveController.WaveStarted -= HandleWaveStarted;
        waveController.RemainingTimeChanged -= HandleRemainingTimeChanged;
        waveController.WaveCompleted -= HandleWaveCompleted;
        waveController.MoteDamaged -= HandleMoteDamaged;
        waveController.MoteEscaped -= HandleMoteEscaped;
        waveController.MoteDefeated -= HandleMoteDefeated;
        waveController.AllWavesCompleted -= HandleAllWavesCompleted;
    }
}
