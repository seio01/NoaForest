using System;
using UnityEngine;
using UnityEngine.UI;

public class UI_BlessingCollectionInfoPopup : PopupBase
{
    [Header("State")]
    [SerializeField] private GameObject frameSlider;
    [SerializeField] private GameObject frameLocked;
    [SerializeField] private GameObject frameStats;
    [SerializeField] private GameObject frameAcquire;

    [Header("Blessing")]
    [SerializeField] private Image imageBlessing;
    [SerializeField] private Image imageRarity;
    [SerializeField] private Sprite spriteUnacquired;
    [SerializeField] private TextBase textBlessingName;
    [SerializeField] private TextBase textLevel;
    [SerializeField] private TextBase textInfo;
    [SerializeField] private TextBase textStats;

    [Header("Piece Progress")]
    [SerializeField] private Slider sliderPiece;
    [SerializeField] private TextBase textPieceCount;

    [Header("Level Up")]
    [SerializeField] private TextBase textCurrency;
    [SerializeField] private ButtonBase buttonLevelUp;
    [SerializeField] private ButtonBase buttonMove;

    private BlessingSO _blessingData;
    private Action<BlessingSO> _onUpgraded;
    private bool _isUpgrading;

    protected override void OnAwake()
    {
        base.OnAwake();
        if (buttonLevelUp)
            buttonLevelUp.OnClick.AddListener(OnClickLevelUp);
        if (buttonMove)
            buttonMove.OnClick.AddListener(OnClickMove);

        Managers.Currency.CurrencyChanged += OnCurrencyChanged;
    }

    protected override void Destroy()
    {
        Managers.Currency.CurrencyChanged -= OnCurrencyChanged;
        base.Destroy();
    }

    public void SetData(BlessingSO blessingData, Action<BlessingSO> onUpgraded)
    {
        _blessingData = blessingData;
        _onUpgraded = onUpgraded;
        Refresh();
    }

    private void Refresh()
    {
        if (!_blessingData)
            return;

        bool isOwned = Managers.Collection.IsUnlocked(_blessingData);
        SetOwnershipState(isOwned);
        SetBlessingIdentity(isOwned);

        if (isOwned)
            SetOwnedInfo();
        else
            SetUnacquiredInfo();
    }

    private void SetOwnershipState(bool isOwned)
    {
        if (frameSlider)
            frameSlider.SetActive(isOwned);
        if (frameLocked)
            frameLocked.SetActive(!isOwned);
        if (frameStats)
            frameStats.SetActive(isOwned);
        if (frameAcquire)
            frameAcquire.SetActive(!isOwned);
        if (textInfo)
            textInfo.gameObject.SetActive(isOwned);
        if (buttonMove)
            buttonMove.gameObject.SetActive(!isOwned);
        if (buttonLevelUp)
            buttonLevelUp.gameObject.SetActive(isOwned);
    }

    private void SetBlessingIdentity(bool isOwned)
    {
        if (imageBlessing)
        {
            imageBlessing.sprite = isOwned
                ? Managers.ContentIcon.GetLoadedSprite(Define.ContentIconType.Blessing, _blessingData.Id)
                : spriteUnacquired;
            imageBlessing.gameObject.SetActive(imageBlessing.sprite);
        }

        if (imageRarity)
            imageRarity.sprite = Managers.ContentIcon.GetBlessingRaritySprite(_blessingData.Rarity);
        if (textBlessingName)
            textBlessingName.text = _blessingData.DisplayName;
    }

    private void SetOwnedInfo()
    {
        int currentLevel = Managers.Collection.GetLevel(_blessingData);
        int pieceCount = Managers.Collection.GetBlessingPieceCount(_blessingData);
        int pieceCost = Managers.Collection.GetUpgradePieceCost(_blessingData, currentLevel);
        bool isMaximumLevel = currentLevel >= BlessingSO.MAX_LEVEL;

        if (textLevel)
            textLevel.text = $"<color={Constants.Olive1}>LV.{currentLevel}</color> / {BlessingSO.MAX_LEVEL}";
        if (textInfo)
            textInfo.text = _blessingData.Description;
        if (textStats)
            textStats.text = _blessingData.GetEffectDescription(currentLevel);

        SetPieceProgress(pieceCount, pieceCost, isMaximumLevel);
        SetLevelUpState(currentLevel, isMaximumLevel);
    }

    private void SetPieceProgress(int pieceCount, int pieceCost, bool isMaximumLevel)
    {
        if (isMaximumLevel)
        {
            if (sliderPiece)
            {
                sliderPiece.minValue = 0;
                sliderPiece.maxValue = 1;
                sliderPiece.value = 1;
            }
            if (textPieceCount)
                textPieceCount.text = "MAX";
            return;
        }

        int requiredPieceCount = Mathf.Max(1, pieceCost);
        if (sliderPiece)
        {
            sliderPiece.minValue = 0;
            sliderPiece.maxValue = requiredPieceCount;
            sliderPiece.value = Mathf.Clamp(pieceCount, 0, requiredPieceCount);
        }
        if (textPieceCount)
            textPieceCount.text = $"{pieceCount:N0}/{pieceCost:N0}";
    }

    private void SetLevelUpState(int currentLevel, bool isMaximumLevel)
    {
        int elementCost = Managers.Collection.GetUpgradeCost(_blessingData, currentLevel);
        bool canUpgrade = !_isUpgrading && !isMaximumLevel && Managers.Collection.CanUpgrade(_blessingData);

        if (textCurrency)
        {
            textCurrency.text = isMaximumLevel ? "MAX" : elementCost.ToString("N0");
            textCurrency.SetTextColor(canUpgrade ? Define.TextColorPalette.White : Define.TextColorPalette.Brown1);
        }
        if (buttonLevelUp)
            buttonLevelUp.Interactable = canUpgrade;
    }

    private void SetUnacquiredInfo()
    {
        if (textCurrency)
        {
            textCurrency.text = "-";
            textCurrency.SetTextColor(Define.TextColorPalette.Brown1);
        }
        if (buttonLevelUp)
            buttonLevelUp.Interactable = false;
    }

    private async void OnClickLevelUp()
    {
        if (!_blessingData || _isUpgrading || !Managers.Collection.CanUpgrade(_blessingData))
            return;

        BlessingSO upgradingBlessing = _blessingData;
        int currentLevel = Managers.Collection.GetLevel(upgradingBlessing);
        _isUpgrading = true;
        SetLevelUpState(currentLevel, false);
        Managers.UI.OpenLoading<UI_LoadingTransparent>();
        bool isSuccess = await Managers.Collection.UpgradeAsync(upgradingBlessing);
        Managers.UI.CloseLoading();

        if (!this)
            return;

        _isUpgrading = false;
        if (isSuccess)
        {
            Managers.Sound.Play(Define.AudioClip.LevelUp, Define.AudioSourceType.Sfx, Define.AudioPath.Home);
            _onUpgraded?.Invoke(upgradingBlessing);
        }

        Refresh();
    }

    private void OnClickMove()
    {
        if (!_blessingData || Managers.Collection.IsUnlocked(_blessingData))
            return;

        Managers.UI.CloseAllPopup();
        Managers.UI.OpenPopup<UI_BlessingSummonPopup>("UI_BlessingSummonPopup");
    }

    private void OnCurrencyChanged(Define.CurrencyType currencyType, int amount)
    {
        if (currencyType == Define.CurrencyType.ElementCore && _blessingData)
            Refresh();
    }
}
