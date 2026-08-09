using UnityEngine;
using UnityEngine.UI;

public class UI_Currency : UI_Base
{
    [SerializeField] private Define.CurrencyType currencyType;
    [SerializeField] private TextBase textCurrency;
    [SerializeField] private Image imageCurrencyIcon;

    public Define.CurrencyType CurrencyType => currencyType;

    public void SetAmount(int amount)
    {
        if(textCurrency)
            textCurrency.text = amount.ToString("N0");
    }

    
}
