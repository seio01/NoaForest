using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float referenceAspect = 0.75f;
    [SerializeField, Min(0.01f)] private float referenceOrthographicSize = 5f;

    private Camera _camera;
    private Vector2Int _lastResolution = new Vector2Int(-1, -1);

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        _lastResolution = new Vector2Int(-1, -1);
        ApplyCameraSize();
    }

    private void Update()
    {
        ApplyCameraSize();
    }

    private void ApplyCameraSize()
    {
        int screenWidth = Screen.width;
        int screenHeight = Screen.height;
        if (screenWidth <= 0 || screenHeight <= 0) return;

        Vector2Int currentResolution = new Vector2Int(screenWidth, screenHeight);
        if (currentResolution == _lastResolution) return;

        _lastResolution = currentResolution;
        _camera.ResetAspect();
        float currentAspect = (float)screenWidth / screenHeight;
        float aspectScale = Mathf.Min(1f, referenceAspect / currentAspect);
        _camera.orthographicSize = referenceOrthographicSize * aspectScale;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[CameraController] Camera resized: {screenWidth}x{screenHeight}, aspect={currentAspect:F3}, orthographicSize={_camera.orthographicSize:F3}", this);
#endif
    }
}
