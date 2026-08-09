using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UI_RewardFlyIcon : MonoBehaviour, IPoolable
{
    [Header("UI")]
    [SerializeField] private Image imageIcon;

    [Header("Animation")]
    [SerializeField] private float animationScatterDuration = 0.22f;
    [SerializeField] private float animationHoldDuration = 0.1f;
    [SerializeField] private float animationFlyDuration = 0.52f;
    [SerializeField, Range(0f, 1f)] private float animationShrinkStartProgress = 0.8f;
    [SerializeField] private float animationDestinationScale = 0.35f;

    private RectTransform _rectTransform;
    private Sequence _sequence;
    private Action<UI_RewardFlyIcon> _releaseCallback;

    public RectTransform RectTransform
    {
        get
        {
            if (!_rectTransform) _rectTransform = transform as RectTransform;
            return _rectTransform;
        }
    }

    public void Play(Sprite spriteIcon, Vector2 destinationPosition, Vector2 scatterOffset, Action arrivedCallback, Action<UI_RewardFlyIcon> releaseCallback)
    {
        StopAnimation();
        SetIcon(spriteIcon);
        _releaseCallback = releaseCallback;

        RectTransform rectTransform = RectTransform;
        if (!rectTransform || !imageIcon)
        {
            Complete();
            return;
        }

        Vector2 scatterPosition = rectTransform.anchoredPosition + scatterOffset;
        float flyDistance = Vector2.Distance(scatterPosition, destinationPosition);
        rectTransform.localScale = Vector3.zero;
        SetIconAlpha(0f);

        Tween flyTween = rectTransform
            .DOAnchorPos(destinationPosition, animationFlyDuration)
            .SetEase(Ease.InCubic)
            .OnUpdate(() => UpdateFlyScale(rectTransform, destinationPosition, flyDistance));

        _sequence = DOTween.Sequence()
            .AppendCallback(() => SetIconAlpha(1f))
            .Append(rectTransform.DOAnchorPos(scatterPosition, animationScatterDuration).SetEase(Ease.OutCubic))
            .Join(rectTransform.DOScale(Vector3.one, animationScatterDuration).SetEase(Ease.OutBack))
            .AppendInterval(animationHoldDuration)
            .Append(flyTween)
            .AppendCallback(() =>
            {
                rectTransform.localScale = Vector3.one * animationDestinationScale;
                arrivedCallback?.Invoke();
            })
            .SetUpdate(true)
            .OnComplete(Complete);
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

    private void SetIcon(Sprite spriteIcon)
    {
        if (!imageIcon) return;

        imageIcon.sprite = spriteIcon;
        imageIcon.enabled = spriteIcon;
    }

    private void Complete()
    {
        Action<UI_RewardFlyIcon> releaseCallback = _releaseCallback;
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

    private void SetIconAlpha(float alpha)
    {
        if (!imageIcon) return;

        Color color = imageIcon.color;
        color.a = alpha;
        imageIcon.color = color;
    }

    private void UpdateFlyScale(RectTransform rectTransform, Vector2 destinationPosition, float flyDistance)
    {
        float travelProgress = flyDistance > 0f
            ? 1f - Vector2.Distance(rectTransform.anchoredPosition, destinationPosition) / flyDistance
            : 1f;
        float shrinkProgress = Mathf.InverseLerp(animationShrinkStartProgress, 1f, travelProgress);
        rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, animationDestinationScale, shrinkProgress);
    }

    private void ResetVisuals()
    {
        if (RectTransform) RectTransform.localScale = Vector3.one;
        if (!imageIcon) return;

        SetIconAlpha(1f);
        imageIcon.sprite = null;
        imageIcon.enabled = false;
    }

    private void OnDisable()
    {
        StopAnimation();
    }
}
