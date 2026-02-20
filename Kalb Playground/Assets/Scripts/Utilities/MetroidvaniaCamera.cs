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

public class MetroidvaniaCamera : MonoBehaviour
{
    // ====================================================================
    // SECTION 1: CORE SETTINGS
    // ====================================================================
    
    [Header("Core Settings")]
    public Transform player;
    [SerializeField] private KalbController playerController;
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
    // NEW SECTION: FALL SPEED FOLLOWING
    // ====================================================================
    
    [Header("Fall Speed Following")]
    [SerializeField] private bool enableFallSpeedFollowing = true;
    [SerializeField] private AnimationCurve fallSpeedMultiplier = AnimationCurve.Linear(0, 1, 20, 3);
    [SerializeField] private float minFallSpeed = 5f;
    [SerializeField] private float maxFallSpeed = 30f;
    [SerializeField] private float minCameraMultiplier = 1f;
    [SerializeField] private float maxCameraMultiplier = 3f;
    [SerializeField] private float verticalLeadMultiplier = 1.5f;
    [SerializeField] private float fallResponseTime = 0.1f;
    [SerializeField] private float fallReturnTime = 0.3f;
    
    [Header("Fall Anticipation")]
    [SerializeField] private bool enableFallAnticipation = true;
    [SerializeField] private float anticipationThreshold = 5f;
    [SerializeField] private float anticipationLead = 2f;
    [SerializeField] private AnimationCurve anticipationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Dynamic Vertical Deadzone")]
    [SerializeField] private bool useDynamicDeadzone = true;
    [SerializeField] private float baseVerticalDeadzone = 0.5f;
    [SerializeField] private float maxVerticalDeadzone = 2f;
    [SerializeField] private AnimationCurve deadzoneFallCurve = AnimationCurve.Linear(0, 1, 20, 3);
    
    // Private fall tracking variables
    private float currentFallMultiplier = 1f;
    private float targetFallMultiplier = 1f;
    private float fallMultiplierVelocity = 0f;
    private float previousPlayerY = 0f;
    private float currentVerticalVelocity = 0f;
    private float verticalVelocitySmooth = 0f;
    private float lastGroundedY = 0f;
    private bool wasGrounded = true;
    private float anticipationTimer = 0f;
    private float fallStartTime = 0f;
    private float temporarySpeedMultiplier = 1f;
    private float temporarySpeedTimer = 0f;
    private Coroutine temporarySpeedCoroutine;
    
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
    // NEW SECTION: LOOK UP/DOWN FUNCTIONALITY
    // ====================================================================
    
    [Header("Look Up/Down Settings")]
    [SerializeField] private bool enableLookUpDown = true;
    [SerializeField] private float lookUpOffset = 3.5f;
    [SerializeField] private float lookDownOffset = -2.5f;
    [SerializeField] private float lookSmoothTime = 0.15f;
    [SerializeField] private float returnSmoothTime = 0.2f;
    [SerializeField] private bool invertLook = false;
    
    [Header("Look Input Settings")]
    [SerializeField] private bool useModifierKey = false;
    [SerializeField] private Key lookModifierKey = Key.LeftShift;
    [SerializeField] private bool useSeparateLookKeys = true;
    [SerializeField] private Key lookUpKey = Key.UpArrow;
    [SerializeField] private Key lookDownKey = Key.DownArrow;
    [SerializeField] private float verticalInputThreshold = 0.5f;
    
    [Header("Look Behavior")]
    [SerializeField] private bool autoReturnToCenter = true;
    [SerializeField] private float lookHoldDelay = 0.1f;
    [SerializeField] private bool limitLookToBounds = true;
    
    [Header("Look Conditions")]
    [SerializeField] private bool requireIdleAndGrounded = true;
    
