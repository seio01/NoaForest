using System;
using TMPro;
using UnityEngine;

public class UI_ProfilePopup : PopupBase
{
    [SerializeField] private UI_Profile profilePreview;
    [SerializeField] private TMP_InputField inputName;
    [SerializeField] private TextBase textCount;
    [SerializeField] private ButtonBase buttonCancel;
    [SerializeField] private ButtonBase buttonSave;

    public const int MAX_NAME_LENGTH = 12;

    private Action _onSaved;
    private bool _isSaving;

    protected override void OnAwake()
    {
        base.OnAwake();
        Bind();
    }
    
    public void SetData(Action onSaved)
    {
        _onSaved = onSaved;

        UserData userData = Managers.Profile.CurrentUserData;
        if (profilePreview)
            profilePreview.SetProfile(userData);

        SetCount("");
    }

    private void Bind()
    {
        if (inputName)
        {
            inputName.characterLimit = MAX_NAME_LENGTH;
            inputName.onValueChanged.AddListener(SetCount);
        }
        if (buttonCancel)
            buttonCancel.OnClick.AddListener(OnClickCancel);
        if (buttonSave)
            buttonSave.OnClick.AddListener(OnClickSave);
    }

    private void SetCount(string value)
    {
        if (textCount)
            textCount.text = $"{value.Length}/{MAX_NAME_LENGTH}";
    }

    private void OnClickCancel()
    {
        if (_isSaving) return;
        
        Managers.UI.ClosePopup();
    }

    private async void OnClickSave()
    {
        if (_isSaving || !inputName)
            return;

        if (string.IsNullOrWhiteSpace(inputName.text))
        {
            Managers.UI.ShowToast("닉네임을 입력해 주세요.");
            return;
        }
        if (!Managers.Profile.IsValidName(inputName.text))
        {
            Managers.UI.ShowToast("사용할 수 없는 닉네임입니다.");
            return;
        }

        SetSaving(true);
        Managers.UI.OpenLoading<UI_LoadingTransparent>();
        bool isSuccess = await Managers.Profile.UpdateNameAsync(inputName.text);
        Managers.UI.CloseLoading();

        if (!isSuccess)
        {
            SetSaving(false);
            Managers.UI.ShowToast("닉네임 저장에 실패했습니다.");
            return;
        }

        Managers.UI.ShowToast("닉네임 저장에 성공했습니다.");

        profilePreview?.SetProfile(Managers.Profile.CurrentUserData);
        _onSaved?.Invoke();
        Managers.UI.ClosePopup();
    }

    private void SetSaving(bool isSaving)
    {
        _isSaving = isSaving;
        if (buttonCancel)
            buttonCancel.Interactable = !isSaving;
        if (buttonSave)
            buttonSave.Interactable = !isSaving;
        if (inputName)
            inputName.interactable = !isSaving;
    }

}
