using UnityEngine;

public class UI_PurifyWaveTimer : MonoBehaviour
{
    private const int COUNTDOWN_START_SECONDS = 3;

    [SerializeField] private TextBase textTime;

    public void SetRemainingTime(int remainingSeconds)
    {
        bool isCountdownTime = remainingSeconds > 0 && remainingSeconds <= COUNTDOWN_START_SECONDS;
        if (!isCountdownTime)
        {
            gameObject.SetActive(false);
            return;
        }

        if (textTime) textTime.text = remainingSeconds.ToString();
        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }
}