    // Private look variables
    private float currentLookOffset = 0f;
    private float targetLookOffset = 0f;
    private float lookVelocity = 0f;
    private bool isLooking = false;
    private float lookHoldTimer = 0f;
    private Vector3 baseTargetPosition;
    
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
            previousPlayerY = playerPos.y;
            lastGroundedY = playerPos.y;
        }

        // Initialize shake with random offset for Perlin noise
        shakeNoiseOffset = Random.Range(0f, 100f);

        // Store original camera speed
        originalCameraSpeed = cameraSpeed;
    }
    
    void FixedUpdate()
    {
        if (player == null) return;
        
        // Update vertical velocity tracking
        UpdateVerticalVelocity();
        
        // Update fall speed multiplier
        UpdateFallSpeedMultiplier();
        
        // Update look up/down input
        if (enableLookUpDown)
        {
            UpdateLookInput();
        }
        
        // Update screen shake
        UpdateEnhancedScreenShake();
        
        // Calculate base target position with fall speed influence
        Vector3 playerPos = player.position;
        Vector3 cameraPos = transform.position;
        
        // Track grounded state for fall anticipation
        bool isGrounded = playerController != null ? playerController.IsEffectivelyGrounded() : false;
        
        if (isGrounded && !wasGrounded)
        {
            // Player just landed - trigger landing effects
            OnPlayerLanded();
        }
        
        if (isGrounded)
        {
            lastGroundedY = playerPos.y;
            anticipationTimer = 0f;
        }
        else if (wasGrounded && !isGrounded)
        {
            // Player just left ground - start fall tracking
            fallStartTime = Time.time;
        }
        
        wasGrounded = isGrounded;
        
        // Calculate base target with fall speed influence
        float targetY = playerPos.y;
        
        if (enableFallSpeedFollowing && !isGrounded && currentVerticalVelocity < -minFallSpeed)
        {
            // Apply fall speed multiplier to vertical camera movement
            float fallInfluence = Mathf.Abs(currentVerticalVelocity) * currentFallMultiplier * verticalLeadMultiplier;
            
            // Add anticipation for fast falls
            if (enableFallAnticipation && Mathf.Abs(currentVerticalVelocity) > anticipationThreshold)
            {
                float anticipationProgress = Mathf.Clamp01((Time.time - fallStartTime) / 0.5f);
                float extraLead = anticipationLead * anticipationCurve.Evaluate(anticipationProgress);
                fallInfluence += extraLead;
            }
            
            targetY -= fallInfluence * Time.fixedDeltaTime;
        }
        
        baseTargetPosition = new Vector3(playerPos.x, targetY, transform.position.z);
        
        // Apply dynamic deadzone if enabled
        float currentDeadzone = deadzoneRadius;
        if (useDynamicDeadzone && !isGrounded && currentVerticalVelocity < 0)
        {
            float fallRatio = Mathf.Clamp01(Mathf.Abs(currentVerticalVelocity) / maxFallSpeed);
            float deadzoneMultiplier = deadzoneFallCurve.Evaluate(Mathf.Abs(currentVerticalVelocity));
            currentDeadzone = Mathf.Lerp(baseVerticalDeadzone, maxVerticalDeadzone, fallRatio * deadzoneMultiplier);
        }
        
        // Calculate distance from camera center with dynamic deadzone
        float distance = Vector2.Distance(
            new Vector2(playerPos.x, playerPos.y),
            new Vector2(cameraPos.x, cameraPos.y)
        );
        
        // Only move if outside deadzone
        if (distance > currentDeadzone)
        {
            targetPosition = baseTargetPosition;
        }
        
        // Apply look up/down offset
        if (enableLookUpDown)
        {
            float smoothTimeToUse = isLooking ? lookSmoothTime : returnSmoothTime;
            currentLookOffset = Mathf.SmoothDamp(currentLookOffset, targetLookOffset, ref lookVelocity, smoothTimeToUse, Mathf.Infinity, Time.fixedDeltaTime);
            targetPosition.y += currentLookOffset;
        }
        
        // Apply boundaries
        if (useCameraBounds)
        {
            ApplyBoundaries();
        }
        
        // Apply screen shake
        Vector3 finalTargetPosition = targetPosition + shakeOffset;
        
        // Apply camera speed modifier
        float effectiveCameraSpeed = cameraSpeed * currentCameraSpeedModifier * currentFallMultiplier * temporarySpeedMultiplier;
        
        // Smooth movement
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
    // NEW SECTION: FALL SPEED CALCULATION
    // ====================================================================
    
    private void UpdateVerticalVelocity()
    {
        if (player == null) return;
        
        // Calculate raw vertical velocity
        float currentY = player.position.y;
        float rawVelocity = (currentY - previousPlayerY) / Time.fixedDeltaTime;
        previousPlayerY = currentY;
        
        // Smooth the velocity for more stable camera movement
        verticalVelocitySmooth = Mathf.Lerp(verticalVelocitySmooth, rawVelocity, Time.fixedDeltaTime * 10f);
        currentVerticalVelocity = verticalVelocitySmooth;
    }
    
    private void UpdateFallSpeedMultiplier()
    {
        float fallSpeed = Mathf.Abs(currentVerticalVelocity);
        
        // Calculate target multiplier based on fall speed
        if (fallSpeed > minFallSpeed && currentVerticalVelocity < 0) // Only when falling
        {
            float normalizedFallSpeed = Mathf.Clamp01((fallSpeed - minFallSpeed) / (maxFallSpeed - minFallSpeed));
            targetFallMultiplier = Mathf.Lerp(minCameraMultiplier, maxCameraMultiplier, 
                fallSpeedMultiplier.Evaluate(fallSpeed));
        }
        else
        {
            targetFallMultiplier = 1f;
        }
        
        // Smooth the multiplier change
        float smoothTime = (targetFallMultiplier > currentFallMultiplier) ? fallResponseTime : fallReturnTime;
        currentFallMultiplier = Mathf.SmoothDamp(currentFallMultiplier, targetFallMultiplier, 
            ref fallMultiplierVelocity, smoothTime);
    }
    
    private void OnPlayerLanded()
    {
        float fallDistance = Mathf.Abs(player.position.y - lastGroundedY);
        float fallSpeed = Mathf.Abs(currentVerticalVelocity);
        
        // Trigger camera shake on hard landings
        if (fallDistance > 3f || fallSpeed > 10f)
        {
            TriggerHardLandingShake(fallSpeed, fallDistance);
        }
        
        // Quickly reset fall multiplier on landing
        targetFallMultiplier = 1f;
    }
    
    // ====================================================================
    // NEW SECTION: LOOK UP/DOWN INPUT HANDLING
    // ====================================================================
    
    private bool CanLook()
    {
        if (playerController == null) return true;
        
        if (requireIdleAndGrounded)
        {
            bool isIdle = Mathf.Abs(playerController.InputHandler.MoveInput.x) < 0.1f;
            bool isGrounded = playerController.IsEffectivelyGrounded();
            return isIdle && isGrounded;
        }
        
        return true;
    }
    
    private void UpdateLookInput()
    {
        if (Keyboard.current == null && Gamepad.current == null) return;
        
        bool canLook = CanLook();
        bool wantsToLookUp = false;
        bool wantsToLookDown = false;
        
        if (canLook)
        {
            if (Keyboard.current != null)
            {
                bool modifierPressed = !useModifierKey || 
                    (lookModifierKey == Key.LeftShift && Keyboard.current.leftShiftKey.isPressed) ||
                    (lookModifierKey == Key.RightShift && Keyboard.current.rightShiftKey.isPressed);
                
                if (useSeparateLookKeys)
                {
                    wantsToLookUp = Keyboard.current[lookUpKey].isPressed && modifierPressed;
                    wantsToLookDown = Keyboard.current[lookDownKey].isPressed && modifierPressed;
                }
                else
                {
                    wantsToLookUp = (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed) && modifierPressed;
                    wantsToLookDown = (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed) && modifierPressed;
                }
            }
            
            if (Gamepad.current != null)
            {
                Vector2 rightStick = Gamepad.current.rightStick.ReadValue();
                
                if (Mathf.Abs(rightStick.y) > verticalInputThreshold)
                {
                    if (rightStick.y > 0)
                        wantsToLookUp = true;
                    else
                        wantsToLookDown = true;
                }
            }
            
            if (invertLook)
            {
                bool temp = wantsToLookUp;
                wantsToLookUp = wantsToLookDown;
                wantsToLookDown = temp;
            }
        }
        
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
            lookHoldTimer = 0f;
            
            if (autoReturnToCenter)
            {
                targetLookOffset = 0f;
                isLooking = false;
            }
        }
        
        if (limitLookToBounds && useCameraBounds)
        {
            float camHeight = cam.orthographicSize;
            float playerY = player.position.y;
            float topBound = maxBounds.y - camHeight;
            float bottomBound = minBounds.y + camHeight;
            
            float maxLookUp = topBound - playerY;
            float maxLookDown = bottomBound - playerY;
            
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
    // SECTION 13: BOUNDARY SYSTEM
    // ====================================================================
    
    private void ApplyBoundaries()
    {
        if (!useCameraBounds) return;
        
        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;
        
        float leftBound = minBounds.x + camWidth;
        float rightBound = maxBounds.x - camWidth;
        float bottomBound = minBounds.y + camHeight;
        float topBound = maxBounds.y - camHeight;
        
        targetPosition.x = Mathf.Clamp(targetPosition.x, leftBound, rightBound);
        targetPosition.y = Mathf.Clamp(targetPosition.y, bottomBound, topBound);
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
            activeShake.timer -= Time.fixedDeltaTime;
            
            float progress = 1f - (activeShake.timer / activeShake.duration);
            float decay = shakeDecayCurve.Evaluate(progress);
            float currentIntensity = activeShake.intensity * decay;
            
            float time = Time.time * screenShakeFrequency + shakeNoiseOffset;
            
            float shakeX = (Mathf.PerlinNoise(time, 0f) * 2f - 1f) * currentIntensity;
            float shakeY = (Mathf.PerlinNoise(0f, time) * 2f - 1f) * currentIntensity;
            
            if (activeShake.direction != Vector3.zero)
            {
                float directionalBias = 0.7f;
                Vector3 directionalShake = activeShake.direction.normalized * currentIntensity * directionalBias;
                shakeX += directionalShake.x;
                shakeY += directionalShake.y;
            }
            
            shakeOffset = new Vector3(shakeX, shakeY, 0);
            
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
    
    public void TriggerScreenShake(float intensity, float duration, Vector3 direction = default, bool isHardImpact = false)
    {
        if (activeShake.timer > 0 && !isHardImpact)
        {
            if (intensity > activeShake.intensity)
            {
                activeShake.intensity = intensity;
                activeShake.duration = duration;
                activeShake.timer = duration;
                activeShake.direction = direction;
                activeShake.isHardImpact = isHardImpact;
            }
            else if (Mathf.Abs(intensity - activeShake.intensity) < 0.05f)
            {
                activeShake.timer = Mathf.Max(activeShake.timer, duration);
            }
        }
        else
        {
            activeShake = new ShakeData
            {
                intensity = intensity,
                duration = duration,
                timer = duration,
                direction = direction,
                isHardImpact = isHardImpact
            };
        }
        
        if (isHardImpact && enableImpactPause && impactPauseCoroutine == null)
        {
            impactPauseCoroutine = StartCoroutine(ImpactPauseEffect(intensity, duration));
        }
    }
    
    public void TriggerHardLandingShake(float fallSpeed, float fallDistance)
    {
        float normalizedFallSpeed = Mathf.Clamp01(Mathf.Abs(fallSpeed) / 30f);
        float normalizedFallDistance = Mathf.Clamp01(fallDistance / 10f);
        
        float impactFactor = (normalizedFallSpeed * 0.7f) + (normalizedFallDistance * 0.3f);
        
        float intensity = Mathf.Lerp(0.15f, 0.35f, impactFactor);
        float duration = Mathf.Lerp(0.2f, 0.4f, impactFactor);
        
        Vector3 direction = new Vector3(0, 0.8f, 0);
        
        float pauseStrength = Mathf.Lerp(0.3f, 0.05f, impactFactor);
        
        impactPauseDuration = Mathf.Lerp(0.08f, 0.15f, impactFactor);
        impactPauseStrength = pauseStrength;
        
        TriggerScreenShake(intensity, duration, direction, true);
    }
    
    private IEnumerator ImpactPauseEffect(float intensity, float duration)
    {
        if (!enableImpactPause) yield break;
        
        float pauseTimer = 0f;
        float originalModifier = currentCameraSpeedModifier;
        
        while (pauseTimer < impactPauseDuration)
        {
            pauseTimer += Time.fixedDeltaTime;
            float progress = pauseTimer / impactPauseDuration;
            
            currentCameraSpeedModifier = Mathf.Lerp(impactPauseStrength, 1f, 
                pauseRecoveryCurve.Evaluate(progress));
            
            yield return new WaitForFixedUpdate();
        }
        
        float recoveryTimer = 0f;
        float recoveryDuration = 0.1f;
        
        while (recoveryTimer < recoveryDuration)
        {
            recoveryTimer += Time.fixedDeltaTime;
            float progress = recoveryTimer / recoveryDuration;
            
            currentCameraSpeedModifier = Mathf.Lerp(currentCameraSpeedModifier, 1f, progress);
            
            yield return new WaitForFixedUpdate();
        }
        
        currentCameraSpeedModifier = 1f;
        impactPauseCoroutine = null;
    }
    
    // ====================================================================
    // PUBLIC METHODS
    // ====================================================================
    
    public void SetLookOffset(float offset)
    {
        targetLookOffset = offset;
        isLooking = (offset != 0);
    }
    
    public void ResetLook()
    {
        targetLookOffset = 0f;
        isLooking = false;
    }
    
    public float GetCurrentLookOffset()
    {
        return currentLookOffset;
    }
    
    public bool IsLooking()
    {
        return isLooking;
    }
    
    public float GetCurrentFallMultiplier()
    {
        return currentFallMultiplier;
    }
    
    public float GetCurrentVerticalVelocity()
    {
        return currentVerticalVelocity;
    }

    public void SetTemporaryFollowSpeed(float multiplier, float duration)
    {
        if (temporarySpeedCoroutine != null)
            StopCoroutine(temporarySpeedCoroutine);
        
        temporarySpeedCoroutine = StartCoroutine(TemporaryFollowSpeedRoutine(multiplier, duration));
    }

    private IEnumerator TemporaryFollowSpeedRoutine(float multiplier, float duration)
    {
        temporarySpeedMultiplier = multiplier;
        temporarySpeedTimer = duration;
        
        while (temporarySpeedTimer > 0)
        {
            temporarySpeedTimer -= Time.deltaTime;
            yield return null;
        }
        
        temporarySpeedMultiplier = 1f;
        temporarySpeedCoroutine = null;
    }
    
    // ====================================================================
    // SECTION 18: DEBUG & VISUALIZATION
    // ====================================================================
    
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            Camera editorCam = GetComponent<Camera>();
            if (editorCam == null) editorCam = Camera.main;
            
            if (editorCam != null)
            {
                float camHeight = editorCam.orthographicSize;
                float camWidth = camHeight * editorCam.aspect;
                
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
            
            Gizmos.DrawLine(
                new Vector3(playerPos.x - 0.5f, playerPos.y + lookUpOffset, 0),
                new Vector3(playerPos.x + 0.5f, playerPos.y + lookUpOffset, 0)
            );
            
            Gizmos.DrawLine(
                new Vector3(playerPos.x - 0.5f, playerPos.y + lookDownOffset, 0),
                new Vector3(playerPos.x + 0.5f, playerPos.y + lookDownOffset, 0)
            );
            
            if (Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(
                    new Vector3(playerPos.x - 0.5f, playerPos.y + currentLookOffset, 0),
                    new Vector3(playerPos.x + 0.5f, playerPos.y + currentLookOffset, 0)
                );
            }
        }
        
        // Draw fall speed multiplier debug
        if (Application.isPlaying && player != null)
        {
            Gizmos.color = Color.magenta;
            float multiplierSize = currentFallMultiplier * 0.5f;
            Gizmos.DrawWireSphere(player.position + Vector3.up * 2f, multiplierSize);
            
            // Draw velocity vector
            Gizmos.color = Color.blue;
            Vector3 velocityEnd = player.position + new Vector3(0, currentVerticalVelocity * 0.1f, 0);
            Gizmos.DrawLine(player.position, velocityEnd);
        }
    }
}