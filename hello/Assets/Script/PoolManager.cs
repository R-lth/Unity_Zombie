using System.Collections.Generic;
using UnityEngine;

public enum PoolId
{
    None,
    PlayerBullet,
    NormalZombie,
    BossZombie,
    FieldItem,
    Potion,
    Heart
}

public interface IPoolable
{
    void OnPoolSpawned();
    void OnPoolDespawned();
}

public interface IPool
{
    PoolId Id { get; }
    Component Prefab { get; }
    void Return(Component instance);
}

public interface IPool<T> : IPool where T : Component, IPoolable
{
    T Rent(Vector3 position, Quaternion rotation, Transform parent = null);
    void Return(T instance);
}

public sealed class PoolManager : MonoBehaviour
{
    private static PoolManager instance;
    private readonly Dictionary<PoolId, IPool> pools = new();

    public static PoolManager Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindAnyObjectByType<PoolManager>();

            if (instance == null)
            {
                GameObject managerObject = new GameObject("Pool Manager");
                instance = managerObject.AddComponent<PoolManager>();
            }

            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    public bool Register<T>(
        PoolId id,
        T prefab,
        int initialSize,
        int maxSize)
        where T : Component, IPoolable
    {
        if (id == PoolId.None || prefab == null)
        {
            Debug.LogError($"Invalid pool registration: {id}.", this);
            return false;
        }

        if (pools.TryGetValue(id, out IPool registeredPool))
        {
            bool matches = registeredPool is IPool<T> &&
                           registeredPool.Prefab == prefab;

            if (!matches)
            {
                Debug.LogError(
                    $"Pool ID '{id}' is already registered to " +
                    $"'{registeredPool.Prefab.name}'.",
                    this);
            }

            return matches;
        }

        Transform poolRoot = CreatePoolRoot(id);
        pools.Add(
            id,
            new Pool<T>(
                id,
                prefab,
                poolRoot,
                initialSize,
                maxSize));
        return true;
    }

    public bool IsRegistered(PoolId id)
    {
        return pools.ContainsKey(id);
    }

    public T Rent<T>(
        PoolId id,
        Vector3 position,
        Quaternion rotation,
        Transform parent = null)
        where T : Component, IPoolable
    {
        if (!pools.TryGetValue(id, out IPool pool))
        {
            Debug.LogError($"Pool ID '{id}' is not registered.", this);
            return null;
        }

        if (pool is not IPool<T> typedPool)
        {
            Debug.LogError(
                $"Pool ID '{id}' contains {pool.Prefab.GetType().Name}, " +
                $"not {typeof(T).Name}.",
                this);
            return null;
        }

        return typedPool.Rent(position, rotation, parent);
    }

    public void Return<T>(T instanceToReturn)
        where T : Component, IPoolable
    {
        if (instanceToReturn == null)
        {
            return;
        }

        PooledObject handle =
            instanceToReturn.GetComponent<PooledObject>();

        if (handle == null || handle.Owner == null)
        {
            // Scene-placed objects can share the return API. They are simply
            // disabled until a scene system decides to reactivate them.
            instanceToReturn.OnPoolDespawned();
            instanceToReturn.gameObject.SetActive(false);
            return;
        }

        handle.Owner.Return(instanceToReturn);
    }

    private Transform CreatePoolRoot(PoolId id)
    {
        GameObject rootObject = new GameObject($"{id} Pool");
        rootObject.transform.SetParent(transform);
        rootObject.SetActive(false);
        return rootObject.transform;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}

public sealed class Pool<T> : IPool<T> where T : Component, IPoolable
{
    private readonly T prefab;
    private readonly Transform poolRoot;
    private readonly Stack<T> available = new();
    private readonly int maxSize;
    private int totalCount;

    public PoolId Id { get; }
    public Component Prefab => prefab;

    public Pool(
        PoolId id,
        T prefab,
        Transform poolRoot,
        int initialSize,
        int maxSize)
    {
        Id = id;
        this.prefab = prefab;
        this.poolRoot = poolRoot;
        this.maxSize = Mathf.Max(1, Mathf.Max(initialSize, maxSize));

        Prewarm(initialSize);
    }

    public T Rent(
        Vector3 position,
        Quaternion rotation,
        Transform parent = null)
    {
        T instance = available.Count > 0 ? available.Pop() : Create();
        PooledObject handle = instance.GetComponent<PooledObject>();
        Transform targetTransform = instance.transform;

        targetTransform.SetParent(parent, false);
        targetTransform.SetPositionAndRotation(position, rotation);
        handle.IsRented = true;
        instance.gameObject.SetActive(true);
        instance.OnPoolSpawned();
        return instance;
    }

    public void Return(T instance)
    {
        if (instance == null)
        {
            return;
        }

        PooledObject handle = instance.GetComponent<PooledObject>();

        if (handle == null || handle.Owner != this || !handle.IsRented)
        {
            return;
        }

        handle.IsRented = false;
        instance.OnPoolDespawned();
        instance.gameObject.SetActive(false);
        instance.transform.SetParent(poolRoot, false);

        if (available.Count < maxSize)
        {
            available.Push(instance);
        }
        else
        {
            UnityEngine.Object.Destroy(instance.gameObject);
            totalCount--;
        }
    }

    void IPool.Return(Component instance)
    {
        if (instance is T typedInstance)
        {
            Return(typedInstance);
            return;
        }

        Debug.LogError(
            $"Cannot return {instance.GetType().Name} to Pool<{typeof(T).Name}> '{Id}'.");
    }

    private void Prewarm(int count)
    {
        int targetCount = Mathf.Min(Mathf.Max(0, count), maxSize);

        while (totalCount < targetCount)
        {
            T instance = Create();
            available.Push(instance);
        }
    }

    private T Create()
    {
        T instance = UnityEngine.Object.Instantiate(prefab, poolRoot);
        instance.name = prefab.name;
        instance.gameObject.SetActive(false);

        PooledObject handle = instance.GetComponent<PooledObject>();

        if (handle == null)
        {
            handle = instance.gameObject.AddComponent<PooledObject>();
        }

        handle.Owner = this;
        handle.IsRented = false;
        totalCount++;
        return instance;
    }
}

public sealed class PooledObject : MonoBehaviour
{
    internal IPool Owner { get; set; }
    internal bool IsRented { get; set; }
}
