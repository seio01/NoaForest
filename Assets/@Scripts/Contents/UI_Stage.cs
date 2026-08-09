using System;
using UnityEngine;
using UnityEngine.UI;

public class UI_Stage : UI_Base
{
    [Header("Stage UI")]
    [SerializeField] private Image imageStage;
    [SerializeField] private TextBase textStageName;
    [SerializeField] private Button buttonStage;
    [SerializeField] private ButtonBase buttonInfo;
    [SerializeField] private GameObject playingBadge;
    [SerializeField] private GameObject frameLock;

    private StageSO _stageData;
    private Action<StageSO> _onClickStage;
    private Action<StageSO> _onClickInfo;
    private bool _isUnlocked;

    public Define.StageId StageId => _stageData != null ? _stageData.StageId : Define.StageId.None;

    private void Awake()
    {
        if (buttonStage)
            buttonStage.onClick.AddListener(HandleStageButtonClicked);
        if (buttonInfo)
            buttonInfo.OnClick.AddListener(HandleInfoButtonClicked);
    }

    public void SetData(StageSO stageData, bool isSelected, bool isUnlocked, Action<StageSO> onClickStage, Action<StageSO> onClickInfo)
    {
        if (stageData == null) return;

        _stageData = stageData;
        _onClickStage = onClickStage;
        _onClickInfo = onClickInfo;

        if (imageStage)
        {
            imageStage.sprite = stageData.SpriteStage;
        }

        if (textStageName)
        {
            textStageName.text = stageData.StageName;
        }

        SetUnlocked(isUnlocked);
        SetSelected(isSelected);
    }

    public void SetSelected(bool isSelected)
    {
        if (playingBadge)
        {
            playingBadge.SetActive(_isUnlocked && isSelected);
        }
    }

    private void SetUnlocked(bool isUnlocked)
    {
        _isUnlocked = isUnlocked;

        if(frameLock)
            frameLock.SetActive(!isUnlocked);
    }

    private void HandleStageButtonClicked()
    {
        if (_stageData == null) return;
        if (!_isUnlocked)
        {
            Managers.UI.ShowToast("이전 스테이지를 먼저 정화해 주세요.");
            return;
        }

        _onClickStage?.Invoke(_stageData);
    }

    private void HandleInfoButtonClicked()
    {
        if (_stageData == null) return;

        _onClickInfo?.Invoke(_stageData);
    }
}
