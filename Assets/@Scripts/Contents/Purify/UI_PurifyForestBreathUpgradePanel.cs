using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UI_PurifyForestBreathUpgradePanel : UI_Base
{
    [SerializeField] private RectTransform panelObject;

    [SerializeField] private TextBase textLevel;
    [SerializeField] private TextBase textPowerBefore;
    [SerializeField] private GameObject arrow;
    [SerializeField] private TextBase textPowerAfter;
    [SerializeField] private GameObject frameCost;
    [SerializeField] private GameObject frameBottom;
    [SerializeField] private TextBase textCost;
    [SerializeField] private ButtonBase buttonClose;
    [SerializeField] private ButtonBase buttonUpgrade;

    private const float SLIDE_DISTANCE = 120f;

    private PurifyForestBreathController _forestBreathController;
    private Sequence _transitionSequence;
    private Vector2 _shownAnchoredPosition;
    private bool _isShown;

    private void Awake()
    {
        _shownAnchoredPosition = panelObject.anchoredPosition;
        if(buttonClose)
            buttonClose.OnClick.AddListener(OnClickCloseButton);
        if(buttonUpgrade)
            buttonUpgrade.OnClick.AddListener(OnClickUpgradeButton);
    }

    public void SetData(PurifyForestBreathController forestBreathController)
    {
        _forestBreathController = forestBreathController;
        Refresh();
    }

    public void SetVisible(bool isVisible)
    {
        if (_isShown == isVisible) return;

        bool wasActive = gameObject.activeSelf;
        _isShown = isVisible;
        _transitionSequence?.Kill();

        if (isVisible)
        {
            gameObject.SetActive(true);
            Refresh();
        }

        var canvasGroup = panelObject.GetComponent<CanvasGroup>();
        if (isVisible && !wasActive)
        {
            panelObject.anchoredPosition = _shownAnchoredPosition - Vector2.up * SLIDE_DISTANCE;
            canvasGroup.alpha = 0f;
        }

        Vector2 targetPosition = isVisible ? _shownAnchoredPosition : _shownAnchoredPosition - Vector2.up * SLIDE_DISTANCE;
        float targetAlpha = isVisible ? 1f : 0f;
        Ease transitionEase = isVisible ? Ease.OutCubic : Ease.InCubic;

        _transitionSequence = DOTween.Sequence()
            .Join(panelObject.DOAnchorPos(targetPosition, 0.25f).SetEase(transitionEase))
            .Join(canvasGroup.DOFade(targetAlpha, 0.25f))
            .OnComplete(() =>
            {
                if (!isVisible)
                {
                    panelObject.anchoredPosition = _shownAnchoredPosition;
                    canvasGroup.alpha = 1f;
                    gameObject.SetActive(false);
                }

                _transitionSequence = null;
            });
    }


    public void Refresh()
    {
        bool isMaximumLevel = _forestBreathController.IsMaximumLevel;
        int currentLevel = _forestBreathController.CurrentLevel;
        
        if(textLevel)
            textLevel.text = $"LV.{currentLevel}";
        if(textPowerBefore)
            textPowerBefore.text = $"현재 피해\n{_forestBreathController.CurrentPower}";
        if(textPowerAfter)
        {
            textPowerAfter.gameObject.SetActive(!isMaximumLevel);
            if (!isMaximumLevel)
                textPowerAfter.text = $"다음 피해\n{_forestBreathController.NextPower}";
        }
        if(arrow)
            arrow.SetActive(!isMaximumLevel);
        if(frameCost)
            frameCost.SetActive(!isMaximumLevel);
        if(frameBottom)
        {
            frameBottom.gameObject.SetActive(!isMaximumLevel);
        }

        if(textCost && !isMaximumLevel)
            textCost.text = _forestBreathController.UpgradeCost.ToString();

        LayoutRebuilder.ForceRebuildLayoutImmediate(arrow.transform.parent as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(frameBottom.transform.parent as RectTransform);
    }

    private void OnClickUpgradeButton()
    {
        if (!_forestBreathController.IsMaximumLevel && !_forestBreathController.HasEnoughUpgradeEnergy)
        {
            Managers.UI.ShowToast("정화 에너지가 부족합니다.");
            return;
        }

        _forestBreathController.Upgrade();
        Refresh();
    }

    private void OnClickCloseButton()
    {
        SetVisible(false);
    }
}
