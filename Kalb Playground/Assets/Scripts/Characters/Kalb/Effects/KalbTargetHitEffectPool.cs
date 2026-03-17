// KalbTargetHitEffectPool.cs
using System.Collections.Generic;
using UnityEngine;

public class KalbTargetHitEffectPool : MonoBehaviour
{
    [System.Serializable]
    public class EffectPool
    {
        public string poolName;
        public GameObject prefab;
        public int initialSize = 10;
        public bool expandable = true;
    }

    [Header("Effect Pools")]
    [SerializeField] private List<EffectPool> pools;

    [Header("Default White Spark Effect")]
    [SerializeField] private GameObject whiteSparkPrefab;

    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, GameObject> prefabDictionary;

    private static KalbTargetHitEffectPool _instance;
    public static KalbTargetHitEffectPool Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<KalbTargetHitEffectPool>();
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            // Don't use DontDestroyOnLoad so it cleans up with scene
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitializePools();
    }

    private void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        prefabDictionary = new Dictionary<string, GameObject>();

        // Add default white spark pool
        if (whiteSparkPrefab != null)
        {
            AddPool("WhiteSpark", whiteSparkPrefab, 20);
        }

        // Add custom pools
        foreach (var pool in pools)
        {
            AddPool(pool.poolName, pool.prefab, pool.initialSize);
        }
    }

    private void AddPool(string poolName, GameObject prefab, int size)
    {
        if (poolDictionary.ContainsKey(poolName)) return;

        Queue<GameObject> objectPool = new Queue<GameObject>();
        prefabDictionary[poolName] = prefab;

        for (int i = 0; i < size; i++)
        {
            GameObject obj = CreateNewEffect(prefab);
            obj.SetActive(false);
            objectPool.Enqueue(obj);
        }

        poolDictionary.Add(poolName, objectPool);
    }

    private GameObject CreateNewEffect(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.name = $"{prefab.name}_{System.Guid.NewGuid().ToString().Substring(0, 4)}";

        // Ensure the hit effect script is attached
        if (obj.GetComponent<KalbTargetHitEffect>() == null)
        {
            obj.AddComponent<KalbTargetHitEffect>();
        }

        return obj;
    }

    public GameObject GetHitEffect(Vector3 position, string poolName = "WhiteSpark")
    {
        if (!poolDictionary.ContainsKey(poolName))
        {
            Debug.LogWarning($"Pool {poolName} doesn't exist. Using WhiteSpark.");
            poolName = "WhiteSpark";

            if (!poolDictionary.ContainsKey(poolName))
                return null;
        }

        Queue<GameObject> pool = poolDictionary[poolName];
        GameObject effect;

        if (pool.Count > 0)
        {
            effect = pool.Dequeue();
        }
        else
        {
            // Pool empty, create new if expandable
            effect = CreateNewEffect(prefabDictionary[poolName]);
        }

        // Position and activate
        effect.transform.position = position;
        effect.SetActive(true);

        return effect;
    }

    public void ReturnEffect(GameObject effect)
    {
        if (effect == null) return;

        // Find which pool this belongs to by checking name prefix
        string poolName = null;
        foreach (var kvp in prefabDictionary)
        {
            if (effect.name.StartsWith(kvp.Value.name))
            {
                poolName = kvp.Key;
                break;
            }
        }

        if (poolName != null && poolDictionary.ContainsKey(poolName))
        {
            effect.SetActive(false);
            poolDictionary[poolName].Enqueue(effect);
        }
        else
        {
            Destroy(effect);
        }
    }

    public void PrewarmPool(string poolName, int count)
    {
        if (!poolDictionary.ContainsKey(poolName) || !prefabDictionary.ContainsKey(poolName))
            return;

        Queue<GameObject> pool = poolDictionary[poolName];
        GameObject prefab = prefabDictionary[poolName];

        for (int i = 0; i < count; i++)
        {
            GameObject obj = CreateNewEffect(prefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }
}