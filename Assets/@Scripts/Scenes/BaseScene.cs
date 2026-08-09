using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public abstract class BaseScene : UI_Base
{
    private CancellationTokenSource _lifetimeCancellationTokenSource;

    private async void Start()
    {
        _lifetimeCancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = _lifetimeCancellationTokenSource.Token;

        Debug.Log($"[BaseScene] Initialization started: {Managers.Scene.CurrentScene}");

        try
        {
            await InitializeSceneAsync(cancellationToken);

            //초기화를 기다리는 동안 씬이 파괴되었는지의 여부 판단
            //CancellationToken에 취소 요청이 들어왔는지 확인하고, 
            //취소됐다면 OperationCanceledException을 발생
            cancellationToken.ThrowIfCancellationRequested();

            OnSceneReady();
            Managers.UI.CloseLoading();

            Debug.Log($"[BaseScene] Initialization completed: {Managers.Scene.CurrentScene}");
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Managers.UI.CloseLoading();
        }
    }

    protected virtual Task InitializeSceneAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual void OnSceneReady()
    {
    }

    protected virtual void OnSceneExit()
    {
    }

    private void OnDestroy()
    {
        //씬 파괴됐을때 비동기 작업 정리
        _lifetimeCancellationTokenSource?.Cancel();
        
        OnSceneExit();
        _lifetimeCancellationTokenSource?.Dispose();
        _lifetimeCancellationTokenSource = null;
    }

    private DateTime _lastPauseTime = DateTime.MinValue;
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Debug.Log("앱이 일시 중지됨 (백그라운드)");
            _lastPauseTime = DateTime.Now;
        }
        else
        {
            Debug.Log("앱이 재개됨 (포그라운드)");
            if (_lastPauseTime != DateTime.MinValue)
            {
                TimeSpan elapsedTime = DateTime.Now - _lastPauseTime;
                Debug.Log($"앱이 일시 중지된 시간: {elapsedTime.TotalSeconds}초");
            }
        }
    }
}
