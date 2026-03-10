// KalbParticlePool.cs
using System.Collections.Generic;
using UnityEngine;

public class KalbParticlePool : MonoBehaviour
{
    [System.Serializable]
    public class ParticlePool
    {
        public string poolName;
        public ParticleSystem prefab;
        public int initialSize = 5;
    }
    
    [SerializeField] private List<ParticlePool> pools;
    
    // Dictionary for quick lookup
    private Dictionary<string, Queue<ParticleSystem>> poolDictionary;
    private Dictionary<string, ParticleSystem> prefabDictionary;
    
    private void Awake()
    {
        InitializePools();
    }
    
    private void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<ParticleSystem>>();
        prefabDictionary = new Dictionary<string, ParticleSystem>();
        
        foreach (var pool in pools)
        {
            Queue<ParticleSystem> objectPool = new Queue<ParticleSystem>();
            
            // Create initial objects
            for (int i = 0; i < pool.initialSize; i++)
            {
                ParticleSystem ps = CreateNewParticle(pool.prefab);
                ps.gameObject.SetActive(false);
                objectPool.Enqueue(ps);
            }
            
            poolDictionary.Add(pool.poolName, objectPool);
            prefabDictionary.Add(pool.poolName, pool.prefab);
        }
    }
    
    private ParticleSystem CreateNewParticle(ParticleSystem prefab)
    {
        ParticleSystem ps = Instantiate(prefab, transform);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        return ps;
    }
    
    public ParticleSystem GetParticle(string poolName, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(poolName))
        {
            Debug.LogWarning($"Pool {poolName} doesn't exist");
            return null;
        }
        
        Queue<ParticleSystem> pool = poolDictionary[poolName];
        
        ParticleSystem ps;
        
        if (pool.Count > 0)
        {
            ps = pool.Dequeue();
        }
        else
        {
            // Pool empty, create new one
            ps = CreateNewParticle(prefabDictionary[poolName]);
        }
        
        // Position and prepare
        ps.transform.position = position;
        ps.transform.rotation = rotation;
        ps.gameObject.SetActive(true);
        ps.Play();
        
        // Start coroutine to return to pool after finish
        StartCoroutine(ReturnToPoolAfterDelay(ps, poolName, 
            ps.main.duration + ps.main.startLifetime.constantMax));
        
        return ps;
    }
    
    private System.Collections.IEnumerator ReturnToPoolAfterDelay(
        ParticleSystem ps, string poolName, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        ps.gameObject.SetActive(false);
        poolDictionary[poolName].Enqueue(ps);
    }
}