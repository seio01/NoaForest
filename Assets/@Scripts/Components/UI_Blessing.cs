using System;
using UnityEngine;
using UnityEngine.UI;

public class UI_Blessing : UI_Base
{
    [SerializeField] private Image imageIcon;
    [SerializeField] private TextBase textName;
    [SerializeField] private GameObject imageSelected;
    [SerializeField] private Button buttonClick;

    public event Action<UI_Blessing> Clicked;

    public BlessingSO Data { get; private set; }

    private void Awake()
    {
        if (buttonClick)
            buttonClick.onClick.AddListener(OnClick);
    }

    public void SetData(BlessingSO blessing)
    {
        Data = blessing;

        if (imageIcon)
        {
            imageIcon.sprite = blessing ? Managers.ContentIcon.GetLoadedSprite(Define.ContentIconType.Blessing, blessing.Id) : null;
            imageIcon.enabled = imageIcon.sprite;
        }

        if (textName)
            textName.text = blessing ? blessing.DisplayName : "-";

        if (buttonClick)
            buttonClick.interactable = blessing;

        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        if (imageSelected)
            imageSelected.SetActive(isSelected);
    }

    private void OnClick()
    {
        if (Data)
            Clicked?.Invoke(this);
    }
}
