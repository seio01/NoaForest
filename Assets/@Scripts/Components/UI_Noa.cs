using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Noa : UI_Base
{
   [SerializeField] private Image imageNoa;
   [SerializeField] private Image imageQuestion;
   [SerializeField] private RectTransform starFrame;
   [SerializeField] private GameObject[] imageStars = Array.Empty<GameObject>();

    public void SetData(Noa noa)
    {
        if(noa == null)
        {
            Clear();
            return;
        }

        ToggleQuestion(false);
        SetNoaImage(noa.Icon);
        SetTier(noa.Data.Tier);
    }

    public void SetData(NoaSO noaData)
    {
        if(noaData == null)
        {
            Clear();
            return;
        }

        Sprite icon = Managers.ContentIcon.GetLoadedSprite(Define.ContentIconType.Noa, noaData.IconId);
        if(icon == null)
        {
            Debug.LogError($"[UI_Noa] Noa sprite is not loaded: {noaData.IconId}");
            Clear();
            return;
        }

        ToggleQuestion(false);
        SetNoaImage(icon);
        SetTier(noaData.Tier);
    }

    public void Clear()
    {
        ToggleQuestion(true);
    }

    private void SetNoaImage(Sprite icon)
    {
        if(imageNoa)
            imageNoa.sprite = icon;
    } 

    private void SetTier(Define.NoaTier tier)
    {
        int visibleStarCount = Mathf.Clamp((int)tier, 0, imageStars.Length);
        for (int index = 0; index < imageStars.Length; index++)
        {
            imageStars[index].SetActive(index < visibleStarCount);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(starFrame);
    }

    private void ToggleQuestion(bool show)
    {
        if(imageQuestion)
            imageQuestion.gameObject.SetActive(show);

        if(imageNoa)
            imageNoa.gameObject.SetActive(!show);
    }
}
