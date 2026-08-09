using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public interface IPoolable
{
    void OnGet();
    void OnRelease();
}

public interface IPoolOrderReceiver
{
    void SetPoolOrder(int poolOrder);
}

public class Pool
{
    private readonly GameObject prefab;
    private int createdCount;
    private readonly int maxCount;
    private readonly Transform root;
    private readonly Stack<GameObject> inactivePoolObjs = new();
    private readonly List<IPoolOrderReceiver> _activeOrderReceivers = new();

    public Pool(GameObject prefab, int maxCount, Transform root)
    {
        this.prefab = prefab;
        this.maxCount = maxCount;
        this.root = root;
    }

    public GameObject Get()
    {
        GameObject obj = null;

        if (inactivePoolObjs.Count > 0)
        {
            obj = inactivePoolObjs.Pop();
        }
        else if (createdCount < maxCount)
        {
            obj = Object.Instantiate(prefab, root);
            createdCount++;
        }
        else
        {
            return null;
        }

        obj.SetActive(true);
        if (obj.TryGetComponent<IPoolable>(out var poolable))
            poolable.OnGet();

        if (obj.TryGetComponent<IPoolOrderReceiver>(out var poolOrderReceiver))
        {
            _activeOrderReceivers.Add(poolOrderReceiver);
            UpdateActivePoolOrder();
        }

        return obj;
    }

    public void Release(GameObject obj)
    {
        if (obj == null) return;

        if (obj.TryGetComponent<IPoolable>(out var poolable))
            poolable.OnRelease();

        if (obj.TryGetComponent<IPoolOrderReceiver>(out var poolOrderReceiver))
        {
            _activeOrderReceivers.Remove(poolOrderReceiver);
            UpdateActivePoolOrder();
        }

        obj.SetActive(false);
        obj.transform.SetParent(root, false);
        inactivePoolObjs.Push(obj);
    }

    private void UpdateActivePoolOrder()
    {
        for (int index = 0; index < _activeOrderReceivers.Count; index++)
            _activeOrderReceivers[index].SetPoolOrder(index);
    }
}

public class PurifyPoolController : MonoBehaviour
{
    private readonly Dictionary<string, Pool> pools = new();
    private Transform root;

    private void Awake()
    {
        root = new GameObject("@Pool_Root").transform;
        root.SetParent(transform, false);
    }

    public void CreatePool(GameObject prefab, int maxCount)
    {
        if (prefab == null) return;

        if (!prefab.TryGetComponent<PooledObject>(out var pooledObject) || pooledObject == null)
        {
            Debug.LogError($"[PoolManager] PooledObject가 프리팹({prefab.name})에 없습니다. 풀 생성 불가.");
            return;
        }

        string key = pooledObject.PrefabKey;
        if (string.IsNullOrEmpty(key)) return;
        if (HasPool(key)) return;

        var pool = new Pool(prefab, maxCount, CreateRoot(prefab));
        pools.Add(key, pool);
    }

    public GameObject Get(GameObject prefab, int maxCount)
    {
        if (prefab == null) return null;

        if (!prefab.TryGetComponent<PooledObject>(out var pooledObject) || pooledObject == null)
        {
            Debug.LogError($"[PoolManager] PooledObject가 프리팹({prefab.name})에 없습니다. Get 불가.");
            return null;
        }

        string key = pooledObject.PrefabKey;
        if (string.IsNullOrEmpty(key)) return null;

        if (!HasPool(key))
            CreatePool(prefab, maxCount);

        if (!pools.TryGetValue(key, out var pool))
            return null;

        return pool.Get();
    }

    public void Release(GameObject obj)
    {
        if (obj == null) return;

        if (!obj.TryGetComponent<PooledObject>(out var pooledObject) || pooledObject == null)
        {
            Debug.LogWarning($"[PoolManager] Release 대상({obj.name})에 PooledObject가 없습니다. 반납 무시.");
            return;
        }

        if (string.IsNullOrEmpty(pooledObject.PrefabKey))
        {
            Debug.LogWarning($"[PoolManager] Release 대상({obj.name})의 PrefabKey가 비어 있습니다. 반납 무시.");
            return;
        }

        if (!pools.TryGetValue(pooledObject.PrefabKey, out var pool))
        {
            Debug.LogWarning($"[PoolManager] key({pooledObject.PrefabKey}) 풀을 찾지 못했습니다. 반납 무시.");
            return;
        }

        pool.Release(obj);
    }

    private Transform CreateRoot(GameObject prefab)
    {
        var childRoot = new GameObject($"@{prefab.name}_Root").transform;
        childRoot.SetParent(root, false);
        return childRoot;
    }

    private bool HasPool(string key)
    {
        return pools.ContainsKey(key);
    }
}
