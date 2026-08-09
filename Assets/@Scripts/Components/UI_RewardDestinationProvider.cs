using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class UI_RewardDestinationProvider : MonoBehaviour
{
    [Serializable]
    private class RewardDestinationBinding
    {
        [SerializeField] private Define.CurrencyType currencyType;
        [SerializeField] private RectTransform rectTarget;

        public Define.CurrencyType CurrencyType => currencyType;
        public RectTransform RectTarget => rectTarget;
    }

    [Header("Dependencies")]
    [SerializeField] private UI_RewardFlyIcon rewardFlyIconPrefab;
    [SerializeField] private UI_CurrencyBar currencyBar;
    [SerializeField] private List<RewardDestinationBinding> destinationBindings;

    [Header("Presentation")]
    [SerializeField] private Vector2 effectStartPosition = new Vector2(0f, -80f);
    [SerializeField] private float scatterRadius = 90f;
    [SerializeField] private float rewardGroupInterval = 0.18f;
    [SerializeField] private float batchCompleteDelay = 1.15f;
    [SerializeField] private int maximumPooledIconCount = 24;

    private readonly Dictionary<Define.CurrencyType, RectTransform> _destinationByType = new();
    private RectTransform _rectEffectRoot;
    private Canvas _canvas;
    private Pool _pool;
    private Coroutine _playRoutine;

    private void Awake()
    {
        _rectEffectRoot = transform as RectTransform;
        _canvas = GetComponentInParent<Canvas>();

        if (rewardFlyIconPrefab && _rectEffectRoot && maximumPooledIconCount > 0)
            _pool = new Pool(rewardFlyIconPrefab.gameObject, maximumPooledIconCount, _rectEffectRoot);
    }

    private void OnEnable()
    {
        Managers.Reward.PendingRewardAdded += HandlePendingRewardAdded;
        StartPlayRoutine();
    }

    private void OnDisable()
    {
        Managers.Reward.PendingRewardAdded -= HandlePendingRewardAdded;
        if (_playRoutine != null) StopCoroutine(_playRoutine);
        _playRoutine = null;
    }

    private void BuildRegistry()
    {
        _destinationByType.Clear();

        if (currencyBar)
        {
            Array currencyTypes = Enum.GetValues(typeof(Define.CurrencyType));
            foreach (Define.CurrencyType currencyType in currencyTypes)
            {
                RectTransform rectTarget = currencyBar.GetCurrencyTarget(currencyType);
                if (rectTarget) _destinationByType[currencyType] = rectTarget;
            }
        }

        if (destinationBindings == null) return;

        foreach (RewardDestinationBinding binding in destinationBindings)
        {
            if (binding != null && binding.RectTarget)
                _destinationByType[binding.CurrencyType] = binding.RectTarget;
        }
    }

    private void HandlePendingRewardAdded(Define.Scene destinationScene)
    {
        if (Managers.Scene.CurrentScene == destinationScene) StartPlayRoutine();
    }

    private void StartPlayRoutine()
    {
        if (_playRoutine == null && isActiveAndEnabled) _playRoutine = StartCoroutine(PlayPendingRoutine());
    }

    private IEnumerator PlayPendingRoutine()
    {
        yield return null;
        BuildRegistry();
        Canvas.ForceUpdateCanvases();

        Define.Scene? currentScene = Managers.Scene.CurrentScene;
        if (currentScene == null)
        {
            _playRoutine = null;
            yield break;
        }

        while (true)
        {
            List<RewardPresentationBatch> batches = Managers.Reward.TakePending(currentScene.Value);
            if (batches.Count == 0) break;

            foreach (var batch in batches)
            {
                foreach (var item in batch.Items)
                {
                    PlayReward(item);
                    yield return new WaitForSecondsRealtime(rewardGroupInterval);
                }

                yield return new WaitForSecondsRealtime(batchCompleteDelay);
            }
        }

        _playRoutine = null;
    }

    private void PlayReward(RewardPresentationItem item)
    {
        if (!_destinationByType.TryGetValue(item.CurrencyType, out RectTransform rectTarget) || !rectTarget)
        {
            Debug.LogWarning($"[UI_RewardDestinationProvider] Reward destination is missing: {item.CurrencyType}");
            return;
        }

        Sprite spriteIcon = Managers.ContentIcon.GetCurrencyIcon(item.CurrencyType);
        if (!spriteIcon)
        {
            Debug.LogWarning($"[UI_RewardDestinationProvider] Reward icon is missing: {item.CurrencyType}");
            return;
        }

        Vector2? destinationPosition = GetTargetLocalPosition(rectTarget);
        if (destinationPosition == null) return;

        Managers.Sound.Play(Define.AudioClip.RewardGet, Define.AudioSourceType.Sfx, Define.AudioPath.Common);
        int iconCount = Mathf.Clamp(item.Amount, 1, 5);
        for (int index = 0; index < iconCount; index++)
        {
            UI_RewardFlyIcon rewardFlyIcon = GetRewardIcon();
            if (!rewardFlyIcon) break;

            bool playArrivalPunch = index == iconCount - 1;
            Vector2 scatterOffset = UnityEngine.Random.insideUnitCircle * scatterRadius;
            rewardFlyIcon.Play(
                spriteIcon,
                destinationPosition.Value,
                scatterOffset,
                () =>
                {
                    if (playArrivalPunch && rectTarget) rectTarget.DOPunchScale(Vector3.one * 0.08f, 0.2f, 1, 0.3f).SetUpdate(true).SetLink(rectTarget.gameObject);
                },
                ReleaseRewardIcon);
        }
    }

    private UI_RewardFlyIcon GetRewardIcon()
    {
        if (_pool == null || !_rectEffectRoot) return null;

        GameObject instance = _pool.Get();
        if (!instance || !instance.TryGetComponent(out UI_RewardFlyIcon rewardFlyIcon)) return null;

        RectTransform rectTransform = rewardFlyIcon.RectTransform;
        rectTransform.SetParent(_rectEffectRoot, false);
        rectTransform.SetAsLastSibling();
        rectTransform.anchoredPosition = effectStartPosition;
        return rewardFlyIcon;
    }

    private void ReleaseRewardIcon(UI_RewardFlyIcon rewardIcon)
    {
        if (rewardIcon && _pool != null) _pool.Release(rewardIcon.gameObject);
    }

    private Vector2? GetTargetLocalPosition(RectTransform rectTarget)
    {
        if (!rectTarget || !_rectEffectRoot || !_canvas) return null;

        Camera canvasCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        Vector3 targetCenter = rectTarget.TransformPoint(rectTarget.rect.center);
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(canvasCamera, targetCenter);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectEffectRoot, screenPosition, canvasCamera, out Vector2 localPosition)
            ? localPosition
            : null;
    }
}
