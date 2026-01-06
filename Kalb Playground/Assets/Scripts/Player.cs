using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    // ====================================================================
    // INSPECTOR CONFIGURATION - PUBLIC VARIABLES
    // ====================================================================
    
    [Header("Ability Unlocks")]
    public bool runUnlocked = false;
    public bool dashUnlocked = false;
    public bool wallJumpUnlocked = false;
    public bool doubleJumpUnlocked = false;
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpForce = 12f;
    [Range(0, 0.3f)] public float movementSmoothing = 0.05f;
    
    [Header("Jump Timing")]
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.1f;
    public float jumpCutMultiplier = 0.5f;
    
    [Header("Double Jump")]
    public bool hasDoubleJump = true;
    public float doubleJumpForce = 10f;
    
    [Header("Air Control")]
    public float airControlMultiplier = 0.5f;
    public float maxAirSpeed = 10f;
    public float airAcceleration = 15f;
    
    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.5f;
    public bool canAirDash = true;
    public bool resetAirDashOnGround = true;
    public int maxAirDashes = 1;
    
    [Header("Wall Slide & Jump Settings")]
    public float wallSlideSpeed = 2f;
    public float wallJumpForce = 11f;
    public Vector2 wallJumpAngle = new Vector2(1, 2);
    public float wallJumpDuration = 0.2f;
    public float wallStickTime = 0.25f;
    
    [Header("Wall Cling Settings")]
    public float wallClingTime = 0.2f;
    public float wallClingSlowdown = 0.3f;
    
    [Header("Falling & Landing Settings")]
    public float maxFallSpeed = -20f;
    public float hardLandingThreshold = -15f;
    public float hardLandingStunTime = 0.3f;
    public float fallingGravityScale = 2.5f;
    public float normalGravityScale = 2f;
    public float quickFallGravityMultiplier = 1.2f;
    
    [Header("Screen-Height Hard Landing")]
    public bool useScreenHeightForHardLanding = true; // Toggle between methods
    public float minScreenHeightForHardLanding = 0.8f; // 80% of screen height
    public float screenHeightDetectionOffset = 1.0f; // Camera tracking offset
    [Tooltip("If false, uses velocity threshold only")]
    public bool requireBothConditions = true; // Need screen height AND velocity
    
    [Header("Screen Shake Settings")]
    public bool enableScreenShake = true;
    public float hardLandingShakeIntensity = 0.15f;
    public float hardLandingShakeDuration = 0.25f;
    
    [Header("Attack Settings")]
    public float attackCooldown = 0.5f;
    public float attackDuration = 0.2f;
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;
    public int attackDamage = 20;
    
    [Header("Environment Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public Transform wallCheck;
    public float wallCheckDistance = 0.05f;
    public float wallCheckOffset = 0.02f;
    public LayerMask environmentLayer;

    [Header("Swimming Settings")]
    public float swimSpeed = 3f;
    public float swimFastSpeed = 5f;
    public float swimDashSpeed = 10f;
    public float swimJumpForce = 8f;
    public float waterSurfaceOffset = 1.20f; // How much of the face is above water
    public float waterEntrySpeedReduction = 0.5f; // Slow down when entering water
    public LayerMask waterLayer; // Set this to the Water layer in the Inspector
    public float waterCheckRadius = 0.5f;
    public Transform waterCheckPoint; // Point to check for water (around chest level)
    public float waterEntryGravity = 0.5f; // Low gravity after entry
    public float buoyancyStrength = 50f; // HIGHER = Faster response (was 15f)
    public float buoyancyDamping = 10f; // HIGHER = Less oscillation
    public float maxBuoyancyForce = 20f; // Limit maximum force
    
    [Header("Floating Effect Settings")]
    public float floatAmplitude = 0.05f; // How high/low the player bobs
    public float floatFrequency = 1f; // How fast the player bobs
    public float floatSmoothness = 5f; // How smooth the bobbing is
    public bool enableFloating = true; // Toggle floating effect

    [Header("Ledge Settings")]
    public float ledgeDetectionDistance = 0.5f;
    public float ledgeGrabOffsetY = 0.15f; // Vertical offset for hand position
    public float ledgeGrabOffsetX = 0.8f; // Horizontal offset from wall
    public float ledgeClimbTime = 0.3f;
    public float ledgeJumpForce = 12f;
    public Vector2 ledgeJumpAngle = new Vector2(1, 2);
    public float ledgeClimbCheckRadius = 0.2f;
    public Transform ledgeCheckPoint; // Point to check for ledges
    
    // ====================================================================
    // INTERNAL STATE - PRIVATE VARIABLES
    // ====================================================================
    
    // Component References
    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction attackAction;
    private Animator animator;
    private CameraShake cameraShake;
    private Camera mainCamera; // Reference for screen height calculation
    
    // Input & Movement State
    private Vector2 moveInput;
    private Vector3 velocity = Vector3.zero;
    private bool facingRight = true;
    private bool isGrounded;
    private bool isRunning = false;
    
    // Action State Flags
    private bool isWallSliding;
    private bool isDashing = false;
    private bool isAttacking = false;
    private bool isWallJumping = false;
    private bool isTouchingWall;
    private bool isWallClinging = false;
    private bool isHardLanding = false;
    
    // Timers & Cooldowns
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private float attackTimer = 0f;
    private float attackCooldownTimer = 0f;
    private float wallJumpTimer = 0f;
    private float wallStickTimer = 0f;
    private float wallClingTimer = 0f;
    private float hardLandingTimer = 0f;
    
    // Jump & Air Movement Variables
    private float coyoteTimeCounter = 0f;
    private float jumpBufferCounter = 0f;
    private bool isJumpButtonHeld = false;
    private bool hasDoubleJumped = false;
    
    // Falling & Landing Tracking
    private float currentFallSpeed = 0f;
    private float peakHeight = 0f;
    private float fallStartHeight = 0f;
    private float totalFallDistance = 0f;
    private float screenHeightInUnits = 0f; // Screen height converted to world units
    private bool fellFromOffScreen = false; // Track if player fell from off-screen
    
    // Wall Collision Prevention
    private bool isAgainstWall = false;
    private float wallNormalDistance = 0.05f;
    private int lastWallSide = 0;
    
    // Wall Interaction Variables
    private int wallSide = 0;
    private enum WallSlideState { None, Starting, Sliding, Jumping }
    private WallSlideState wallSlideState = WallSlideState.None;
    
    // Dash Variables
    private Vector2 dashDirection = Vector2.right;
    private int airDashCount = 0;

    // Swimming State
    private bool isInWater = false;
    private bool isSwimming = false;
    private bool isSwimDashing = false;
    private bool wasInWater = false;
    private float swimDashTimer = 0f;
    private float swimDashCooldownTimer = 0f;
    private float swimDashDuration = 0.15f;
    private float swimDashCooldown = 0.3f;
    private Vector2 swimDashDirection = Vector2.right;
    private float waterSurfaceY = 0f;
    private Collider2D currentWaterCollider = null;
    private float preDashGravityScale;

    // Floating Effect Variables
    private float floatTimer = 0f;
    private float currentFloatOffset = 0f;
    private float targetFloatOffset = 0f;
    private Vector3 originalPosition; // Store original position for reference

    // Ledge Variables
    private bool isLedgeGrabbing = false;
    private bool isLedgeClimbing = false;
    private bool ledgeDetected = false;
    private Vector2 ledgePosition;
    private float ledgeClimbTimer = 0f;
    private int ledgeSide = 0;
    private float ledgeGrabTime = 0f; 
    private float minLedgeGrabTime = 0.2f; 
    private bool climbInputQueued = false; 
    
    // ====================================================================
    // UNITY LIFE CYCLE METHODS
    // ====================================================================
    
    /// <summary>
    /// Initializes component references and sets up required objects
    /// </summary>
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        dashAction = playerInput.actions["Dash/Run"];
        attackAction = playerInput.actions["Attack"];
        
        animator = GetComponent<Animator>();

        originalPosition = transform.position;
        
        // Get camera reference for screen height calculations
        mainCamera = Camera.main;
        CalculateScreenHeightInUnits();
        
        // Get or create CameraShake component
        SetupCameraShake();
        
        SetupMissingObjects();
        
    }
    
    /// <summary>
    /// Calculates the screen height in world units for accurate fall detection
    /// </summary>
    private void CalculateScreenHeightInUnits()
    {
        if (mainCamera != null)
        {
            // Convert screen height from pixels to world units
            float screenHeightInPixels = Screen.height;
            screenHeightInUnits = mainCamera.orthographicSize * 2f;
            
            // Alternative method using viewport
            Vector3 topOfScreen = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 1f, 0));
            Vector3 bottomOfScreen = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, 0));
            screenHeightInUnits = Vector3.Distance(topOfScreen, bottomOfScreen);
            
        }
        else
        {
            // Default fallback value
            screenHeightInUnits = 10f;
            Debug.LogWarning("Main camera not found. Using default screen height: " + screenHeightInUnits);
        }
    }
    
    /// <summary>
    /// Calculates the visible screen bounds in world units
    /// </summary>
    private Bounds GetScreenBounds()
    {
        if (mainCamera == null) return new Bounds(transform.position, new Vector3(20, 10, 0));
        
        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));
        Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, mainCamera.nearClipPlane));
        
        Bounds bounds = new Bounds();
        bounds.SetMinMax(bottomLeft, topRight);
        
        return bounds;
    }
    
    /// <summary>
    /// Checks if the player's fall started from off-screen (above camera view)
    /// </summary>
    private bool CheckIfFellFromOffScreen(float fallStartY)
    {
        if (mainCamera == null) return false;
        
        Bounds screenBounds = GetScreenBounds();
        
        // Add offset to account for player being partially visible
        float adjustedTop = screenBounds.max.y + screenHeightDetectionOffset;
        
        // Return true if fall started above the visible screen area
        return fallStartY > adjustedTop;
    }
    
    /// <summary>
    /// Checks if the fall distance is at least one screen height
    /// </summary>
    private bool CheckScreenHeightFall(float fallDistance)
    {
        float requiredDistance = screenHeightInUnits * minScreenHeightForHardLanding;
        
        // Debug visualization
        Debug.DrawLine(
            new Vector3(transform.position.x - 1f, fallStartHeight, 0),
            new Vector3(transform.position.x + 1f, fallStartHeight, 0),
            Color.cyan
        );
        
        Debug.DrawLine(
            new Vector3(transform.position.x - 1f, fallStartHeight - requiredDistance, 0),
            new Vector3(transform.position.x + 1f, fallStartHeight - requiredDistance, 0),
            Color.yellow
        );
        
        return fallDistance >= requiredDistance;
    }
    
    /// <summary>
    /// Sets up camera shake component reference
    /// </summary>
    private void SetupCameraShake()
    {
        if (Camera.main != null)
        {
            cameraShake = Camera.main.GetComponent<CameraShake>();
            
            if (cameraShake == null)
            {
                cameraShake = Camera.main.gameObject.AddComponent<CameraShake>();
            }
        }
        else
        {
            Debug.LogWarning("Main camera not found. Screen shake will not work.");
        }
    }
    
    /// <summary>
    /// Handles input processing, state updates, and non-physics logic
    /// </summary>
    void Update()
    {
        // 1. Input Reading
        moveInput = moveAction.ReadValue<Vector2>();
        UpdateJumpButtonState();
        
        // 2. Timer Updates
        UpdateTimers();
        
        // 3. Environment Checks
        CheckWall();
        CheckWater();
        
        // 4. Ledge Detection (only if not already grabbing)
        if (!isLedgeGrabbing && !isLedgeClimbing)
        {
            ledgeDetected = CheckForLedge();
        }
        
        // 5. Input Handling
        HandleSwimInput();
        HandleDashInput();
        HandleRunInput();
        HandleJumpInput();
        HandleAttackInput();
        
        // 6. Ledge Input Handling
        HandleLedgeInput();
        
        // 7. State Management
        HandleWallSlide();
        HandleLedgeClimb();
        
        // 8. Visual Feedback
        SetAnimation(moveInput.x);

        DisplayDebugInfo();
    }
    
    /// <summary>
    /// Handles physics-based movement and collision checks
    /// </summary>
    void FixedUpdate()
    {
        // 1. Environment Detection
        UpdateGroundCheck();
        
        // 2. Gravity Control (skip if ledge grabbing)
        if (!isLedgeGrabbing && !isLedgeClimbing)
        {
            UpdateGravity();
        }
        
        // 3. Wall Collision Prevention
        PreventWallStick();
        
        // 4. Movement Execution (skip if ledge grabbing/climbing)
        if (isLedgeGrabbing || isLedgeClimbing)
        {
            rb.linearVelocity = Vector2.zero;
        }
        else if (isSwimming)
        {
            HandleSwimMovement(); 
        }
        else
        {
            HandleMovement(); 
        }
        
        // 5. Sprite Orientation (skip if ledge grabbing/climbing)
        if (!isLedgeGrabbing && !isLedgeClimbing && !isDashing && !isAttacking && !isWallJumping && !isWallSliding && !isHardLanding && !isSwimDashing)
        {
            HandleFlip();
        }
    }
    
    // ====================================================================
    // INITIALIZATION METHODS
    // ====================================================================
    
    /// <summary>
    /// Creates necessary child objects if not assigned in inspector
    /// </summary>
    private void SetupMissingObjects()
    {
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.parent = transform;
            groundCheckObj.transform.localPosition = new Vector3(0, -0.65f, 0);
            groundCheck = groundCheckObj.transform;
        }
        
        if (wallCheck == null)
        {
            GameObject wallCheckObj = new GameObject("WallCheck");
            wallCheckObj.transform.parent = transform;
            wallCheckObj.transform.localPosition = new Vector3(0.5f, 0, 0);
            wallCheck = wallCheckObj.transform;
        }
        
        if (attackPoint == null)
        {
            GameObject attackPointObj = new GameObject("AttackPoint");
            attackPointObj.transform.parent = transform;
            attackPointObj.transform.localPosition = new Vector3(0.5f, 0, 0);
            attackPoint = attackPointObj.transform;
        }

        if (waterCheckPoint == null)
        {
            GameObject waterCheckObj = new GameObject("WaterCheck");
            waterCheckObj.transform.parent = transform;
            waterCheckObj.transform.localPosition = new Vector3(0, 0.2f, 0);
            waterCheckPoint = waterCheckObj.transform;
        }
        if (ledgeCheckPoint == null)
        {
            GameObject ledgeCheckObj = new GameObject("LedgeCheck");
            ledgeCheckObj.transform.parent = transform;
            ledgeCheckObj.transform.localPosition = new Vector3(0, 0.5f, 0);
            ledgeCheckPoint = ledgeCheckObj.transform;
        }
    }
    
    // ====================================================================
    // SCREEN SHAKE METHODS
    // ====================================================================
    
    /// <summary>
    /// Triggers screen shake based on fall impact intensity
    /// </summary>
    private void TriggerLandingScreenShake(float fallSpeed, float fallDistance)
    {
        if (!enableScreenShake || cameraShake == null) return;
        
        // Calculate shake intensity based on both speed and distance
        float normalizedFallSpeed = Mathf.Abs(fallSpeed) / Mathf.Abs(maxFallSpeed);
        float normalizedFallDistance = Mathf.Clamp01(fallDistance / (screenHeightInUnits * 2f));
        
        // Combine both factors for more dramatic shakes from higher falls
        float combinedFactor = (normalizedFallSpeed * 0.6f) + (normalizedFallDistance * 0.4f);
        
        float intensity = Mathf.Lerp(0.1f, hardLandingShakeIntensity, combinedFactor);
        float duration = Mathf.Lerp(0.1f, hardLandingShakeDuration, combinedFactor);
        
        cameraShake.Shake(intensity, duration);
    }
    
    // ====================================================================
    // INPUT PROCESSING METHODS
    // ====================================================================
    
    /// <summary>
    /// Tracks jump button state for variable jump height
    /// </summary>
    private void UpdateJumpButtonState()
    {
        if (jumpAction.IsPressed())
        {
            isJumpButtonHeld = true;
        }
        else if (jumpAction.WasReleasedThisFrame())
        {
            isJumpButtonHeld = false;
            if (rb.linearVelocity.y > 0 && !isDashing && !isWallJumping && !isWallSliding && !isHardLanding)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }
        }
    }
    
    /// <summary>
    /// Processes dash/run input
    /// </summary>
    private void HandleDashInput()
    {
        if (!dashUnlocked || isHardLanding || isSwimming) return;
        
        if (dashAction.triggered && !isDashing && dashCooldownTimer <= 0 && !isAttacking && !isWallSliding)
        {
            bool canDash = isGrounded || isWallSliding;
            
            if (!isGrounded && !isWallSliding && canAirDash)
            {
                canDash = airDashCount < maxAirDashes;
            }
            
            if (canDash)
            {
                StartDash();
                if (!isGrounded && !isWallSliding)
                {
                    airDashCount++;
                }
            }
        }
    }
    
    /// <summary>
    /// Determines if player is running
    /// </summary>
    private void HandleRunInput()
    {
        if (!runUnlocked || isHardLanding)
        {
            isRunning = false;
            return;
        }
        
        if (dashAction.IsPressed() && isGrounded && !isDashing && !isAttacking && !isWallSliding)
        {
            isRunning = true;
        }
        else
        {
            isRunning = false;
        }
    }
    
    /// <summary>
    /// Processes jump input with buffering and coyote time
    /// </summary>
    private void HandleJumpInput()
    {
        if (isHardLanding || isSwimming) return;
        
        if (jumpAction.triggered)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        
        if (jumpBufferCounter > 0)
        {
            if (isWallSliding && wallJumpUnlocked)
            {
                WallJump();
                jumpBufferCounter = 0;
                hasDoubleJumped = false;
            }
            else if (!isGrounded && coyoteTimeCounter <= 0 && doubleJumpUnlocked && hasDoubleJump && !hasDoubleJumped && !isDashing && !isAttacking)
            {
                DoubleJump();
                jumpBufferCounter = 0;
            }
            else if (coyoteTimeCounter > 0)
            {
                NormalJump();
                jumpBufferCounter = 0;
                coyoteTimeCounter = 0;
            }
        }
    }
    
    /// <summary>
    /// Processes attack input
    /// </summary>
    private void HandleAttackInput()
    {
        if (isHardLanding) return;
        
        if (attackAction.triggered && !isAttacking && attackCooldownTimer <= 0 && !isDashing)
        {
            StartAttack();
        }
    }

    /// <summary>
    /// Handles swimming-specific input
    /// </summary>
    private void HandleSwimInput()
    {
        if (!isSwimming || isHardLanding) return;
        
        // Handle swim dash
        if (dashAction.triggered && !isSwimDashing && swimDashCooldownTimer <= 0)
        {
            StartSwimDash();
        }
        
        // Handle swim jump (jumping out of water)
        if (jumpAction.triggered && !isSwimDashing)
        {
            SwimJump();
        }
    }

    /// <summary>
    /// Handles ledge-related inputs
    /// </summary>
    private void HandleLedgeInput()
    {   
        // Auto-grab ledge when detected and falling past it
        if (ledgeDetected && !isLedgeGrabbing && !isLedgeClimbing && rb.linearVelocity.y < 0)
        {
            // Check if player is at the right height to grab
            float playerBottom = GetComponent<Collider2D>().bounds.min.y;
            float ledgeTop = ledgePosition.y;
            
            // Player should be slightly below the ledge to grab it
            if (playerBottom < ledgeTop && playerBottom > ledgeTop - 1.0f)
            {
                GrabLedge();
            }
        }
        
        // If already grabbing ledge, handle input
        if (isLedgeGrabbing)
        {
            // Calculate input direction relative to ledge
            float inputDirection = 0f;
            
            // Prioritize vertical input first
            if (Mathf.Abs(moveInput.y) > Mathf.Abs(moveInput.x))
            {
                inputDirection = Mathf.Sign(moveInput.y);
            }
            else
            {
                inputDirection = Mathf.Sign(moveInput.x);
            }
            
            // CLIMB UP (press Up OR towards the ledge)
            if (moveInput.y > 0.5f || (inputDirection == ledgeSide && Mathf.Abs(moveInput.x) > 0.5f))
            {
                // Queue the climb input if we haven't been grabbing long enough
                if (ledgeGrabTime < minLedgeGrabTime)
                {
                    climbInputQueued = true;
                }
                else if (!isLedgeClimbing)
                {
                    // Enough time has passed, allow climbing
                    ClimbLedge();
                }
            }
            // LET GO (press Down OR away from the ledge)
            else if (moveInput.y < -0.5f || (inputDirection == -ledgeSide && Mathf.Abs(moveInput.x) > 0.5f))
            {
                // Allow immediate release when pressing down or away from wall
                ReleaseLedge();
            }
            // LET GO (press jump button)
            else if (jumpAction.triggered)
            {
                LedgeJump();
                return;
            }
            
            // Check if we have a queued climb input and enough time has passed
            if (climbInputQueued && ledgeGrabTime >= minLedgeGrabTime && !isLedgeClimbing)
            {
                ClimbLedge();
                climbInputQueued = false;
            }
        }
    }
    
    // ====================================================================
    // MOVEMENT & PHYSICS METHODS
    // ====================================================================
    
    /// <summary>
    /// Main movement handler
    /// </summary>
    private void HandleMovement()
    {
        if (isHardLanding)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        
        float currentSpeed = moveSpeed;
        
        if (isRunning && isGrounded)
        {
            currentSpeed = runSpeed;
        }
        
        if (isDashing)
        {
            rb.linearVelocity = dashDirection * dashSpeed;
            rb.gravityScale = 0;
        }
        else if (isWallJumping)
        {
            float controlForce = 5f;
            rb.AddForce(new Vector2(moveInput.x * controlForce, 0));
            
            float maxWallJumpSpeed = 10f;
            if (Mathf.Abs(rb.linearVelocity.x) > maxWallJumpSpeed)
            {
                rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * maxWallJumpSpeed, rb.linearVelocity.y);
            }
        }
        else if (isAttacking)
        {
            Vector2 targetVelocity = new Vector2(0, rb.linearVelocity.y);
            rb.linearVelocity = Vector3.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocity, movementSmoothing);
        }
        else if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            
            if (rb.linearVelocity.y > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            }
        }
        else
        {
            float currentMoveInput = moveInput.x;
            
            if (isAgainstWall && !isWallSliding)
            {
                if (Mathf.Sign(currentMoveInput) == lastWallSide)
                {
                    currentMoveInput = 0;
                }
            }
            
            Vector2 targetVelocity = new Vector2(currentMoveInput * currentSpeed, rb.linearVelocity.y);
            
            if (!isGrounded)
            {
                ApplyAirControl();
            }
            
            if (wallJumpUnlocked && isWallSliding)
            {
                if (wallStickTimer > 0 && isTouchingWall)
                {
                    if (Mathf.Sign(moveInput.x) == -wallSide || moveInput.x == 0)
                    {
                        targetVelocity.x = 0;
                    }
                }
            }
            
            rb.linearVelocity = Vector3.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocity, movementSmoothing);
            
            if (!isWallSliding && isAgainstWall && Mathf.Sign(rb.linearVelocity.x) == lastWallSide)
            {
                rb.linearVelocity = new Vector2(
                    Mathf.MoveTowards(rb.linearVelocity.x, 0, Time.deltaTime * 20f),
                    rb.linearVelocity.y
                );
            }
        }
    }
    
    /// <summary>
    /// Gravity system
    /// </summary>
    private void UpdateGravity()
    {
        if (isDashing || isWallSliding || isHardLanding || isWallJumping | isSwimming)
        {
            return;
        }
        
        currentFallSpeed = rb.linearVelocity.y;
        
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = fallingGravityScale;
            
            if (rb.linearVelocity.y < maxFallSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
            }
        }
        else if (rb.linearVelocity.y > 0 && !isJumpButtonHeld)
        {
            rb.gravityScale = fallingGravityScale * quickFallGravityMultiplier;
        }
        else
        {
            rb.gravityScale = normalGravityScale;
        }
    }
    
    /// <summary>
    /// Applies air control physics
    /// </summary>
    private void ApplyAirControl()
    {
        if (isGrounded || isWallSliding || isDashing || isHardLanding) return;
        
        float targetXVelocity = moveInput.x * moveSpeed * airControlMultiplier;
        float velocityDifference = targetXVelocity - rb.linearVelocity.x;
        
        rb.AddForce(Vector2.right * velocityDifference * airAcceleration);
        
        if (Mathf.Abs(rb.linearVelocity.x) > maxAirSpeed)
        {
            rb.linearVelocity = new Vector2(
                Mathf.Sign(rb.linearVelocity.x) * maxAirSpeed,
                rb.linearVelocity.y
            );
        }
    }
    
    /// <summary>
    /// Flips player sprite based on movement direction
    /// </summary>
    private void HandleFlip()
    {
        if (Mathf.Abs(moveInput.x) < 0.1f || isHardLanding) return;
            
        if (moveInput.x > 0 && !facingRight)
        {
            Flip();
            UpdateAttackPointPosition();
        }
        else if (moveInput.x < 0 && facingRight)
        {
            Flip();
            UpdateAttackPointPosition();
        }
    }
    
    // ====================================================================
    // ACTION IMPLEMENTATION METHODS
    // ====================================================================
    
    /// <summary>
    /// Executes a normal ground jump
    /// </summary>
    private void NormalJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        peakHeight = transform.position.y;
        fallStartHeight = transform.position.y;
        fellFromOffScreen = CheckIfFellFromOffScreen(fallStartHeight);
    }
    
    /// <summary>
    /// Executes a double jump
    /// </summary>
    private void DoubleJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, doubleJumpForce);
        hasDoubleJumped = true;
        peakHeight = transform.position.y;
        fallStartHeight = transform.position.y;
        fellFromOffScreen = CheckIfFellFromOffScreen(fallStartHeight);
    }
    
    /// <summary>
    /// Executes a wall jump with screen shake
    /// </summary>
    private void WallJump()
    {
        isWallJumping = true;
        isWallSliding = false;
        isWallClinging = false;
        wallSlideState = WallSlideState.Jumping;
        wallClingTimer = 0f;
        wallJumpTimer = wallJumpDuration;
        
        rb.linearVelocity = Vector2.zero;
        Vector2 jumpDir = new Vector2(-wallSide * wallJumpAngle.x, wallJumpAngle.y).normalized;
        rb.AddForce(jumpDir * wallJumpForce, ForceMode2D.Impulse);
        
        if (wallSide == 1 && !facingRight)
        {
            Flip();
        }
        else if (wallSide == -1 && facingRight)
        {
            Flip();
        }
        
        airDashCount = 0;
        peakHeight = transform.position.y;
        fallStartHeight = transform.position.y;
        fellFromOffScreen = CheckIfFellFromOffScreen(fallStartHeight);
        
    }
    
    /// <summary>
    /// Starts dash movement with screen shake
    /// </summary>
    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        
        dashDirection = facingRight ? Vector2.right : Vector2.left;
        
        if (!isGrounded && Mathf.Abs(moveInput.x) > 0.1f && !isWallSliding)
        {
            dashDirection = new Vector2(Mathf.Sign(moveInput.x), 0);
        }
        
    }
    
    /// <summary>
    /// Ends dash movement
    /// </summary>
    private void EndDash()
    {
        isDashing = false;
        rb.gravityScale = normalGravityScale;
        
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }
    }
    
    /// <summary>
    /// Starts attack animation with screen shake
    /// </summary>
    private void StartAttack()
    {
        isAttacking = true;
        attackTimer = attackDuration;
        attackCooldownTimer = attackCooldown;
        
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        
    }
    
    /// <summary>
    /// Ends attack state
    /// </summary>
    private void EndAttack()
    {
        isAttacking = false;
    }
    
    /// <summary>
    /// Enhanced hard landing detection with screen-height check
    /// </summary>
    private void CheckForHardLanding(float fallSpeed, float fallDistance)
    {
        bool meetsVelocityCondition = fallSpeed <= hardLandingThreshold;
        bool meetsHeightCondition = CheckScreenHeightFall(fallDistance);
        
        // Choose detection method based on settings
        bool shouldHardLand = false;
        
        if (useScreenHeightForHardLanding)
        {
            if (requireBothConditions)
            {
                // Both conditions must be met (Hollow Knight style)
                shouldHardLand = meetsHeightCondition && meetsVelocityCondition;
            }
            else
            {
                // Either condition is enough
                shouldHardLand = meetsHeightCondition || meetsVelocityCondition;
            }
        }
        else
        {
            // Fallback to original velocity-only detection
            shouldHardLand = meetsVelocityCondition && fallDistance > 2f;
        }
        
        // Additional off-screen check for dramatic falls
        bool fellFromOffScreenPosition = fellFromOffScreen || CheckIfFellFromOffScreen(fallStartHeight);
        
        // If player fell from off-screen, force hard landing for dramatic effect
        if (fellFromOffScreenPosition && fallDistance > screenHeightInUnits * 0.5f)
        {
            shouldHardLand = true;
        }
        
        if (shouldHardLand)
        {
            StartHardLanding();
            
            // Debug output
            string method = useScreenHeightForHardLanding ? "Screen Height" : "Velocity Only";
        }
        else if (fallDistance > 0.5f)
        {
            // Soft landing
            animator.Play("Player_land");
        }
    }
    
    /// <summary>
    /// Initiates hard landing state with screen shake
    /// </summary>
    private void StartHardLanding()
    {
        isHardLanding = true;
        hardLandingTimer = hardLandingStunTime;
        
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        
        animator.Play("Player_hard_land");
        
        // Screen shake for hard landing - intensity based on fall speed and distance
        TriggerLandingScreenShake(currentFallSpeed, totalFallDistance);
    }
    
    /// <summary>
    /// Ends hard landing recovery
    /// </summary>
    private void EndHardLanding()
    {
        isHardLanding = false;
        rb.gravityScale = normalGravityScale;
        animator.Play("Player_idle");
    }

    /// <summary>
    /// Starts a swim dash
    /// </summary>
    private void StartSwimDash()
    {
        isSwimDashing = true;
        swimDashTimer = swimDashDuration;
        swimDashCooldownTimer = swimDashCooldown;
        
        // Save the current gravity scale before dash
        preDashGravityScale = rb.gravityScale;
        
        // Set gravity to 0 during dash
        rb.gravityScale = 0f;
        
        // Determine dash direction
        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            swimDashDirection = new Vector2(Mathf.Sign(moveInput.x), 0);
        }
        else
        {
            swimDashDirection = facingRight ? Vector2.right : Vector2.left;
        }
    }

    /// <summary>
    /// Ends swim dash
    /// </summary>
    private void EndSwimDash()
    {
        isSwimDashing = false;
        
        // Restore the gravity scale from before the dash
        rb.gravityScale = preDashGravityScale;
        
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, rb.linearVelocity.y * 0.5f);
    }

    /// <summary>
    /// Jumps out of water
    /// </summary>
    private void SwimJump()
    {
        if (!isSwimming || isSwimDashing) return;
        
        // Jump out of water
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, swimJumpForce);
        
        // Exit swimming state
        isSwimming = false;
        rb.gravityScale = normalGravityScale;
        
        // Reset coyote time for potential double jump
        coyoteTimeCounter = coyoteTime;
    }

    /// <summary>
    /// Handles swimming movement with fast buoyancy
    /// </summary>
    private void HandleSwimMovement()
    {
        if (!isSwimming || isHardLanding) return;
        
        // Handle swim dash first
        if (isSwimDashing)
        {
            rb.linearVelocity = swimDashDirection * swimDashSpeed;
            rb.gravityScale = 0f; // No gravity during dash
            return;
        }
        
        // Apply fast buoyancy FIRST (most important) - always active
        ApplyBuoyancy();
        
        // Calculate horizontal movement
        float currentSwimSpeed = swimSpeed;
        
        // Check for fast swimming (when holding dash button)
        if (dashAction.IsPressed() && !isSwimDashing)
        {
            currentSwimSpeed = swimFastSpeed;
        }
        
        // Apply horizontal movement with velocity control instead of forces
        float targetXVelocity = moveInput.x * currentSwimSpeed;
        float currentXVelocity = rb.linearVelocity.x;
        
        // Smooth horizontal movement but prioritize buoyancy
        float horizontalAcceleration = isInWater ? 25f : 15f; // Faster in water
        float newXVelocity = Mathf.MoveTowards(currentXVelocity, targetXVelocity, 
                                            Time.fixedDeltaTime * horizontalAcceleration);
        
        // Only apply floating effect when not actively moving horizontally
        if (enableFloating && Mathf.Abs(moveInput.x) < 0.1f)
        {
            // Update original position reference for floating
            if (Mathf.Abs(currentFloatOffset) > 0.01f)
            {
                originalPosition = new Vector3(originalPosition.x, 
                                            transform.position.y - currentFloatOffset, 
                                            originalPosition.z);
            }
            
            ApplyFloatingEffect();
            
            // Apply the floating offset
            Vector3 currentPos = transform.position;
            transform.position = new Vector3(currentPos.x, 
                                        originalPosition.y + currentFloatOffset, 
                                        currentPos.z);
        }
        else if (enableFloating)
        {
            // When moving horizontally, reduce floating effect intensity
            floatTimer += Time.deltaTime * floatFrequency * 0.5f; // Slower bobbing when moving
            targetFloatOffset = Mathf.Sin(floatTimer * Mathf.PI * 2f) * floatAmplitude * 0.3f; // Reduced amplitude
            currentFloatOffset = Mathf.Lerp(currentFloatOffset, targetFloatOffset, Time.deltaTime * floatSmoothness);
        }
        
        // Apply the velocities - buoyancy handles vertical, we handle horizontal
        rb.linearVelocity = new Vector2(newXVelocity, rb.linearVelocity.y);
        
        // Clamp horizontal speed
        float maxHorizontalSpeed = currentSwimSpeed * 1.2f;
        if (Mathf.Abs(rb.linearVelocity.x) > maxHorizontalSpeed)
        {
            rb.linearVelocity = new Vector2(
                Mathf.Sign(rb.linearVelocity.x) * maxHorizontalSpeed,
                rb.linearVelocity.y
            );
        }
        
        // Force buoyancy correction if player is too high above target
        if (currentWaterCollider != null)
        {
            float playerHeight = GetComponent<Collider2D>().bounds.extents.y * 2f;
            float targetY = waterSurfaceY + waterSurfaceOffset - (playerHeight * 0.8f);
            float currentY = transform.position.y;
            float yDifference = targetY - currentY;
            
            // If player is significantly above target position (more buoyant than expected)
            if (yDifference > 0.5f) // Increased threshold for more aggressive correction
            {
                // Apply additional downward force
                rb.AddForce(new Vector2(0, -Mathf.Min(yDifference * 5f, 10f)));
                
                // OR use direct position adjustment as fallback
                if (yDifference > 1.0f)
                {
                    float newY = Mathf.Lerp(currentY, targetY, Time.fixedDeltaTime * 15f);
                    rb.MovePosition(new Vector2(transform.position.x, newY));
                }
            }
        }
    }

    /// <summary>
    /// Applies fast, responsive buoyancy force
    /// </summary>
    private void ApplyBuoyancy()
    {
        if (!isSwimming || currentWaterCollider == null) return;
        
        // Calculate target position (face above water)
        float playerHeight = GetComponent<Collider2D>().bounds.extents.y * 2f;
        float targetY = waterSurfaceY + waterSurfaceOffset - (playerHeight * 0.8f);
        
        // Adjust target Y with floating offset when enabled
        if (enableFloating && !isSwimDashing)
        {
            targetY += currentFloatOffset;
        }
        
        // Current position
        float currentY = transform.position.y;
        float depthDifference = targetY - currentY;
        
        // Apply STRONG, FAST buoyancy force
        float buoyancyForce = depthDifference * buoyancyStrength;
        
        // Apply strong damping to prevent oscillation
        float dampingForce = -rb.linearVelocity.y * buoyancyDamping;
        
        // Combined force
        float totalForce = buoyancyForce + dampingForce;
        
        // Clamp to maximum force
        totalForce = Mathf.Clamp(totalForce, -maxBuoyancyForce, maxBuoyancyForce);
        
        // Apply the force
        rb.AddForce(new Vector2(0, totalForce));
        
        // Debug visualization
        Debug.DrawLine(
            new Vector3(transform.position.x - 0.5f, targetY, 0),
            new Vector3(transform.position.x + 0.5f, targetY, 0),
            Color.green
        );
        
        // Draw force vector
        Debug.DrawRay(
            transform.position,
            new Vector3(0, totalForce * 0.1f, 0),
            Color.yellow
        );
    }

    /// <summary>
    /// Direct position correction for instant buoyancy (backup system)
    /// </summary>
    private void ApplyDirectBuoyancyCorrection()
    {
        if (!isSwimming || currentWaterCollider == null) return;
        
        // Only apply if significantly off target
        float playerHeight = GetComponent<Collider2D>().bounds.extents.y * 2f;
        float targetY = waterSurfaceY + waterSurfaceOffset - (playerHeight * 0.8f);
        float currentY = transform.position.y;
        float yDifference = Mathf.Abs(targetY - currentY);
        
        // If more than 0.3 units off, apply direct correction
        if (yDifference > 0.3f)
        {
            // Fast direct movement toward target
            float newY = Mathf.Lerp(currentY, targetY, Time.fixedDeltaTime * 10f);
            rb.MovePosition(new Vector2(transform.position.x, newY));
        }
    }

    /// <summary>
    /// Applies floating/bobbing effect when swimming
    /// </summary>
    private void ApplyFloatingEffect()
    {
        if (!isSwimming || isSwimDashing || !enableFloating) return;
        
        // Update timer based on frequency
        floatTimer += Time.deltaTime * floatFrequency;
        
        // Calculate sine wave for natural bobbing
        float sineWave = Mathf.Sin(floatTimer * Mathf.PI * 2f);
        
        // Calculate target offset with reduced effect when moving horizontally
        float horizontalMovementFactor = Mathf.Clamp01(1f - Mathf.Abs(moveInput.x) * 0.5f);
        targetFloatOffset = sineWave * floatAmplitude * horizontalMovementFactor;
        
        // Smoothly interpolate to target offset
        currentFloatOffset = Mathf.Lerp(currentFloatOffset, targetFloatOffset, Time.deltaTime * floatSmoothness);
        
        // Apply the floating offset to position
        Vector3 currentPos = transform.position;
        transform.position = new Vector3(currentPos.x, originalPosition.y + currentFloatOffset, currentPos.z);
        
        // Debug visualization
        Debug.DrawLine(
            new Vector3(transform.position.x - 0.3f, originalPosition.y, 0),
            new Vector3(transform.position.x + 0.3f, originalPosition.y, 0),
            Color.magenta
        );
    }

    /// <summary>
    /// Handles ledge climbing animation and movement
    /// </summary>
    private void HandleLedgeClimb()
    {
        if (!isLedgeClimbing) return;
        
        if (ledgeClimbTimer > 0)
        {
            ledgeClimbTimer -= Time.deltaTime;
            
            // Get player collider for accurate positioning
            Collider2D playerCollider = GetComponent<Collider2D>();
            if (playerCollider != null)
            {
                float playerHeight = playerCollider.bounds.size.y;
                
                // Calculate target position - standing on the platform
                Vector3 climbTarget = new Vector3(
                    ledgePosition.x + (ledgeSide * 0.3f), // Move slightly away from edge
                    ledgePosition.y + (playerHeight * 0.5f), // Center player vertically on platform
                    transform.position.z
                );
                
                // Smooth movement during climb
                float climbProgress = 1f - (ledgeClimbTimer / ledgeClimbTime);
                transform.position = Vector3.Lerp(transform.position, climbTarget, climbProgress * 5f * Time.deltaTime);
                
                // Draw debug line
                Debug.DrawLine(transform.position, climbTarget, Color.cyan);
            }
        }
        else
        {
            // Climb finished
            isLedgeClimbing = false;
            rb.gravityScale = normalGravityScale;
            
            // Small hop at the end for polish
            rb.linearVelocity = new Vector2(0, 3f);
            
            Debug.Log("Ledge climb complete!");
        }
    }

    
    // ====================================================================
    // WALL INTERACTION METHODS
    // ====================================================================
    
    /// <summary>
    /// Prevents player from getting stuck on walls
    /// </summary>
    private void PreventWallStick()
    {
        if (isGrounded || Mathf.Abs(moveInput.x) < 0.1f || isHardLanding)
        {
            isAgainstWall = false;
            lastWallSide = 0;
            return;
        }
        
        if (wallJumpUnlocked && (isWallSliding || isWallClinging))
        {
            return;
        }
        
        float direction = Mathf.Sign(moveInput.x);
        Vector2 origin = transform.position;
        
        float rayLength = 0.6f;
        int rayCount = 5;
        float totalHeight = 1.0f;
        
        bool hitWall = false;
        float closestDistance = rayLength;
        
        for (int i = 0; i < rayCount; i++)
        {
            float height = (i / (float)(rayCount - 1) - 0.5f) * totalHeight;
            Vector2 rayOrigin = origin + new Vector2(0, height);
            
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, 
                new Vector2(direction, 0), 
                rayLength, 
                environmentLayer);
            
            Debug.DrawRay(rayOrigin, new Vector2(direction * rayLength, 0), 
                hit.collider != null ? Color.red : Color.green);
            
            if (hit.collider != null && hit.distance < closestDistance)
            {
                hitWall = true;
                closestDistance = hit.distance;
                
                if (hit.distance < wallNormalDistance)
                {
                    Vector2 pushBack = new Vector2(-direction * (wallNormalDistance - hit.distance), 0);
                    transform.position += (Vector3)pushBack * 0.5f;
                }
            }
        }
        
        if (hitWall && Mathf.Sign(moveInput.x) == direction)
        {
            if (lastWallSide != direction)
            {
                lastWallSide = (int)direction;
                isAgainstWall = true;
            }
            
            if (Mathf.Sign(moveInput.x) == lastWallSide)
            {
                if (!isWallSliding)
                {
                    moveInput = new Vector2(0, moveInput.y);
                    
                    if (Mathf.Sign(rb.linearVelocity.x) == lastWallSide && Mathf.Abs(rb.linearVelocity.x) > 0.1f)
                    {
                        float reduction = Mathf.Lerp(rb.linearVelocity.x, 0, Time.deltaTime * 15f);
                        rb.linearVelocity = new Vector2(reduction, rb.linearVelocity.y);
                    }
                }
            }
        }
        else
        {
            isAgainstWall = false;
            
            if (Mathf.Abs(moveInput.x) < 0.1f || Mathf.Sign(moveInput.x) != lastWallSide)
            {
                lastWallSide = 0;
            }
        }
    }
    
    /// <summary>
    /// Handles wall slide physics
    /// </summary>
    private void HandleWallSlide()
    {
        if (!wallJumpUnlocked || isHardLanding || isLedgeGrabbing || isLedgeClimbing) 
        {
            isWallSliding = false;
            isWallClinging = false;
            wallSlideState = WallSlideState.None;
            return;
        }
        
        if (wallClingTimer > 0)
        {
            wallClingTimer -= Time.deltaTime;
        }
        
        if (isTouchingWall)
        {
            if (wallSlideState == WallSlideState.None)
            {
                wallSlideState = WallSlideState.Starting;
                if (rb.linearVelocity.y > 0)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                }
            }
            else if (wallSlideState == WallSlideState.Starting)
            {
                wallSlideState = WallSlideState.Sliding;
            }
            
            isWallSliding = true;
            
            float currentSlideSpeed = rb.linearVelocity.y;
            
            if (isWallClinging)
            {
                float clingSpeed = -wallSlideSpeed * wallClingSlowdown;
                
                if (currentSlideSpeed < clingSpeed)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, clingSpeed);
                }
                
                if (resetAirDashOnGround)
                {
                    airDashCount = 0;
                }
            }
            else
            {
                if (isJumpButtonHeld && currentSlideSpeed < 0)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentSlideSpeed * 0.7f);
                }
                else if (currentSlideSpeed < -wallSlideSpeed)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
                }
                
                if (resetAirDashOnGround)
                {
                    airDashCount = 0;
                }
            }
        }
        else
        {
            wallSlideState = WallSlideState.None;
            isWallSliding = false;
            isWallClinging = false;
        }
    }
    
    /// <summary>
    /// Enhanced wall detection
    /// </summary>
    private void CheckWall()
    {
        if (!wallJumpUnlocked || isHardLanding || isLedgeGrabbing || isLedgeClimbing)
        {
            isTouchingWall = false;
            wallSide = 0;
            isWallSliding = false;
            isWallClinging = false;
            wallSlideState = WallSlideState.None;
            return;
        }
        
        Vector2 wallCheckPos = wallCheck.position;
        Vector2 offset = new Vector2(wallCheckOffset, 0);
        
        bool touchingRightWall = false;
        bool touchingLeftWall = false;
        
        RaycastHit2D middleRightHit = Physics2D.Raycast(
            wallCheckPos + offset, 
            Vector2.right, 
            wallCheckDistance, 
            environmentLayer
        );
        
        RaycastHit2D middleLeftHit = Physics2D.Raycast(
            wallCheckPos - offset, 
            Vector2.left, 
            wallCheckDistance, 
            environmentLayer
        );
        
        touchingRightWall = middleRightHit.collider != null && !isGrounded;
        touchingLeftWall = middleLeftHit.collider != null && !isGrounded;
        
        isTouchingWall = false;
        wallSide = 0;
        
        if (touchingRightWall && moveInput.x > 0.1f)
        {
            isTouchingWall = true;
            wallSide = 1;
            wallClingTimer = wallClingTime;
            isWallClinging = false;
        }
        else if (touchingLeftWall && moveInput.x < -0.1f)
        {
            isTouchingWall = true;
            wallSide = -1;
            wallClingTimer = wallClingTime;
            isWallClinging = false;
        }
        else if ((touchingRightWall || touchingLeftWall) && wallClingTimer > 0)
        {
            if (touchingRightWall)
            {
                wallSide = 1;
            }
            else if (touchingLeftWall)
            {
                wallSide = -1;
            }
            
            bool switchingDirection = (wallSide == 1 && moveInput.x < -0.1f) || 
                                    (wallSide == -1 && moveInput.x > 0.1f);
            
            if (switchingDirection)
            {
                isWallClinging = true;
                isTouchingWall = true;
            }
        }
    }

    /// <summary>
    /// Detects ledges that can be grabbed
    /// </summary>
    private bool CheckForLedge()
    {
        if (isGrounded || isDashing || isAttacking || isHardLanding || isSwimming || isWallSliding)
            return false;

        // Only check for ledges when falling
        if (rb.linearVelocity.y >= 0)
            return false;

        // Determine which side to check based on facing direction and movement input
        float checkDirection = facingRight ? 1f : -1f;
        
        // Also consider movement input - if moving opposite direction, use that
        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            checkDirection = Mathf.Sign(moveInput.x);
        }
        
        ledgeSide = (int)checkDirection;
        
        // Get player collider bounds for accurate positioning
        Collider2D playerCollider = GetComponent<Collider2D>();
        if (playerCollider == null) return false;
        
        Vector2 playerCenter = playerCollider.bounds.center;
        float playerHalfWidth = playerCollider.bounds.extents.x;
        float playerHalfHeight = playerCollider.bounds.extents.y;
        
        // Position for checking wall (at player's side)
        Vector2 wallCheckPos = new Vector2(
            playerCenter.x + (checkDirection * (playerHalfWidth + 0.05f)),
            playerCenter.y
        );
        
        // Position for checking ledge (above player's head)
        Vector2 ledgeCheckPos = new Vector2(
            playerCenter.x + (checkDirection * (playerHalfWidth + 0.05f)),
            playerCenter.y + playerHalfHeight * 0.8f  // 80% up the player's height
        );
        
        // Position for checking ground at ledge (above player's head)
        Vector2 groundCheckPos = new Vector2(
            playerCenter.x + (checkDirection * (playerHalfWidth + 0.3f)), // Slightly further out
            playerCenter.y + playerHalfHeight * 0.8f
        );
        
        // DEBUG VISUALIZATION - Always draw these to see what's being checked
        Debug.DrawRay(wallCheckPos, Vector2.right * checkDirection * 0.2f, Color.red, 0.1f);
        Debug.DrawRay(ledgeCheckPos, Vector2.right * checkDirection * 0.2f, Color.green, 0.1f);
        Debug.DrawRay(groundCheckPos, Vector2.down * (playerHalfHeight * 1.5f), Color.blue, 0.1f);
        
        // 1. Check for wall at side (must have wall)
        RaycastHit2D wallHit = Physics2D.Raycast(
            wallCheckPos,
            Vector2.right * checkDirection,
            0.2f,
            environmentLayer
        );
        
        if (wallHit.collider == null)
        {
            // No wall - can't grab ledge
            return false;
        }
        
        // 2. Check for NO wall at ledge height (must have empty space above wall)
        RaycastHit2D ledgeHit = Physics2D.Raycast(
            ledgeCheckPos,
            Vector2.right * checkDirection,
            0.2f,
            environmentLayer
        );
        
        if (ledgeHit.collider != null)
        {
            // There's still wall at this height - not a ledge
            return false;
        }
        
        // 3. Check for ground/surface at the top of the wall
        RaycastHit2D groundHit = Physics2D.Raycast(
            groundCheckPos,
            Vector2.down,
            playerHalfHeight * 1.5f, // Check far enough down
            environmentLayer
        );
        
        if (groundHit.collider == null)
        {
            // No ground above - not a ledge
            return false;
        }
        
        // Found a valid ledge!
        ledgePosition = groundHit.point;
        
        // Add extra debug info
        Debug.Log($"Ledge detected! Position: {ledgePosition}, Side: {ledgeSide}");
        
        return true;
    }

    /// <summary>
    /// Grabs onto a detected ledge
    /// </summary>
    private void GrabLedge()
    {
        if (!ledgeDetected || isLedgeGrabbing)
            return;
        
        Debug.Log("Grabbing ledge!");
        
        isLedgeGrabbing = true;
        isLedgeClimbing = false;
        ledgeClimbTimer = 0f;
        ledgeGrabTime = 0f;
        climbInputQueued = false;
        
        // Stop all movement
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        
        // Get player collider for accurate positioning
        Collider2D playerCollider = GetComponent<Collider2D>();
        if (playerCollider == null) return;
        
        float playerHeight = playerCollider.bounds.size.y;
        
        // CRITICAL FIX: Position player BELOW the ledge, not on top of it
        // ledgePosition.y is the TOP of the platform where you stand
        // We want player's hands at ledge level, so body is below
        
        float grabX = ledgePosition.x - (ledgeSide * ledgeGrabOffsetX);
        
        // IMPORTANT: Position player so their UPPER body is at ledge level
        // Player hangs with hands at ledge, body extends downward
        // 0.7f means hands are at 70% of player height (roughly chest/shoulder level)
        float hangOffset = playerHeight * ledgeGrabOffsetY; // How much body extends below hands
        
        // Calculate position so player's "grab point" (hands) aligns with ledge
        // We subtract hangOffset to position the player's transform below the ledge
        float grabY = ledgePosition.y - hangOffset;
        
        // Alternative: Use a more precise calculation
        // float playerBottom = playerCollider.bounds.min.y;
        // float playerTop = playerCollider.bounds.max.y;
        // float grabY = ledgePosition.y - (playerTop - playerBottom) * 0.8f;
        
        // Snap to ledge position
        Vector3 targetPosition = new Vector3(grabX, grabY, transform.position.z);
        transform.position = targetPosition;
        
        // Face the wall
        if ((ledgeSide == 1 && !facingRight) || (ledgeSide == -1 && facingRight))
        {
            Flip();
        }
        
        // Reset other states
        isDashing = false;
        isWallSliding = false;
        isWallClinging = false;
        wallSlideState = WallSlideState.None;
        
        // Reset ledge detection to prevent re-grabbing
        ledgeDetected = false;
        
        // Trigger animation
        animator.Play("Player_ledge_grab");
        
        Debug.Log($"Ledge grab complete. Position: {transform.position}, Target: {targetPosition}");
        Debug.Log($"Ledge Y: {ledgePosition.y}, Player Y: {transform.position.y}, Hang Offset: {hangOffset}");
    }

    /// <summary>
    /// Releases from the ledge
    /// </summary>
    private void ReleaseLedge()
    {
        if (!isLedgeGrabbing) return;
        
        isLedgeGrabbing = false;
        rb.gravityScale = normalGravityScale;
        
        // Small downward push when releasing
        rb.linearVelocity = new Vector2(0, -2f);
    }

    /// <summary>
    /// Climbs up onto the ledge
    /// </summary>
    private void ClimbLedge()
    {
        if (!isLedgeGrabbing || isLedgeClimbing)
            return;
        
        Debug.Log("Climbing ledge!");
        
        isLedgeClimbing = true;
        isLedgeGrabbing = false;
        ledgeClimbTimer = ledgeClimbTime;
        
        // Play climb animation
        animator.Play("Player_ledge_climb");
        
        // Get player collider for accurate positioning
        Collider2D playerCollider = GetComponent<Collider2D>();
        if (playerCollider == null) return;
        
        float playerHeight = playerCollider.bounds.size.y;
        
        // Calculate final position after climb
        // Player should end up STANDING on the platform
        // 1. Move slightly away from edge (so not clipping into wall)
        // 2. Move up so feet are on platform
        
        // Platform surface is at ledgePosition.y
        // Player's feet should be at ledgePosition.y
        
        float climbX = ledgePosition.x + (ledgeSide * 0.3f); // Move slightly away from edge
        float climbY = ledgePosition.y + (playerHeight * 0.5f); // Center player on platform
        
        // Debug information
        Debug.Log($"Climbing to: X={climbX}, Y={climbY}");
        Debug.Log($"Current pos: {transform.position}, Ledge pos: {ledgePosition}");
    }

    /// <summary>
    /// Jumps away from the ledge
    /// </summary>
    private void LedgeJump()
    {
        if (!isLedgeGrabbing)
            return;
        
        isLedgeGrabbing = false;
        isLedgeClimbing = false;
        
        // Restore gravity
        rb.gravityScale = normalGravityScale;
        
        // Apply jump force
        Vector2 jumpDir = new Vector2(-ledgeSide * ledgeJumpAngle.x, ledgeJumpAngle.y).normalized;
        rb.AddForce(jumpDir * ledgeJumpForce, ForceMode2D.Impulse);
        
        // Face away from wall for jump
        if (ledgeSide == 1 && !facingRight)
        {
            Flip();
        }
        else if (ledgeSide == -1 && facingRight)
        {
            Flip();
        }
        
        // Reset air dash
        airDashCount = 0;
        
        // Track jump height
        peakHeight = transform.position.y;
        fallStartHeight = transform.position.y;
        fellFromOffScreen = CheckIfFellFromOffScreen(fallStartHeight);
    }
    
    // ====================================================================
    // ENVIRONMENT DETECTION & UTILITY METHODS
    // ====================================================================
    
    /// <summary>
    /// Enhanced ground check with screen-height fall detection
    /// </summary>
    private void UpdateGroundCheck()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, environmentLayer);

        // Release ledge if grounded
        if (isGrounded && (isLedgeGrabbing || isLedgeClimbing))
        {
            ReleaseLedge();
        }
        
        // Track peak height during jump/rise
        if (!isGrounded && !wasGrounded && rb.linearVelocity.y > 0)
        {
            peakHeight = transform.position.y;
        }
        
        // LANDING DETECTION - Screen-height based
        if (!wasGrounded && isGrounded)
        {
            // Calculate fall distance
            totalFallDistance = fallStartHeight - transform.position.y;
            
            // Check for hard landing using screen-height detection
            CheckForHardLanding(currentFallSpeed, totalFallDistance);
            
            // Reset fall tracking
            fallStartHeight = transform.position.y;
            totalFallDistance = 0f;
            fellFromOffScreen = false;
        }
        
        // Start tracking fall when beginning to descend
        if (wasGrounded && !isGrounded && rb.linearVelocity.y <= 0)
        {
            fallStartHeight = transform.position.y;
            fellFromOffScreen = CheckIfFellFromOffScreen(fallStartHeight);
        }
        
        // Reset various states when grounded
        if (isGrounded)
        {
            airDashCount = 0;
            hasDoubleJumped = false;
            isWallSliding = false;
            isTouchingWall = false;
        }
    }
    
    /// <summary>
    /// Updates all active timers and cooldowns
    /// </summary>
    private void UpdateTimers()
    {
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
        
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                EndDash();
            }
        }
        
        if (attackCooldownTimer > 0)
        {
            attackCooldownTimer -= Time.deltaTime;
        }
        
        if (isAttacking)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0)
            {
                EndAttack();
            }
        }
        
        if (isWallJumping)
        {
            wallJumpTimer -= Time.deltaTime;
            if (wallJumpTimer <= 0)
            {
                isWallJumping = false;
            }
        }
        
        if (isTouchingWall && !isGrounded)
        {
            wallStickTimer -= Time.deltaTime;
        }
        else
        {
            wallStickTimer = wallStickTime;
        }
        
        if (isGrounded)
        {
            wallClingTimer = 0f;
            isWallClinging = false;
        }
        
        if (isHardLanding)
        {
            hardLandingTimer -= Time.deltaTime;
            if (hardLandingTimer <= 0)
            {
                EndHardLanding();
            }
        }
        
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            hasDoubleJumped = false;
        }
        else if (coyoteTimeCounter > 0)
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
        
        if (jumpBufferCounter > 0)
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (swimDashCooldownTimer > 0)
        {
            swimDashCooldownTimer -= Time.deltaTime;
        }
        
        if (isSwimDashing)
        {
            swimDashTimer -= Time.deltaTime;
            if (swimDashTimer <= 0)
            {
                EndSwimDash();
            }
        }

        if (isLedgeClimbing)
        {
            ledgeClimbTimer -= Time.deltaTime;
            if (ledgeClimbTimer <= 0)
            {
                isLedgeClimbing = false;
                rb.gravityScale = normalGravityScale;
            }
        }

        if (isLedgeGrabbing)
        {
            ledgeGrabTime += Time.deltaTime;
        }
        else
        {
            ledgeGrabTime = 0f;
        }
    }
    
    /// <summary>
    /// Flips player sprite horizontally
    /// </summary>
    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }
    
    /// <summary>
    /// Updates attack point position
    /// </summary>
    private void UpdateAttackPointPosition()
    {
        if (attackPoint != null)
        {
            attackPoint.localPosition = new Vector3(
                facingRight ? Mathf.Abs(attackPoint.localPosition.x) : -Mathf.Abs(attackPoint.localPosition.x),
                attackPoint.localPosition.y,
                attackPoint.localPosition.z
            );
        }
    }
    
    /// <summary>
    /// Controls animation states
    /// </summary>
    private void SetAnimation(float horizontalInput)
    {
        if (isLedgeClimbing)
        {
            animator.Play("Player_ledge_climb");
        }
        else if (isLedgeGrabbing)
        {
            animator.Play("Player_ledge_grab");
        }
        else if (isHardLanding)
        {
            animator.Play("Player_hard_land");
        }
        else if (isSwimming) // Add swimming animations
        {
            if (isSwimDashing)
            {
                animator.Play("Player_dash"); // Reuse dash animation or create "Player_swim_dash"
            }
            else if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                if (dashAction.IsPressed() && !isSwimDashing)
                {
                    animator.Play("Player_swim_fast"); // Fast swimming
                }
                else
                {
                    animator.Play("Player_swim"); // Normal swimming
                }
            }
            else
            {
                animator.Play("Player_swim_idle"); // Floating in water
            }
        }
        else if (isAttacking)
        {
            animator.Play("Player_attack");
        }
        else if (isDashing && dashUnlocked) 
        {
            animator.Play("Player_dash");
        }
        else if (isWallSliding && wallJumpUnlocked) 
        {
            animator.Play("Player_wallslide");
        }
        else if (isWallJumping && wallJumpUnlocked) 
        {
            animator.Play("Player_jump");
        }
        else if (isGrounded)
        {
            if (horizontalInput == 0)
            {
                animator.Play("Player_idle");
            }
            else
            {
                if (isRunning && runUnlocked) 
                {
                    animator.Play("Player_run");
                }
                else
                {
                    animator.Play("Player_walk");
                }
            }
        }
        else
        {
            if (rb.linearVelocity.y > 0)
            {
                animator.Play("Player_jump");
            }
            else
            {
                float fallSpeedNormalized = Mathf.Abs(rb.linearVelocity.y) / Mathf.Abs(maxFallSpeed);
                animator.SetFloat("FallSpeed", fallSpeedNormalized);
                animator.Play("Player_fall");
            }
        }
    }

    /// <summary>
    /// Checks if the player is in water
    /// </summary>
    private void CheckWater()
    {
        wasInWater = isInWater;
        
        // Check multiple points for reliable detection
        Vector2 checkCenter = transform.position;
        float checkRadius = 0.4f;
        
        Collider2D waterCollider = Physics2D.OverlapCircle(
            checkCenter, 
            checkRadius, 
            waterLayer
        );
        
        isInWater = waterCollider != null;
        currentWaterCollider = waterCollider;
        
        if (isInWater && waterCollider != null)
        {
            waterSurfaceY = waterCollider.bounds.max.y;
        }
        
        // Handle state changes
        if (isInWater && !wasInWater)
        {
            OnEnterWater();
        }
        else if (!isInWater && wasInWater)
        {
            OnExitWater();
        }
    }

    /// <summary>
    /// Called when player enters water
    /// </summary>
    private void OnEnterWater()
    {
        isSwimming = true;
        
        // Initialize floating effect
        floatTimer = Random.Range(0f, Mathf.PI * 2f); // Random starting point for variety
        originalPosition = transform.position;
        currentFloatOffset = 0f;
        targetFloatOffset = 0f;
        
        // Fast entry - immediately set to appropriate downward speed
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x * 0.7f,
                -3f
            );
        }
        else
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x * 0.7f,
                Mathf.Min(rb.linearVelocity.y, 2f) * 0.3f
            );
        }
        
        // Set low gravity for buoyancy feel
        rb.gravityScale = waterEntryGravity;
        
        // Reset other states
        isDashing = false;
        isWallJumping = false;
        wallSlideState = WallSlideState.None;
        isWallSliding = false;
        
        airDashCount = 0;
        hasDoubleJumped = false;
        
    }

    /// <summary>
    /// Called when player exits water
    /// </summary>
    private void OnExitWater()
    {
        isSwimming = false;
        isSwimDashing = false;
        isInWater = false;
        
        // Restore normal physics IMMEDIATELY
        rb.gravityScale = normalGravityScale;
        
        swimDashCooldownTimer = 0f;
        
    }
    
    // ====================================================================
    // ABILITY UNLOCK METHODS
    // ====================================================================
    
    public void UnlockDash()
    {
        dashUnlocked = true;
    }
    
    public void UnlockRun()
    {
        runUnlocked = true;
    }
    
    public void UnlockWallJump()
    {
        wallJumpUnlocked = true;
    }
    
    public void UnlockDoubleJump()
    {
        doubleJumpUnlocked = true;
    }
    
    public void UnlockAllAbilities()
    {
        UnlockDash();
        UnlockRun();
        UnlockWallJump();
        UnlockDoubleJump();
    }
    
    public void ResetAbilities()
    {
        dashUnlocked = false;
        runUnlocked = false;
        wallJumpUnlocked = false;
        doubleJumpUnlocked = false;
    }
    
    // ====================================================================
    // EDITOR VISUALIZATION
    // ====================================================================
    
    void OnDrawGizmosSelected()
    {
        /*if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        
        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            Vector2 offset = new Vector2(wallCheckOffset, 0);
            Gizmos.DrawLine(wallCheck.position + (Vector3)offset, 
                          wallCheck.position + (Vector3)offset + Vector3.right * wallCheckDistance);
            Gizmos.DrawLine(wallCheck.position - (Vector3)offset, 
                          wallCheck.position - (Vector3)offset + Vector3.left * wallCheckDistance);
        }
        
        if (attackPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
        
        if (!Application.isPlaying || mainCamera == null) return;
        
        // Fall tracking visualization
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            new Vector3(transform.position.x - 1f, fallStartHeight, 0),
            new Vector3(transform.position.x + 1f, fallStartHeight, 0)
        );
        
        // Screen height threshold visualization
        float requiredDistance = screenHeightInUnits * minScreenHeightForHardLanding;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            new Vector3(transform.position.x - 1f, fallStartHeight - requiredDistance, 0),
            new Vector3(transform.position.x + 1f, fallStartHeight - requiredDistance, 0)
        );
        
        // Current position
        Gizmos.color = Color.white;
        Gizmos.DrawLine(
            new Vector3(transform.position.x - 1f, transform.position.y, 0),
            new Vector3(transform.position.x + 1f, transform.position.y, 0)
        );
        
        // Screen bounds visualization (for off-screen detection)
        Bounds screenBounds = GetScreenBounds();
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(screenBounds.center, screenBounds.size);
        
        // Off-screen detection area
        Gizmos.color = Color.magenta;
        Vector3 offScreenTop = new Vector3(screenBounds.center.x, screenBounds.max.y + screenHeightDetectionOffset, 0);
        Gizmos.DrawLine(
            new Vector3(screenBounds.min.x, offScreenTop.y, 0),
            new Vector3(screenBounds.max.x, offScreenTop.y, 0)
        );

        if (isSwimming && currentWaterCollider != null)
        {
            // Draw target surface position
            float targetY = waterSurfaceY + waterSurfaceOffset;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                new Vector3(transform.position.x - 0.5f, targetY, 0),
                new Vector3(transform.position.x + 0.5f, targetY, 0)
            );
            
            // Draw current position
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                new Vector3(transform.position.x - 0.5f, transform.position.y, 0),
                new Vector3(transform.position.x + 0.5f, transform.position.y, 0)
            );
        }

        if (isSwimming && enableFloating)
        {
            // Draw floating effect range
            Gizmos.color = Color.cyan;
            float floatRange = floatAmplitude * 2f;
            Gizmos.DrawWireCube(
                new Vector3(transform.position.x, originalPosition.y, 0),
                new Vector3(0.5f, floatRange, 0)
            );
            
            // Draw current float offset
            Gizmos.color = Color.white;
            Gizmos.DrawLine(
                new Vector3(transform.position.x - 0.25f, originalPosition.y + currentFloatOffset, 0),
                new Vector3(transform.position.x + 0.25f, originalPosition.y + currentFloatOffset, 0)
            );
        }*/

        // Ledge detection visualization
        if (ledgeCheckPoint != null && Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(ledgeCheckPoint.position, 0.1f);
            
            if (ledgeDetected)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(ledgePosition, new Vector3(0.3f, 0.1f, 0));
            }
        }

        // Ledge positioning debug
        if (Application.isPlaying && isLedgeGrabbing)
        {
            // Draw where the player should be hanging
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(ledgePosition, 0.1f);
            
            // Draw player's current position
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
            
            // Draw line between them
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, ledgePosition);
            
            // Draw the platform surface
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(
                new Vector3(ledgePosition.x - 0.5f, ledgePosition.y, 0),
                new Vector3(ledgePosition.x + 0.5f, ledgePosition.y, 0)
            );
        }
    }

    /// <summary>
    /// Draws debug information in the scene view
    /// </summary>
    private void OnDrawGizmos()
    {
        // Only draw in Play mode
        if (!Application.isPlaying) return;
        
        // Draw ledge detection info
        if (ledgeCheckPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(ledgeCheckPoint.position, 0.1f);
            
            if (ledgeDetected)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(ledgePosition, new Vector3(0.3f, 0.1f, 0));
                
                // Draw line from player to ledge
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, ledgePosition);
            }
        }
        
        // Draw state info above player
        Vector3 textPos = transform.position + Vector3.up * 2f;
        
        // Use Handles for text (requires UnityEditor namespace)
        #if UNITY_EDITOR
        string stateInfo = $"LedgeDetected: {ledgeDetected}\n" +
                        $"IsLedgeGrabbing: {isLedgeGrabbing}\n" +
                        $"IsLedgeClimbing: {isLedgeClimbing}\n" +
                        $"LedgeSide: {ledgeSide}\n" +
                        $"LedgePos: {ledgePosition}\n" +
                        $"VelocityY: {rb.linearVelocity.y:F2}";
        
        UnityEditor.Handles.Label(textPos, stateInfo);
        #endif
    }

    /// <summary>
    /// Displays debug information on screen
    /// </summary>
    private void DisplayDebugInfo()
    {
        // Create a debug string
        string debugInfo = $"Ledge System Debug:\n" +
                        $"LedgeDetected: {ledgeDetected}\n" +
                        $"IsGrabbing: {isLedgeGrabbing}\n" +
                        $"IsClimbing: {isLedgeClimbing}\n" +
                        $"LedgeSide: {ledgeSide}\n" +
                        $"VelocityY: {rb.linearVelocity.y:F2}\n" +
                        $"MoveInput: {moveInput}\n" +
                        $"IsGrounded: {isGrounded}\n";
        
        // Log to console for debugging
        if (ledgeDetected && !isLedgeGrabbing && !isLedgeClimbing)
        {
            Debug.Log(debugInfo);
        }
    }
}



// ====================================================================
// CAMERA SHAKE COMPONENT
// ====================================================================

public class CameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    public float shakeIntensity = 0.1f;
    public float shakeDuration = 0.2f;
    public float dampingSpeed = 1.0f;
    
    private Vector3 initialPosition;
    private float shakeTimer = 0f;
    private float currentIntensity = 0f;
    
    void Start()
    {
        initialPosition = transform.localPosition;
    }
    
    void Update()
    {
        if (shakeTimer > 0)
        {
            transform.localPosition = initialPosition + Random.insideUnitSphere * currentIntensity;
            shakeTimer -= Time.deltaTime * dampingSpeed;
            currentIntensity = Mathf.Lerp(0f, currentIntensity, shakeTimer / shakeDuration);
        }
        else
        {
            shakeTimer = 0f;
            transform.localPosition = initialPosition;
        }
    }
    
    public void Shake(float intensity, float duration)
    {
        if (intensity > currentIntensity)
        {
            currentIntensity = intensity;
        }
        
        if (duration > shakeTimer)
        {
            shakeTimer = duration;
            this.shakeDuration = duration;
        }
    }
    
    public void StopShake()
    {
        shakeTimer = 0f;
        transform.localPosition = initialPosition;
    }
}