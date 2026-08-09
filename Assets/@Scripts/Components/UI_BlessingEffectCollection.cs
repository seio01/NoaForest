using System;
using UnityEngine;
using UnityEngine.UI;

public class UI_BlessingEffectCollection : UI_Base
{
    [SerializeField] private Image imageIcon;
    [SerializeField] private TextBase textName;
    [SerializeField] private TextBase textEffect;
    [SerializeField] private GameObject imageSelected;
    [SerializeField] private Button buttonClick;

    public event Action<UI_BlessingEffectCollection> Clicked;

    public BlessingSO Data { get; private set; }

    private void Awake()
    {
        if (buttonClick)
            buttonClick.onClick.AddListener(OnClick);
    }

    public void SetData(BlessingSO blessingData)
    {
        Data = blessingData;

        if (imageIcon)
        {
            imageIcon.sprite = Managers.ContentIcon.GetLoadedSprite(Define.ContentIconType.Blessing, blessingData.Id);
        }

        if (textName)
            textName.text = blessingData.DisplayName;

        if (textEffect)
        {
            int level = blessingData ? Managers.Collection.GetLevel(blessingData) : 1;
            textEffect.text = blessingData ? blessingData.GetSimplifiedDescription(level) : "-";
        }

        if (buttonClick)
            buttonClick.interactable = blessingData;

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
