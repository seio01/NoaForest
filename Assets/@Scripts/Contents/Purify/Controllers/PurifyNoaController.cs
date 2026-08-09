using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PurifyNoaController : MonoBehaviour
{
    private class SlotState
    {
        private const float NOA_POSITION_Y_OFFSET = 0.6f;

        public SlotState(Collider2D collider)
        {
            Collider = collider;
        }

        public Collider2D Collider { get; }
        public Transform Transform => Collider.transform;
        public Noa Occupant { get; private set; }
        public bool IsOccupied => Occupant;

        public bool TryPlace(Noa noa)
        {
            if (IsOccupied || !noa)
            {
                return false;
            }

            Occupant = noa;
            Occupant.transform.SetParent(Transform, true);
            Occupant.transform.localPosition = new Vector3(0f, NOA_POSITION_Y_OFFSET, 0f);
            Occupant.transform.localRotation = Quaternion.identity;
            return true;
        }

        public Noa Clear()
        {
            Noa removedNoa = Occupant;
            Occupant = null;
            return removedNoa;
        }
    }

    [SerializeField] private Noa noaPrefab;
    [SerializeField] private PurifyNoaVisualCatalogSO noaVisualCatalog;

    [Header("Scene Components")]
    [FormerlySerializedAs("purifyManager")]
    [SerializeField] private PurifyGameManager purifyGameManager;
    [SerializeField] private Transform noaSlotRoot;
    [SerializeField] private Camera cameraMain;

    [Header("Effects")]
    [SerializeField] private NoaParticleEffect particleEffectNoaSummon;

    private const int SLOT_HIT_RESULT_CAPACITY = 8;
    private const int SUMMON_EFFECT_POOL_CAPACITY = 4;
    private readonly List<SlotState> _slots = new();
    private readonly List<Noa> _mergeCandidates = new();
    private readonly Dictionary<Collider2D, SlotState> _slotsByCollider = new();
    private readonly HashSet<string> _availableNoaIds = new(StringComparer.Ordinal);
    private readonly Collider2D[] _slotHitResults = new Collider2D[SLOT_HIT_RESULT_CAPACITY];
    private bool _isInitialized;
    private SlotState _selectedSlot;
    private Noa _selectedMergeMaterial;
    private int _nextNoaRenderOrder;
    private Vector3 _summonNoaParticleOffect = new Vector3(0, -0.25f, 0);

    public event Action<Transform> SlotSelected;
    public event Action<Noa> NoaSelected;
    public event Action<Noa> MergeMaterialChanged;
    public event Action SlotSelectionCleared;
    public event Action<Transform, NoaSO> NoaPlaced;

    public NoaGroupSO NoaGroup => Managers.GameData.Noas;
    public List<Noa> MergeCandidates => _mergeCandidates;
    public Noa SelectedMergeMaterial => _selectedMergeMaterial;
    public bool CanSummonSelectedSlot =>
        _isInitialized &&
        purifyGameManager.IsRunning &&
        _availableNoaIds.Count > 0 &&
        _selectedSlot != null &&
        !_selectedSlot.IsOccupied &&
        purifyGameManager.CurrentEnergy >= purifyGameManager.SummonCost;

    private void Update()
    {
        if (!_isInitialized || !purifyGameManager.IsRunning)
        {
            return;
        }

        if (Input.touchCount > 0)
        {
            HandleTouchInput();
            return;
        }

        if (!Input.GetMouseButtonDown(0) || Utils.IsPointerOverUi(Input.mousePosition))
        {
            return;
        }

        TrySelectSlot(Input.mousePosition);
    }

    public bool Initialize()
    {
        if (_isInitialized)
        {
            return true;
        }

        if (!NoaGroup || !noaPrefab || !noaVisualCatalog || !purifyGameManager || !noaSlotRoot || !cameraMain)
        {
            Debug.LogError("[PurifyNoaController] Required dependency is missing.");
            return false;
        }

        BoxCollider2D[] slotColliders = noaSlotRoot.GetComponentsInChildren<BoxCollider2D>(true);
        if (slotColliders.Length == 0)
        {
            Debug.LogError("[PurifyNoaController] No slot collider was found.");
            return false;
        }

        foreach (var slotCollider in slotColliders)
        {
            if (!slotCollider || _slotsByCollider.ContainsKey(slotCollider))
            {
                continue;
            }

            var slot = new SlotState(slotCollider);
            _slots.Add(slot);
            _slotsByCollider.Add(slotCollider, slot);
        }

        _isInitialized = true;
        return true;
    }

    public bool SetAvailableNoas(List<string> noaIds)
    {
        _availableNoaIds.Clear();
        if (!NoaGroup || noaIds == null)
            return false;

        foreach (string noaId in noaIds)
        {
            NoaSO noaData = NoaGroup.GetNoa(noaId);
            if (!noaData)
            {
                Debug.LogWarning($"[PurifyNoaController] Available Noa data is missing: {noaId}");
                continue;
            }

            _availableNoaIds.Add(noaData.Id);
        }

        if (_availableNoaIds.Count > 0)
            return true;

        Debug.LogError("[PurifyNoaController] No available Noa is registered.");
        return false;
    }

    public bool TrySummonSelectedSlot()
    {
        if (!CanSummonSelectedSlot)
        {
            return false;
        }

        NoaSO summonedNoa = DrawNoa();
        if (summonedNoa == null)
        {
            return false;
        }

        Sprite sprite = Managers.ContentIcon.GetLoadedSprite(Define.ContentIconType.Noa, summonedNoa.IconId);
        if (!sprite)
        {
            Debug.LogError($"[PurifyNoaController] Noa sprite is not loaded: {summonedNoa.IconId}");
            return false;
        }

        NoaVisualSO visualData = GetNoaVisual(summonedNoa);
        if (!visualData) return false;

        SlotState targetSlot = _selectedSlot;
        Noa noa = Instantiate(noaPrefab, targetSlot.Transform);
        int level = Managers.Collection.GetLevel(summonedNoa);
        if (!noa.SetData(summonedNoa, visualData, sprite, NoaGroup.Stats, level, purifyGameManager.BlessingEffects, purifyGameManager, purifyGameManager.PoolController) || !targetSlot.TryPlace(noa))
        {
            Destroy(noa.gameObject);
            return false;
        }

        noa.FacePosition(noaSlotRoot.position);

        int summonCost = purifyGameManager.SummonCost;
        if (!purifyGameManager.TrySpendEnergy(summonCost))
        {
            targetSlot.Clear();
            Destroy(noa.gameObject);
            return false;
        }

        ApplyNextNoaRenderOrder(noa);
        Managers.Sound.Play(Define.AudioClip.SummonNoa, Define.AudioSourceType.Sfx, Define.AudioPath.Purify);
        PlaySummonEffect(noa.transform.position);

        NoaPlaced?.Invoke(targetSlot.Transform, summonedNoa);
        ClearSelection();
        return true;
    }

    public Noa MergeSelectedNoa()
    {
        if (!_isInitialized || !purifyGameManager.IsRunning || _selectedSlot == null || !_selectedSlot.IsOccupied || !_selectedMergeMaterial) return null;

        Noa targetNoa = _selectedSlot.Occupant;
        Noa mergeMaterial = _selectedMergeMaterial;
        if (!IsMergeCandidate(targetNoa, mergeMaterial)) return null;

        var materialSlot = GetOccupiedSlot(mergeMaterial);
        if(materialSlot == null) return null;

        NoaSO upgradedData = targetNoa.Data.NextTierNoa;
        if (!upgradedData)
        {
            return null;
        }

        Sprite upgradedSprite = Managers.ContentIcon.GetLoadedSprite(
            Define.ContentIconType.Noa,
            upgradedData.IconId);
        if (!upgradedSprite)
        {
            Debug.LogError(
                $"[PurifyNoaController] Noa sprite is not loaded: {upgradedData.IconId}");
            return null;
        }

        NoaVisualSO visualData = GetNoaVisual(upgradedData);
        if (!visualData) return null;

        int level = Managers.Collection.GetLevel(upgradedData);
        if (!targetNoa.SetData(upgradedData, visualData, upgradedSprite, NoaGroup.Stats, level, purifyGameManager.BlessingEffects, purifyGameManager, purifyGameManager.PoolController))
        {
            return null;
        }

        materialSlot.Clear();
        SetMergeMaterial(null);
        Destroy(mergeMaterial.gameObject);

        Managers.Sound.Play(Define.AudioClip.SummonNoa, Define.AudioSourceType.Sfx, Define.AudioPath.Purify);
        PlaySummonEffect(targetNoa.transform.position);

        NoaPlaced?.Invoke(_selectedSlot.Transform, upgradedData);
        ClearSelection();
        return targetNoa;
    }

    public void ClearSelection()
    {
        SetMergeMaterial(null);
        if (_selectedSlot == null) return;

        if (_selectedSlot.IsOccupied)
        {
            _selectedSlot.Occupant.SetSelected(false);
        }

        _mergeCandidates.Clear();
        _selectedSlot = null;
        SlotSelectionCleared?.Invoke();
    }

    private void PlaySummonEffect(Vector3 position)
    {
        if (!particleEffectNoaSummon || !Managers.UserSetting.CurrentSetting.IsEffectEnabled) return;

        PurifyPoolController poolController = purifyGameManager.PoolController;
        GameObject effectObject = poolController.Get(particleEffectNoaSummon.gameObject, SUMMON_EFFECT_POOL_CAPACITY);
        if (effectObject && effectObject.TryGetComponent(out NoaParticleEffect particleEffect))
            particleEffect.Play(poolController, position + _summonNoaParticleOffect);
    }

    private void ApplyNextNoaRenderOrder(Noa noa)
    {
        noa.SetRenderOrder(_nextNoaRenderOrder);
        _nextNoaRenderOrder++;
    }

    private void HandleTouchInput()
    {
        for (int index = 0; index < Input.touchCount; index++)
        {
            Touch touch = Input.GetTouch(index);
            if (touch.phase != TouchPhase.Began || Utils.IsPointerOverUi(touch.position))
            {
                continue;
            }

            TrySelectSlot(touch.position);
            return;
        }
    }

    private void TrySelectSlot(Vector2 screenPosition)
    {
        Vector3 worldPosition = cameraMain.ScreenToWorldPoint(screenPosition);
        int hitCount = Physics2D.OverlapPointNonAlloc(worldPosition, _slotHitResults);

        for (int index = 0; index < hitCount; index++)
        {
            Collider2D hitCollider = _slotHitResults[index];
            _slotHitResults[index] = null;

            if (!hitCollider || !_slotsByCollider.TryGetValue(hitCollider, out SlotState slot))
            {
                continue;
            }

            SelectSlot(slot);
            return;
        }

        ClearSelection();
    }

    private void SelectSlot(SlotState slot)
    {
        if (TryToggleMergeMaterial(slot)) return;
        SetMergeMaterial(null);

        if (_selectedSlot != null && _selectedSlot.IsOccupied)
        {
            _selectedSlot.Occupant.SetSelected(false);
        }

        _selectedSlot = slot;
        if (_selectedSlot.IsOccupied)
        {
            RefreshMergeCandidates();
            _selectedSlot.Occupant.SetSelected(true);
            NoaSelected?.Invoke(_selectedSlot.Occupant);
            return;
        }

        _mergeCandidates.Clear();
        SlotSelected?.Invoke(_selectedSlot.Transform);
    }

    private bool TryToggleMergeMaterial(SlotState slot)
    {
        if (_selectedSlot == null || slot == _selectedSlot || !slot.IsOccupied || !_mergeCandidates.Contains(slot.Occupant)) return false;
        SetMergeMaterial(_selectedMergeMaterial == slot.Occupant ? null : slot.Occupant);
        return true;
    }

    private void SetMergeMaterial(Noa mergeMaterial)
    {
        if (_selectedMergeMaterial == mergeMaterial) return;
        _selectedMergeMaterial = mergeMaterial;
        MergeMaterialChanged?.Invoke(_selectedMergeMaterial);
    }

    private void RefreshMergeCandidates()
    {
        _mergeCandidates.Clear();
        if (_selectedSlot == null || !_selectedSlot.IsOccupied)
        {
            return;
        }

        Noa targetNoa = _selectedSlot.Occupant;
        foreach (SlotState slot in _slots)
        {
            if (!slot.IsOccupied || slot == _selectedSlot)
            {
                continue;
            }

            if (IsMergeCandidate(targetNoa, slot.Occupant))
            {
                _mergeCandidates.Add(slot.Occupant);
            }
        }
    }

    private SlotState GetOccupiedSlot(Noa noa)
    {
        foreach (SlotState slot in _slots)
        {
            if (slot.Occupant != noa)
            {
                continue;
            }

            return slot;
        }

        return null;
    }

    private bool IsMergeCandidate(Noa targetNoa, Noa candidateNoa)
    {
        return targetNoa &&
               candidateNoa &&
               targetNoa != candidateNoa &&
               targetNoa.Data != null &&
               candidateNoa.Data != null &&
               targetNoa.Data.NextTierNoa &&
               IsAvailableNoa(targetNoa.Data.NextTierNoa) &&
               targetNoa.Data.Element == candidateNoa.Data.Element &&
               targetNoa.Data.Tier == candidateNoa.Data.Tier;
    }

    private NoaSO DrawNoa()
    {
        Define.NoaTier? tier = DrawTier();
        if (!tier.HasValue)
            return null;

        int candidateCount = 0;
        foreach (NoaSO noaData in NoaGroup.Noas)
        {
            if (noaData && noaData.Tier == tier.Value && IsAvailableNoa(noaData))
                candidateCount++;
        }

        if (candidateCount == 0)
            return null;

        int selectedIndex = UnityEngine.Random.Range(0, candidateCount);
        foreach (NoaSO noaData in NoaGroup.Noas)
        {
            if (!noaData || noaData.Tier != tier.Value || !IsAvailableNoa(noaData))
                continue;

            if (selectedIndex == 0)
                return noaData;

            selectedIndex--;
        }

        return null;
    }

    private Define.NoaTier? DrawTier()
    {
        PurifyBalanceSO balance = purifyGameManager.Balance;
        float tier1Weight = HasAvailableNoa(Define.NoaTier.Tier1) ? balance.Tier1SummonProbability : 0f;
        float tier2Weight = HasAvailableNoa(Define.NoaTier.Tier2) ? balance.Tier2SummonProbability : 0f;
        float tier3Weight = HasAvailableNoa(Define.NoaTier.Tier3) ? balance.Tier3SummonProbability : 0f;
        PurifySummonTierWeights adjustedWeights = purifyGameManager.BlessingEffects.CalculateSummonTierWeights(tier1Weight, tier2Weight, tier3Weight);
        tier1Weight = adjustedWeights.Tier1;
        tier2Weight = adjustedWeights.Tier2;
        tier3Weight = adjustedWeights.Tier3;
        float totalWeight = tier1Weight + tier2Weight + tier3Weight;
        if (totalWeight <= 0f)
        {
            Debug.LogError("[PurifyNoaController] Available Noa summon weight is invalid.");
            return null;
        }

        float roll = UnityEngine.Random.value * totalWeight;

        if (roll < tier1Weight)
            return Define.NoaTier.Tier1;

        return roll < tier1Weight + tier2Weight ? Define.NoaTier.Tier2 : Define.NoaTier.Tier3;
    }

    private bool HasAvailableNoa(Define.NoaTier tier)
    {
        foreach (NoaSO noaData in NoaGroup.Noas)
        {
            if (noaData && noaData.Tier == tier && IsAvailableNoa(noaData))
                return true;
        }

        return false;
    }

    private bool IsAvailableNoa(NoaSO noaData)
    {
        return noaData && _availableNoaIds.Contains(noaData.Id);
    }

    private NoaVisualSO GetNoaVisual(NoaSO noaData)
    {
        NoaVisualSO visualData = noaVisualCatalog.GetVisual(noaData);
        if (!visualData)
            Debug.LogError($"[PurifyNoaController] Noa visual is missing: {noaData.Id}");
        return visualData;
    }

    //Test
    #if UNITY_EDITOR
    public bool TrySummonForTest(Define.ElementType element, Define.NoaTier tier)
    {
        if (!_isInitialized || !purifyGameManager || !purifyGameManager.IsRunning)
        {
            Debug.LogWarning("[PurifyNoaController] Test summon requires Purify Play Mode to be running.");
            return false;
        }

        NoaSO noaData = NoaGroup.GetNoa(element, tier);
        if (!noaData)
        {
            Debug.LogWarning($"[PurifyNoaController] Test Noa data is missing: {element}, {tier}");
            return false;
        }

        SlotState targetSlot = GetTestSummonSlot();
        if (targetSlot == null)
        {
            Debug.LogWarning("[PurifyNoaController] No empty slot is available for test summon.");
            return false;
        }

        Sprite sprite = Managers.ContentIcon.GetLoadedSprite(Define.ContentIconType.Noa, noaData.IconId);
        NoaVisualSO visualData = GetNoaVisual(noaData);
        if (!sprite || !visualData)
        {
            Debug.LogWarning($"[PurifyNoaController] Test Noa visual resource is missing: {noaData.Id}");
            return false;
        }

        Noa noa = Instantiate(noaPrefab, targetSlot.Transform);
        int level = Mathf.Max(1, Managers.Collection.GetLevel(noaData));
        if (!noa.SetData(noaData, visualData, sprite, NoaGroup.Stats, level, purifyGameManager.BlessingEffects, purifyGameManager, purifyGameManager.PoolController) || !targetSlot.TryPlace(noa))
        {
            Destroy(noa.gameObject);
            return false;
        }

        ApplyNextNoaRenderOrder(noa);
        noa.FacePosition(noaSlotRoot.position);
        Managers.Sound.Play(Define.AudioClip.SummonNoa, Define.AudioSourceType.Sfx, Define.AudioPath.Purify);
        PlaySummonEffect(noa.transform.position);
        NoaPlaced?.Invoke(targetSlot.Transform, noaData);
        ClearSelection();
        Debug.Log($"[PurifyNoaController] Test Noa summoned: {noaData.Id}");
        return true;
    }

    private SlotState GetTestSummonSlot()
    {
        if (_selectedSlot != null && !_selectedSlot.IsOccupied)
            return _selectedSlot;

        foreach (SlotState slot in _slots)
        {
            if (!slot.IsOccupied)
                return slot;
        }

        return null;
    }
#endif
}
