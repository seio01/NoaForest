using System;
using UnityEngine;

public class UI_PurifySettingPopup : PopupBase
{
    [Header("Sound")]
    [SerializeField] private UI_Toggle toggleBgm;
    [SerializeField] private UI_Toggle toggleSfx;
    [SerializeField] private UI_Toggle toggleVibration;

    [Header("Play")]
    [SerializeField] private UI_Toggle toggleEffect;
    [SerializeField] private UI_Toggle toggleDamageFont;
    [SerializeField] private ButtonBase buttonExitPurify;

    private Action _onExitPurify;

    protected override void OnAwake()
    {
        base.OnAwake();
        BindEvents();
    }

    public void SetData(Action onExitPurify)
    {
        _onExitPurify = onExitPurify;
        RefreshView();
    }

    private void BindEvents()
    {
        if (toggleBgm)
            toggleBgm.ValueChanged += OnBgmValueChanged;
        if (toggleSfx)
            toggleSfx.ValueChanged += OnSfxValueChanged;
        if (toggleVibration)
            toggleVibration.ValueChanged += OnVibrationValueChanged;
        if (toggleEffect)
            toggleEffect.ValueChanged += OnEffectValueChanged;
        if (toggleDamageFont)
            toggleDamageFont.ValueChanged += OnDamageFontValueChanged;
        if (buttonExitPurify)
            buttonExitPurify.OnClick.AddListener(OnClickExitPurify);
    }

    private void RefreshView()
    {
        UserSettingData settingData = Managers.UserSetting.CurrentSetting;
        toggleBgm?.SetValueWithoutNotify(settingData.IsBgmEnabled);
        toggleSfx?.SetValueWithoutNotify(settingData.IsSfxEnabled);
        toggleVibration?.SetValueWithoutNotify(settingData.IsVibrationEnabled);
        toggleEffect?.SetValueWithoutNotify(settingData.IsEffectEnabled);
        toggleDamageFont?.SetValueWithoutNotify(settingData.IsDamageFontEnabled);
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

    private void OnEffectValueChanged(bool isOn)
    {
        HandleUserSettingSaveResult(Managers.UserSetting.SetEffectEnabled(isOn));
        Debug.Log($"[UI_PurifySettingPopup] Effect setting changed: {(isOn ? "ON" : "OFF")}");
    }

    private void OnDamageFontValueChanged(bool isOn)
    {
        HandleUserSettingSaveResult(Managers.UserSetting.SetDamageFontEnabled(isOn));
        Debug.Log($"[UI_PurifySettingPopup] Damage font setting changed: {(isOn ? "ON" : "OFF")}");
    }

    private void OnClickExitPurify()
    {
        Managers.UI.OpenPopup<UI_ConfirmPopup>("UI_ConfirmPopup", popup =>
        {
            popup.SetData(new ConfirmPopupData
            {
                title = "정화 종료",
                info = "정화를 종료하면 실패 처리되며 보상을 받을 수 없습니다.\n정말 종료하시겠습니까?",
                hasImage = false,
                leftButtonData = new ConfirmPopupButtonData
                {
                    name = "취소",
                    color = ButtonColorType.White,
                    clickAction = Managers.UI.ClosePopup
                },
                rightButtonData = new ConfirmPopupButtonData
                {
                    name = "종료하기",
                    color = ButtonColorType.Red,
                    clickAction = OnConfirmExitPurify
                }
            });
        });
    }

    private void OnConfirmExitPurify()
    {
        Managers.UI.CloseAllPopup();
        _onExitPurify?.Invoke();
    }
}
