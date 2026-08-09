using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UI_PurifyBlessingPanel : UI_Base
{
    [SerializeField] private RectTransform panelObject;
    [SerializeField] private GameObject blessingFrame;
    [SerializeField] private GameObject emptyFrame;
    [SerializeField] private RectTransform blessingRoot;
    [SerializeField] private UI_Blessing blessingPrefab;
    [SerializeField] private TextBase textBlessingName;
    [SerializeField] private TextBase textBlessingInfo;
    [SerializeField] private ButtonBase buttonClose;
    [SerializeField] private ButtonBase buttonUse;

    private const float SLIDE_DISTANCE = 120f;
    private const string EMPTY_TEXT = "-";

    private List<BlessingSO> _blessings = new();
    private List<UI_Blessing> _items = new();
    private Sequence _transitionSequence;
    private BlessingSO _selectedBlessing;
    private Vector2 _shownAnchoredPosition;
    private bool _isShown;

    public event Action<BlessingSO> UseRequested;

    private void Awake()
    {
        _shownAnchoredPosition = panelObject ? panelObject.anchoredPosition : Vector2.zero;

        if (buttonClose)
            buttonClose.OnClick.AddListener(OnClickCloseButton);
        if (buttonUse)
            buttonUse.OnClick.AddListener(OnClickUseButton);
    }


    public void SetData(List<BlessingSO> blessings)
    {
        _blessings.Clear();

        foreach(var blessing in blessings)
        {
            if(blessing)
                _blessings.Add(blessing);
        }

        RenderItems();
        _selectedBlessing = _items.Count > 0 ? _items[0].Data : null;
        if (_items.Count > 0)
            _items[0].SetSelected(true);
        Refresh();
    }

    public void SetVisible(bool isVisible)
    {
        if (_isShown == isVisible && gameObject.activeSelf == isVisible) return;

        bool wasActive = gameObject.activeSelf;
        _isShown = isVisible;
        _transitionSequence?.Kill();

        if (isVisible)
        {
            gameObject.SetActive(true);
            Refresh();
        }

        if (!panelObject)
        {
            gameObject.SetActive(isVisible);
            return;
        }

        CanvasGroup canvasGroup = panelObject.GetComponent<CanvasGroup>();
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
        bool hasBlessings = _blessings.Count > 0;
        if (blessingFrame)
            blessingFrame.SetActive(hasBlessings);
        if (emptyFrame)
            emptyFrame.SetActive(!hasBlessings);

        bool hasUsableBlessing = _blessings.Any(blessing => blessing && blessing.IsUsable);

        if (buttonUse)
        {
            buttonUse.gameObject.SetActive(hasUsableBlessing);
            buttonUse.Interactable = hasUsableBlessing && _selectedBlessing && _selectedBlessing.IsUsable;
        }

        if (textBlessingName)
            textBlessingName.text = _selectedBlessing ? _selectedBlessing.DisplayName : EMPTY_TEXT;
        if (textBlessingInfo)
            textBlessingInfo.text = _selectedBlessing
                ? _selectedBlessing.GetEffectDescription(Managers.Collection.GetLevel(_selectedBlessing), false)
                : EMPTY_TEXT;

        RebuildLayout();
    }

    private void RenderItems()
    {
        foreach(var item in _items)
        {
            Destroy(item.gameObject);
        }
        _items.Clear();

        foreach(var blessing in _blessings)
        {
            var item = Instantiate(blessingPrefab, blessingRoot);
            item.Clicked += OnClickBlessing;
            item.SetData(blessing);
            _items.Add(item);
        }
    }

    private void OnClickBlessing(UI_Blessing selectedItem)
    {
        _selectedBlessing = selectedItem.Data;
        foreach (var item in _items)
        {
            item.SetSelected(item == selectedItem);
        }

        Refresh();
    }

    private void OnClickUseButton()
    {
        if (!_selectedBlessing || !_selectedBlessing.IsUsable) return;

        UseRequested?.Invoke(_selectedBlessing);
        Refresh();
    }

    private void OnClickCloseButton()
    {
        Debug.Log("dfdf");
        SetVisible(false);
    }

    private void RebuildLayout()
    {
        Canvas.ForceUpdateCanvases();
        if (blessingRoot)
            LayoutRebuilder.ForceRebuildLayoutImmediate(blessingRoot);
        if (blessingFrame)
            LayoutRebuilder.ForceRebuildLayoutImmediate(blessingFrame.transform as RectTransform);
        if (panelObject)
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelObject);
    }
}
