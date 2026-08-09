using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Loading : UI_Base
{
    [SerializeField] private TextBase textLoading;

    public void SetText(string text)
    {
        if(!textLoading) return;

        textLoading.text = text;
    }
}
