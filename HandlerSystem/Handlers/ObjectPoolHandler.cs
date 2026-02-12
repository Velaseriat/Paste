using System;
using System.Collections.Generic;
using Felsan.Scripts.Shared.Handlers;
using UnityEngine;

namespace Felsan.Scripts.Shared.Handlers
{
    /// <summary>
    /// Generic object pool for GameObjects to reduce instantiation overhead and garbage collection.
    /// </summary>
    public class ObjectPoolHandler : ABaseHandler
    {
        [Header("Pool Settings")]
        [SerializeField] private int defaultPoolSize = 20;
        [SerializeField] private int maxPoolSize = 100;
        [SerializeField] private bool expandPool = true;

        private readonly Dictionary<string, PoolData> _pools = new();
        private readonly Dictionary<GameObject, string> _pooledObjects = new();

        [Serializable]
        private class PoolData
        {
            public GameObject Prefab;
            public Queue<GameObject> AvailableObjects = new();
            public List<GameObject> ActiveObjects = new();
            public int TotalCreated;
            public int MaxSize;
        }
        
        public override void Initialize(HandlerManager handlerManager)
        {
            // If already initialized due to dependency on another Handler, exit
            if (handlerState == HandlerState.Initialized) return;

            // Enable to initializing
            handlerState = HandlerState.Initializing;

            // Call base class to finish the initializing process
            base.Initialize(handlerManager);
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        
        /// <summary>
        /// Creates a pool for the specified prefab.
        /// </summary>
        public void CreatePool(GameObject prefab, int initialSize = -1, int maxSize = -1)
        {
            if (prefab == null)
            {
                LogError("Cannot create pool for null prefab");
                return;
            }

            var poolKey = GetPoolKey(prefab);
            
            if (_pools.ContainsKey(poolKey))
            {
                LogWarning($"Pool for {prefab.name} already exists");
                return;
            }

            var poolData = new PoolData
            {
                Prefab = prefab,
                MaxSize = maxSize > 0 ? maxSize : this.maxPoolSize
            };

            _pools[poolKey] = poolData;

            // Pre-populate the pool
            var size = initialSize > 0 ? initialSize : defaultPoolSize;
            for (int i = 0; i < size; i++)
            {
                var obj = CreatePooledObject(poolData);
                poolData.AvailableObjects.Enqueue(obj);
            }

            Log($"Created pool for {prefab.name} with {size} initial objects");
        }

        /// <summary>
        /// Gets an object from the pool or creates a new one if needed.
        /// </summary>
        public GameObject Get(GameObject prefab, Vector3 position = default, Quaternion rotation = default)
        {
            if (prefab == null) return null;

            var poolKey = GetPoolKey(prefab);
            
            if (!_pools.TryGetValue(poolKey, out var poolData))
            {
                // Auto-create pool if it doesn't exist
                CreatePool(prefab);
                poolData = _pools[poolKey];
            }

            GameObject obj;

            if (poolData.AvailableObjects.Count > 0)
            {
                // Get from pool
                obj = poolData.AvailableObjects.Dequeue();
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.SetActive(true);
            }
            else if (expandPool && poolData.TotalCreated < poolData.MaxSize)
            {
                // Create new object
                obj = CreatePooledObject(poolData);
                obj.transform.position = position;
                obj.transform.rotation = rotation;
            }
            else
            {
                // Pool is full, return null or reuse oldest object
                if (poolData.ActiveObjects.Count > 0)
                {
                    obj = poolData.ActiveObjects[0];
                    poolData.ActiveObjects.RemoveAt(0);
                    obj.transform.position = position;
                    obj.transform.rotation = rotation;
                    LogWarning($"Pool for {prefab.name} is full, reusing oldest object");
                }
                else
                {
                    LogError($"Pool for {prefab.name} is full and no active objects to reuse");
                    return null;
                }
            }

            poolData.ActiveObjects.Add(obj);
            _pooledObjects[obj] = poolKey;

            // Notify the object it's been spawned
            var poolable = obj.GetComponent<IPoolable>();
            poolable?.OnSpawned();

            return obj;
        }

        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        public void Return(GameObject obj)
        {
            if (obj == null) return;

            if (!_pooledObjects.TryGetValue(obj, out var poolKey))
            {
                LogWarning($"Object {obj.name} is not from a pool, destroying instead");
                Destroy(obj);
                return;
            }

            if (!_pools.TryGetValue(poolKey, out var poolData))
            {
                LogError($"Pool data not found for {obj.name}");
                return;
            }

            // Notify the object it's being returned
            var poolable = obj.GetComponent<IPoolable>();
            poolable?.OnReturned();

            // Reset object state
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;

            poolData.ActiveObjects.Remove(obj);
            poolData.AvailableObjects.Enqueue(obj);
            _pooledObjects.Remove(obj);
        }

        /// <summary>
        /// Returns all active objects from a specific pool.
        /// </summary>
        public void ReturnAll(GameObject prefab)
        {
            if (prefab == null) return;

            var poolKey = GetPoolKey(prefab);
            
            if (!_pools.TryGetValue(poolKey, out var poolData))
            {
                LogWarning($"Pool for {prefab.name} doesn't exist");
                return;
            }

            var activeObjects = new List<GameObject>(poolData.ActiveObjects);
            foreach (var obj in activeObjects)
            {
                Return(obj);
            }
        }

        /// <summary>
        /// Returns all active objects from all pools.
        /// </summary>
        public void ReturnAll()
        {
            foreach (var poolData in _pools.Values)
            {
                var activeObjects = new List<GameObject>(poolData.ActiveObjects);
                foreach (var obj in activeObjects)
                {
                    Return(obj);
                }
            }
        }

        /// <summary>
        /// Destroys a specific pool and all its objects.
        /// </summary>
        public void DestroyPool(GameObject prefab)
        {
            if (prefab == null) return;

            var poolKey = GetPoolKey(prefab);
            
            if (!_pools.TryGetValue(poolKey, out var poolData))
            {
                LogWarning($"Pool for {prefab.name} doesn't exist");
                return;
            }

            // Return all active objects first
            ReturnAll(prefab);

            // Destroy all available objects
            while (poolData.AvailableObjects.Count > 0)
            {
                var obj = poolData.AvailableObjects.Dequeue();
                Destroy(obj);
            }

            _pools.Remove(poolKey);
            Log($"Destroyed pool for {prefab.name}");
        }

        /// <summary>
        /// Gets pool statistics for debugging.
        /// </summary>
        public (int totalPools, int totalActive, int totalAvailable) GetPoolStats()
        {
            int totalActive = 0;
            int totalAvailable = 0;

            foreach (var poolData in _pools.Values)
            {
                totalActive += poolData.ActiveObjects.Count;
                totalAvailable += poolData.AvailableObjects.Count;
            }

            return (_pools.Count, totalActive, totalAvailable);
        }

        /// <summary>
        /// Gets detailed statistics for a specific pool.
        /// </summary>
        public (int active, int available, int totalCreated, int maxSize)? GetPoolStats(GameObject prefab)
        {
            if (prefab == null) return null;

            var poolKey = GetPoolKey(prefab);
            
            if (!_pools.TryGetValue(poolKey, out var poolData))
                return null;

            return (poolData.ActiveObjects.Count, poolData.AvailableObjects.Count, 
                   poolData.TotalCreated, poolData.MaxSize);
        }

        private GameObject CreatePooledObject(PoolData poolData)
        {
            var obj = Instantiate(poolData.Prefab, transform);
            obj.name = $"{poolData.Prefab.name}_Pooled_{poolData.TotalCreated}";
            obj.SetActive(false);
            poolData.TotalCreated++;
            return obj;
        }

        private string GetPoolKey(GameObject prefab)
        {
            return prefab.name; // You could use prefab.GetInstanceID() for more uniqueness
        }

        private void OnDestroy()
        {
            // Clean up all pools
            foreach (var poolData in _pools.Values)
            {
                while (poolData.AvailableObjects.Count > 0)
                {
                    var obj = poolData.AvailableObjects.Dequeue();
                    if (obj != null)
                        Destroy(obj);
                }
            }
        }
    }

    /// <summary>
    /// Interface for objects that need to know when they're spawned or returned to pool.
    /// </summary>
    public interface IPoolable
    {
        void OnSpawned();
        void OnReturned();
    }
} 