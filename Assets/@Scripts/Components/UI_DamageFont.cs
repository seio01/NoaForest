using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UI_DamageFont : MonoBehaviour, IPoolable
{
    [Header("UI")]
    [SerializeField] private Image imageIcon;
    [SerializeField] private TextBase textValue;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation")]
    [SerializeField] private float animationRiseDistance = 30f;
    [SerializeField] private float animationDuration = 0.7f;
    [SerializeField] private float animationFadeDelay = 0.3f;
    [SerializeField] private float animationStartScale = 0.85f;

    private RectTransform _rectTransform;
    private Sequence _sequence;
    private Action<UI_DamageFont> _releaseCallback;

    public RectTransform RectTransform
    {
        get
        {
            if (!_rectTransform) _rectTransform = transform as RectTransform;
            return _rectTransform;
        }
    }

    public void Play(string value, Sprite spriteIcon, Action<UI_DamageFont> releaseCallback)
    {
        StopAnimation();
        SetContent(value, spriteIcon);
        _releaseCallback = releaseCallback;

        RectTransform rectTransform = RectTransform;
        if (!rectTransform || !canvasGroup)
        {
            Complete();
            return;
        }

        Vector2 startPosition = rectTransform.anchoredPosition;
        rectTransform.localScale = Vector3.one * animationStartScale;
        canvasGroup.alpha = 1f;

        float duration = Mathf.Max(0.01f, animationDuration);
        float fadeDelay = Mathf.Clamp(animationFadeDelay, 0f, duration);
        _sequence = DOTween.Sequence()
            .Join(rectTransform.DOAnchorPosY(startPosition.y + animationRiseDistance, duration).SetEase(Ease.OutCubic))
            .Join(rectTransform.DOScale(Vector3.one, duration * 0.25f).SetEase(Ease.OutBack))
            .Insert(fadeDelay, canvasGroup.DOFade(0f, duration - fadeDelay).SetEase(Ease.InQuad))
            .SetUpdate(true)
            .OnComplete(Complete);
    }

    public void PlayCurrency(string value, Define.CurrencyType currencyType, Action<UI_DamageFont> releaseCallback)
    {
        Play(value, Managers.ContentIcon.GetCurrencyIcon(currencyType), releaseCallback);
    }

    public void OnGet()
    {
        ResetVisuals();
    }

    public void OnRelease()
    {
        StopAnimation();
        ResetVisuals();
    }

    private void SetContent(string value, Sprite spriteIcon)
    {
        if (textValue) textValue.text = value;
        if (!imageIcon) return;

        imageIcon.sprite = spriteIcon;
        imageIcon.gameObject.SetActive(spriteIcon);
    }

    private void Complete()
    {
        Action<UI_DamageFont> releaseCallback = _releaseCallback;
        _releaseCallback = null;
        _sequence = null;
        releaseCallback?.Invoke(this);
    }

    private void StopAnimation()
    {
        _sequence?.Kill();
        _sequence = null;
        _releaseCallback = null;
    }

    private void ResetVisuals()
    {
        if (RectTransform) RectTransform.localScale = Vector3.one;
        if (canvasGroup) canvasGroup.alpha = 1f;
        if (textValue) textValue.text = string.Empty;
        if (!imageIcon) return;

        imageIcon.sprite = null;
        imageIcon.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        StopAnimation();
    }
}
