using System;
using UnityEngine;
using UnityEngine.UI;

public class UI_SettingPopup : PopupBase
{
    [Header("Sound")]
    [SerializeField] private UI_Toggle toggleBgm;
    [SerializeField] private UI_Toggle toggleSfx;
    [SerializeField] private UI_Toggle toggleVibration;

    [Header("Account")]
    [SerializeField] private GameObject frameNoLink;
    [SerializeField] private GameObject frameYesLink;
    [SerializeField] private ButtonBase buttonLinkAccount;
    [SerializeField] private ButtonBase buttonLogout;
    [SerializeField] private Button buttonWithdraw;

    [Header("User Info")]
    [SerializeField] private TextBase textUserId;
    [SerializeField] private TextBase textVersion;
    [SerializeField] private ButtonBase buttonCopy;


    private bool _isProcessingAccount;

    protected override void OnAwake()
    {
        base.OnAwake();
        BindButtons();
    }

    private void BindButtons()
    {
        if (toggleBgm)
            toggleBgm.ValueChanged += OnBgmValueChanged;
        if (toggleSfx)
            toggleSfx.ValueChanged += OnSfxValueChanged;
        if (toggleVibration)
            toggleVibration.ValueChanged += OnVibrationValueChanged;
        
        if(buttonLinkAccount)
            buttonLinkAccount.OnClick.AddListener(OnClickLinkAccount);
        if(buttonLogout)
            buttonLogout.OnClick.AddListener(OnClickLogout);
        if(buttonCopy)
            buttonCopy.OnClick.AddListener(OnClickCopy);
        if(buttonWithdraw)
            buttonWithdraw.onClick.AddListener(OnClickWithdraw);
    }

    public void RefreshView()
    {
        UserSettingData settingData = Managers.UserSetting.CurrentSetting;
        toggleBgm?.SetValueWithoutNotify(settingData.IsBgmEnabled);
        toggleSfx?.SetValueWithoutNotify(settingData.IsSfxEnabled);
        toggleVibration?.SetValueWithoutNotify(settingData.IsVibrationEnabled);
        RefreshAccountState();

        if (textUserId)
            textUserId.text = Managers.Profile.CurrentUserData?.Id ?? string.Empty;
        if (textVersion)
            textVersion.text = Application.version;
    }

    private void OnBgmValueChanged(bool isOn)
    {
        bool isSaved = Managers.UserSetting.SetBgmEnabled(isOn);
        ApplySoundSettings();
        HandleUserSettingSaveResult(isSaved);
    }

    private void OnSfxValueChanged(bool isOn)
    {
        bool isSaved = Managers.UserSetting.SetSfxEnabled(isOn);
        ApplySoundSettings();
        HandleUserSettingSaveResult(isSaved);
    }

    private void OnVibrationValueChanged(bool isOn)
    {
        HandleUserSettingSaveResult(Managers.UserSetting.SetVibrationEnabled(isOn));
    }

    private void HandleUserSettingSaveResult(bool isSaved)
    {
        if (!isSaved)
            Managers.UI.ShowToast("설정을 저장하지 못했습니다.");
    }

    private void ApplySoundSettings()
    {
        UserSettingData settingData = Managers.UserSetting.CurrentSetting;
        Managers.Sound.ApplySettings(settingData.IsBgmEnabled, settingData.IsSfxEnabled);
    }

    private async void OnClickLinkAccount()
    {
        if (_isProcessingAccount)
            return;

        SetAccountProcessing(true);
        GoogleAccountLinkResult result = GoogleAccountLinkResult.Failed;
        try
        {
            Managers.UI.OpenLoading<UI_LoadingTransparent>();
            result = await Managers.Auth.LinkGuestWithGoogleAsync();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[UI_SettingPopup] Google account link failed: {exception}");
        }
        finally
        {
            Managers.UI.CloseLoading();
            SetAccountProcessing(false);
            RefreshAccountState();
        }

        if (result == GoogleAccountLinkResult.ExistingAccount)
        {
            OpenExistingGoogleAccountConfirm();
            return;
        }

        if (result == GoogleAccountLinkResult.Linked)
            Managers.UI.ShowToast("구글 계정이 연동되었습니다.");
        else
            Managers.UI.ShowToast("구글 계정을 연동하지 못했습니다.");
    }

    private void OpenExistingGoogleAccountConfirm()
    {
        Managers.UI.OpenPopup<UI_ConfirmPopup>("UI_ConfirmPopup", popup =>
        {
            popup.SetData(new ConfirmPopupData
            {
                title = "확인",
                info = "이미 연동된 계정입니다.\n기존 계정의 게임 데이터를 불러오시겠습니까?",
                hasImage = false,
                leftButtonData = new ConfirmPopupButtonData
                {
                    name = "취소",
                    color = ButtonColorType.White,
                    clickAction = OnCancelExistingGoogleAccount
                },
                rightButtonData = new ConfirmPopupButtonData
                {
                    name = "확인",
                    color = ButtonColorType.Olive,
                    clickAction = OnConfirmExistingGoogleAccount
                }
            });
        });
    }

