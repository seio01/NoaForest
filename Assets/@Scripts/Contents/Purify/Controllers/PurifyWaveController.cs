using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PurifyWaveState
{
    None,
    Idle,
    Running,
    Finished,
    Stopped
}

public class PurifyWaveController : MonoBehaviour
{
    private readonly HashSet<Mote> _activeMotes = new();
    private readonly List<Mote> _activeMoteSnapshot = new();

    private StageSO _stage;
    private MoteGroupSO _moteGroup;
    private PurifyMoteRoute _moteRoute;
    private PurifyPoolController _poolController;
    private GameObject _motePrefab;
    private Coroutine _waveCoroutine;
    private PurifyRouteSide _nextRouteSide = PurifyRouteSide.Left;
    private PurifyWaveState _currentState;
    private bool _isInitialized;
    private int _maximumMoteCount;

    public event Action<PurifyWaveState> StateChanged;
    public event Action<int> WaveStarted;
    public event Action<int> RemainingTimeChanged;
    public event Action<int, int> WaveCompleted;
    public event Action<Vector3, float> MoteDamaged;
    public event Action<int> MoteEscaped;
    public event Action<int> MoteDefeated;
    public event Action AllWavesCompleted;

    public int CurrentWave { get; private set; }

    public Mote TryGetClosestMote(Vector3 origin, float range)
    {
        if (!_isInitialized || range <= 0f)
        {
            return null;
        }

        float closestDistanceSqr = range * range;
        Mote closestMote = null;
        foreach (Mote mote in _activeMotes)
        {
            if (!mote || !mote.IsTargetable)
            {
                continue;
            }

            float distanceSqr = (mote.transform.position - origin).sqrMagnitude;
            if (distanceSqr > closestDistanceSqr)
            {
                continue;
            }

            closestDistanceSqr = distanceSqr;
            closestMote = mote;
        }

        return closestMote;
    }

    public int DamageAllActiveMotes(float damage)
    {
        if (!_isInitialized || _currentState != PurifyWaveState.Running || damage <= 0f) return 0;

        _activeMoteSnapshot.Clear();
        foreach (Mote mote in _activeMotes)
        {
            if (mote && mote.IsTargetable) _activeMoteSnapshot.Add(mote);
        }

        int damagedMoteCount = 0;
        foreach (Mote mote in _activeMoteSnapshot)
        {
            if (!mote || !mote.IsTargetable) continue;

            mote.TakeDamage(damage);
            damagedMoteCount++;
        }

        _activeMoteSnapshot.Clear();
        return damagedMoteCount;
    }

    public bool Initialize(StageSO stage, MoteGroupSO moteGroup, PurifyMoteRoute moteRoute, PurifyPoolController poolController, GameObject motePrefab)
    {
        _stage = stage;
        _moteGroup = moteGroup;
        _moteRoute = moteRoute;
        _poolController = poolController;
        _motePrefab = motePrefab;

        if (!_stage || !_stage.WaveSet || !_moteGroup || !_moteRoute || !_poolController || !_motePrefab)
        {
            Debug.LogError("[PurifyWaveController] Required dependency is missing.");
            return false;
        }

        _maximumMoteCount = CalculateMaximumMoteCount(_stage.WaveSet);
        if (_maximumMoteCount <= 0)
        {
            Debug.LogError("[PurifyWaveController] WaveSet has no Motes.");
            return false;
        }

        _activeMoteSnapshot.Capacity = _maximumMoteCount;
        _isInitialized = true;
        ChangeState(PurifyWaveState.Idle);
        return true;
    }

    public void StartWaves()
    {
        if (!_isInitialized)
        {
            Debug.LogError("[PurifyWaveController] Controller is not initialized.");
            return;
        }

        if (_currentState == PurifyWaveState.Running) return;

        CurrentWave = 0;
        _nextRouteSide = PurifyRouteSide.Left;
        ChangeState(PurifyWaveState.Running);
    }

    public void StopWaves()
    {
        if (_currentState != PurifyWaveState.Stopped) ChangeState(PurifyWaveState.Stopped);

        ReleaseActiveMotes();
    }

    private void ChangeState(PurifyWaveState nextState)
    {
        if (_currentState == nextState) return;

        ExitState(_currentState);
        _currentState = nextState;
        EnterState(_currentState);
        StateChanged?.Invoke(_currentState);
    }

    private void EnterState(PurifyWaveState state)
    {
        if (state != PurifyWaveState.Running) return;

        _waveCoroutine = StartCoroutine(WaveSequenceRoutine());
    }

    private void ExitState(PurifyWaveState state)
    {
        if (state != PurifyWaveState.Running || _waveCoroutine == null) return;

        StopCoroutine(_waveCoroutine);
        _waveCoroutine = null;
    }

    private IEnumerator WaveSequenceRoutine()
    {
        for (int waveNumber = 1; waveNumber <= PurifyWaveSetSO.WAVE_COUNT; waveNumber++)
        {
            if (_currentState != PurifyWaveState.Running) yield break;

            PurifyWaveData waveData = _stage.WaveSet.GetWave(waveNumber);

            MoteSO moteData = _moteGroup.GetMote(waveData.Element);
            if (moteData == null)
            {
                Debug.LogError($"[PurifyWaveController] Mote data is missing: {waveData.Element}");
                _waveCoroutine = null;
                ChangeState(PurifyWaveState.Stopped);
                yield break;
            }

            CurrentWave = waveNumber;
            WaveStarted?.Invoke(CurrentWave);

            yield return RunWaveRoutine(waveData, moteData);

            if (_currentState != PurifyWaveState.Running) yield break;

            if (CurrentWave == PurifyWaveSetSO.WAVE_COUNT)
                yield return WaitForActiveMotesResolvedRoutine();

            if (_currentState != PurifyWaveState.Running) yield break;

            WaveCompleted?.Invoke(CurrentWave, waveData.ClearRewardAmount);
        }

        _waveCoroutine = null;
        ChangeState(PurifyWaveState.Finished);
        AllWavesCompleted?.Invoke();
    }

