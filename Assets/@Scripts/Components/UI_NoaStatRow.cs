using UnityEngine;

public class UI_NoaStatRow : UI_Base
{
    [SerializeField] private TextBase textBefore;
    [SerializeField] private TextBase textAfter;
    [SerializeField] private GameObject imageArrow;

    public void SetData(float? currentValue, float? nextValue, bool isMaximumLevel)
    {
        if (textBefore)
        {
            textBefore.gameObject.SetActive(!isMaximumLevel);
            textBefore.text = GetStatText(currentValue);
        }
        if (imageArrow)
            imageArrow.SetActive(!isMaximumLevel);
        if (textAfter)
        {
            textAfter.gameObject.SetActive(true);
            textAfter.text = GetStatText(isMaximumLevel ? currentValue : nextValue);
        }
    }

    private static string GetStatText(float? value)
    {
        return value.HasValue ? value.Value.ToString("0.##") : "-";
    }
}
