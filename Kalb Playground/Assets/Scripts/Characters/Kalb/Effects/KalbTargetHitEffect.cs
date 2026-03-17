// KalbTargetHitEffect.cs
using UnityEngine;

public class KalbTargetHitEffect : MonoBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private float lifetime = 0.2f;
    [SerializeField] private bool randomizeRotation = true;
    [SerializeField] private bool randomizeScale = true;
    [SerializeField] private Vector2 scaleRange = new Vector2(0.7f, 1.3f);

    [Header("Pooling")]
    [SerializeField] private bool usePooling = true;

    private ParticleSystem[] particleSystems;
    private float timer;
    private bool isPlaying = false;

    private void Awake()
    {
        // Get all particle systems in children
        particleSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void OnEnable()
    {
        timer = lifetime;
        isPlaying = true;

        // Randomize rotation for variety
        if (randomizeRotation)
        {
            transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        }

        // Randomize scale for variety
        if (randomizeScale)
        {
            float scaleMultiplier = Random.Range(scaleRange.x, scaleRange.y);
            transform.localScale = Vector3.one * scaleMultiplier;
        }

        // Play all particle systems
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                ps.Play();
            }
        }
    }

    private void Update()
    {
        if (!isPlaying) return;

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        isPlaying = false;

        // Stop all particle systems
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        if (usePooling)
        {
            // Try to return to pool
            KalbTargetHitEffectPool pool = FindFirstObjectByType<KalbTargetHitEffectPool>();
            if (pool != null)
            {
                pool.ReturnEffect(gameObject);
                return;
            }
        }

        // Fallback to destroy
        Destroy(gameObject);
    }
}