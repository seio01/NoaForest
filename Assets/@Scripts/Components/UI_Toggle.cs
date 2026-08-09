using System;
using UnityEngine;
using UnityEngine.UI;

public class UI_Toggle : UI_Base
{
    [SerializeField] private Sprite spriteToggleOn;
    [SerializeField] private Sprite spriteToggleOff;

    private Button _buttonToggle;
    private Image _imageToggle;
    private TextBase _textState;

    public bool IsOn { get; private set; }
    public event Action<bool> ValueChanged;

    public bool Interactable
    {
        get => _buttonToggle && _buttonToggle.interactable;
        set
        {
            if (_buttonToggle)
                _buttonToggle.interactable = value;
        }
    }

    private void Awake()
    {
        _buttonToggle = GetComponent<Button>();
        _imageToggle = GetComponent<Image>();
        _textState = GetComponentInChildren<TextBase>(true);
        _buttonToggle.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        if (_buttonToggle)
            _buttonToggle.onClick.RemoveListener(OnClick);
    }

    public void SetValueWithoutNotify(bool isOn)
    {
        IsOn = isOn;
        RefreshView();
    }

    private void OnClick()
    {
        IsOn = !IsOn;
        RefreshView();
        ValueChanged?.Invoke(IsOn);
    }

    private void RefreshView()
    {
        if (_imageToggle)
            _imageToggle.sprite = IsOn ? spriteToggleOn : spriteToggleOff;

        if (!_textState)
            return;

        _textState.text = IsOn ? "ON" : "OFF";
        _textState.SetTextColor(IsOn ? Define.TextColorPalette.White : Define.TextColorPalette.Olive2);

        RectTransform textRect = _textState.GetComponent<RectTransform>();
        Vector2 anchoredPosition = textRect.anchoredPosition;
        anchoredPosition.x = IsOn ? -20f : 20f;
        textRect.anchoredPosition = anchoredPosition;
    }
}
