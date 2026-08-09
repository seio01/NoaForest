using UnityEngine;

public class UI_MergeSelection : UI_Base
{
    [SerializeField] private GameObject imageBase;
    [SerializeField] private GameObject imageMaterial;
    [SerializeField] private GameObject gameObjectImageCheck;

    public void SetRole(bool isBase)
    {
        if(imageBase)
            imageBase.SetActive(isBase);

        if(imageMaterial)
            imageMaterial.SetActive(!isBase);

        SetChecked(false);
    }

    public void SetChecked(bool isChecked)
    {
        if(gameObjectImageCheck)
            gameObjectImageCheck.SetActive(isChecked);
    }
}
