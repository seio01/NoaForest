using System;
using UnityEngine;
using UnityEngine.UI;

public class UI_NoaCollectionInfoPopup : PopupBase
{
    [Header("State")]
    [SerializeField] private GameObject frameNoaInfo;
    [SerializeField] private GameObject frameLock;

    [Header("Noa")]
    [SerializeField] private Image imageNoa;
    [SerializeField] private Image imageElement;
    [SerializeField] private GameObject frameStars;
    [SerializeField] private GameObject[] imageStars = Array.Empty<GameObject>();
    [SerializeField] private TextBase textNoaName;
    [SerializeField] private TextBase textDescription;

    [Header("Level")]
    [SerializeField] private TextBase textLevel;
    [SerializeField] private TextBase textLevelProgress;
    [SerializeField] private Slider sliderLevel;

    [Header("Stats")]
    [SerializeField] private UI_NoaStatRow statPower;
    [SerializeField] private UI_NoaStatRow statInterval;
    [SerializeField] private UI_NoaStatRow statRange;

    [Header("Level Up")]
    [SerializeField] private TextBase textCost;
    [SerializeField] private ButtonBase buttonLevelUp;

    [Header("Unlock")]
    [SerializeField] private Image imageNoaLock;
    [SerializeField] private TextBase textNoaLock;
    [SerializeField] private TextBase textCurrencyLock;
    [SerializeField] private ButtonBase buttonUnlock;

    private NoaSO _noaData;
    private Action<NoaSO> _onChanged;
    private bool _isUpgrading;
    private bool _isUnlocking;

    protected override void OnAwake()
    {
        base.OnAwake();
        if (buttonLevelUp)
            buttonLevelUp.OnClick.AddListener(OnClickLevelUp);
        if (buttonUnlock)
            buttonUnlock.OnClick.AddListener(OnClickUnlock);

        Managers.Currency.CurrencyChanged += OnCurrencyChanged;
    }

    protected override void Destroy()
    {
        Managers.Currency.CurrencyChanged -= OnCurrencyChanged;
        base.Destroy();
    }

    public void SetData(NoaSO noaData, Action<NoaSO> onChanged)
    {
        _noaData = noaData;
        _onChanged = onChanged;
        Refresh();
    }

    private void Refresh()
    {
        if (!_noaData)
            return;

        bool isUnlocked = Managers.Collection.IsUnlocked(_noaData);
        SetOwnershipState(isUnlocked);

        if (isUnlocked)
            SetUnlockedInfo();
        else
            SetLockedInfo();
    }

    private void SetOwnershipState(bool isUnlocked)
    {
        if (frameNoaInfo)
            frameNoaInfo.SetActive(isUnlocked);
        if (frameLock)
            frameLock.SetActive(!isUnlocked);
        if (buttonLevelUp)
            buttonLevelUp.gameObject.SetActive(isUnlocked);
        if (buttonUnlock)
            buttonUnlock.gameObject.SetActive(!isUnlocked);
    }

    private void SetUnlockedInfo()
    {
        NoaGroupSO noaCatalog = Managers.GameData.Noas;
        if (!noaCatalog || !noaCatalog.Stats)
            return;

        int currentLevel = Managers.Collection.GetLevel(_noaData);
        int maximumLevel = Managers.Collection.GetMaximumLevel(_noaData);
        bool isMaximumLevel = currentLevel >= maximumLevel;
        NoaCalculatedStats? currentStats = noaCatalog.Stats.GetCalculatedStats(_noaData.Tier, currentLevel);
        NoaCalculatedStats? nextStats = isMaximumLevel ? null : noaCatalog.Stats.GetCalculatedStats(_noaData.Tier, currentLevel + 1);

        SetNoaIdentity();
        SetLevel(currentLevel, maximumLevel);
        SetStats(currentStats, nextStats, isMaximumLevel);
        SetLevelUpState(currentLevel, isMaximumLevel);
    }

    private void SetNoaIdentity()
    {
        if (imageNoa)
        {
            imageNoa.sprite = Managers.ContentIcon.GetLoadedSprite(Define.ContentIconType.Noa, _noaData.IconId);
            imageNoa.gameObject.SetActive(imageNoa.sprite);
        }

        if (imageElement)
        {
            imageElement.sprite = Managers.ContentIcon.GetElementSprite(_noaData.Element);
            imageElement.gameObject.SetActive(imageElement.sprite);
        }

        if (textNoaName)
            textNoaName.text = _noaData.DisplayName;
        if (textDescription)
            textDescription.text = _noaData.Description;

        SetStars((int)_noaData.Tier);
    }

    private void SetStars(int starCount)
    {
        if (frameStars)
            frameStars.SetActive(starCount > 0);

        int visibleStarCount = Mathf.Clamp(starCount, 0, imageStars.Length);
        for (int index = 0; index < imageStars.Length; index++)
        {
            if (imageStars[index])
                imageStars[index].SetActive(index < visibleStarCount);
        }
    }

