using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIReferenceSpaceScaler : MonoBehaviour
{
    public enum ReferenceScaleMode
    {
        Width,
        Height,
        Fit,
        Envelope
    }

    [SerializeField] private RectTransform rectTransformContent;
    [SerializeField] private Vector2 vector2ReferenceSize = new Vector2(1080f, 1920f);
    [SerializeField] private ReferenceScaleMode referenceScaleMode = ReferenceScaleMode.Fit;

    private RectTransform _rectTransform;

    private void Awake()
    {
        CacheRectTransform();
    }

    private void OnEnable()
    {
        ApplyScale();
    }

    private void Start()
    {
        ApplyScale();
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyScale();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
            ApplyScale();
    }
#endif

    public void Refresh()
    {
        ApplyScale();
    }

    private void CacheRectTransform()
    {
        if (!_rectTransform)
            _rectTransform = GetComponent<RectTransform>();
    }

    private void ApplyScale()
    {
        CacheRectTransform();
        if (!rectTransformContent || vector2ReferenceSize.x <= 0.01f || vector2ReferenceSize.y <= 0.01f)
            return;

        Vector2 targetSize = _rectTransform.rect.size;
        if (targetSize.x <= 0f || targetSize.y <= 0f)
            return;

        float widthScale = targetSize.x / vector2ReferenceSize.x;
        float heightScale = targetSize.y / vector2ReferenceSize.y;
        float scale = GetScale(widthScale, heightScale);
        rectTransformContent.localScale = new Vector3(scale, scale, 1f);
    }

    private float GetScale(float widthScale, float heightScale)
    {
        switch (referenceScaleMode)
        {
            case ReferenceScaleMode.Width:
                return widthScale;
            case ReferenceScaleMode.Height:
                return heightScale;
            case ReferenceScaleMode.Envelope:
                return Mathf.Max(widthScale, heightScale);
            default:
                return Mathf.Min(widthScale, heightScale);
        }
    }
}
