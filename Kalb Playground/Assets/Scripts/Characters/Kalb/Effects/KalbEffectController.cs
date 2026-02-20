// KalbEffectController.cs
using UnityEngine;
using System.Collections.Generic;

public class KalbEffectController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private KalbEffectSettings settings;
    [SerializeField] private Transform groundCheckPoint;
    
    // References
    private KalbController controller;
    private KalbMovement movement;
    private KalbCollisionDetector collisionDetector;
    private KalbInputHandler inputHandler;
    
    // Components
    private KalbGroundDust groundDust;
    private ParticleSystem currentRunDust;
    private ParticleSystem landingDustPool;
    
    // State tracking
    private bool wasGrounded = true;
    private float lastDustSpawnPosition;
    private Vector2 lastGroundNormal = Vector2.up;
    
    // Object pooling
    private Queue<ParticleSystem> dustPool = new Queue<ParticleSystem>();
    private List<ParticleSystem> activeDust = new List<ParticleSystem>();
    
    private void Awake()
    {
        InitializeComponents();
        InitializeDustSystem();
    }
    
    private void InitializeComponents()
    {
        controller = GetComponent<KalbController>();
        movement = GetComponent<KalbMovement>();
        collisionDetector = GetComponent<KalbCollisionDetector>();
        inputHandler = GetComponent<KalbInputHandler>();
        
        if (groundCheckPoint == null)
            groundCheckPoint = transform;
            
        if (settings == null)
            settings = ScriptableObject.CreateInstance<KalbEffectSettings>();
    }
    
    private void InitializeDustSystem()
    {
        if (settings.useObjectPooling)
            InitializeDustPool();
            
        // Subscribe to events
        controller.OnStateChanged += HandleStateChanged;
        controller.OnLanded += HandleLanded;
        controller.OnWallSlideStarted += HandleWallSlideStarted;
    }
    
    private void InitializeDustPool()
    {
        for (int i = 0; i < settings.poolSize; i++)
        {
            if (settings.groundDustPrefab != null)
            {
                GameObject dustObj = Instantiate(settings.groundDustPrefab, transform);
                dustObj.SetActive(false);
                
                ParticleSystem ps = dustObj.GetComponent<ParticleSystem>();
                if (ps != null)
                    dustPool.Enqueue(ps);
            }
        }
    }
    
    private void Update()
    {
        UpdateGroundDetection();
        UpdateRunDust();
    }
    
    private void UpdateGroundDetection()
    {
        bool isGrounded = controller.IsEffectivelyGrounded();
        
        // Landing detection
        if (!wasGrounded && isGrounded)
        {
            HandleLanding();
        }
        
        wasGrounded = isGrounded;
    }
    
    private void UpdateRunDust()
    {
        if (!controller.IsEffectivelyGrounded()) return;
        
        float currentSpeed = Mathf.Abs(controller.Rb.linearVelocity.x);
        bool isRunning = currentSpeed >= settings.minRunSpeedForDust && 
                        Mathf.Abs(inputHandler.MoveInput.x) > 0.1f;
        
        if (isRunning)
        {
            SpawnRunDust(currentSpeed);
        }
    }
    
    private void SpawnRunDust(float currentSpeed)
    {
        // Distance-based spawning (prevents dust at every frame)
        float currentX = transform.position.x;
        if (Mathf.Abs(currentX - lastDustSpawnPosition) < settings.dustSpawnDistance)
            return;
            
        lastDustSpawnPosition = currentX;
        
        // Calculate emission rate based on speed
        float speedRatio = Mathf.Clamp01(currentSpeed / settings.minRunSpeedForDust);
        float emissionRate = settings.dustEmissionRate * 
                            settings.emissionRateBySpeed.Evaluate(speedRatio);
        
        // Get pooled dust or create new
        ParticleSystem dust = GetDustParticle();
        if (dust == null) return;
        
        // Position at feet with ground normal
        Vector3 spawnPos = groundCheckPoint.position + Vector3.down * settings.dustSpawnOffsetY;
        dust.transform.position = spawnPos;
        dust.transform.rotation = Quaternion.FromToRotation(Vector3.up, lastGroundNormal);
        
        // Configure emission
        var emission = dust.emission;
        emission.rateOverTime = emissionRate;
        
        // Play
        dust.gameObject.SetActive(true);
        dust.Play();
        
        // Track for cleanup
        activeDust.Add(dust);
    }
    
    private void HandleLanding()
    {
        float landingVelocity = Mathf.Abs(controller.Rb.linearVelocity.y);
        
        if (landingVelocity >= Mathf.Abs(settings.minLandingVelocity))
        {
            SpawnLandingDust();
        }
    }
    
    private void SpawnLandingDust()
    {
        if (settings.landingDustPrefab == null) return;
        
        GameObject landingDust = Instantiate(settings.landingDustPrefab, 
            groundCheckPoint.position, 
            Quaternion.identity);
            
        ParticleSystem ps = landingDust.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
            Destroy(landingDust, settings.landingDustDuration);
        }
    }
    
    private void HandleWallSlideStarted()
    {
        // Spawn wall slide dust
        if (settings.wallDustPrefab == null) return;
        
        // Implementation for wall slide dust
    }
    
    private void HandleStateChanged()
    {
        // Spawn wall slide dust
        if (settings.wallDustPrefab == null) return;
        
        // Implementation for wall slide dust
    }
    private void HandleLanded()
    {
        // Spawn wall slide dust
        if (settings.wallDustPrefab == null) return;
        
        // Implementation for wall slide dust
    }
    
    private ParticleSystem GetDustParticle()
    {
        if (!settings.useObjectPooling)
        {
            GameObject dustObj = Instantiate(settings.groundDustPrefab);
            return dustObj.GetComponent<ParticleSystem>();
        }
        
        if (dustPool.Count > 0)
            return dustPool.Dequeue();
            
        // Pool exhausted, create new
        GameObject newDust = Instantiate(settings.groundDustPrefab, transform);
        return newDust.GetComponent<ParticleSystem>();
    }
    
    private void ReturnDustToPool(ParticleSystem dust)
    {
        if (!settings.useObjectPooling)
        {
            Destroy(dust.gameObject);
            return;
        }
        
        dust.Stop();
        dust.gameObject.SetActive(false);
        dustPool.Enqueue(dust);
        activeDust.Remove(dust);
    }
    
    private void OnDestroy()
    {
        // Clean up active particles
        foreach (var dust in activeDust)
        {
            if (dust != null)
                Destroy(dust.gameObject);
        }
    }
}