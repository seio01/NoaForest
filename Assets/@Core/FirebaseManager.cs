using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using UnityEngine;

public enum FirebaseInitializeState
{
    None,
    Initializing,
    Initialized,
    Failed
}

public class FirebaseManager
{
    
    private FirebaseApp _firebaseApp;
    private Task _initializeTask;
    private FirebaseInitializeState _initializeState = FirebaseInitializeState.None;

    public bool IsInitialized => _initializeState == FirebaseInitializeState.Initialized;
    public FirebaseInitializeState InitializeState => _initializeState;
    public FirebaseApp App => _firebaseApp;

    public Task InitializeAsync()
    {
        if(IsInitialized) return Task.CompletedTask;

        if (_initializeState == FirebaseInitializeState.Initializing && _initializeTask != null) return _initializeTask;

        _initializeTask ??= InitializeInternalAsync();
        return _initializeTask;
    }

    private async Task InitializeInternalAsync()
    {
        _initializeState = FirebaseInitializeState.Initializing;

        //Firebase 의존성 체크
        DependencyStatus dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

        if(dependencyStatus != DependencyStatus.Available)
        {
            //초기화 실패
            _initializeState = FirebaseInitializeState.Failed;
            Debug.LogError($"[FirebaseManager] Could not resolve Firebase dependencies: {dependencyStatus}");
            return;
        }

        // FirebaseApp 초기화
        _firebaseApp = FirebaseApp.DefaultInstance;
        _initializeState = FirebaseInitializeState.Initialized;

        Debug.Log("[FirebaseManager] Firebase initialized.");
    }
}
