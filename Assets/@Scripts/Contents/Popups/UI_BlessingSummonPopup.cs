using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UI_BlessingSummonPopup : PopupBase
{
    [Header("HUD")]
    [SerializeField] private CanvasGroup canvasGroupBackground;
    [SerializeField] private RectTransform rectTopHud;
    [SerializeField] private RectTransform rectBottomHud;

    [Header("Summon")]
    [SerializeField] private TextBase textBlessingTicket;
    [SerializeField] private ButtonBase buttonSummon;
    [SerializeField] private Image imageDim;
    [SerializeField] private Image imageRock;
    [SerializeField] private Image imageGlow;
    [SerializeField] private Image imageResult;
    [SerializeField] private Animator animatorRock;
    [SerializeField] private Animator animatorGlow;

    private const float OPEN_FADE_DURATION = 0.35f;
    private const float OPEN_HUD_DELAY = 0.1f;
    private const float OPEN_HUD_DURATION = 0.55f;
    private const float HUD_MOVE_DURATION = 0.35f;
    private const float SUMMON_BUILDUP_DURATION = 1.2f;
    private const float RESULT_REVEAL_DURATION = 0.5f;
    private const float RESULT_START_OFFSET_Y = 80f;

    private Vector2 _topHudOrigin;
    private Vector2 _bottomHudOrigin;
    private Vector2 _resultOrigin;
    private Sprite _rockSpriteOrigin;
    private Sprite _glowSpriteOrigin;
    
    private Sequence _openSequence;
    private Sequence _summonSequence;
    private Sequence _resultSequence;
    private Coroutine _summonCoroutine;
    private BlessingSO _resultData;
    private bool _isSummoning;

    protected override void OnAwake()
    {
        base.OnAwake();

        Managers.Sound.Play(Define.AudioClip.BlessingSummonBGM, Define.AudioSourceType.Bgm);
        CacheAnimationOrigins();
        ResetAnimationImmediately();
        PlayOpenAnimation();

        if (buttonSummon)
            buttonSummon.OnClick.AddListener(OnClickSummon);

        Managers.Currency.CurrencyChanged += OnCurrencyChanged;
        SetBlessingTicketCount(Managers.Currency.GetCurrency(Define.CurrencyType.BlessingTicket));
        RefreshSummonButton();
    }

    protected override void Destroy()
    {
        Managers.Sound.Play(Define.AudioClip.HomeBGM, Define.AudioSourceType.Bgm);
        
        if (_summonCoroutine != null)
            StopCoroutine(_summonCoroutine);

        _openSequence?.Kill();
        _summonSequence?.Kill();
        _resultSequence?.Kill();
        Managers.Currency.CurrencyChanged -= OnCurrencyChanged;
        base.Destroy();
    }

    private void SetBlessingTicketCount(int amount)
    {
        if (textBlessingTicket)
            textBlessingTicket.text = amount.ToString("N0");
    }

    private void OnCurrencyChanged(Define.CurrencyType currencyType, int amount)
    {
        if (currencyType == Define.CurrencyType.BlessingTicket)
        {
            SetBlessingTicketCount(amount);
            RefreshSummonButton();
        }
    }

    private async void OnClickSummon()
    {
        if (_isSummoning || Managers.Collection.IsSummonRequestPending)
            return;

        if (!Managers.Collection.HasSummonableBlessing())
        {
            Managers.UI.ShowToast("모든 정화 가호가 최대 레벨입니다.");
            return;
        }
        if (Managers.Currency.GetCurrency(Define.CurrencyType.BlessingTicket) < 1)
        {
            Managers.UI.ShowToast("가호 소환 티켓이 부족합니다.");
            return;
        }
        if (!Managers.Collection.CanSummonBlessing())
            return;

        if (buttonSummon)
            buttonSummon.Interactable = false;

        Managers.UI.OpenLoading<UI_LoadingTransparent>();
        BlessingSummonResponse response = await Managers.Collection.SummonBlessingAsync();
        Managers.UI.CloseLoading();
        RefreshSummonButton();

        if (response == null)
        {
            Managers.UI.ShowToast("가호 소환에 실패했습니다.");
            RefreshSummonButton();
            return;
        }

        BlessingSO resultData = Managers.GameData.Blessings?.GetBlessing(response.ItemId);
        if (!resultData)
        {
            Debug.LogError($"[UI_BlessingSummonPopup] Summon result data is missing: {response.ItemId}");
            Managers.UI.ShowToast("소환 결과 가호 데이터를 찾을 수 없습니다.");
            RefreshSummonButton();
            return;
        }

        Debug.Log($"[UI_BlessingSummonPopup] Summon result: {response.ItemId}, IsNew: {response.IsNew}, AcquiredPieceCount: {response.AcquiredPieceCount}");
        if (response.IsNew)
            Managers.Sound.Play(Define.AudioClip.LevelUp, Define.AudioSourceType.Sfx, Define.AudioPath.Home);
        PlaySummonAnimation(resultData);
    }

    private void PlaySummonAnimation(BlessingSO blessingData)
    {
        if (!blessingData || _isSummoning)
            return;

        _resultData = blessingData;
        _isSummoning = true;
        _openSequence?.Complete();
        RefreshSummonButton();

        PrepareSummonVisuals();
        PlayBuildUpAnimation();
        _summonCoroutine = StartCoroutine(PlayResultRoutine());
    }

    private void CacheAnimationOrigins()
    {
        _topHudOrigin = rectTopHud ? rectTopHud.anchoredPosition : Vector2.zero;
        _bottomHudOrigin = rectBottomHud ? rectBottomHud.anchoredPosition : Vector2.zero;
        _resultOrigin = imageResult ? imageResult.rectTransform.anchoredPosition : Vector2.zero;
        _rockSpriteOrigin = imageRock ? imageRock.sprite : null;
        _glowSpriteOrigin = imageGlow ? imageGlow.sprite : null;
    }

    private void ResetAnimationImmediately()
    {
        if (canvasGroupBackground)
            canvasGroupBackground.alpha = 1f;
        if (rectTopHud)
            rectTopHud.anchoredPosition = _topHudOrigin;
        if (rectBottomHud)
            rectBottomHud.anchoredPosition = _bottomHudOrigin;
        if (imageDim)
            imageDim.color = SetAlpha(imageDim.color, 0f);
        if (imageRock)
        {
            imageRock.sprite = _rockSpriteOrigin;
            imageRock.rectTransform.localRotation = Quaternion.identity;
        }
        if (imageGlow)
        {
            imageGlow.sprite = _glowSpriteOrigin;
            imageGlow.color = Color.white;
            imageGlow.gameObject.SetActive(false);
        }
        if (imageResult)
        {
            imageResult.rectTransform.anchoredPosition = _resultOrigin;
            imageResult.rectTransform.localScale = Vector3.one;
            imageResult.color = Color.white;
            imageResult.gameObject.SetActive(false);
        }
        if (animatorRock)
            animatorRock.enabled = false;
        if (animatorGlow)
            animatorGlow.enabled = false;
    }

    private void PlayOpenAnimation()
    {
        _openSequence?.Kill();

        if (canvasGroupBackground)
            canvasGroupBackground.alpha = 0f;
        if (rectTopHud)
            rectTopHud.anchoredPosition = new Vector2(_topHudOrigin.x, _topHudOrigin.y + rectTopHud.rect.height);
        if (rectBottomHud)
        {
            float bottomMoveDistance = rectBottomHud.rect.height + Mathf.Abs(_bottomHudOrigin.y);
            rectBottomHud.anchoredPosition = new Vector2(_bottomHudOrigin.x, _bottomHudOrigin.y - bottomMoveDistance);
        }

        _openSequence = DOTween.Sequence().SetUpdate(true);
        if (canvasGroupBackground)
            _openSequence.Join(canvasGroupBackground.DOFade(1f, OPEN_FADE_DURATION));
        if (rectTopHud)
            _openSequence.Insert(OPEN_HUD_DELAY, rectTopHud.DOAnchorPos(_topHudOrigin, OPEN_HUD_DURATION).SetEase(Ease.OutCubic));
        if (rectBottomHud)
            _openSequence.Insert(OPEN_HUD_DELAY, rectBottomHud.DOAnchorPos(_bottomHudOrigin, OPEN_HUD_DURATION).SetEase(Ease.OutBack));
    }

    private void PrepareSummonVisuals()
    {
        _summonSequence?.Kill();
        _resultSequence?.Kill();

        if (imageResult)
            imageResult.gameObject.SetActive(false);
        if (imageGlow)
        {
            imageGlow.color = Color.white;
            imageGlow.gameObject.SetActive(true);
            Managers.Sound.Play(Define.AudioClip.BlessingSummonGlow, Define.AudioSourceType.Sfx, Define.AudioPath.Home);
        }
        if (animatorRock)
        {
            animatorRock.speed = 1f;
            animatorRock.enabled = true;
            animatorRock.Play(0, 0, 0f);
        }
        if (animatorGlow)
        {
            animatorGlow.speed = 1f;
            animatorGlow.enabled = true;
            animatorGlow.Play(0, 0, 0f);
        }
    }

    private void PlayBuildUpAnimation()
    {
        Managers.Sound.Play(Define.AudioClip.BlessingSummonBuildUp, Define.AudioSourceType.Sfx, Define.AudioPath.Home);
        _summonSequence = DOTween.Sequence().SetUpdate(true);
        if (rectTopHud)
            _summonSequence.Join(rectTopHud.DOAnchorPosY(_topHudOrigin.y + rectTopHud.rect.height, HUD_MOVE_DURATION).SetEase(Ease.InCubic));
        if (rectBottomHud)
        {
            float bottomMoveDistance = rectBottomHud.rect.height + Mathf.Abs(_bottomHudOrigin.y);
            _summonSequence.Join(rectBottomHud.DOAnchorPosY(_bottomHudOrigin.y - bottomMoveDistance, HUD_MOVE_DURATION).SetEase(Ease.InCubic));
        }
        if (imageDim)
            _summonSequence.Join(imageDim.DOFade(0.65f, HUD_MOVE_DURATION));
        if (imageRock)
            _summonSequence.Join(imageRock.rectTransform.DOPunchRotation(new Vector3(0f, 0f, 2f), SUMMON_BUILDUP_DURATION, 8, 0.5f));
    }

    private IEnumerator PlayResultRoutine()
    {
        yield return new WaitForSecondsRealtime(SUMMON_BUILDUP_DURATION);
        if (!this || !_resultData)
            yield break;

        PlayResultAnimation();
        yield return new WaitForSecondsRealtime(RESULT_REVEAL_DURATION + 0.5f);
        if (this && _resultData)
            OpenSimpleInfoPopup();

        _summonCoroutine = null;
    }

    private void PlayResultAnimation()
    {
        if (animatorRock)
            animatorRock.enabled = false;

        Sprite resultSprite = Managers.ContentIcon.GetLoadedSprite(Define.ContentIconType.Blessing, _resultData.Id);
        if (imageResult)
        {
            imageResult.sprite = resultSprite;
            imageResult.rectTransform.anchoredPosition = _resultOrigin + Vector2.down * RESULT_START_OFFSET_Y;
            imageResult.rectTransform.localScale = Vector3.one * 0.6f;
            imageResult.color = SetAlpha(Color.white, 0f);
            imageResult.gameObject.SetActive(resultSprite);
        }

        _resultSequence = DOTween.Sequence().SetUpdate(true);
        if (imageGlow)
        {
            _resultSequence.Join(imageGlow.DOFade(0f, RESULT_REVEAL_DURATION * 0.7f).OnComplete(() =>
            {
                if (animatorGlow)
                    animatorGlow.enabled = false;
                imageGlow.gameObject.SetActive(false);
            }));
        }
        if (imageResult)
        {
            _resultSequence.Join(imageResult.DOFade(1f, RESULT_REVEAL_DURATION * 0.5f));
            _resultSequence.Join(imageResult.rectTransform.DOAnchorPos(_resultOrigin, RESULT_REVEAL_DURATION).SetEase(Ease.OutCubic));
            _resultSequence.Join(imageResult.rectTransform.DOScale(Vector3.one, RESULT_REVEAL_DURATION).SetEase(Ease.OutBack));
        }
    }

    private void OpenSimpleInfoPopup()
    {
        Managers.Sound.Play(Define.AudioClip.BlessingReveal, Define.AudioSourceType.Sfx, Define.AudioPath.Home);
        BlessingSO resultData = _resultData;
        Managers.UI.OpenPopup<UI_BlessingSimpleInfoPopup>("UI_BlessingSimpleInfoPopup", popup =>
        {
            popup.SetData(resultData);
            popup.OnAfterClose += ResetSummonView;
        });
    }

    private void ResetSummonView()
    {
        if (!this)
            return;

        _summonSequence?.Kill();
        _resultSequence?.Kill();
        _resultSequence = DOTween.Sequence().SetUpdate(true);
        if (rectTopHud)
            _resultSequence.Join(rectTopHud.DOAnchorPos(_topHudOrigin, HUD_MOVE_DURATION).SetEase(Ease.OutCubic));
        if (rectBottomHud)
            _resultSequence.Join(rectBottomHud.DOAnchorPos(_bottomHudOrigin, HUD_MOVE_DURATION).SetEase(Ease.OutCubic));
        if (imageDim)
            _resultSequence.Join(imageDim.DOFade(0f, HUD_MOVE_DURATION));
        if (imageResult)
            _resultSequence.Join(imageResult.DOFade(0f, HUD_MOVE_DURATION * 0.6f));

        _resultSequence.OnComplete(() =>
        {
            ResetAnimationImmediately();
            _resultData = null;
            _isSummoning = false;
            RefreshSummonButton();
        });
    }

    private void RefreshSummonButton()
    {
        if (!buttonSummon)
            return;

        buttonSummon.Interactable = !Managers.Collection.IsSummonRequestPending;
        buttonSummon.IsDisabled = _isSummoning || !Managers.Collection.CanSummonBlessing();
    }

    private Color SetAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
