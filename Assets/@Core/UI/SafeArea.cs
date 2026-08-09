using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    [Header("Options")]
    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private bool continuousCheck = true;
    [SerializeField] private float checkInterval = 0.25f;

    private RectTransform _rect;
    private Rect _lastSafeArea;
    private Vector2Int _lastResolution;
    private float _nextCheckTime;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        ApplySafeArea();
    }

    private void Start()
    {
        if (applyOnStart)
            ApplySafeArea();
    }

    private void Update()
    {
        if (!continuousCheck) return;
        if (Time.unscaledTime < _nextCheckTime) return;

        _nextCheckTime = Time.unscaledTime + checkInterval;

        if (_lastResolution.x != Screen.width ||
            _lastResolution.y != Screen.height ||
            _lastSafeArea != Screen.safeArea)
        {
            ApplySafeArea();
        }
    }

    private void ApplySafeArea()
    {
        if (!_rect)
            _rect = GetComponent<RectTransform>();

        if (Screen.width <= 0 || Screen.height <= 0) return;

        Rect safe = Screen.safeArea;

        _lastSafeArea = safe;
        _lastResolution = new Vector2Int(Screen.width, Screen.height);

        Vector2 anchorMin = safe.position;
        Vector2 anchorMax = safe.position + safe.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        if (!(anchorMin.x >= 0f && anchorMin.y >= 0f && anchorMax.x <= 1f && anchorMax.y <= 1f))
            return;

        _rect.anchorMin = anchorMin;
        _rect.anchorMax = anchorMax;

        _rect.offsetMin = Vector2.zero;
        _rect.offsetMax = Vector2.zero;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        ApplySafeArea();
    }
#endif
}
