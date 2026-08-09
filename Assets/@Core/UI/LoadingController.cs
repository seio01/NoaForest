using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingController
{
    private UI_Loading _uiLoading;
    private bool _isOpening = false;
    private int _openRequestId;

    public void OpenLoading<T>(Action<T> callback = null) where T : UI_Loading
    {
        if(_uiLoading)
        {
            callback?.Invoke(_uiLoading as T);
            return;
        }

        if(_isOpening) return;

        _isOpening = true;
        int openRequestId = ++_openRequestId;

        var name = typeof(T).Name;
        Managers.Resource.LoadAsync<GameObject>($"Prefabs/Loadings/{name}", (prefab) =>
        {
            if (openRequestId != _openRequestId) return;

            _isOpening = false;
            if(prefab != null)
            {
                var loadingObj = UnityEngine.Object.Instantiate(prefab);
                loadingObj.transform.SetParent(Managers.UI.GetorCreateLoadingRoot().transform, false);
                var uiLoading = loadingObj.GetComponent<T>();
                if(uiLoading)
                {
                    _uiLoading = uiLoading;
                    callback?.Invoke(uiLoading);
                    return;
                }

                UnityEngine.Object.Destroy(loadingObj);
            }
        });
    }

    public void CloseLoading()
    {
        _openRequestId++;
        _isOpening = false;
        
        if(!_uiLoading) return;

        UnityEngine.Object.Destroy(_uiLoading.gameObject);
        _uiLoading = null;
    }
}
