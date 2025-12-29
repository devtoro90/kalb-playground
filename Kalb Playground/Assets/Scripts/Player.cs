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
    public float wallSlideGravity = 0.5f;
    
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
    public float dashShakeIntensity = 0.15f;
    public float dashShakeDuration = 0.15f;
    public float attackShakeIntensity = 0.1f;
    public float attackShakeDuration = 0.1f;
    public float wallJumpShakeIntensity = 0.08f;
    public float wallJumpShakeDuration = 0.1f;
    
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
    private bool wasAgainstWallLastFrame = false;
    private int lastWallSide = 0;
    private float blockedMoveInput = 0f;
    private bool isInputBlocked = false;
    private float wallDetectionHeight = 1.0f;
    private float wallDetectionStep = 0.2f;
    private bool isAgainstWallAnywhere = false;
    
    // Wall Interaction Variables
    private int wallSide = 0;
    private enum WallSlideState { None, Starting, Sliding, Jumping }
    private WallSlideState wallSlideState = WallSlideState.None;
    
    // Dash Variables
    private Vector2 dashDirection = Vector2.right;
    private int airDashCount = 0;
    
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
        
        // Get camera reference for screen height calculations
        mainCamera = Camera.main;
        CalculateScreenHeightInUnits();
        
        // Get or create CameraShake component
        SetupCameraShake();
        
        SetupMissingObjects();
        
        Debug.Log("Screen height for hard landing: " + (screenHeightInUnits * minScreenHeightForHardLanding) + " units");
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
            
            Debug.Log("Screen Height in World Units: " + screenHeightInUnits);
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
                Debug.Log("CameraShake component added to main camera");
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
        
        // 4. Input Handling
        HandleDashInput();
        HandleRunInput();
        HandleJumpInput();
        HandleAttackInput();
        
        // 5. State Management
        HandleWallSlide();
        
        // 6. Visual Feedback
        SetAnimation(moveInput.x);
    }
    
    /// <summary>
    /// Handles physics-based movement and collision checks
    /// </summary>
    void FixedUpdate()
    {
        // 1. Environment Detection
        UpdateGroundCheck();
        
        // 2. Gravity Control
        UpdateGravity();
        
        // 3. Wall Collision Prevention
        PreventWallStick();
        
        // 4. Movement Execution
        HandleMovement();
        
        // 5. Sprite Orientation
        if (!isDashing && !isAttacking && !isWallJumping && !isWallSliding && !isHardLanding)
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
        if (!dashUnlocked || isHardLanding) return;
        
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
        if (isHardLanding) return;
        
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
    /// Hollow Knight-style gravity system
    /// </summary>
    private void UpdateGravity()
    {
        if (isDashing || isWallSliding || isHardLanding || isWallJumping)
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
            Debug.Log("Forced hard landing: Fell from off-screen!");
        }
        
        if (shouldHardLand)
        {
            StartHardLanding();
            
            // Debug output
            string method = useScreenHeightForHardLanding ? "Screen Height" : "Velocity Only";
            Debug.Log($"Hard Landing! ({method}) " +
                     $"Fall Speed: {fallSpeed:F1}, " +
                     $"Distance: {fallDistance:F1}, " +
                     $"Screen Height: {screenHeightInUnits * minScreenHeightForHardLanding:F1}, " +
                     $"Off-screen: {fellFromOffScreenPosition}");
        }
        else if (fallDistance > 0.5f)
        {
            // Soft landing with screen shake based on impact
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
        if (!wallJumpUnlocked || isHardLanding) 
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
        if (!wallJumpUnlocked || isHardLanding)
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
        if (isHardLanding)
        {
            animator.Play("Player_hard_land");
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
    
    // ====================================================================
    // ABILITY UNLOCK METHODS
    // ====================================================================
    
    public void UnlockDash()
    {
        dashUnlocked = true;
        Debug.Log("Dash unlocked!");
    }
    
    public void UnlockRun()
    {
        runUnlocked = true;
        Debug.Log("Run unlocked!");
    }
    
    public void UnlockWallJump()
    {
        wallJumpUnlocked = true;
        Debug.Log("Wall jump unlocked!");
    }
    
    public void UnlockDoubleJump()
    {
        doubleJumpUnlocked = true;
        Debug.Log("Double jump unlocked!");
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
        Debug.Log("All abilities reset to locked state.");
    }
    
    // ====================================================================
    // EDITOR VISUALIZATION
    // ====================================================================
    
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
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