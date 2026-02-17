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
    [SerializeField] private KalbController playerController; // Reference to player controller
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
    public float impactPauseDuration = 0.1f;
    public float impactPauseStrength = 0.05f;
    public AnimationCurve pauseRecoveryCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool enableImpactPause = true;
    
    // ====================================================================
    // NEW SECTION: LOOK UP/DOWN FUNCTIONALITY
    // ====================================================================
    
    [Header("Look Up/Down Settings")]
    [SerializeField] private bool enableLookUpDown = true;
    [SerializeField] private float lookUpOffset = 3.5f;      // How far up to look
    [SerializeField] private float lookDownOffset = -2.5f;   // How far down to look
    [SerializeField] private float lookSmoothTime = 0.15f;   // Smoothing for look movement
    [SerializeField] private float returnSmoothTime = 0.2f;  // Smoothing when returning to center
    [SerializeField] private bool invertLook = false;        // Invert up/down direction
    
    [Header("Look Input Settings")]
    [SerializeField] private bool useModifierKey = false;    // Require modifier key (Shift/Ctrl)
    [SerializeField] private Key lookModifierKey = Key.LeftShift; // Modifier key if enabled
    [SerializeField] private bool useSeparateLookKeys = true; // Use dedicated up/down keys
    [SerializeField] private Key lookUpKey = Key.UpArrow;     // Key to look up
    [SerializeField] private Key lookDownKey = Key.DownArrow; // Key to look down
    [SerializeField] private float verticalInputThreshold = 0.5f; // Threshold for gamepad stick
    
    [Header("Look Behavior")]
    [SerializeField] private bool autoReturnToCenter = true;  // Auto return when not looking
    [SerializeField] private float lookHoldDelay = 0.1f;      // Delay before starting look
    [SerializeField] private bool limitLookToBounds = true;   // Keep look within camera bounds
    
    // NEW: Condition settings
    [Header("Look Conditions")]
    [SerializeField] private bool requireIdleAndGrounded = true; // Only look when idle & grounded
    
    // Private look variables
    private float currentLookOffset = 0f;
    private float targetLookOffset = 0f;
    private float lookVelocity = 0f;
    private bool isLooking = false;
    private float lookHoldTimer = 0f;
    private Vector3 baseTargetPosition;
    
    // ====================================================================
    // SECTION 2: BASIC FOLLOW (Unlocked by default)
    // ====================================================================
    
    [Header("Basic Follow")]
    [Tooltip("Deadzone radius where camera doesn't move")]
    public float deadzoneRadius = 0.5f;
    
    [Tooltip("How quickly camera responds to player movement")]
    public float responsiveness = 5f;

    [Header("Camera Smoothing")]
    public float cameraSmoothTime = 0.1f;
    public float maxCameraSpeed = 15f;
    
    private Vector3 currentVelocity = Vector3.zero;    
    private Vector3 velocity = Vector3.zero;
    private Camera cam;
    private Vector2 cameraHalfSize;
    
    // Multiple shake sources support
    private struct ShakeData
    {
        public float intensity;
        public float duration;
        public float timer;
        public Vector3 direction;
        public bool isHardImpact;
    }
    
    private ShakeData activeShake;
    private Vector3 shakeOffset = Vector3.zero;
    private float shakeNoiseOffset;
    private float originalCameraSpeed;
    private float currentCameraSpeedModifier = 1f;
    private Coroutine impactPauseCoroutine;
    
    // Private fields
    private Vector3 targetPosition;
    private Vector3 smoothedPosition;
    
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
        
        // Try to get player controller if not assigned
        if (playerController == null && player != null)
        {
            playerController = player.GetComponent<KalbController>();
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
            baseTargetPosition = targetPosition;
        }

        // Initialize shake with random offset for Perlin noise
        shakeNoiseOffset = Random.Range(0f, 100f);

        // Store original camera speed
        originalCameraSpeed = cameraSpeed;
    }
    
    void FixedUpdate()
    {
        if (player == null) return;
        
        // Update look up/down input with conditions
        if (enableLookUpDown)
        {
            UpdateLookInput();
        }
        
        // Update screen shake FIRST
        UpdateEnhancedScreenShake();
        
        // Calculate base target position (player position)
        Vector3 playerPos = player.position;
        Vector3 cameraPos = transform.position;
        
        // Calculate distance from camera center
        float distance = Vector2.Distance(
            new Vector2(playerPos.x, playerPos.y),
            new Vector2(cameraPos.x, cameraPos.y)
        );
        
        // Base target is player position
        baseTargetPosition = new Vector3(playerPos.x, playerPos.y, transform.position.z);
        
        // Only move if outside deadzone
        if (distance > deadzoneRadius)
        {
            targetPosition = baseTargetPosition;
        }
        
        // Apply look up/down offset to target Y position
        if (enableLookUpDown)
        {
            // Smooth the look offset
            float smoothTimeToUse = isLooking ? lookSmoothTime : returnSmoothTime;
            currentLookOffset = Mathf.SmoothDamp(currentLookOffset, targetLookOffset, ref lookVelocity, smoothTimeToUse, Mathf.Infinity, Time.fixedDeltaTime);
            
            // Apply the offset to target position
            targetPosition.y += currentLookOffset;
        }
        
        // Apply boundaries (including look offset limits)
        if (useCameraBounds)
        {
            ApplyBoundaries();
        }
        
        // Apply screen shake offset to target position
        Vector3 finalTargetPosition = targetPosition + shakeOffset;
        
        // Apply camera speed modifier for impact pauses
        float effectiveCameraSpeed = cameraSpeed * currentCameraSpeedModifier;
        
        // Smooth movement using SmoothDamp with modified speed
        smoothedPosition = Vector3.SmoothDamp(
            transform.position, 
            finalTargetPosition, 
            ref currentVelocity, 
            1f / effectiveCameraSpeed,
            maxCameraSpeed,
            Time.fixedDeltaTime
        );
        
        // Round to prevent sub-pixel movement
        smoothedPosition.x = Mathf.Round(smoothedPosition.x * 100f) / 100f;
        smoothedPosition.y = Mathf.Round(smoothedPosition.y * 100f) / 100f;
        
        transform.position = smoothedPosition;
    }
    
    // ====================================================================
    // NEW SECTION: LOOK UP/DOWN INPUT HANDLING
    // ====================================================================
    
    private bool CanLook()
    {
        // If we don't have player controller, default to allowing look
        if (playerController == null) return true;
        
        // Check if we require idle and grounded
        if (requireIdleAndGrounded)
        {
            // Check if player is idle (not moving horizontally) and grounded
            bool isIdle = Mathf.Abs(playerController.InputHandler.MoveInput.x) < 0.1f;
            bool isGrounded = playerController.IsEffectivelyGrounded();
            
            // Also check if player is in idle state specifically (optional, more precise)
            bool isInIdleState = playerController.IdleState != null && 
                                 playerController.GetType().GetField("stateMachine")?.GetValue(playerController) is KalbStateMachine stateMachine &&
                                 stateMachine.CurrentState is KalbIdleState;
            
            // For platformers, being grounded with no input is usually sufficient
            return isIdle && isGrounded;
        }
        
        return true;
    }
    
    private void UpdateLookInput()
    {
        // Skip if Keyboard.current is not available
        if (Keyboard.current == null && Gamepad.current == null) return;
        
        // Check if we can look based on player state
        bool canLook = CanLook();
        
        bool wantsToLookUp = false;
        bool wantsToLookDown = false;
        
        // Only process input if we can look
        if (canLook)
        {
            // Check keyboard input
            if (Keyboard.current != null)
            {
                // Check modifier key if required
                bool modifierPressed = !useModifierKey || 
                    (lookModifierKey == Key.LeftShift && Keyboard.current.leftShiftKey.isPressed) ||
                    (lookModifierKey == Key.RightShift && Keyboard.current.rightShiftKey.isPressed) ||
                    (lookModifierKey == Key.LeftCtrl && Keyboard.current.leftCtrlKey.isPressed) ||
                    (lookModifierKey == Key.RightCtrl && Keyboard.current.rightCtrlKey.isPressed);
                
                if (useSeparateLookKeys)
                {
                    // Dedicated look keys
                    wantsToLookUp = Keyboard.current[lookUpKey].isPressed && modifierPressed;
                    wantsToLookDown = Keyboard.current[lookDownKey].isPressed && modifierPressed;
                }
                else
                {
                    // Use vertical arrows with optional modifier
                    wantsToLookUp = (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed) && modifierPressed;
                    wantsToLookDown = (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed) && modifierPressed;
                }
            }
            
            // Check gamepad input if available
            if (Gamepad.current != null)
            {
                Vector2 rightStick = Gamepad.current.rightStick.ReadValue();
                
                // Use right stick for look (more natural for gamepad)
                if (Mathf.Abs(rightStick.y) > verticalInputThreshold)
                {
                    if (rightStick.y > 0)
                        wantsToLookUp = true;
                    else
                        wantsToLookDown = true;
                }
            }
            
            // Apply inversion
            if (invertLook)
            {
                bool temp = wantsToLookUp;
                wantsToLookUp = wantsToLookDown;
                wantsToLookDown = temp;
            }
        }
        
        // Update look state with hold timer
        if ((wantsToLookUp || wantsToLookDown) && canLook)
        {
            lookHoldTimer += Time.fixedDeltaTime;
            
            if (lookHoldTimer >= lookHoldDelay)
            {
                if (wantsToLookUp)
                {
                    targetLookOffset = lookUpOffset;
                    isLooking = true;
                }
                else if (wantsToLookDown)
                {
                    targetLookOffset = lookDownOffset;
                    isLooking = true;
                }
            }
        }
        else
        {
            // No input or can't look - reset look
            lookHoldTimer = 0f;
            
            if (autoReturnToCenter)
            {
                targetLookOffset = 0f;
                isLooking = false;
            }
        }
        
        // Ensure look offset stays within camera bounds if enabled
        if (limitLookToBounds && useCameraBounds)
        {
            float camHeight = cam.orthographicSize;
            float playerY = player.position.y;
            float topBound = maxBounds.y - camHeight;
            float bottomBound = minBounds.y + camHeight;
            
            // Calculate maximum possible look offset while staying in bounds
            float maxLookUp = topBound - playerY;
            float maxLookDown = bottomBound - playerY;
            
            // Clamp target offset
            if (targetLookOffset > 0)
            {
                targetLookOffset = Mathf.Min(targetLookOffset, maxLookUp);
            }
            else if (targetLookOffset < 0)
            {
                targetLookOffset = Mathf.Max(targetLookOffset, maxLookDown);
            }
        }
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
        // Draw camera center bounds
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
                float directionalBias = 0.7f;
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
        float intensity = Mathf.Lerp(0.15f, 0.35f, impactFactor);
        float duration = Mathf.Lerp(0.2f, 0.4f, impactFactor);
        
        // Add strong upward bias for hard landings
        Vector3 direction = new Vector3(0, 0.8f, 0);
        
        // Calculate pause strength based on impact
        float pauseStrength = Mathf.Lerp(0.3f, 0.05f, impactFactor);
        
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
        float recoveryDuration = 0.1f;
        
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
        
        // 2. Camera pause (slows camera follow without affecting game time)
        float pauseTime = Mathf.Lerp(0.1f, 0.2f, impactFactor);
        float pauseStrength = Mathf.Lerp(0.2f, 0.05f, impactFactor);
        
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
    // PUBLIC METHODS FOR LOOK UP/DOWN
    // ====================================================================
    
    /// <summary>
    /// Manually set the look offset (for scripted camera movements)
    /// </summary>
    public void SetLookOffset(float offset)
    {
        targetLookOffset = offset;
        isLooking = (offset != 0);
    }
    
    /// <summary>
    /// Reset look to center
    /// </summary>
    public void ResetLook()
    {
        targetLookOffset = 0f;
        isLooking = false;
    }
    
    /// <summary>
    /// Get current look offset value
    /// </summary>
    public float GetCurrentLookOffset()
    {
        return currentLookOffset;
    }
    
    /// <summary>
    /// Check if camera is currently looking up/down
    /// </summary>
    public bool IsLooking()
    {
        return isLooking;
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
        
        // Draw look up/down range
        if (enableLookUpDown && player != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 playerPos = player.position;
            
            // Draw look up range
            Gizmos.DrawLine(
                new Vector3(playerPos.x - 0.5f, playerPos.y + lookUpOffset, 0),
                new Vector3(playerPos.x + 0.5f, playerPos.y + lookUpOffset, 0)
            );
            
            // Draw look down range
            Gizmos.DrawLine(
                new Vector3(playerPos.x - 0.5f, playerPos.y + lookDownOffset, 0),
                new Vector3(playerPos.x + 0.5f, playerPos.y + lookDownOffset, 0)
            );
            
            // Draw current look offset
            if (Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(
                    new Vector3(playerPos.x - 0.5f, playerPos.y + currentLookOffset, 0),
                    new Vector3(playerPos.x + 0.5f, playerPos.y + currentLookOffset, 0)
                );
            }
        }
    }
}