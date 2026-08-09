using System;
using UnityEngine;
using UnityEngine.UI;

public class UI_Collection : UI_Base
{
    [Header("Frame")]
    [SerializeField] private GameObject imageLock;
    [SerializeField] private GameObject frameStars;

    [Header("Image")]
    [SerializeField] private Image imageIcon;
    [SerializeField] private Image imageRarity;
    [SerializeField] private GameObject[] imageStars = Array.Empty<GameObject>();

    [Header("Text")]
    [SerializeField] private TextBase textName;
    [SerializeField] private TextBase textLevel;

    [Header("Slider")]
    [SerializeField] private Slider sliderLevel;

    [Header("Button")]
    [SerializeField] private Button buttonClick;

    private Action _onClicked;

    private void Awake()
    {
        if (buttonClick)
            buttonClick.onClick.AddListener(OnClick);
    }

    public void SetData(CollectionItemViewData data, Action onClicked)
    {
        _onClicked = onClicked;
        if (data == null)
            return;

        SetIcon(data.icon);
        SetRarity(data.rarityBadge);
        SetStars(data.starCount);
        SetProgress(data.progress);

        if (textName)
            textName.text = data.name;
        if (imageLock)
            imageLock.SetActive(data.isLocked);

    }


    private void SetIcon(Sprite icon)
    {
        if (!imageIcon)
            return;

        imageIcon.sprite = icon;
        imageIcon.gameObject.SetActive(icon);
    }

    private void SetRarity(Sprite rarityBadge)
    {
        if (!imageRarity)
            return;

        imageRarity.sprite = rarityBadge;
        imageRarity.gameObject.SetActive(rarityBadge);
    }

    private void SetStars(int? starCount)
    {
        bool hasStars = starCount.HasValue;
        if (frameStars)
            frameStars.SetActive(hasStars);

        int visibleStarCount = Mathf.Clamp(starCount ?? 0, 0, imageStars.Length);
        for (int index = 0; index < imageStars.Length; index++)
        {
            if (imageStars[index])
                imageStars[index].SetActive(index < visibleStarCount);
        }
    }

    private void SetProgress(CollectionProgressViewData progress)
    {
        bool hasProgress = progress != null;
        if (sliderLevel)
        {
            sliderLevel.gameObject.SetActive(hasProgress);
            if (hasProgress)
            {
                int required = Mathf.Max(1, progress.required);
                sliderLevel.minValue = 0;
                sliderLevel.maxValue = required;
                sliderLevel.value = progress.isMaximum ? required : Mathf.Clamp(progress.current, 0, required);
            }
        }
        if (textLevel)
        {
            textLevel.gameObject.SetActive(hasProgress);
            if (hasProgress)
                textLevel.text = string.IsNullOrEmpty(progress.displayText)
                    ? progress.isMaximum ? "MAX" : $"{progress.current:N0}/{progress.required:N0}"
                    : progress.displayText;
        }
    }

    private void OnClick()
    {
        _onClicked?.Invoke();
    }
}
