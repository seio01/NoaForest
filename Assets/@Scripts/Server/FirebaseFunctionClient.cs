using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Functions;
using Newtonsoft.Json;
using UnityEngine;

public class FirebaseFunctionClient
{
    private const string REGION = "asia-northeast3";

    private FirebaseFunctions _firebaseFunctions;
    private Task _initializeTask;

    public bool IsInitialized => _firebaseFunctions != null;

    public async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        _initializeTask ??= InitializeInternalAsync();
        await _initializeTask;

        if (!IsInitialized)
        {
            _initializeTask = null;
        }
    }

    public async Task<object> CallAsync(string functionName, Dictionary<string, object> request)
    {
        if (string.IsNullOrWhiteSpace(functionName) || request == null)
        {
            Debug.LogWarning("[FirebaseFunctionClient] Function call request is invalid.");
            return null;
        }

        if (!await EnsureReadyAsync())
        {
            return null;
        }

        try
        {
            HttpsCallableResult result = await _firebaseFunctions.GetHttpsCallable(functionName).CallAsync(request);
            return result.Data;
        }
        catch (FunctionsException exception)
        {
            Debug.LogError($"[FirebaseFunctionClient] {functionName} failed. ErrorCode: {exception.ErrorCode}, Message: {exception.Message}");
            return null;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[FirebaseFunctionClient] {functionName} failed. {exception}");
            return null;
        }
    }

    public async Task<TResponse> CallAsync<TResponse>(string functionName, Dictionary<string, object> request) where TResponse : class
    {
        object responseData = await CallAsync(functionName, request);
        if (responseData == null)
            return null;

        try
        {
            string json = JsonConvert.SerializeObject(responseData);
            return JsonConvert.DeserializeObject<TResponse>(json);
        }
        catch (JsonException exception)
        {
            Debug.LogError($"[FirebaseFunctionClient] Failed to parse {functionName} response: {exception.Message}");
            return null;
        }
    }

    private async Task InitializeInternalAsync()
    {
        await Managers.Firebase.InitializeAsync();
        if (!Managers.Firebase.IsInitialized)
        {
            Debug.LogError("[FirebaseFunctionClient] Firebase is not initialized.");
            return;
        }

        _firebaseFunctions = FirebaseFunctions.GetInstance(Managers.Firebase.App, REGION);
        Debug.Log($"[FirebaseFunctionClient] Initialized. Region: {REGION}");
    }

    private async Task<bool> EnsureReadyAsync()
    {
        await InitializeAsync();
        if (!IsInitialized)
        {
            return false;
        }

        await Managers.Auth.InitializeAsync();
        if (!Managers.Auth.IsLoggedIn)
        {
            Debug.LogWarning("[FirebaseFunctionClient] Firebase user is not signed in.");
            return false;
        }

        return true;
    }
}
