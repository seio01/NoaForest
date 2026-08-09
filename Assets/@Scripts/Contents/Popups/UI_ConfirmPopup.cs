using System;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmPopupButtonData
{
    public string name;
    public ButtonColorType color;
    public Action clickAction;
}

public class ConfirmPopupData
{
    public string title;
    public string info;
    public bool hasImage;
    public ConfirmPopupButtonData leftButtonData;
    public ConfirmPopupButtonData rightButtonData;
}

public class UI_ConfirmPopup : PopupBase
{
    [SerializeField] private RectTransform popup;
    [SerializeField] private RectTransform content;
    [SerializeField] private RectTransform contentArea;
    [SerializeField] private RectTransform buttonArea;
    [SerializeField] private TextBase textTitle;

    [Header("Content Area")]
    [SerializeField] private GameObject imageInfo;
    [SerializeField] private TextBase textInfo;

    [Header("Button Area")]
    [SerializeField] private ButtonBase buttonLeft;
    [SerializeField] private ButtonBase buttonRight;

    public void SetData(ConfirmPopupData data)
    {
        if(textTitle)
            textTitle.text = data.title;

        if(textInfo)
            textInfo.text = data.info;

        if(imageInfo)
            imageInfo.SetActive(data.hasImage);
        
        if(buttonArea)
            buttonArea.gameObject.SetActive(data.leftButtonData != null || data.rightButtonData != null);

        if(data.leftButtonData != null)
            SetButton(buttonLeft, data.leftButtonData.name, data.leftButtonData.clickAction, data.leftButtonData.color);

        if(data.rightButtonData != null)
            SetButton(buttonRight, data.rightButtonData.name, data.rightButtonData.clickAction, data.rightButtonData.color);

        RebuildLayout();
    }

    private void SetButton(ButtonBase button, string name, Action onClick, ButtonColorType buttonColor)
    {
        if(!button) return;

        button.gameObject.SetActive(true);

        button.SetButtonText(name);

        button.OnClick.RemoveAllListeners();
        button.OnClick.AddListener(onClick);

        button.ColorType = buttonColor;
    }

    private void RebuildLayout()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(buttonArea);
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentArea);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        LayoutRebuilder.ForceRebuildLayoutImmediate(popup);
    }

}
