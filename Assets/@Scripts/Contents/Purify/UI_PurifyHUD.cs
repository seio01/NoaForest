using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UI_PurifyHUD : MonoBehaviour
{
    private const string FINAL_WAVE_TIME_TEXT = "--:--";

    [Header("Text")]
    [SerializeField] private TextBase textFlow;
    [SerializeField] private TextBase textTime;
    [SerializeField] private TextBase textTreeHp;
    [SerializeField] private TextBase textBreathLevel;
    [Header("Image")]
    [SerializeField] private Image imageHpGauge;
    [SerializeField] private TextBase textEnergy;
    [SerializeField] private Image imageBreathGauge;
    [Header("Button")]
    [SerializeField] private ButtonBase buttonSetting;
    [SerializeField] private ButtonBase buttonInfo;
    [SerializeField] private ButtonBase buttonBlessing;
    [SerializeField] private ButtonBase buttonBreathUpgrade;
    [SerializeField] private ButtonBase buttonBreathTrigger;

    private Tween _treeHealthTween;
    private Sequence _breathPulseSequence;
    private Vector3 _breathButtonBaseScale = Vector3.one;
    private int _currentWave;
    private bool _treeValueInit;

    public event Action OnSettingClicked;
    public event Action OnInfoClicked;
    public event Action OnBlessingClicked;
    public event Action OnBreathUpgradeClicked;
    public event Action OnBreathTriggerClicked;

    private void Awake()
    {
        if (buttonBreathTrigger) _breathButtonBaseScale = buttonBreathTrigger.transform.localScale;

        if (buttonSetting) 
            buttonSetting.OnClick.AddListener(OnClickSettingButton);
        if (buttonInfo)
            buttonInfo.OnClick.AddListener(OnClickInfoButton);
        if (buttonBlessing) 
            buttonBlessing.OnClick.AddListener(OnClickBlessingButton);
        if (buttonBreathUpgrade) 
            buttonBreathUpgrade.OnClick.AddListener(OnClickBreathUpgradeButton);
        if (buttonBreathTrigger) 
            buttonBreathTrigger.OnClick.AddListener(OnClickBreathTriggerButton);
    }

    public void SetFlow(int waveNumber)
    {
        _currentWave = waveNumber;
        if (textFlow) textFlow.text = "FLOW " + waveNumber.ToString("00");
        if (_currentWave == PurifyWaveSetSO.WAVE_COUNT && textTime) textTime.text = FINAL_WAVE_TIME_TEXT;
    }

    public void SetRemainingTime(int remainingSeconds)
    {
        if (!textTime) return;
        if (_currentWave == PurifyWaveSetSO.WAVE_COUNT)
        {
            textTime.text = FINAL_WAVE_TIME_TEXT;
            return;
        }

        int minutes = remainingSeconds / 60;
        int seconds = remainingSeconds % 60;
        textTime.text = $"{minutes:00} : {seconds:00}";
    }

    public void SetTreeHealth(int currentHealth, int maximumHealth)
    {
        if (textTreeHp) textTreeHp.text = $"{currentHealth}/{maximumHealth}";
        if (!imageHpGauge) return;

        float targetFill = maximumHealth > 0 ? Mathf.Clamp01((float)currentHealth / maximumHealth) : 0f;
        _treeHealthTween?.Kill();
        if (!_treeValueInit)
        {
            imageHpGauge.fillAmount = targetFill;
            _treeValueInit = true;
            return;
        }

        _treeHealthTween = imageHpGauge.DOFillAmount(targetFill, 0.35f).SetEase(Ease.OutCubic).SetUpdate(true);
    }

    public void SetEnergy(int currentEnergy)
    {
        if (textEnergy) textEnergy.text = currentEnergy.ToString();
    }

    public void SetForestBreathCharge(float progress)
    {
        if (imageBreathGauge) imageBreathGauge.fillAmount = Mathf.Clamp01(progress);
    }

    public void SetForestBreathLevel(int level)
    {
        if (textBreathLevel) textBreathLevel.text = $"LV.{level}";
    }

    public void SetForestBreathButtonState(bool isTriggerActive, bool isUpgradeInteractable)
    {
        if (buttonBreathTrigger)
        {
            buttonBreathTrigger.gameObject.SetActive(isTriggerActive);
            if (isTriggerActive) StartBreathButtonPulse();
            else StopBreathButtonPulse();
        }
        if (buttonBreathUpgrade) buttonBreathUpgrade.Interactable = isUpgradeInteractable;
    }

    private void StartBreathButtonPulse()
    {
        if (_breathPulseSequence != null || !buttonBreathTrigger) return;

        Transform buttonTransform = buttonBreathTrigger.transform;
        buttonTransform.localScale = _breathButtonBaseScale;
        _breathPulseSequence = DOTween.Sequence()
            .Append(buttonTransform.DOScale(_breathButtonBaseScale * 1.07f, 0.4f).SetEase(Ease.InOutSine))
            .Append(buttonTransform.DOScale(_breathButtonBaseScale, 0.4f).SetEase(Ease.InOutSine))
            .SetLoops(-1)
            .SetUpdate(true);
    }

    private void StopBreathButtonPulse()
    {
        _breathPulseSequence?.Kill();
        _breathPulseSequence = null;
        if (buttonBreathTrigger) buttonBreathTrigger.transform.localScale = _breathButtonBaseScale;
    }

    private void OnDisable()
    {
        _treeHealthTween?.Kill();
        _treeHealthTween = null;
        StopBreathButtonPulse();
    }

    private void OnClickSettingButton()
    {
        OnSettingClicked?.Invoke();
    }

    private void OnClickInfoButton()
    {
        OnInfoClicked?.Invoke();
    }

    private void OnClickBlessingButton()
    {
        OnBlessingClicked?.Invoke();
    }

    private void OnClickBreathUpgradeButton()
    {
        OnBreathUpgradeClicked?.Invoke();
    }

    private void OnClickBreathTriggerButton()
    {
        OnBreathTriggerClicked?.Invoke();
    }
}
