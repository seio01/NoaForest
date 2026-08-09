using UnityEngine;
using UnityEngine.UI;

public class UI_BlessingSimpleInfoPopup : PopupBase
{
    [SerializeField] private Image imageBlessing;
    [SerializeField] private Image imageRarity;
    [SerializeField] private TextBase textBlessingName;
    [SerializeField] private TextBase textInfo;
    [SerializeField] private TextBase textEffect;

    public void SetData(BlessingSO blessingData)
    {
        if (!blessingData)
            return;

        SetImage(blessingData);

        SetText(blessingData);
        
    }

    private void SetImage(BlessingSO blessingData)
    {
        if (imageBlessing)
        {
            imageBlessing.sprite = Managers.ContentIcon.GetLoadedSprite(Define.ContentIconType.Blessing, blessingData.Id);
            imageBlessing.gameObject.SetActive(imageBlessing.sprite);
        }
        if (imageRarity)
        {
            imageRarity.sprite = Managers.ContentIcon.GetBlessingRaritySprite(blessingData.Rarity);
            imageRarity.gameObject.SetActive(imageRarity.sprite);
        }
    }
    
    private void SetText(BlessingSO blessingData)
    {
        if (textBlessingName)
            textBlessingName.text = blessingData.DisplayName;
        if (textInfo)
            textInfo.text = blessingData.Description;
        if (textEffect)
            textEffect.text = blessingData.GetEffectDescription(Managers.Collection.GetLevel(blessingData), false);
    }
}
