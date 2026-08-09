using DG.Tweening;
using UnityEngine;

public class UI_Toast : UI_Base
{
    [SerializeField] private TextBase textToast;
    [SerializeField] private RectTransform rectTransformToast;
    [SerializeField] private CanvasGroup canvasGroupToast;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float holdDuration = 1.6f;
    [SerializeField] private float fadeOutDuration = 0.18f;

    [Header("Motion")]
    [SerializeField] private float enterOffsetY = -28f;
    [SerializeField] private float exitOffsetY = 12f;
    [SerializeField] private float appearFromScale = 0.98f;

    private Sequence _sequence;

    public void Show(string text, Define.ToastPosition position)
    {
        if (!rectTransformToast || !canvasGroupToast || !textToast) return;

        KillTween();

        textToast.text = text;
        ApplyPosition(position);

        Vector2 restingPosition = rectTransformToast.anchoredPosition;
        Vector2 enterPosition = restingPosition + Vector2.up * enterOffsetY;
        Vector2 exitPosition = restingPosition + Vector2.up * exitOffsetY;

        canvasGroupToast.alpha = 0f;
        rectTransformToast.anchoredPosition = enterPosition;
        rectTransformToast.localScale = Vector3.one * appearFromScale;

        _sequence = DOTween.Sequence();
        _sequence.Append(canvasGroupToast.DOFade(1f, fadeInDuration).SetEase(Ease.OutCubic));
        _sequence.Join(rectTransformToast.DOAnchorPos(restingPosition, fadeInDuration).SetEase(Ease.OutCubic));
        _sequence.Join(rectTransformToast.DOScale(1f, fadeInDuration).SetEase(Ease.OutCubic));
        _sequence.AppendInterval(holdDuration);
        _sequence.Append(canvasGroupToast.DOFade(0f, fadeOutDuration).SetEase(Ease.InCubic));
        _sequence.Join(rectTransformToast.DOAnchorPos(exitPosition, fadeOutDuration).SetEase(Ease.InCubic));
        _sequence.SetUpdate(true);
        _sequence.OnComplete(() => Destroy(gameObject));
    }

    private void ApplyPosition(Define.ToastPosition position)
    {
        if (position != Define.ToastPosition.Middle) return;

        Vector2 center = Vector2.one * 0.5f;
        rectTransformToast.anchorMin = center;
        rectTransformToast.anchorMax = center;
        rectTransformToast.pivot = center;
        rectTransformToast.anchoredPosition = Vector2.zero;
    }

    private void OnDestroy()
    {
        KillTween();
    }

    private void KillTween()
    {
        if (_sequence != null && _sequence.IsActive()) _sequence.Kill();

        _sequence = null;

        if (rectTransformToast) rectTransformToast.DOKill();
        if (canvasGroupToast) canvasGroupToast.DOKill();
    }
}