    private void SetLevel(int currentLevel, int maximumLevel)
    {
        if (textLevel)
            textLevel.text = $"LV.{currentLevel}";
        if (textLevelProgress)
            textLevelProgress.text = $"{currentLevel:N0}/{maximumLevel:N0}";
        if (sliderLevel)
        {
            sliderLevel.minValue = 1;
            sliderLevel.maxValue = Mathf.Max(1, maximumLevel);
            sliderLevel.value = Mathf.Clamp(currentLevel, 1, maximumLevel);
        }
    }

    private void SetStats(NoaCalculatedStats? currentStats, NoaCalculatedStats? nextStats, bool isMaximumLevel)
    {
        if (statPower)
            statPower.SetData(currentStats?.PurifyPower, nextStats?.PurifyPower, isMaximumLevel);
        if (statInterval)
            statInterval.SetData(currentStats?.PurifyInterval, nextStats?.PurifyInterval, isMaximumLevel);
        if (statRange)
            statRange.SetData(currentStats?.PurifyRange, nextStats?.PurifyRange, isMaximumLevel);
    }

    private void SetLevelUpState(int currentLevel, bool isMaximumLevel)
    {
        int cost = Managers.Collection.GetUpgradeCost(_noaData, currentLevel);
        bool canUpgrade = !_isUpgrading && !isMaximumLevel && Managers.Collection.CanUpgrade(_noaData);

        if (textCost)
        {
            textCost.text = isMaximumLevel ? "MAX" : cost.ToString("N0");
            textCost.SetTextColor(canUpgrade ? Define.TextColorPalette.White : Define.TextColorPalette.Brown1);
        }
        if (buttonLevelUp)
            buttonLevelUp.Interactable = canUpgrade;
    }

    private void SetLockedInfo()
    {
        if (imageNoaLock)
        {
            imageNoaLock.sprite = Managers.ContentIcon.GetLoadedSprite(Define.ContentIconType.Noa, _noaData.IconId);
            imageNoaLock.gameObject.SetActive(imageNoaLock.sprite);
        }
        if (textNoaLock)
            textNoaLock.text = _noaData.DisplayName;

        int currentAmount = Managers.Currency.GetCurrency(Define.CurrencyType.NoaMemory);
        if (textCurrencyLock)
        {
            textCurrencyLock.text = currentAmount >= _noaData.UnlockCost
                ? $"{currentAmount:N0} / {_noaData.UnlockCost:N0}"
                : $"<color=#DE5728>{currentAmount:N0}</color> / {_noaData.UnlockCost:N0}";
        }
        if (buttonUnlock)
        {
            buttonUnlock.Interactable = !_isUnlocking;
            buttonUnlock.IsDisabled = !Managers.Collection.CanUnlock(_noaData);
        }
    }

    private async void OnClickLevelUp()
    {
        if (!_noaData || _isUpgrading || !Managers.Collection.CanUpgrade(_noaData))
            return;

        NoaSO upgradingNoa = _noaData;
        int currentLevel = Managers.Collection.GetLevel(upgradingNoa);
        _isUpgrading = true;
        SetLevelUpState(currentLevel, false);
        Managers.UI.OpenLoading<UI_LoadingTransparent>();
        bool isSuccess = await Managers.Collection.UpgradeAsync(upgradingNoa);
        Managers.UI.CloseLoading();

        if (!this)
            return;

        _isUpgrading = false;
        if (isSuccess)
        {
            Managers.Sound.Play(Define.AudioClip.LevelUp, Define.AudioSourceType.Sfx, Define.AudioPath.Home);
            _onChanged?.Invoke(upgradingNoa);
        }

        Refresh();
    }

    private async void OnClickUnlock()
    {
        if (!_noaData || _isUnlocking)
            return;

        if (!Managers.Collection.IsPreviousTierUnlocked(_noaData))
        {
            Managers.UI.ShowToast("이전 단계 노아를 먼저 해금해 주세요.");
            return;
        }
        if (Managers.Currency.GetCurrency(Define.CurrencyType.NoaMemory) < _noaData.UnlockCost)
        {
            Managers.UI.ShowToast("노아의 기억 조각이 부족합니다.");
            return;
        }
        if (!Managers.Collection.CanUnlock(_noaData))
            return;

        NoaSO unlockingNoa = _noaData;
        _isUnlocking = true;
        SetLockedInfo();
        Managers.UI.OpenLoading<UI_LoadingTransparent>();
        bool isSuccess = await Managers.Collection.UnlockNoaAsync(unlockingNoa);
        Managers.UI.CloseLoading();

        if (!this)
            return;

        _isUnlocking = false;
        if (!isSuccess)
        {
            SetLockedInfo();
            Managers.UI.ShowToast("노아 해금에 실패했습니다.");
            return;
        }

        Managers.Sound.Play(Define.AudioClip.LevelUp, Define.AudioSourceType.Sfx, Define.AudioPath.Home);
        _onChanged?.Invoke(unlockingNoa);
        Managers.UI.ClosePopup();
    }

    private void OnCurrencyChanged(Define.CurrencyType currencyType, int amount)
    {
        if (!_noaData)
            return;

        if (currencyType == Define.CurrencyType.Seed || currencyType == Define.CurrencyType.NoaMemory)
            Refresh();
    }

}
