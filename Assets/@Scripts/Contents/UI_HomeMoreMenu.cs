using DG.Tweening;
using UnityEngine;

public class UI_HomeMoreMenu : UI_Base
{
    private const float CLOSED_SCALE = 0.9f;

    [SerializeField] private ButtonBase buttonSetting;
    [SerializeField] private ButtonBase buttonRanking;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private float durationAnimation = 0.18f;

    private Sequence _animationSequence;
    private bool _isOpen;

    private void Awake()
    {
        if(buttonSetting)
            buttonSetting.OnClick.AddListener(OnClickSetting);
        if(buttonRanking)
            buttonRanking.OnClick.AddListener(OnClickRanking);
    }

    public void Toggle()
    {
        if (_isOpen)
        {
            Hide();
            return;
        }

        Show();
    }

    public void Show()
    {
        if (!gameObject.activeSelf)
        {
            canvasGroup.alpha = 0f;
            rectTransform.localScale = Vector3.one * CLOSED_SCALE;
            gameObject.SetActive(true);
        }

        _isOpen = true;
        PlayAnimation(1f, Vector3.one, false);
    }

    public void Hide()
    {
        _isOpen = false;
        PlayAnimation(0f, Vector3.one * CLOSED_SCALE, true);
    }

    private void PlayAnimation(float targetAlpha, Vector3 targetScale, bool deactivateOnComplete)
    {
        _animationSequence?.Kill();

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        _animationSequence = DOTween.Sequence()
            .SetUpdate(true)
            .Join(canvasGroup
                .DOFade(targetAlpha, durationAnimation)
                .SetEase(Ease.OutCubic))
            .Join(rectTransform
                .DOScale(targetScale, durationAnimation)
                .SetEase(Ease.OutCubic))
            .OnComplete(() =>
            {
                _animationSequence = null;

                if (deactivateOnComplete)
                {
                    gameObject.SetActive(false);
                    return;
                }

                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            });
    }

    private void OnDisable()
    {
        _animationSequence?.Kill();
        _animationSequence = null;
        _isOpen = false;
    }

    private void OnClickSetting()
    {
        Managers.UI.OpenPopup<UI_SettingPopup>("UI_SettingPopup", (popup) =>
        {
            popup.RefreshView();
        });
    }

    private void OnClickRanking()
    {
        Debug.Log("[UI_HomeMoreMenu] Ranking clicked");
        Managers.UI.ShowToast("준비중 입니다");
    }
}
