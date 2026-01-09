using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public enum CameraFollowMode
{
    Basic,
    Advanced,
    HollowKnightStyle
}

[System.Serializable]
public enum CameraPriority
{
    PlayerPosition,
    PlayerVelocity,
    LookAhead,
    CustomTarget
}

[System.Serializable]
public class CameraUpgradeData
{
    public string upgradeName;
    public string description;
    public int cost;
    public bool unlocked = false;
}

public class MetroidvaniaCamera : MonoBehaviour
{
    // ====================================================================
    // SECTION 1: CORE SETTINGS
    // ====================================================================
    
    [Header("Core Settings")]
    public Transform player;
    public CameraFollowMode followMode = CameraFollowMode.Basic;
    public float cameraSpeed = 5f;
    
    [Header("Camera Boundaries")]
    public bool useCameraBounds = true;
    public Vector2 minBounds = new Vector2(-10, -10);
    public Vector2 maxBounds = new Vector2(10, 10);
    public Collider2D cameraBoundsCollider;
    
    [Header("Screen Shake")]
    public float screenShakeDamping = 1.0f;
    private Vector3 screenShakeOffset = Vector3.zero;
    private float screenShakeTimer = 0f;

    [Header("Enhanced Screen Shake")]
    public float screenShakeIntensity = 0.15f;
    public float screenShakeDuration = 0.25f;
    public float screenShakeFrequency = 60f;
    public AnimationCurve shakeDecayCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    [Header("Impact Effects")]
    public float impactPauseDuration = 0.1f; // How long to pause camera follow
    public float impactPauseStrength = 0.05f; // How much to slow down (0 = stop, 1 = normal)
    public AnimationCurve pauseRecoveryCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool enableImpactPause = true;
    
    // Multiple shake sources support
    private struct ShakeData
    {
        public float intensity;
        public float duration;
        public float timer;
        public Vector3 direction;
        public bool isHardImpact; // Flag for hard landings
    }
    
    private ShakeData activeShake;
    private Vector3 shakeOffset = Vector3.zero;
    private float shakeNoiseOffset;
    private float originalCameraSpeed;
    private float currentCameraSpeedModifier = 1f;
    private Coroutine impactPauseCoroutine;

    
    
    // ====================================================================
    // SECTION 2: BASIC FOLLOW (Unlocked by default)
    // ====================================================================
    
    [Header("Basic Follow")]
    [Tooltip("Deadzone radius where camera doesn't move")]
    public float deadzoneRadius = 0.5f;
    
    [Tooltip("How quickly camera responds to player movement")]
    public float responsiveness = 5f;

    [Header("Camera Smoothing")]
    public float cameraSmoothTime = 0.1f; // Lower = faster, Higher = slower
    public float maxCameraSpeed = 15f; // Limit maximum speed
    
    private Vector3 currentVelocity = Vector3.zero;    
    private Vector3 velocity = Vector3.zero;
    private Camera cam;
    private Vector2 cameraHalfSize;
    
    // ====================================================================
    // SECTION 10: INITIALIZATION
    // ====================================================================
    
    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
        
        // Calculate camera half size in world units
        float height = cam.orthographicSize;
        float width = height * cam.aspect;
        cameraHalfSize = new Vector2(width, height);
        
        // Initialize target position
        if (player != null)
        {
            Vector3 playerPos = player.position;
            targetPosition = new Vector3(playerPos.x, playerPos.y, transform.position.z);
            transform.position = targetPosition;
        }

        // Initialize shake with random offset for Perlin noise
        shakeNoiseOffset = Random.Range(0f, 100f);