    private IEnumerator RunWaveRoutine(PurifyWaveData waveData, MoteSO moteData)
    {
        float elapsedTime = 0f;
        int spawnedCount = 0;
        int previousRemainingSeconds = -1;

        while (elapsedTime < waveData.Duration && _currentState == PurifyWaveState.Running)
        {
            SpawnScheduledMotes(waveData, moteData, elapsedTime, ref spawnedCount);
            NotifyRemainingTime(waveData.Duration, elapsedTime, ref previousRemainingSeconds);

            yield return null;
            elapsedTime = Mathf.Min(waveData.Duration, elapsedTime + Time.deltaTime);
        }

        if (_currentState != PurifyWaveState.Running) yield break;

        SpawnScheduledMotes(waveData, moteData, waveData.Duration, ref spawnedCount);
        NotifyRemainingTime(waveData.Duration, waveData.Duration, ref previousRemainingSeconds);
    }

    private IEnumerator WaitForActiveMotesResolvedRoutine()
    {
        while (_activeMotes.Count > 0 && _currentState == PurifyWaveState.Running) yield return null;
    }

    private void SpawnScheduledMotes(PurifyWaveData waveData, MoteSO moteData, float elapsedTime, ref int spawnedCount)
    {
        while (spawnedCount < waveData.MoteCount && spawnedCount * waveData.SpawnInterval <= elapsedTime)
        {
            SpawnMote(waveData, moteData);
            spawnedCount++;
        }
    }

    private void NotifyRemainingTime(float duration, float elapsedTime, ref int previousRemainingSeconds)
    {
        int remainingSeconds = Mathf.Max(0, Mathf.CeilToInt(duration - elapsedTime));
        if (remainingSeconds == previousRemainingSeconds) return;

        previousRemainingSeconds = remainingSeconds;
        RemainingTimeChanged?.Invoke(remainingSeconds);
    }

    private void SpawnMote(PurifyWaveData waveData, MoteSO moteData)
    {
        Sprite spriteMote = Managers.ContentIcon.GetLoadedSprite(Define.ContentIconType.Mote, moteData.Id);
        if (spriteMote == null)
        {
            Debug.LogError($"[PurifyWaveController] Mote Sprite is not loaded: {moteData.Id}");
            return;
        }

        if (!moteData.AnimatorControllerMote)
        {
            Debug.LogError($"[PurifyWaveController] Mote AnimatorController is missing: {moteData.Id}");
            return;
        }

        GameObject instance = _poolController.Get(_motePrefab, _maximumMoteCount);
        if (!instance || !instance.TryGetComponent(out Mote mote))
        {
            Debug.LogError("[PurifyWaveController] Failed to get a Mote instance.");
            return;
        }

        PurifyRouteSide routeSide = _nextRouteSide;
        PurifyRoutePoint[] route = _moteRoute.GetRoute(routeSide);
        _nextRouteSide = _nextRouteSide == PurifyRouteSide.Left ? PurifyRouteSide.Right : PurifyRouteSide.Left;

        _activeMotes.Add(mote);

        float healthMultiplier = _stage.MoteHealthMultiplier * waveData.HealthMultiplier;
        int escapeDamage = Mathf.Max(1, Mathf.RoundToInt(moteData.EscapeDamage * _stage.MoteEscapeDamageMultiplier));
        mote.Init(
            moteData,
            spriteMote,
            moteData.AnimatorControllerMote,
            route,
            routeSide,
            healthMultiplier,
            escapeDamage,
            HandleMoteDamaged,
            HandleMoteDefeated,
            HandleMoteRouteCompleted);
    }

    private void HandleMoteDamaged(Mote mote, float damage)
    {
        if (!mote || damage <= 0f) return;

        MoteDamaged?.Invoke(mote.DamageFontWorldPosition, damage);
    }

    private void HandleMoteDefeated(Mote mote)
    {
        if (!mote || !_activeMotes.Remove(mote)) return;

        int killReward = mote.KillReward;
        _poolController.Release(mote.gameObject);
        MoteDefeated?.Invoke(killReward);
    }

    private void HandleMoteRouteCompleted(Mote mote)
    {
        if (!mote || !_activeMotes.Remove(mote)) return;

        int escapeDamage = mote.EscapeDamage;
        _poolController.Release(mote.gameObject);
        MoteEscaped?.Invoke(escapeDamage);
    }

    private void ReleaseActiveMotes()
    {
        if (_activeMotes.Count == 0) return;

        Mote[] activeMotes = new Mote[_activeMotes.Count];
        _activeMotes.CopyTo(activeMotes);
        _activeMotes.Clear();

        foreach (Mote mote in activeMotes)
        {
            if (mote) _poolController.Release(mote.gameObject);
        }
    }

    private int CalculateMaximumMoteCount(PurifyWaveSetSO waveSet)
    {
        int maximumMoteCount = 0;
        foreach (PurifyWaveData waveData in waveSet.Waves) maximumMoteCount += waveData.MoteCount;
        return maximumMoteCount;
    }

    private void OnDestroy()
    {
        StopWaves();
    }
}
