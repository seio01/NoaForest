using System;
using System.Collections.Generic;
using UnityEngine;

public class UI_CurrencyBar : UI_Base
{
    private const int CURRENCY_SORTING_ORDER = 1000;

    [SerializeField] private List<UI_Currency> currencyItems;

    private readonly Dictionary<Define.CurrencyType, UI_Currency> _currencyByType = new();
    private Canvas _canvas;
    private int _originSortingOrder;

    private void Awake()
    {
        BuildCurrencyLookup();
        InitCanvas();
    }

    private void OnEnable()
    {
        Managers.UI.CurrencyOverlayChanged += OnCurrencyOverlayChanged;
        Managers.Currency.CurrencyChanged += OnCurrencyChanged;
        RefreshAllCurrencies(Managers.Currency.GetCurrency);
        OnCurrencyOverlayChanged(Managers.UI.ShouldShowCurrencyAbovePopup);
    }

    private void OnDisable()
    {
        Managers.UI.CurrencyOverlayChanged -= OnCurrencyOverlayChanged;
        Managers.Currency.CurrencyChanged -= OnCurrencyChanged;
    }

    public void RefreshAllCurrencies(Func<Define.CurrencyType, int> getAmount)
    {
        if (getAmount == null)
        {
            return;
        }

        foreach (var currencyEntry in _currencyByType)
        {
            currencyEntry.Value.SetAmount(getAmount.Invoke(currencyEntry.Key));
        }
    }

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    public RectTransform GetCurrencyTarget(Define.CurrencyType currencyType)
    {
        return _currencyByType.TryGetValue(currencyType, out UI_Currency currency)
            ? currency.transform as RectTransform
            : null;
    }

    private void OnCurrencyChanged(Define.CurrencyType currencyType, int amount)
    {
        if (_currencyByType.TryGetValue(currencyType, out UI_Currency currency))
        {
            currency.SetAmount(amount);
        }
    }

    private void BuildCurrencyLookup()
    {
        _currencyByType.Clear();

        foreach (var currency in currencyItems)
        {
            if (currency == null) continue;

            if (_currencyByType.ContainsKey(currency.CurrencyType))
            {
                Debug.LogWarning($"[UI_CurrencyBar] Duplicate currency type: {currency.CurrencyType}");
                continue;
            }

            _currencyByType.Add(currency.CurrencyType, currency);
        }
    }

    private void OnCurrencyOverlayChanged(bool shouldOverlayPopup)
    {
        if(_canvas == null) return;

        if (shouldOverlayPopup)
        {
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = CURRENCY_SORTING_ORDER;
            return;
        }

        _canvas.sortingOrder = _originSortingOrder;
        _canvas.overrideSorting = false;
    }

    private void InitCanvas()
    {
        _canvas = Utils.GetorAddComponent<Canvas>(gameObject);
        _originSortingOrder = _canvas.sortingOrder;
    }
}