        // Store original camera speed
        originalCameraSpeed = cameraSpeed;
    }
    
    void FixedUpdate()
    {
        if (player == null) return;
        
        // Update screen shake FIRST (before calculating target position)
        UpdateEnhancedScreenShake();
        
        // Calculate target position
        Vector3 playerPos = player.position;
        Vector3 cameraPos = transform.position;
        
        // Calculate distance from camera center
        float distance = Vector2.Distance(
            new Vector2(playerPos.x, playerPos.y),
            new Vector2(cameraPos.x, cameraPos.y)
        );
        
        // Only move if outside deadzone
        if (distance > deadzoneRadius)
        {
            targetPosition = new Vector3(playerPos.x, playerPos.y, transform.position.z);
        }
        
        // Apply boundaries
        if (useCameraBounds)
        {
            ApplyBoundaries();
        }
        
        // Apply screen shake offset to target position
        Vector3 finalTargetPosition = targetPosition + shakeOffset;
        
        // Apply camera speed modifier for impact pauses
        float effectiveCameraSpeed = cameraSpeed * currentCameraSpeedModifier;
        
        // Smooth movement using SmoothDamp with modified speed
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position, 
            finalTargetPosition, 
            ref currentVelocity, 
            1f / effectiveCameraSpeed, // Use effective speed
            maxCameraSpeed,
            Time.fixedDeltaTime
        );
        
        // Round to prevent sub-pixel movement
        smoothedPosition.x = Mathf.Round(smoothedPosition.x * 100f) / 100f;
        smoothedPosition.y = Mathf.Round(smoothedPosition.y * 100f) / 100f;
        
        transform.position = smoothedPosition;
    }
    
    // ====================================================================
    // SECTION 13: BOUNDARY SYSTEM - FIXED VERSION
    // ====================================================================
    
    private void ApplyBoundaries()
    {
        if (!useCameraBounds) return;
        
        // Calculate camera bounds in world space
        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;
        
        // Calculate effective bounds (limiting camera edges, not center)
        float leftBound = minBounds.x + camWidth;
        float rightBound = maxBounds.x - camWidth;
        float bottomBound = minBounds.y + camHeight;
        float topBound = maxBounds.y - camHeight;
        
        // Clamp target position so camera edges stay within bounds
        targetPosition.x = Mathf.Clamp(targetPosition.x, leftBound, rightBound);
        targetPosition.y = Mathf.Clamp(targetPosition.y, bottomBound, topBound);
        
        // Debug visualization
        DebugDrawBounds(leftBound, rightBound, bottomBound, topBound);
    }
    
    private void DebugDrawBounds(float left, float right, float bottom, float top)
    {
        // Draw camera center bounds (what you had before)
        Debug.DrawLine(new Vector3(minBounds.x, minBounds.y, 0), new Vector3(maxBounds.x, minBounds.y, 0), Color.green);
        Debug.DrawLine(new Vector3(maxBounds.x, minBounds.y, 0), new Vector3(maxBounds.x, maxBounds.y, 0), Color.green);
        Debug.DrawLine(new Vector3(maxBounds.x, maxBounds.y, 0), new Vector3(minBounds.x, maxBounds.y, 0), Color.green);
        Debug.DrawLine(new Vector3(minBounds.x, maxBounds.y, 0), new Vector3(minBounds.x, minBounds.y, 0), Color.green);
        
        // Draw camera edge bounds (actual limits)
        Debug.DrawLine(new Vector3(left, bottom, 0), new Vector3(right, bottom, 0), Color.yellow);
        Debug.DrawLine(new Vector3(right, bottom, 0), new Vector3(right, top, 0), Color.yellow);
        Debug.DrawLine(new Vector3(right, top, 0), new Vector3(left, top, 0), Color.yellow);
        Debug.DrawLine(new Vector3(left, top, 0), new Vector3(left, bottom, 0), Color.yellow);
    }
    
    // ====================================================================
    // SECTION 14: SCREEN SHAKE SYSTEM
    // ====================================================================
    
    public void TriggerScreenShake(float intensity, float duration)
    {
        screenShakeIntensity = Mathf.Max(screenShakeIntensity, intensity);
        screenShakeDuration = Mathf.Max(screenShakeDuration, duration);
        screenShakeTimer = duration;
    }
    
    private void UpdateEnhancedScreenShake()
    {
        if (activeShake.timer > 0)
        {
            // Reduce timer
            activeShake.timer -= Time.fixedDeltaTime;
            
            // Calculate progress (0 to 1)
            float progress = 1f - (activeShake.timer / activeShake.duration);
            
            // Apply decay curve
            float decay = shakeDecayCurve.Evaluate(progress);
            float currentIntensity = activeShake.intensity * decay;
            
            // Generate Perlin noise-based shake (smoother than random)
            float time = Time.time * screenShakeFrequency + shakeNoiseOffset;
            
            // Create shake in all directions
            float shakeX = (Mathf.PerlinNoise(time, 0f) * 2f - 1f) * currentIntensity;
            float shakeY = (Mathf.PerlinNoise(0f, time) * 2f - 1f) * currentIntensity;
            
            // Apply optional directional bias
            if (activeShake.direction != Vector3.zero)
            {
                float directionalBias = 0.7f; // 70% in the direction, 30% random
                Vector3 directionalShake = activeShake.direction.normalized * currentIntensity * directionalBias;
                shakeX += directionalShake.x;
                shakeY += directionalShake.y;
            }
            
            shakeOffset = new Vector3(shakeX, shakeY, 0);
            
            // If shake ended, reset
            if (activeShake.timer <= 0)
            {
                activeShake.timer = 0;
                shakeOffset = Vector3.zero;
            }
        }
        else
        {
            shakeOffset = Vector3.zero;
        }
    }
    
    // ====================================================================
    // ENHANCED SCREEN SHAKE WITH IMPACT PAUSE
    // ====================================================================
    
    public void TriggerScreenShake(float intensity, float duration, Vector3 direction = default, bool isHardImpact = false)
    {
        // For hard landings, we want to combine or override existing shakes
        if (activeShake.timer > 0 && !isHardImpact)
        {
            // If new shake is stronger, override
            if (intensity > activeShake.intensity)
            {
                activeShake.intensity = intensity;
                activeShake.duration = duration;
                activeShake.timer = duration;
                activeShake.direction = direction;
                activeShake.isHardImpact = isHardImpact;
            }
            // If similar intensity, extend duration
            else if (Mathf.Abs(intensity - activeShake.intensity) < 0.05f)
            {
                activeShake.timer = Mathf.Max(activeShake.timer, duration);
            }
        }
        else
        {
            // Start new shake
            activeShake = new ShakeData
            {
                intensity = intensity,
                duration = duration,
                timer = duration,
                direction = direction,
                isHardImpact = isHardImpact
            };
        }
        
        // If this is a hard impact and pause is enabled, trigger pause effect
        if (isHardImpact && enableImpactPause && impactPauseCoroutine == null)
        {
            impactPauseCoroutine = StartCoroutine(ImpactPauseEffect(intensity, duration));
        }
    }
    
    // Special shake for hard landings with built-in pause
    public void TriggerHardLandingShake(float fallSpeed, float fallDistance)
    {
        // Calculate shake intensity based on fall impact
        float normalizedFallSpeed = Mathf.Clamp01(Mathf.Abs(fallSpeed) / 30f);
        float normalizedFallDistance = Mathf.Clamp01(fallDistance / 10f);
        
        // Combined impact factor (weighted toward speed)
        float impactFactor = (normalizedFallSpeed * 0.7f) + (normalizedFallDistance * 0.3f);
        
        // Scale intensity and duration based on impact
        float intensity = Mathf.Lerp(0.15f, 0.35f, impactFactor); // Increased range
        float duration = Mathf.Lerp(0.2f, 0.4f, impactFactor);
        
        // Add strong upward bias for hard landings
        Vector3 direction = new Vector3(0, 0.8f, 0); // 80% upward bias
        
        // Calculate pause strength based on impact
        float pauseStrength = Mathf.Lerp(0.3f, 0.05f, impactFactor); // Lower = stronger pause
        
        // Update pause settings based on impact
        impactPauseDuration = Mathf.Lerp(0.08f, 0.15f, impactFactor);
        impactPauseStrength = pauseStrength;
        
        // Trigger with upward bias and hard impact flag
        TriggerScreenShake(intensity, duration, direction, true);
    }
    
    // Impact pause effect - slows down camera movement briefly
    private IEnumerator ImpactPauseEffect(float intensity, float duration)
    {
        if (!enableImpactPause) yield break;
        
        float pauseTimer = 0f;
        float originalModifier = currentCameraSpeedModifier;
        
        // Initial strong pause (camera almost stops)
        while (pauseTimer < impactPauseDuration)
        {
            pauseTimer += Time.fixedDeltaTime;
            float progress = pauseTimer / impactPauseDuration;
            
            // Apply pause strength - camera moves very slowly
            currentCameraSpeedModifier = Mathf.Lerp(impactPauseStrength, 1f, 
                pauseRecoveryCurve.Evaluate(progress));
            
            yield return new WaitForFixedUpdate();
        }
        
        // Smooth recovery to normal speed
        float recoveryTimer = 0f;
        float recoveryDuration = 0.1f; // Brief recovery period
        
        while (recoveryTimer < recoveryDuration)
        {
            recoveryTimer += Time.fixedDeltaTime;
            float progress = recoveryTimer / recoveryDuration;
            
            currentCameraSpeedModifier = Mathf.Lerp(currentCameraSpeedModifier, 1f, progress);
            
            yield return new WaitForFixedUpdate();
        }
        
        // Ensure back to normal
        currentCameraSpeedModifier = 1f;
        impactPauseCoroutine = null;
    }
    
    // Alternative: Frame-freeze effect (more dramatic)
    public void TriggerHardLandingWithFreeze(float fallSpeed, float fallDistance)
    {
        StartCoroutine(HardLandingWithFreezeCoroutine(fallSpeed, fallDistance));
    }
    
    private IEnumerator HardLandingWithFreezeCoroutine(float fallSpeed, float fallDistance)
    {
        // Calculate impact strength
        float impactFactor = Mathf.Clamp01((Mathf.Abs(fallSpeed) + fallDistance) / 40f);
        
        // 1. Brief freeze (optional - comment out if you don't want to affect game time)
        // float freezeTime = Mathf.Lerp(0.03f, 0.07f, impactFactor);
        // Time.timeScale = 0.1f; // Slow down time
        // yield return new WaitForSecondsRealtime(freezeTime * 0.5f);
        // Time.timeScale = 1f; // Resume time
        
        // 2. Camera pause (slows camera follow without affecting game time)
        float pauseTime = Mathf.Lerp(0.1f, 0.2f, impactFactor);
        float pauseStrength = Mathf.Lerp(0.2f, 0.05f, impactFactor); // Lower = stronger pause
        
        // Store original values
        float originalPauseDuration = impactPauseDuration;
        float originalPauseStrength = impactPauseStrength;
        
        // Set temporary values
        impactPauseDuration = pauseTime;
        impactPauseStrength = pauseStrength;
        
        // Trigger the shake with pause
        TriggerHardLandingShake(fallSpeed, fallDistance);
        
        // Wait for pause to complete
        yield return new WaitForSeconds(pauseTime + 0.1f);
        
        // Restore original values
        impactPauseDuration = originalPauseDuration;
        impactPauseStrength = originalPauseStrength;
    }
    
    // Public method to stop any active pause
    public void StopImpactPause()
    {
        if (impactPauseCoroutine != null)
        {
            StopCoroutine(impactPauseCoroutine);
            impactPauseCoroutine = null;
        }
        currentCameraSpeedModifier = 1f;
    }
    
    // ====================================================================
    // SECTION 18: DEBUG & VISUALIZATION
    // ====================================================================
    
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            // In editor, approximate camera size
            Camera editorCam = GetComponent<Camera>();
            if (editorCam == null) editorCam = Camera.main;
            
            if (editorCam != null)
            {
                float camHeight = editorCam.orthographicSize;
                float camWidth = camHeight * editorCam.aspect;
                
                // Draw camera edge bounds
                if (useCameraBounds)
                {
                    float leftBound = minBounds.x + camWidth;
                    float rightBound = maxBounds.x - camWidth;
                    float bottomBound = minBounds.y + camHeight;
                    float topBound = maxBounds.y - camHeight;
                    
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(
                        new Vector3((leftBound + rightBound) * 0.5f, (bottomBound + topBound) * 0.5f, 0),
                        new Vector3(rightBound - leftBound, topBound - bottomBound, 0)
                    );
                }
            }
        }
        
        // Draw original bounds
        if (useCameraBounds)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(
                new Vector3((minBounds.x + maxBounds.x) * 0.5f, (minBounds.y + maxBounds.y) * 0.5f, 0),
                new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0)
            );
        }
        
        // Draw deadzone
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, deadzoneRadius);
    }
    
    // Private fields that were missing
    private Vector3 targetPosition;
    private Vector3 smoothedPosition;
}