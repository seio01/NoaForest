using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_Profile : UI_Base
{
    [SerializeField] private TextBase textName;
    [SerializeField] private TextBase textLevel;
    [SerializeField] private Slider sliderLevel;
    [SerializeField] private Button buttonClick;

    public event Action Clicked;

    private void Awake() 
    {
        if(buttonClick)
            buttonClick.onClick.AddListener(OnClickButton);    
    }

    public void SetProfile(UserData userData)
    {
        if (userData == null)
            return;

        SetName(userData.Name);
        SetLevel(userData.Level);
        if (sliderLevel)
            sliderLevel.value = sliderLevel.minValue;
    }

    public void OnClickButton()
    {
        Clicked?.Invoke();
    }

    private void SetName(string name)
    {
        if (textName)
            textName.text = string.IsNullOrWhiteSpace(name) ? "-" : name;
    }

    private void SetLevel(int level)
    {
        if (textLevel)
            textLevel.text = $"Lv. {Math.Max(1, level):N0}";
    }
}
