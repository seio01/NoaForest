using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public class PurifyScene : BaseScene
{
    [FormerlySerializedAs("purifyManager")]
    [SerializeField] private PurifyGameManager purifyGameManager;
    [SerializeField] private PurifyNoaController purifyNoaController;
    [SerializeField] private PurifyForestBreathController forestBreathController;
    [SerializeField] private UI_PurifyHUD purifyHud;
    [SerializeField] private UI_PurifyWaveTimer purifyWaveTimer;

    [Header("Bottom Frame")]
    [SerializeField] private UI_PurifyBlessingPanel frameBlessing;
    [SerializeField] private UI_PurifyForestBreathUpgradePanel frameBreathUpgrade;

    [Header("Summon UI")]
    [SerializeField] private RectTransform rectNoaSlotContext;
    [SerializeField] private CanvasGroup imageSelection;
    [SerializeField] private ButtonBase buttonSummon;
    [SerializeField] private Sprite spriteDisabledButton, spriteOriginButton;

    [Header("Merge UI")]
    [SerializeField] private UI_PurifyNoaMergePanel frameNoaMerge;
    [SerializeField] private GameObject mergeSelection;

    [Header("Noa Info UI")]
    [SerializeField] private UI_PurifyNoaInfoPanel frameNoaInfo;

    private const string WAVE_RECOVERY_MESSAGE = "숲의 균형이 조금 회복되었습니다.";
    private const string PURIFY_START_MESSAGE = "정화를 시작합니다.";
    private Sequence _selectionSequence;
    private UI_MergeSelection _baseMergeSelection;
    private readonly Dictionary<Noa, UI_MergeSelection> _mergeSelections = new();
    private List<BlessingSO> _blessings;
    private CancellationToken _sceneCancellationToken;
    private PurifySettlementResponse _settlementResult;
    private bool _isScenePrepared;

    public event Action<BlessingSO> BlessingUseRequested;
    public PurifySettlementResponse SettlementResult => _settlementResult;

    protected override async Task InitializeSceneAsync(CancellationToken cancellationToken)
    {
        _sceneCancellationToken = cancellationToken;

        if (!purifyGameManager || !purifyNoaController || !forestBreathController || !purifyHud || !purifyWaveTimer)
        {
            Debug.LogError("[PurifyScene] Purify scene dependency is missing.");
            return;
        }

        MoteGroupSO moteGroup = purifyGameManager.MoteGroup;
        if (!moteGroup)
        {
            Debug.LogError("[PurifyScene] MoteGroupSO is missing.");
            return;
        }

        NoaGroupSO noaGroup = purifyNoaController.NoaGroup;
        if (!noaGroup)
        {
            Debug.LogError("[PurifyScene] NoaGroupSO is missing.");
            return;
        }

        List<Task> loadTasks = new(3);

        //등록한 정화 가호
        _blessings = Managers.Scene.GetParameter<List<BlessingSO>>(Constants.PURIFY_BLESSINGS_KEY) ?? new();
        Managers.Scene.RemoveParameter(Constants.PURIFY_BLESSINGS_KEY);

        loadTasks.Add(Managers.ContentIcon.PreloadAsync(Define.ContentIconType.Noa));
        loadTasks.Add(Managers.ContentIcon.PreloadAsync(Define.ContentIconType.Blessing));

        loadTasks.Add(Managers.ContentIcon.PreloadAsync(Define.ContentIconType.Mote));

        await Task.WhenAll(loadTasks);
        cancellationToken.ThrowIfCancellationRequested();

        PurifyBlessingEffects blessingEffects = new(_blessings, blessing => Managers.Collection.GetLevel(blessing));
        if (!purifyGameManager.Initialize(blessingEffects) || !purifyNoaController.Initialize())
            return;

        InitializeSceneContent();

        _isScenePrepared = await PreparePurifySessionAsync(cancellationToken);
    }

    protected override void OnSceneReady()
    {
        if (!_isScenePrepared)
            return;

        Managers.Sound.Play(Define.AudioClip.PurifyBGM, Define.AudioSourceType.Bgm);

        UpdateSummonButtonDisabled();
        UpdateForestBreathButton();

        StartCoroutine(Utils.DelayRoutine(1f, purifyGameManager.StartPurify));
    }

    private void InitializeSceneContent()
    {
        if(frameBreathUpgrade)
        {
            frameBreathUpgrade.SetData(forestBreathController);
            frameBreathUpgrade.SetVisible(false);
        }
        if(frameBlessing)
        {
            frameBlessing.SetData(_blessings);
            frameBlessing.SetVisible(false);
        }

        BindEvents();

        //슬롯 선택 UI 숨기기
        HideNoaSlotContext();
        HideNoaMergePanel();
        //노아 정보 UI 숨기기
        HideNoaInfoPanel();

        purifyHud.SetFlow(0);
        purifyHud.SetRemainingTime(0);
        purifyWaveTimer.SetRemainingTime(0);
        purifyHud.SetTreeHealth(purifyGameManager.Balance.StartingTreeHealth, purifyGameManager.Balance.MaximumTreeHealth);
        HandleEnergyChanged(Mathf.FloorToInt(purifyGameManager.Balance.StartingEnergy));
        HandleForestBreathLevelChanged(forestBreathController.CurrentLevel);
        purifyHud.SetForestBreathCharge(forestBreathController.ChargeProgress);

        UpdateSummonButtonDisabled();
        UpdateForestBreathButton();
    }

    protected override void OnSceneExit()
    {
        UnbindEvents();
        StopNoaSlotAnimations();

        if (purifyGameManager)
        {
            purifyGameManager.StopPurify();
        }
    }

    private void BindEvents()
    {
        purifyGameManager.WaveChanged += purifyHud.SetFlow;
        purifyGameManager.WaveChanged += HandleWaveStarted;
        purifyGameManager.RemainingTimeChanged += purifyHud.SetRemainingTime;
        purifyGameManager.RemainingTimeChanged += purifyWaveTimer.SetRemainingTime;
        purifyGameManager.TreeHealthChanged += purifyHud.SetTreeHealth;
        purifyGameManager.EnergyChanged += HandleEnergyChanged;
        purifyGameManager.PurifyCompleted += HandlePurifyCompleted;
        purifyGameManager.StateChanged += HandlePurifyStateChanged;
        forestBreathController.ChargeProgressChanged += purifyHud.SetForestBreathCharge;
        forestBreathController.ReadyChanged += UpdateForestBreathButton;
        forestBreathController.LevelChanged += HandleForestBreathLevelChanged;

        purifyNoaController.SlotSelected += HandleNoaSlotSelected;
        purifyNoaController.NoaSelected += HandleNoaSelected;
        purifyNoaController.MergeMaterialChanged += HandleMergeMaterialChanged;
        purifyNoaController.SlotSelectionCleared += HideNoaSelectionContexts;

        if (buttonSummon)
        {
            buttonSummon.OnClick.AddListener(OnClickSummonButton);
        }

        purifyHud.OnBreathTriggerClicked += OnClickBreathTriggerButton;
        purifyHud.OnBreathUpgradeClicked += OnClickBreathUpgradeButton;
        purifyHud.OnBlessingClicked += OnClickBlessingButton;
        purifyHud.OnSettingClicked += OnClickSettingButton;
        purifyHud.OnInfoClicked += OnClickInfoButton;

        if (frameBlessing)
        {
            frameBlessing.UseRequested += HandleBlessingUseRequested;
        }

        if (frameNoaMerge)
        {
            frameNoaMerge.OnClickMerge.AddListener(OnClickMergeButton);
        }
    }

    private void UnbindEvents()
    {
        if (!purifyGameManager) return;

        purifyGameManager.WaveChanged -= purifyHud.SetFlow;
        purifyGameManager.WaveChanged -= HandleWaveStarted;
        purifyGameManager.RemainingTimeChanged -= purifyHud.SetRemainingTime;
        purifyGameManager.RemainingTimeChanged -= purifyWaveTimer.SetRemainingTime;
        purifyGameManager.TreeHealthChanged -= purifyHud.SetTreeHealth;
        purifyGameManager.EnergyChanged -= HandleEnergyChanged;
        purifyGameManager.PurifyCompleted -= HandlePurifyCompleted;
        purifyGameManager.StateChanged -= HandlePurifyStateChanged;

        if (forestBreathController)
        {
            forestBreathController.ChargeProgressChanged -= purifyHud.SetForestBreathCharge;
            forestBreathController.ReadyChanged -= UpdateForestBreathButton;
            forestBreathController.LevelChanged -= HandleForestBreathLevelChanged;
        }

        if (purifyNoaController)
        {
            purifyNoaController.SlotSelected -= HandleNoaSlotSelected;
            purifyNoaController.NoaSelected -= HandleNoaSelected;
            purifyNoaController.MergeMaterialChanged -= HandleMergeMaterialChanged;
            purifyNoaController.SlotSelectionCleared -= HideNoaSelectionContexts;
        }

        if (purifyHud)
        {
            purifyHud.OnBreathTriggerClicked -= OnClickBreathTriggerButton;
            purifyHud.OnBreathUpgradeClicked -= OnClickBreathUpgradeButton;
            purifyHud.OnBlessingClicked -= OnClickBlessingButton;
            purifyHud.OnSettingClicked -= OnClickSettingButton;
            purifyHud.OnInfoClicked -= OnClickInfoButton;
        }

        if (frameBlessing)
        {
            frameBlessing.UseRequested -= HandleBlessingUseRequested;
        }
    }

    private void HandleEnergyChanged(int currentEnergy)
    {
        purifyHud.SetEnergy(currentEnergy);
        UpdateSummonButtonDisabled();
        if (frameBreathUpgrade) 
            frameBreathUpgrade.Refresh();
    }

    private async void HandlePurifyCompleted(PurifyResultType resultType)
    {
        Debug.Log($"[PurifyScene] Purify completed: {resultType}");
        if (Managers.Purify.IsSettlementRequestPending)
        {
            return;
        }

        if (!Managers.Purify.HasActiveRun)
        {
            Debug.LogError("[PurifyScene] Purify run ID is missing.");
            Managers.UI.ShowToast("정화 결과를 정산할 수 없습니다.");
            return;
        }

        Managers.UI.OpenLoading<UI_LoadingTransparent>();
        PurifySettlementResponse settlementResult = await Managers.Purify.SettleAsync(resultType, purifyGameManager.CompletedFlow, purifyGameManager.CurrentTreeHealth);
        Managers.UI.CloseLoading();

        if (_sceneCancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (settlementResult == null)
        {
            Managers.UI.ShowToast("정화 결과 정산에 실패했습니다.");
            return;
        }

        _settlementResult = settlementResult;
        if (_sceneCancellationToken.IsCancellationRequested)
        {
            return;
        }

        QueueRewardPresentation(_settlementResult);

        //포기 처리
        if (_settlementResult.ResultType == PurifyResultType.Fail && _settlementResult.ForestHp > 0)
        {
            Debug.Log($"[PurifyScene] Purify abandoned. RunId: {_settlementResult.RunId}");
            Managers.Scene.LoadScene(Define.Scene.Home);
            return;
        }

        Debug.Log($"[PurifyScene] Settlement completed. RunId: {_settlementResult.RunId}, Rewards: {_settlementResult.Rewards.Count}");

        bool isFailed = _settlementResult.ResultType == PurifyResultType.Fail && _settlementResult.ForestHp <= 0;
        Managers.Sound.Play(isFailed ? Define.AudioClip.PurifyFailed : Define.AudioClip.PurifySuccess, Define.AudioSourceType.Sfx, Define.AudioPath.Purify);
        Managers.UI.OpenPopup<UI_PurifyResultPopup>("UI_PurifyResultPopup", popup =>
        {
            popup.SetData(_settlementResult, purifyGameManager.Balance.MaximumTreeHealth);
        });
    }

    private void QueueRewardPresentation(PurifySettlementResponse settlementResult)
    {
        if (settlementResult == null || settlementResult.Rewards == null) return;

        List<RewardPresentationItem> rewardItems = new(settlementResult.Rewards.Count);
        foreach (PurifyRewardResponse reward in settlementResult.Rewards)
        {
            if (reward != null && reward.Amount > 0)
                rewardItems.Add(new RewardPresentationItem(reward.RewardType, reward.Amount));
        }

        Managers.Reward.EnqueueBatch(settlementResult.RunId, Define.Scene.Home, rewardItems);
    }

    private async Task<bool> PreparePurifySessionAsync(CancellationToken cancellationToken)
    {
        if (Managers.Purify.IsStartRequestPending || purifyGameManager.Stage == null)
        {
            return false;
        }

        PurifyStartResponse startResult = await Managers.Purify.StartAsync(purifyGameManager.Stage.StageId);

        cancellationToken.ThrowIfCancellationRequested();

        if (startResult == null)
        {
            Managers.UI.ShowToast("정화 시작 정보를 불러오지 못했습니다.");
            return false;
        }

        if (!purifyNoaController.SetAvailableNoas(startResult.AvailableNoaIds))
        {
            Managers.UI.ShowToast("사용 가능한 노아 정보를 불러오지 못했습니다.");
            return false;
        }

        Debug.Log($"[PurifyScene] Purify run ready. RunId: {startResult.RunId}, StageId: {startResult.StageId}");

        return true;
    }

    private void HandleWaveStarted(int flowNumber)
    {
        if (flowNumber <= 0 || flowNumber > PurifyWaveSetSO.WAVE_COUNT) return;
        Haptic.Vibrate();
        Managers.Sound.Play(Define.AudioClip.WaveStart, Define.AudioSourceType.Sfx, Define.AudioPath.Purify, 3f);
        if (flowNumber == 1)
        {
            Managers.UI.ShowToast($"{GetFlowTitle(flowNumber)}\n{PURIFY_START_MESSAGE}", Define.ToastPosition.Middle);
            return;
        }

        PurifyWaveData waveData = purifyGameManager.Stage.WaveSet.GetWave(flowNumber);
        Managers.UI.ShowToast(GetWaveTransitionMessage(waveData.WaveNumber, waveData.Element), Define.ToastPosition.Middle);
    }

    private string GetWaveTransitionMessage(int flowNumber, Define.ElementType element)
    {
        switch (element)
        {
            case Define.ElementType.Water:
                return $"{GetFlowTitle(flowNumber)}\n물 속성 모트가 몰려옵니다.";
            case Define.ElementType.Fire:
                return $"{GetFlowTitle(flowNumber)}\n불 속성 모트가 몰려옵니다.";
            case Define.ElementType.Wind:
                return $"{GetFlowTitle(flowNumber)}\n바람 속성 모트가 몰려옵니다.";
            case Define.ElementType.Earth:
                return $"{GetFlowTitle(flowNumber)}\n땅 속성 모트가 몰려옵니다.";
            default:
                return $"{GetFlowTitle(flowNumber)}\n{WAVE_RECOVERY_MESSAGE}";
        }
    }

    private string GetFlowTitle(int flowNumber)
    {
        return $"<size=40>FLOW {flowNumber:00}</size>";
    }

    private void HandlePurifyStateChanged(PurifyState state)
    {
        if (state != PurifyState.Playing)
        {
            purifyNoaController.ClearSelection();
            if (frameBreathUpgrade) frameBreathUpgrade.SetVisible(false);
            if (frameBlessing) frameBlessing.SetVisible(false);
        }

        UpdateSummonButtonDisabled();
        UpdateForestBreathButton();
        frameBreathUpgrade.Refresh();
    }

    private void HandleForestBreathLevelChanged(int level)
    {
        purifyHud.SetForestBreathLevel(level);
        if (frameBreathUpgrade) 
            frameBreathUpgrade.Refresh();
    }

    private void OnClickBreathTriggerButton()
    {
        if (!forestBreathController.CanTrigger)
        {
            Managers.UI.ShowToast("숲의 숨결이 아직 충전 중입니다.");
            return;
        }

        forestBreathController.Trigger();
    }

    private void OnClickBreathUpgradeButton()
    {
        if (!purifyGameManager.IsRunning) return;

        frameBlessing.SetVisible(false);
        frameBreathUpgrade.SetVisible(true);
    }

    private void OnClickBlessingButton()
    {
        if (!purifyGameManager.IsRunning) return;

        frameBreathUpgrade.SetVisible(false);
        frameBlessing.SetVisible(true);
    }

    private void OnClickSettingButton()
    {
        if (!purifyGameManager.IsRunning) return;

        Managers.UI.OpenPopup<UI_PurifySettingPopup>("UI_PurifySettingPopup", (popup) => popup.SetData(purifyGameManager.GiveUp));
    }

    private void OnClickInfoButton()
    {
        Managers.UI.OpenPopup<UI_ElementRelationPopup>("UI_ElementRelationPopup");
    }

    private void HandleBlessingUseRequested(BlessingSO blessing)
    {
        BlessingUseRequested?.Invoke(blessing);
        frameBlessing.Refresh();
    }

    //빈 슬롯이 선택되었을때
    private void HandleNoaSlotSelected(Transform slot)
    {
        if (!slot || !rectNoaSlotContext) return;

        HideNoaInfoPanel();
        HideNoaMergePanel();
        ClearMergeSelectionUi();

        rectNoaSlotContext.gameObject.SetActive(true);
        rectNoaSlotContext.position = slot.position;

        PlayNoaSlotAnimations();
        UpdateSummonButtonDisabled();
    }

    //노아 슬롯이 선택되었을때
    private void HandleNoaSelected(Noa noa)
    {
        HideNoaSlotContext();
        //노아 정보 UI 띄우기
        ShowNoaInfoPanel(noa);

        //합성 가능할때
        if (purifyNoaController.MergeCandidates.Count > 0)
        {
            ShowMergeContext(noa);
            return;
        }

        HideNoaMergePanel();
    }

    private void OnClickSummonButton()
    {
        if(buttonSummon && buttonSummon.IsDisabled)
        {
            //소환 불가능 - 비용 부족
            Managers.UI.ShowToast("정화 에너지가 부족합니다.");
            return;
        }

        if (!purifyNoaController.TrySummonSelectedSlot())
        {
            //소환 실패
            Managers.UI.ShowToast("노아 소환에 실패하였습니다.");
        }
    }

    private void OnClickMergeButton()
    {
        if (!purifyNoaController.SelectedMergeMaterial)
        {
            Managers.UI.ShowToast("합성에 사용될 노아를 클릭해주세요.");
            return;
        }

        var upgradedNoa = purifyNoaController.MergeSelectedNoa();
        if(upgradedNoa == null)
        {
            Managers.UI.ShowToast("노아 합성에 실패하였습니다.");
            return;
        }

        Debug.Log($"[PurifyScene] Noa merge completed: {upgradedNoa.Data.Id}");
    }

    private void ShowMergeContext(Noa baseNoa)
    {
        if(frameNoaMerge)
            frameNoaMerge.Show(baseNoa);

        CreateBaseMergeSelection(baseNoa);

        foreach (var candidate in purifyNoaController.MergeCandidates)
        {
            CreateMaterialMergeSelection(candidate);
        }
    }

    private void CreateBaseMergeSelection(Noa baseNoa)
    {
        if(!baseNoa || _baseMergeSelection) return;

        _baseMergeSelection = CreateMergeSelection(baseNoa, "Base");
        if(_baseMergeSelection)
            _baseMergeSelection.SetRole(true);
    }

    private void CreateMaterialMergeSelection(Noa candidate)
    {
        if(!candidate || _mergeSelections.ContainsKey(candidate)) return;

        UI_MergeSelection selection = CreateMergeSelection(candidate, "Material");
        if(!selection) return;

        selection.SetRole(false);
        _mergeSelections.Add(candidate, selection);
    }

    private UI_MergeSelection CreateMergeSelection(Noa noa, string role)
    {
        if(!noa) return null;

        GameObject selectionObject = Instantiate(mergeSelection);
        selectionObject.name = $"UI_MergeSelection_{role}_{noa.Data.Id}";
        selectionObject.transform.position = noa.transform.parent.position;

        UI_MergeSelection selection = selectionObject.GetComponent<UI_MergeSelection>();
        if (selection == null)
        {
            Debug.LogError("[PurifyScene] UI_MergeSelection component is missing.");
            Destroy(selectionObject);
            return null;
        }

        Canvas canvas = selection.GetComponent<Canvas>();
        canvas.sortingOrder = 10;
        canvas.overrideSorting = true;
        return selection;
    }

    //합성 재료로 될 노아가 바뀔때
    private void HandleMergeMaterialChanged(Noa mergeMaterial)
    {
        foreach (var selection in _mergeSelections)
        {
            if (selection.Value) selection.Value.SetChecked(selection.Key == mergeMaterial);
        }

        if(frameNoaMerge)
            frameNoaMerge.SetMergeMaterial(mergeMaterial);
    }

    private void ClearMergeSelectionUi()
    {
        if(_baseMergeSelection)
        {
            Destroy(_baseMergeSelection.gameObject);
            _baseMergeSelection = null;
        }

        foreach (var selection in _mergeSelections.Values)
        {
            if (selection)
            {
                Destroy(selection.gameObject);
            }
        }

        _mergeSelections.Clear();
    }

    private void PlayNoaSlotAnimations()
    {
        StopNoaSlotAnimations();

        if (imageSelection)
        {
            _selectionSequence = DOTween.Sequence()
                .Append(imageSelection.DOFade(0.35f, 0.45f))
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }

    private void StopNoaSlotAnimations()
    {
        _selectionSequence?.Kill();
        _selectionSequence = null;

        if(imageSelection)
            imageSelection.alpha = 1;
    }

    private void HideNoaSlotContext()
    {
        StopNoaSlotAnimations();

        ClearMergeSelectionUi();

        if (rectNoaSlotContext)
        {
            rectNoaSlotContext.gameObject.SetActive(false);
        }
    }

    private void HideNoaSelectionContexts()
    {
        HideNoaSlotContext();
        HideNoaMergePanel();
        HideNoaInfoPanel();
    }

    private void HideNoaMergePanel()
    {
        if(frameNoaMerge)
            frameNoaMerge.Hide();
    }

    private void HideNoaInfoPanel()
    {
        if (frameNoaInfo)
        {
            frameNoaInfo.gameObject.SetActive(false);
        }
    }

    private void ShowNoaInfoPanel(Noa noa)
    {
        if(noa == null)
        {
            HideNoaInfoPanel();
            return;
        }

        if(frameNoaInfo)
        {
            frameNoaInfo.SetData(noa);
            frameNoaInfo.gameObject.SetActive(true);
        }
    }

    private void UpdateSummonButtonDisabled()
    {
        if (buttonSummon == null) return;

        buttonSummon.IsDisabled = !purifyNoaController.CanSummonSelectedSlot;
        buttonSummon.Image.sprite = buttonSummon.IsDisabled ? spriteDisabledButton : spriteOriginButton;
    }

    private void UpdateForestBreathButton()
    {
        if (!forestBreathController) return;

        purifyHud.SetForestBreathButtonState(forestBreathController.CanTrigger, purifyGameManager.IsRunning);
    }

#if UNITY_EDITOR
    public bool TrySummonNoaForTest(Define.ElementType element, Define.NoaTier tier)
    {
        if (!purifyNoaController)
        {
            Debug.LogWarning("[PurifyScene] PurifyNoaController is missing.");
            return false;
        }

        return purifyNoaController.TrySummonForTest(element, tier);
    }
#endif
}
