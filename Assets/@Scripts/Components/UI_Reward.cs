using UnityEngine;
using UnityEngine.UI;

public class UI_Reward : UI_Base
{
    [SerializeField] private Image imageIcon;
    [SerializeField] private TextBase textRewardAmount;

    public void SetData(Sprite rewardIcon, int rewardAmount)
    {
        if (imageIcon)
        {
            imageIcon.sprite = rewardIcon;
            imageIcon.enabled = rewardIcon;
        }

        if (textRewardAmount)
        {
            textRewardAmount.text = rewardAmount.ToString("N0");
        }
    }
}
