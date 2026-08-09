using System;
using System.Collections.Generic;
using UnityEngine;

public class UI_StageSelectPopup : PopupBase
{
    [Header("Stage List")]
    [SerializeField] private UI_Stage stagePrefab;
    [SerializeField] private RectTransform rectStageContainer;

    private readonly List<UI_Stage> _stageViews = new();
    private StageSO _selectedStage;
    private Action<StageSO> _onStageSelected;

    public event Action<StageSO> OnStageInfoClicked;

    public void SetData(StageSO selectedStage, Action<StageSO> onStageSelected)
    {
        _selectedStage = selectedStage;
        _onStageSelected = onStageSelected;
    }

    private void Start()
    {
        RenderStages();
    }

    private void RenderStages()
    {
        StageGroupSO stageCatalog = Managers.GameData.Stages;
        if (stageCatalog == null || stagePrefab == null || rectStageContainer == null)
        {
            return;
        }

        ClearExistingStages();

        HashSet<Define.StageId> registeredStageIds = new();

        foreach (var stageData in stageCatalog.StageData)
        {
            if (stageData == null) continue;

            if (!registeredStageIds.Add(stageData.StageId))
            {
                Debug.LogError( $"[UI_StageSelectPopup] Duplicate StageId: {stageData.StageId}");
                continue;
            }

            UI_Stage stageView = Instantiate(stagePrefab, rectStageContainer, false);
            bool isSelected = _selectedStage != null && stageData.StageId == _selectedStage.StageId;
            bool isUnlocked = stageCatalog.IsUnlocked(stageData.StageId, Managers.Data.CurrentSaveData.ClearedStageIds);
            stageView.SetData(stageData, isSelected, isUnlocked, OnClickStage, OnClickStageInfo);
            _stageViews.Add(stageView);
        }
    }

    private void ClearExistingStages()
    {
        var existingStageViews = rectStageContainer.GetComponentsInChildren<UI_Stage>(true);

        foreach (var existingStageView in existingStageViews)
        {
            existingStageView.gameObject.SetActive(false);
            Destroy(existingStageView.gameObject);
        }

        _stageViews.Clear();
    }

    private void OnClickStage(StageSO stageData)
    {
        if (stageData == null) return;

        _selectedStage = stageData;

        foreach (UI_Stage stageView in _stageViews)
        {
            stageView.SetSelected(stageView.StageId == stageData.StageId);
        }

        Debug.Log($"[UI_StageSelectPopup] Stage selected: {stageData.StageName}");
        _onStageSelected?.Invoke(stageData);
        Managers.UI.ClosePopup();
    }

    private void OnClickStageInfo(StageSO stageData)
    {
        if (stageData == null) return;

        Debug.Log($"[UI_StageSelectPopup] Stage info requested: {stageData.StageName}");
        OnStageInfoClicked?.Invoke(stageData);
    }
}
