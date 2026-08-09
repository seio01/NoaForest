using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class SceneManager
{
    private readonly Dictionary<string, object> _parameters = new();
    private bool _isTransitioning;
    public bool IsTransitioning => _isTransitioning;

    public Define.Scene? CurrentScene
    {
        get
        {
            string name = UnitySceneManager.GetActiveScene().name;
            if(Enum.TryParse(name, out Define.Scene currentScene))
                return currentScene;
             return null;
        }
    }

    public void LoadScene(Define.Scene scene)
    {
        if(_isTransitioning) return;

        if(CurrentScene.ToString() == scene.ToString())
        {
            Debug.Log($"[SceneManager] 이미 활성화된 씬입니다: {scene}");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(scene.ToString()))
        {
            Debug.LogError($"[SceneManager] Build Settings에 등록되지 않은 씬입니다: {scene}");
            return;
        }

        _isTransitioning = true;
        Managers.Coroutine.StartCoroutine(LoadSceneRoutine(scene.ToString()));
    }

    public void SetParameter(string key, object value)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("[SceneManager] Scene parameter key is empty.");
            return;
        }

        _parameters[key] = value;
    }

    public T GetParameter<T>(string key)
    {
        if (!string.IsNullOrEmpty(key) && _parameters.TryGetValue(key, out object rawValue) && rawValue is T typedValue)
        {
            return typedValue;
        }

        return default;
    }

    public void RemoveParameter(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        _parameters.Remove(key);
    }

    public void ClearParameters()
    {
        _parameters.Clear();
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        bool isLoadingOpened = false;
        UI_Loading uiLoading  = null;

        //씬 전환 전 UI 정리
        Managers.UI.PrepareForSceneTransition();

        Managers.UI.OpenLoading<UI_Loading>(loading =>
        {
            uiLoading = loading;
            isLoadingOpened = true;
        });

        yield return new WaitUntil(() => isLoadingOpened);

        if(uiLoading == null)
        {
            Debug.LogError("[SceneManager] 로딩 UI를 열지 못해 씬 전환을 중단합니다.");

            FinishTransition();
            yield break;
        }

        //로딩 UI가 실제 화면에 한 프레임 표시되도록 기다림
        yield return null;

        AsyncOperation operation = UnitySceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if(operation == null)
        {
            Debug.LogError($"[SceneManager] 씬 로드 요청에 실패했습니다: {sceneName}");

            FinishTransition();
            yield break;
        }

        yield return operation;

        _isTransitioning = false;
    }

    private void FinishTransition()
    {
        Managers.UI.CloseLoading();
        _isTransitioning = false;
    }

}
