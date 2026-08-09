using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum UI_Type
{
    Popup,
    Toast,
    Loading
}

public class UIManager
{
    private const string GLOBAL_ROOT = "@Global_UI_Root";
    private const string TOAST_ROOT = "@Toast_Root";
    private const string LOADING_ROOT = "@Loading_Root";
    private const string SCENE_ROOT = "@Scene_UI_Root";

    private const int TOAST_ORDER = 19999;
    private const int LOADING_ORDER = 9999;

    private PopupController _popupController;
    private ToastController _toastController;
    private LoadingController _loadingController;

    private GameObject _toastRoot;
    private GameObject _loadingRoot;

    public GameObject GetorCreateGlobalRoot()
    {
        var globalRoot = GameObject.Find(GLOBAL_ROOT);
        if(globalRoot == null)
        {
            globalRoot = new GameObject(GLOBAL_ROOT);
            UnityEngine.Object.DontDestroyOnLoad(globalRoot);
        }

        return globalRoot;
    }

    public GameObject GetorCreateSceneRoot()
    {
        var sceneRoot = GameObject.Find(SCENE_ROOT);
        if(sceneRoot == null)
        {
            sceneRoot = new GameObject(SCENE_ROOT);
        }

        return sceneRoot;
    }

    public GameObject GetorCreateToastRoot()
    {
        if(_toastRoot)
            return _toastRoot;
        
        var globalRoot = GetorCreateGlobalRoot();
        var toastRoot = globalRoot.transform.Find(TOAST_ROOT);
        if(toastRoot != null)
        {
            _toastRoot = toastRoot.gameObject;
            return _toastRoot;
        }

        _toastRoot = new GameObject(TOAST_ROOT);
        _toastRoot.transform.SetParent(globalRoot.transform, false);
        SetCanvas(_toastRoot, UI_Type.Toast);

        return _toastRoot;
    }

    public GameObject GetorCreateLoadingRoot()
    {
        if(_loadingRoot)
            return _loadingRoot;
        
        var globalRoot = GetorCreateGlobalRoot();
        var loadingRoot = globalRoot.transform.Find(LOADING_ROOT);
        if(loadingRoot != null)
        {
            _loadingRoot = loadingRoot.gameObject;
            return _loadingRoot;
        }

        _loadingRoot = new GameObject(LOADING_ROOT);
        _loadingRoot.transform.SetParent(globalRoot.transform, false);
        SetCanvas(_loadingRoot, UI_Type.Loading);

        return _loadingRoot;
    }

    public bool HasOpenPoup => _popupController != null && _popupController.HasOpenPoup;
    public bool ShouldShowCurrencyAbovePopup => _popupController != null && _popupController.ShouldShowCurrencyAbovePopup;

    public event Action<bool> CurrencyOverlayChanged
    {
        add
        {
            if (_popupController != null)
            {
                _popupController.CurrencyOverlayChanged += value;
            }
        }
        remove
        {
            if (_popupController != null)
            {
                _popupController.CurrencyOverlayChanged -= value;
            }
        }
    }

    public void Init()
    {
        _popupController = new PopupController();
        _popupController.InitBackgroundPopup();

        _toastController = new ToastController();
        _loadingController = new LoadingController();
    }

    #region Popup Controls
    public void OpenPopup<T>(string name, Action<T> callback = null) where T : PopupBase
    {
        _popupController?.OpenPopup(name, callback);
    }

    public void ClosePopup()
    {
        _popupController?.ClosePopup();
    }

    public void CloseAllPopup()
    {
        _popupController?.CloseAllPopup();
    }

    public void PrepareForSceneTransition()
    {
        _popupController?.PrepareForSceneTransition();
    }

    public void ClosePopupUntil<T>(string popupName) where T : PopupBase
    {
        _popupController?.ClosePopupUntil<T>(popupName);
    }
    #endregion

    #region Toast Controls
    public void ShowToast(string text, Define.ToastPosition position = Define.ToastPosition.Top)
    {
        if(_toastRoot == null)
        {
            GetorCreateToastRoot();
        }

        _toastController?.ShowToast(text, position);
    }
    #endregion

    #region Loading Controls
    public void OpenLoading<T>(Action<T> callback = null) where T : UI_Loading
    {
        if(_loadingRoot == null)
        {
            GetorCreateLoadingRoot();
        }

        _loadingController?.OpenLoading(callback);
    }

    public void CloseLoading()
    {
         _loadingController?.CloseLoading();
    }
    #endregion

    public void SetCanvas(GameObject go, UI_Type uiType = UI_Type.Popup)
    {
        Canvas canvas = Utils.GetorAddComponent<Canvas>(go);
        CanvasScaler scaler = Utils.GetorAddComponent<CanvasScaler>(go);
        Utils.GetorAddComponent<GraphicRaycaster>(go);

        // canvas init
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = GetMainCamera();
        canvas.overrideSorting = true;

        // canvas scaler init
        int setWidth = 1080;
        int setHeight = 1920;
        int deviceWidth = Screen.width;
        int deviceHeight = Screen.height;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        scaler.referenceResolution = new Vector2(setWidth, setHeight);

        if((float)setWidth/setHeight < (float)deviceWidth/deviceHeight)
        {
            scaler.matchWidthOrHeight = 1;
        }
        else
        {
            scaler.matchWidthOrHeight = 0;
        }

        if(uiType == UI_Type.Toast)
        {
            canvas.sortingOrder = TOAST_ORDER;
        }
        else if(uiType == UI_Type.Loading)
        {
            canvas.sortingOrder = LOADING_ORDER;
        }
    }

    private Camera GetMainCamera()
    {
        Camera camera = Camera.main;

        if(camera == null)
        {
            var obj = GameObject.Find("Main Camera");
            if(obj != null)
                camera = obj.GetComponent<Camera>();
        }

        if(camera == null)
        {
            Debug.LogWarning("[UIManager] Cannot Find Camera");
        }

        return camera;
    }

}
