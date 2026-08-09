using UnityEngine;

public class UI_PurifyNoaMergePanel : UI_Base
{
    [SerializeField] private UI_Noa uiNoaBase;
    [SerializeField] private UI_Noa uiNoaMaterial;
    [SerializeField] private UI_Noa uiNoaResult;
    [SerializeField] private ButtonBase buttonMerge;

    public ActionEvent OnClickMerge = new();

    private void Awake()
    {
        if(buttonMerge)
            buttonMerge.OnClick.AddListener(OnClickMergeButton);
    }

    public void Show(Noa baseNoa)
    {
        if(baseNoa == null)
        {
            Hide();
            return;
        }

        gameObject.SetActive(true);

        if(uiNoaBase)
            uiNoaBase.SetData(baseNoa);

        if(uiNoaMaterial)
            uiNoaMaterial.Clear();

        if(uiNoaResult)
            uiNoaResult.SetData(baseNoa.Data.NextTierNoa);

        SetMergeButtonInteractive(false);
    }

    public void SetMergeMaterial(Noa materialNoa)
    {
        if(uiNoaMaterial)
            uiNoaMaterial.SetData(materialNoa);

        SetMergeButtonInteractive(!materialNoa);
    }

    public void Hide()
    {
        Clear();
        gameObject.SetActive(false);
    }

    private void Clear()
    {
        if(uiNoaBase)
            uiNoaBase.Clear();

        if(uiNoaMaterial)
            uiNoaMaterial.Clear();

        if(uiNoaResult)
            uiNoaResult.Clear();

        SetMergeButtonInteractive(false);
    }

    private void SetMergeButtonInteractive(bool isDisabled)
    {
        if(!buttonMerge) return;

        buttonMerge.Interactable = !isDisabled;
    }

    private void OnClickMergeButton()
    {
        OnClickMerge.Invoke();
    }
}