    private void OnCancelExistingGoogleAccount()
    {
        Managers.Auth.CancelPendingGoogleAccount();
        Managers.UI.ClosePopup();
    }

    private async void OnConfirmExistingGoogleAccount()
    {
        if (_isProcessingAccount)
            return;

        Managers.UI.ClosePopup();
        SetAccountProcessing(true);
        Managers.UI.OpenLoading<UI_LoadingTransparent>();
        bool isSuccess = await Managers.Auth.SignInWithPendingGoogleAccountAsync();
        Managers.UI.CloseLoading();

        if (!isSuccess)
        {
            SetAccountProcessing(false);
            Managers.UI.ShowToast("기존 계정으로 로그인하지 못했습니다.");
            return;
        }

        Managers.Currency.ResetForAccountChange();
        Managers.Profile.ResetForAccountChange();
        SetAccountProcessing(false);
        Managers.Scene.LoadScene(Define.Scene.GameBootstrap);
    }

    private async void OnClickLogout()
    {
        if (_isProcessingAccount)
            return;

        SetAccountProcessing(true);
        Managers.Auth.SignOut();
        await Managers.Auth.SignInAsGuestAsync();

        if (!Managers.Auth.IsLoggedIn)
        {
            SetAccountProcessing(false);
            Managers.UI.ShowToast("로그아웃 후 게스트 로그인에 실패했습니다.");
            return;
        }

        await Managers.Data.LoadAsync();
        SetAccountProcessing(false);
        RefreshView();
        Managers.UI.ShowToast("로그아웃되었습니다.");
    }

    private void SetAccountProcessing(bool isProcessing)
    {
        _isProcessingAccount = isProcessing;
        if (buttonLinkAccount)
            buttonLinkAccount.Interactable = !isProcessing;
        if (buttonLogout)
            buttonLogout.Interactable = !isProcessing;
        if (buttonWithdraw)
            buttonWithdraw.interactable = !isProcessing;
    }

    private void RefreshAccountState()
    {
        bool isLinked = Managers.Auth.IsGoogleLinked;
        frameNoLink?.SetActive(!isLinked);
        frameYesLink?.SetActive(isLinked);

        if (buttonLinkAccount)
            buttonLinkAccount.gameObject.SetActive(!isLinked);
        if (buttonLogout)
            buttonLogout.gameObject.SetActive(isLinked);
    }

    private void OnClickCopy()
    {
        string userId = Managers.Profile.CurrentUserData?.Id;
        if (string.IsNullOrEmpty(userId))
        {
            Managers.UI.ShowToast("복사할 사용자 ID가 없습니다.");
            return;
        }

        GUIUtility.systemCopyBuffer = userId;
        Managers.UI.ShowToast("사용자 ID를 복사했습니다.");
    }

    private void OnClickWithdraw()
    {
        if (_isProcessingAccount)
            return;

        Managers.UI.OpenPopup<UI_ConfirmPopup>("UI_ConfirmPopup", popup =>
        {
            popup.SetData(new ConfirmPopupData
            {
                title = "계정 탈퇴",
                info = "모든 게임 데이터가 삭제되어 복구할 수 없습니다.\n정말 탈퇴하시겠습니까?",
                hasImage = true,
                leftButtonData = new ConfirmPopupButtonData
                {
                    name = "취소",
                    color = ButtonColorType.White,
                    clickAction = Managers.UI.ClosePopup
                },
                rightButtonData = new ConfirmPopupButtonData
                {
                    name = "탈퇴하기",
                    color = ButtonColorType.Red,
                    clickAction = OnConfirmWithdraw
                }
            });
        });
    }

    private async void OnConfirmWithdraw()
    {
        if (_isProcessingAccount)
            return;

        Managers.UI.ClosePopup();
        SetAccountProcessing(true);

        string previousUserId = Managers.Auth.UserId;
        Managers.UI.OpenLoading<UI_LoadingTransparent>();
        bool isSuccess = await Managers.Auth.WithdrawAsync();
        Managers.UI.CloseLoading();
        if (!isSuccess)
        {
            SetAccountProcessing(false);
            Managers.UI.ShowToast("계정 탈퇴에 실패했습니다.");
            return;
        }

        Managers.Currency.ResetForAccountChange();
        Managers.Profile.ResetForAccountChange();
        isSuccess = Managers.Data.DeleteLocalSave(previousUserId);
        if (!isSuccess)
            Debug.LogWarning("[UI_SettingPopup] The account was deleted, but local save cleanup failed.");

        SetAccountProcessing(false);
        Managers.Scene.LoadScene(Define.Scene.GameBootstrap);
    }
}
