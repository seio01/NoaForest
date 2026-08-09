using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UI_PurifyResultPopup : PopupBase
{
    [SerializeField] private TextBase textFlow;
    [SerializeField] private TextBase textSlider;
    [SerializeField] private Image imageRay;
    [SerializeField] private Image imageBest;
    [SerializeField] private Image imageFill;
    [SerializeField] private CanvasGroup canvasGroupBest;
    [SerializeField] private RectTransform rectRewardRoot;
    [SerializeField] private UI_Reward rewardPrefab;
    [SerializeField] private ButtonBase buttonClose;
    private const float BEST_APPEAR_DURATION = 0.3f;
    private const float BEST_SETTLE_DURATION = 0.12f;

    private readonly List<UI_Reward> _rewardItems = new();
    private Tween _rayTween;
    private Sequence _bestSequence;

    protected override void OnAwake()
    {
        base.OnAwake();
        if (buttonClose)
        {
            buttonClose.OnClick.RemoveAllListeners();
            buttonClose.OnClick.AddListener(OnClickClose);
        }
    }

    public void SetData(PurifySettlementResponse settlementResult, int maximumForestHp)
    {
        if (settlementResult == null)
        {
            Debug.LogError("[UI_PurifyResultPopup] Settlement result is missing.");
            return;
        }

        SetFlow(GetResultFlow(settlementResult));
        SetForestRecovery(settlementResult.ForestHp, maximumForestHp);
        SetBestRecord(settlementResult.IsNewBest);
        SetRewards(settlementResult.Rewards);
        PlayRayRotation();
    }

    private int GetResultFlow(PurifySettlementResponse settlementResult)
    {
        if (settlementResult.ResultType != PurifyResultType.Fail || settlementResult.ForestHp > 0) return settlementResult.CompletedFlow;

        return Mathf.Min(settlementResult.CompletedFlow + 1, PurifyWaveSetSO.WAVE_COUNT);
    }

    private void SetFlow(int resultFlow)
    {
        if (textFlow)
        {
            textFlow.text = "FLOW\n" + resultFlow.ToString("00");
        }
    }

    private void SetForestRecovery(int forestHp, int maximumForestHp)
    {
        if(imageFill)
            imageFill.fillAmount = maximumForestHp > 0 ? Mathf.Clamp01((float)forestHp / maximumForestHp) : 0f;

        if(textSlider)
            textSlider.text = $"{forestHp}/{maximumForestHp}";
    }

    private void SetBestRecord(bool isNewBest)
    {
        if (!imageBest) return;

        _bestSequence?.Kill();
        imageBest.gameObject.SetActive(isNewBest);

        if (!isNewBest)
            return;
        //신기록일때만 진행

        Transform bestTransform = imageBest.transform;
        if (canvasGroupBest)
        {
            canvasGroupBest.alpha = 0f;
        }

        bestTransform.localScale = Vector3.one * 0.4f;

        _bestSequence = DOTween.Sequence()
            .AppendInterval(0.2f)
            .Append(bestTransform.DOScale(1.15f, BEST_APPEAR_DURATION).SetEase(Ease.OutBack));

        if (canvasGroupBest)
        {
            _bestSequence.Join(canvasGroupBest.DOFade(1f, BEST_APPEAR_DURATION));
        }

        _bestSequence
            .Append(bestTransform.DOScale(Vector3.one, BEST_SETTLE_DURATION).SetEase(Ease.OutSine))
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    private void SetRewards(List<PurifyRewardResponse> rewards)
    {
        ClearRewards();

        foreach (var reward in rewards)
        {
            var rewardItem = Instantiate(rewardPrefab, rectRewardRoot, false);
            rewardItem.SetData(Managers.ContentIcon.GetCurrencyIcon(reward.RewardType), reward.Amount);
            _rewardItems.Add(rewardItem);
        }
    }

    private void ClearRewards()
    {
        foreach (var rewardItem in _rewardItems)
        {
            if (rewardItem)
            {
                Destroy(rewardItem.gameObject);
            }
        }

        _rewardItems.Clear();
    }

    private void PlayRayRotation()
    {
        if (!imageRay)
        {
            return;
        }

        _rayTween?.Kill();
        imageRay.rectTransform.localRotation = Quaternion.identity;
        _rayTween = imageRay.rectTransform
            .DORotate(new Vector3(0f, 0f, -360f), 10f, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    private void OnClickClose()
    {
        Managers.Scene.LoadScene(Define.Scene.Home);
    }
}
