using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_PurifyNoaInfoPanel : UI_Base
{
    [SerializeField] private Image imageNoa;
    [SerializeField] private Image imageElement;
    [SerializeField] private Image[] imageStars = Array.Empty<Image>();
    [SerializeField] private TextBase textNoaName;
    [SerializeField] private TextBase textPower;
    [SerializeField] private TextBase textInterval;
    [SerializeField] private TextBase textRange;
    [SerializeField] private Sprite spriteElementWater, spriteElementFire, spriteElementWind, spriteElementEarth;

    private Color _inactiveStarColor = new Color32(0x96, 0x96, 0x96, 0xFF);

    public void SetData(Noa noa)
    {
        SetNoaImage(noa.Icon);
        SetElementImage(noa.Data.Element);
        SetTier(noa.Data.Tier);
        SetName(noa.Data.DisplayName);
        SetStats(noa);
    }

    private void SetNoaImage(Sprite icon)
    {
        if(imageNoa)
            imageNoa.sprite = icon;
    }

    private void SetElementImage(Define.ElementType element)
    {
        if (imageElement  == null) return;

        imageElement.sprite = GetElementSprite(element);
        imageElement.enabled = imageElement.sprite;
    }

    private Sprite GetElementSprite(Define.ElementType element)
    {
        switch (element)
        {
            case Define.ElementType.Water:
                return spriteElementWater;
            case Define.ElementType.Fire:
                return spriteElementFire;
            case Define.ElementType.Wind:
                return spriteElementWind;
            case Define.ElementType.Earth:
                return spriteElementEarth;
            default:
                return null;
        }
    }

    private void SetName(string name)
    {
        if(textNoaName)
            textNoaName.text = name;
    }

    private void SetTier(Define.NoaTier tier)
    {
        int visibleStarCount = Mathf.Clamp((int)tier, 0, imageStars.Length);
        for (int index = 0; index < imageStars.Length; index++)
        {
            if (imageStars[index])
            {
                imageStars[index].gameObject.SetActive(true);
                imageStars[index].color = index < visibleStarCount ? Color.white : _inactiveStarColor;
            }
        }
    }

    private void SetStats(Noa noa)
    {
        if(textPower)
            textPower.text = $"{noa.PurifyPower:0.#}";
        if(textInterval)
            textInterval.text = $"{noa.PurifyInterval:0.#}";
        if(textRange)
            textRange.text = $"{noa.PurifyRange:0.#}";
    }
}
