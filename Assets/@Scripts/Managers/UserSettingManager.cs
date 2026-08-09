using System;
using UnityEngine;

public class UserSettingManager
{
    private readonly LocalSaveRepository _repository = new();

    public UserSettingData CurrentSetting { get; private set; } = new();

    public void Initialize()
    {
        _repository.Initialize();
        UserSettingData loadedSetting = _repository.LoadUserSetting();
        if (loadedSetting != null)
        {
            CurrentSetting = loadedSetting;
            return;
        }

        CurrentSetting = new UserSettingData();
        _repository.SaveUserSetting(CurrentSetting);
    }

    public bool SetBgmEnabled(bool isEnabled)
    {
        CurrentSetting.IsBgmEnabled = isEnabled;
        return _repository.SaveUserSetting(CurrentSetting);
    }

    public bool SetSfxEnabled(bool isEnabled)
    {
        CurrentSetting.IsSfxEnabled = isEnabled;
        return _repository.SaveUserSetting(CurrentSetting);
    }

    public bool SetVibrationEnabled(bool isEnabled)
    {
        CurrentSetting.IsVibrationEnabled = isEnabled;
        return _repository.SaveUserSetting(CurrentSetting);
    }

    public bool SetEffectEnabled(bool isEnabled)
    {
        CurrentSetting.IsEffectEnabled = isEnabled;
        return _repository.SaveUserSetting(CurrentSetting);
    }

    public bool SetDamageFontEnabled(bool isEnabled)
    {
        CurrentSetting.IsDamageFontEnabled = isEnabled;
        return _repository.SaveUserSetting(CurrentSetting);
    }
}
