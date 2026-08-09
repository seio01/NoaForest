using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class UI_TabData
{
    public Sprite Sprite { get; }
    public string Text { get; }

    public UI_TabData(Sprite sprite)
    {
        Sprite = sprite;
    }

    public UI_TabData(string text)
    {
        Text = text;
    }

    public UI_TabData(Sprite sprite, string text)
    {
        Sprite = sprite;
        Text = text;
    }
}

public class UI_Tab : UI_Base
{
    [SerializeField] private Image imageIcon;
    [SerializeField] private TextBase textLabel;
    [SerializeField] private GameObject imageSelected;
    [SerializeField] private ButtonBase buttonTab;

    public bool IsSelected { get; private set; }

    public event Action<UI_Tab> Clicked;

    private void Awake()
    {
        if (buttonTab)
            buttonTab.OnClick.AddListener(OnClick);
    }


    public void SetData(UI_TabData data)
    {
        SetIcon(data?.Sprite);
        SetText(data?.Text);
        SetSelected(false);
    }

    public void SetData(Sprite sprite)
    {
        SetData(new UI_TabData(sprite));
    }

    public void SetData(string text)
    {
        SetData(new UI_TabData(text));
    }

    public void SetData(Sprite sprite, string text)
    {
        SetData(new UI_TabData(sprite, text));
    }

    public void SetSelected(bool isSelected)
    {
        IsSelected = isSelected;

        if (imageSelected)
            imageSelected.SetActive(isSelected);
    }

    private void SetIcon(Sprite sprite)
    {
        if (!imageIcon) return;

        imageIcon.sprite = sprite;
        imageIcon.gameObject.SetActive(sprite);
        LayoutRebuilder.ForceRebuildLayoutImmediate(buttonTab.transform as RectTransform);
    }

    private void SetText(string text)
    {
        if (!textLabel) return;

        bool hasText = !string.IsNullOrWhiteSpace(text);
        textLabel.text = hasText ? text : string.Empty;
        textLabel.gameObject.SetActive(hasText);
        LayoutRebuilder.ForceRebuildLayoutImmediate(buttonTab.transform as RectTransform);
    }

    private void OnClick()
    {
        Clicked?.Invoke(this);
    }
}
