using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HomeScene : BaseScene
{
    [Header("Stage")]
    [SerializeField] private Image imageStageHome;

    [Header("TopHUD")]
    [SerializeField] private UI_CurrencyBar currencyBar;
    [SerializeField] private UI_Profile profile;
    [SerializeField] private ButtonBase buttonBlessingAltar;
    [SerializeField] private ButtonBase buttonMore;
    [SerializeField] private UI_HomeMoreMenu homeMoreMenu;

    [Header("FlowRock")]
    [SerializeField] private TextBase textFlow;

    [Header("BottomHUD")]
    [SerializeField] private ButtonBase buttonStage;
    [SerializeField] private ButtonBase buttonNoa;
    [SerializeField] private ButtonBase buttonPurify;
    [SerializeField] private ButtonBase buttonArtifact;

    private StageSO _selectedStage;

    protected override void OnSceneReady()
    {
        Managers.Sound.Play(Define.AudioClip.HomeBGM, Define.AudioSourceType.Bgm);
        RestoreSelectedStage();
        RefreshStageHome();
        BindButtons();
        RefreshFlowText();
        SetProfile();
    }

    private void BindButtons()
    {
        if(profile)
            profile.Clicked += OnClickProfile;
        if(buttonMore)
            buttonMore.OnClick.AddListener(OnClickMore);
        if(buttonStage)
            buttonStage.OnClick.AddListener(OnClickStage);
        if(buttonNoa)
            buttonNoa.OnClick.AddListener(OnClickNoa);
        if(buttonPurify)
            buttonPurify.OnClick.AddListener(OnClickPurify);
        if(buttonArtifact)
            buttonArtifact.OnClick.AddListener(OnClickArtifact);
        if(buttonBlessingAltar)
            buttonBlessingAltar.OnClick.AddListener(OnClickBlessingAltar);
    }

    private void SetProfile()
    {
        if(profile)
            profile.SetProfile(Managers.Profile.CurrentUserData);
    }

    private void SetFlowText(int flow)
    {
        if(textFlow)
            textFlow.text = flow.ToString();
    }

    private void RefreshFlowText()
    {
        int bestFlow = _selectedStage ? Managers.Purify.GetBestFlow(_selectedStage.StageId) : 0;
        SetFlowText(bestFlow);
    }

    private void RefreshStageHome()
    {
        if (!imageStageHome || !_selectedStage || !_selectedStage.SpriteStageHome) return;

        imageStageHome.sprite = _selectedStage.SpriteStageHome;
    }

    private void OnClickMore()
    {
        homeMoreMenu.Toggle();
    }

    private void OnClickProfile()
    {
        Managers.UI.OpenPopup<UI_ProfilePopup>("UI_ProfilePopup", popup =>
        {
            popup.SetData(SetProfile);
        });
    }

    private void OnClickStage()
    {
        Managers.UI.OpenPopup<UI_StageSelectPopup>("UI_StageSelectPopup", popup =>
        {
            popup.SetData(_selectedStage, HandleStageSelected);
        });
    }

    private void HandleStageSelected(StageSO stageData)
    {
        if (stageData == null) return;

        _selectedStage = stageData;
        RefreshStageHome();
        RefreshFlowText();
        Debug.Log($"[HomeScene] Stage selected: {stageData.StageName}");

        SavePatch patch = new SavePatch().Set(SaveField.SelectedStageId, (int)stageData.StageId);

        Managers.Data.Save(patch);
    }

    private void OnClickNoa()
    {
        Managers.UI.OpenPopup<UI_NoaCollectionPopup>("UI_NoaCollectionPopup");
    }

    private void OnClickPurify()
    {
        Managers.UI.OpenPopup<UI_BlessingSelectPopup>("UI_BlessingSelectPopup", popup =>
        {
            popup.SetData(StartPurify);
        });
    }

    private void StartPurify(List<BlessingSO> selectedBlessings)
    {
        if (_selectedStage)
        {
            Managers.Scene.SetParameter(Constants.STAGE_KEY, _selectedStage);
        }

        Managers.Scene.SetParameter(Constants.PURIFY_BLESSINGS_KEY, selectedBlessings);
        Managers.Scene.LoadScene(Define.Scene.Purify);
    }

    private void OnClickArtifact()
    {
        Managers.UI.OpenPopup<UI_BlessingCollectionPopup>("UI_BlessingCollectionPopup");
    }

    private void OnClickBlessingAltar()
    {
        Managers.UI.OpenPopup<UI_BlessingSummonPopup>("UI_BlessingSummonPopup");
    }

    private void RestoreSelectedStage()
    {
        StageGroupSO stageCatalog = Managers.GameData.Stages;
        if (stageCatalog == null)
        {
            Debug.LogError("[HomeScene] Stage catalog is missing.");
            return;
        }

        SaveData saveData = Managers.Data.CurrentSaveData;
        StageSO savedStage = stageCatalog.GetStage((Define.StageId)saveData.SelectedStageId);
        _selectedStage = savedStage != null && stageCatalog.IsUnlocked(savedStage.StageId, saveData.ClearedStageIds)
            ? savedStage
            : stageCatalog.GetStage(Define.StageId.Stage1);
    }
}
