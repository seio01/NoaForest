using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

public class ResourceManager
{
    private Dictionary<string, Object> _resources = new Dictionary<string, Object>();

    private Dictionary<string, List<Action<Object>>> _loadCallbacks = new Dictionary<string, List<Action<Object>>>();

    public void LoadAsync<T>(string path, Action<T> callback = null) where T : Object
    {
        if(_resources.TryGetValue(path, out Object resource))
        {
            callback?.Invoke(resource as T);
            return;
        }

        if(_loadCallbacks.TryGetValue(path, out List<Action<Object>> callbackList))
        {
            callbackList.Add((obj) => callback?.Invoke(obj as T));
            return;
        }

        _loadCallbacks[path] = new List<Action<Object>>();

        if(callback != null)
        {
            _loadCallbacks[path].Add((obj) => callback?.Invoke(obj as T));
        }

        Managers.Coroutine.StartCoroutine(LoadAsyncRoutine<T>(path));
    }

    // Bootstrap에서 리소스 로드 시 사용
    public Task<T> LoadTaskAsync<T>(string path) where T : Object
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.FromException<T>(new ArgumentException("Resource path is empty.", nameof(path)));
        }

        var completionSource = new TaskCompletionSource<T>();

        try
        {
            LoadAsync<T>(path, resource =>
            {
                if (resource == null)
                {
                    completionSource.TrySetException(new InvalidOperationException($"Resource load failed: {path}"));
                    return;
                }

                completionSource.TrySetResult(resource);
            });
        }
        catch (Exception exception)
        {
            completionSource.TrySetException(exception);
        }

        return completionSource.Task;
    }

    public T[] LoadAll<T>(string path) where T : Object
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Debug.LogError("[ResourceManager] Resource path is empty.");
            return null;
        }

        T[] resources = Resources.LoadAll<T>(path);
        if (resources.Length == 0)
        {
            Debug.LogWarning($"[ResourceManager] No resources found at path: {path}");
            return resources;
        }

        foreach (T resource in resources)
        {
            _resources[$"{path}/{resource.name}"] = resource;
        }

        return resources;
    }

    private IEnumerator LoadAsyncRoutine<T>(string path) where T : Object
    {
        var req = Resources.LoadAsync<T>(path);

        yield return req;

        Object asset = req.asset;
        if(asset == null)
        {
            Debug.LogError($"[ResourceManager] Failed to load path: {path}");
            if (_loadCallbacks.TryGetValue(path, out var list))
            {
                foreach (var action in list) action?.Invoke(null);
                _loadCallbacks.Remove(path);
            }
            yield break;
        }

        _resources[path] = asset;

        if(_loadCallbacks.TryGetValue(path, out List<Action<Object>> callbackList))
        {
            foreach(var action in callbackList)
            {
                action?.Invoke(asset);
            }
            _loadCallbacks.Remove(path);
        }
    }

    public void Unload(string path)
    {
        if (_resources.ContainsKey(path))
        {
            _resources.Remove(path);
        }
    }

}
