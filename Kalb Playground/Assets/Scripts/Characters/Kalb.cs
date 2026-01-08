using UnityEngine;
using UnityEngine.InputSystem;

public class Kalb : MonoBehaviour
{
    // ====================================================================
    // SECTION 1: INSPECTOR CONFIGURATION VARIABLES
    // ====================================================================
    
    // All public variables that can be configured in the Unity Inspector
    
    [Header("Ability Unlocks")]
    public bool runUnlocked = false;
    public bool dashUnlocked = false;
    public bool wallJumpUnlocked = false;
    public bool doubleJumpUnlocked = false;
    
    [Header("Basic Movement")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpForce = 12f;
    [Range(0, 0.3f)] public float movementSmoothing = 0.05f;
    public bool facingRight = true;
    
    [Header("Jump & Air Movement")]
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.1f;
    public float jumpCutMultiplier = 0.5f;
    public bool hasDoubleJump = true;
    public float doubleJumpForce = 10f;
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
    
    [Header("Wall Interaction")]
    public float wallSlideSpeed = 2f;
    public float wallJumpForce = 11f;
    public Vector2 wallJumpAngle = new Vector2(1, 2);
    public float wallJumpDuration = 0.2f;
    public float wallStickTime = 0.25f;
    public float wallClingTime = 0.2f;
    public float wallClingSlowdown = 0.3f;
    
    [Header("Falling & Landing")]
    public float maxFallSpeed = -20f;
    public float hardLandingThreshold = -15f;
    public float hardLandingStunTime = 0.3f;
    public float fallingGravityScale = 2.5f;
    public float normalGravityScale = 2f;
    public float quickFallGravityMultiplier = 1.2f;
    
    [Header("Screen-Height Hard Landing")]
    public bool useScreenHeightForHardLanding = true;
    public float minScreenHeightForHardLanding = 0.8f;
    public float screenHeightDetectionOffset = 1.0f;
    [Tooltip("If false, uses velocity threshold only")]
    public bool requireBothConditions = true;
    
    [Header("Screen Shake")]
    public bool enableScreenShake = true;
    public float hardLandingShakeIntensity = 0.15f;
    public float hardLandingShakeDuration = 0.25f;
    
    [Header("Attack")]
    public float attackCooldown = 0.1f;
    public float attackDuration = 0.2f;
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;
    public int attackDamage = 20;

    [Header("Combo Attack System")]
    public int maxComboHits = 3;                     // Maximum combo hits
    public float comboWindow = 0.2f;                 // Time window to continue combo
    public float comboResetTime = 0.6f;              // Time before combo resets
    public bool enableAirCombo = true;               // Can combo in air
    public bool enableWallCombo = true;              // Can combo while wall sliding

    [Header("Combo Attack Settings")]
    public float[] comboDamage = new float[] { 20f, 25f, 35f };     // Damage per combo hit
    public float[] comboKnockback = new float[] { 5f, 7f, 12f };    // Knockback per hit
    public float[] comboRange = new float[] { 0.2f, 0.2f, 0.2f };   // Range per hit
    public float[] comboAttackDurations = new float[] { 0.2f, 0.2f, 0.2f };  // Duration per attack
    public float[] comboCooldowns = new float[] { 0.3f, 0.4f, 0.6f };         // Cooldown per attack

    [Header("Combo Animation Names")]
    public string[] comboAnimations = new string[] { "Kalb_attack1", "Kalb_attack2", "Kalb_attack3" };
    public string comboResetAnimation = "Kalb_attack_reset";
    
    [Header("Environment Detection")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public Transform wallCheck;
    public float wallCheckDistance = 0.05f;
    public float wallCheckOffset = 0.02f;
    public LayerMask environmentLayer;
    
    [Header("Swimming")]
    public float swimSpeed = 3f;
    public float swimFastSpeed = 5f;
    public float swimDashSpeed = 10f;
    public float swimJumpForce = 8f;
    public float waterSurfaceOffset = 1.20f;
    public float waterEntrySpeedReduction = 0.5f;
    public LayerMask waterLayer;
    public float waterCheckRadius = 0.5f;
    public Transform waterCheckPoint;
    public float waterEntryGravity = 0.5f;
    public float buoyancyStrength = 50f;
    public float buoyancyDamping = 10f;
    public float maxBuoyancyForce = 20f;
    
    [Header("Floating Effect")]
    public float floatAmplitude = 0.05f;
    public float floatFrequency = 1f;
    public float floatSmoothness = 5f;
    public bool enableFloating = true;
    
    [Header("Ledge System")]
    public float ledgeDetectionDistance = 0.5f;
    public float ledgeGrabOffsetY = 0.15f;
    public float ledgeGrabOffsetX = 0.55f;
    public float ledgeClimbTime = 0.5f;
    public float ledgeJumpForce = 12f;
    public Vector2 ledgeJumpAngle = new Vector2(1, 2);
    public float ledgeClimbCheckRadius = 0.2f;
    public Transform ledgeCheckPoint;
    public float minLedgeHoldTime = 0.3f;
    public float ledgeReleaseForce = 5f;
    public float ledgeReleaseCooldown = 0.2f;
    
    // ====================================================================
    // SECTION 2: PRIVATE STATE VARIABLES
    // ====================================================================
    
    // Organized into logical groups for clarity
    
    // COMPONENT REFERENCES
    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction attackAction;
    private Animator animator;
    private CameraShake cameraShake;
    private Camera mainCamera;
    private Collider2D playerCollider;
    
    // INPUT STATE
    private Vector2 moveInput;
    private bool isJumpButtonHeld = false;
    private bool isRunning = false;
    private bool attackQueued = false; 
    
    // MOVEMENT STATE
    private Vector3 velocity = Vector3.zero;
    private bool isGrounded;
    private float currentFallSpeed = 0f;
    
    // ACTION STATE FLAGS (organized by system)
    private bool isDashing = false;
    private bool isAttacking = false;
    private bool isWallJumping = false;
    private bool isHardLanding = false;
    private bool isSwimming = false;
    private bool isSwimDashing = false;
    private bool isLedgeGrabbing = false;
    private bool isLedgeClimbing = false;
    private int currentCombo = 0;                   // Current combo count (0 = no combo)
    private bool comboAvailable = true;            // Can start/combo attacks
    private bool isComboFinishing = false;         // Final combo attack is active
    
    // WALL INTERACTION STATE
    private bool isWallSliding;
    private bool isTouchingWall;
    private bool isWallClinging = false;
    private bool isAgainstWall = false;
    private int wallSide = 0;
    private int lastWallSide = 0;
    private float wallNormalDistance = 0.05f;
    
    // WALL SLIDE STATE MACHINE
    private enum WallSlideState { None, Starting, Sliding, Jumping }
    private WallSlideState wallSlideState = WallSlideState.None;
    
    // JUMP & AIR MOVEMENT STATE
    private float coyoteTimeCounter = 0f;
    private float jumpBufferCounter = 0f;
    private bool hasDoubleJumped = false;
    private int airDashCount = 0;
    
    // FALLING & LANDING TRACKING
    private float peakHeight = 0f;
    private float fallStartHeight = 0f;
    private float totalFallDistance = 0f;
    private float screenHeightInUnits = 0f;
    private bool fellFromOffScreen = false;
    
    // TIMERS & COOLDOWNS (organized alphabetically)
    private float attackCooldownTimer = 0f;
    private float attackTimer = 0f;
    private float dashCooldownTimer = 0f;
    private float dashTimer = 0f;
    private float hardLandingTimer = 0f;
    private float wallClingTimer = 0f;
    private float wallJumpTimer = 0f;
    private float wallStickTimer = 0f;
    private float comboWindowTimer = 0f;           // Already declared above, just note it's here
    private float comboResetTimer = 0f;           // Already declared above
    
    // DASH VARIABLES
    private Vector2 dashDirection = Vector2.right;
    
    // SWIMMING STATE
    private bool isInWater = false;
    private bool wasInWater = false;
    private float swimDashTimer = 0f;
    private float swimDashCooldownTimer = 0f;
    private float swimDashDuration = 0.15f;
    private float swimDashCooldown = 0.3f;
    private Vector2 swimDashDirection = Vector2.right;
    private float waterSurfaceY = 0f;
    private Collider2D currentWaterCollider = null;
    private float preDashGravityScale;
    
    // FLOATING EFFECT
    private float floatTimer = 0f;
    private float currentFloatOffset = 0f;
    private float targetFloatOffset = 0f;
    private Vector3 originalPosition;
    
    // LEDGE SYSTEM STATE
    private bool ledgeDetected = false;
    private Vector2 ledgePosition;
    private float ledgeClimbTimer = 0f;
    private int ledgeSide = 0;
    private float ledgeGrabTime = 0f;
    private float currentLedgeHoldTime = 0f;
    private float ledgeReleaseTimer = 0f;
    private bool canGrabLedge = true;
    
    // ====================================================================
    // SECTION 3: UNITY LIFE CYCLE METHODS
    // ====================================================================
    
    /// <summary>
    /// Called when the script instance is loaded
    /// Sets up all component references and initializes systems
    /// </summary>
    void Start()
    {
        InitializeComponents();
        CalculateScreenHeightInUnits();
        SetupCameraShake();
        SetupMissingObjects();
    }
    
    /// <summary>
    /// Called every frame
    /// Handles input reading, timer updates, and non-physics logic
    /// Execution order is carefully maintained for proper state management
    /// </summary>
    void Update()
    {
        // PHASE 1: INPUT READING
        ReadInputs();
        
        // PHASE 2: TIMER UPDATES
        UpdateAllTimers();
        
        // PHASE 3: ENVIRONMENT CHECKS
        CheckEnvironment();
        
        // PHASE 4: STATE-BASED INPUT HANDLING
        HandleStateBasedInputs();
        
        // PHASE 5: STATE MANAGEMENT
        ManagePlayerStates();
        
        // PHASE 6: VISUAL FEEDBACK
        UpdateAnimations();
    }
    
    /// <summary>
    /// Called at fixed time intervals for physics calculations
    /// Handles movement, collisions, and physics-based state changes
    /// </summary>
    void FixedUpdate()
    {
        // PHASE 1: UPDATE ENVIRONMENT STATE
        UpdateGroundCheck();
        
        // PHASE 2: APPLY PHYSICS
        ApplyPhysicsBasedSystems();
        
        // PHASE 3: EXECUTE MOVEMENT
        ExecuteMovement();
        
        // PHASE 4: UPDATE ORIENTATION
        UpdatePlayerOrientation();
    }
    
    // ====================================================================
    // SECTION 4: INITIALIZATION METHODS
    // ====================================================================
    
    /// <summary>
    /// Gets references to all required components
    /// Sets up input system actions
    /// </summary>
    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        dashAction = playerInput.actions["Dash/Run"];
        attackAction = playerInput.actions["Attack"];
        
        animator = GetComponent<Animator>();
        playerCollider = GetComponent<Collider2D>();
        
        originalPosition = transform.position;
        mainCamera = Camera.main;
    }
    
    /// <summary>
    /// Calculates screen height in world units for fall detection
    /// Used for screen-height based hard landing system
    /// </summary>
    private void CalculateScreenHeightInUnits()
    {
        if (mainCamera != null)
        {
            Vector3 topOfScreen = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 1f, 0));
            Vector3 bottomOfScreen = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, 0));
            screenHeightInUnits = Vector3.Distance(topOfScreen, bottomOfScreen);
        }
        else
        {
            screenHeightInUnits = 10f;
            Debug.LogWarning("Main camera not found. Using default screen height.");
        }
    }
    
    /// <summary>
    /// Sets up the camera shake component on the main camera
    /// Creates one if it doesn't exist
    /// </summary>
    private void SetupCameraShake()
    {
        if (Camera.main != null)
        {
            cameraShake = Camera.main.GetComponent<CameraShake>();
            if (cameraShake == null)
                cameraShake = Camera.main.gameObject.AddComponent<CameraShake>();
        }
        else
        {
            Debug.LogWarning("Main camera not found. Screen shake will not work.");
        }
    }
    
    /// <summary>
    /// Creates required child GameObjects if not assigned in Inspector
    /// Prevents null reference errors
    /// </summary>
    private void SetupMissingObjects()
    {
        CreateIfMissing(ref groundCheck, "GroundCheck", new Vector3(0, -0.65f, 0));
        CreateIfMissing(ref wallCheck, "WallCheck", new Vector3(0.5f, 0, 0));
        CreateIfMissing(ref attackPoint, "AttackPoint", new Vector3(0.5f, 0, 0));
        CreateIfMissing(ref waterCheckPoint, "WaterCheck", new Vector3(0, 0.2f, 0));
        CreateIfMissing(ref ledgeCheckPoint, "LedgeCheck", new Vector3(0, 0.5f, 0));
    }
    
    /// <summary>
    /// Helper method to create and assign transform references
    /// </summary>
    private void CreateIfMissing(ref Transform transformRef, string name, Vector3 localPosition)
    {
        if (transformRef == null)
        {
            GameObject obj = new GameObject(name);
            obj.transform.parent = this.transform;
            obj.transform.localPosition = localPosition;
            transformRef = obj.transform;
        }
    }
    
    // ====================================================================
    // SECTION 5: UPDATE PHASE METHODS
    // ====================================================================
    
    /// <summary>
    /// PHASE 1: Reads all player inputs
    /// Called in Update()
    /// </summary>
    private void ReadInputs()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        UpdateJumpButtonState();
    }
    
    /// <summary>
    /// Tracks jump button state for variable jump height
    /// Also handles jump cut when button is released
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
            // Apply jump cut if moving upward and not in special states
            if (rb.linearVelocity.y > 0 && !isDashing && !isWallJumping && 
                !isWallSliding && !isHardLanding)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }
        }
    }
    
    /// <summary>
    /// PHASE 2: Updates all active timers and cooldowns
    /// Called in Update()
    /// </summary>
    private void UpdateAllTimers()
    {
        // General action timers
        UpdateActionTimers();
        
        // Jump & air movement timers
        UpdateJumpTimers();
        
        // Special system timers
        UpdateSwimTimers();
        UpdateLedgeTimers();
        UpdateComboTimers();
        
    }
    
    /// <summary>
    /// Updates timers for attacks, dashes, and wall interactions
    /// </summary>
    private void UpdateActionTimers()
    {
        // Dash timers
        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0) EndDash();
        }
        
        // Attack timers
        if (attackCooldownTimer > 0) attackCooldownTimer -= Time.deltaTime;
        if (isAttacking)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0) EndAttack();
        }
        
        // Wall interaction timers
        if (wallClingTimer > 0) wallClingTimer -= Time.deltaTime;
        if (isWallJumping)
        {
            wallJumpTimer -= Time.deltaTime;
            if (wallJumpTimer <= 0) isWallJumping = false;
        }
        
        // Hard landing timer
        if (isHardLanding)
        {
            hardLandingTimer -= Time.deltaTime;
            if (hardLandingTimer <= 0) EndHardLanding();
        }
    }
    
    /// <summary>
    /// Updates timers related to jumping and air movement
    /// </summary>
    private void UpdateJumpTimers()
    {
        // Coyote time: Allows jumping briefly after leaving ground
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            hasDoubleJumped = false;
        }
        else if (coyoteTimeCounter > 0)
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
        
        // Jump buffer: Allows preemptive jump input before landing
        if (jumpBufferCounter > 0)
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }
    
    /// <summary>
    /// Updates swimming-related timers
    /// </summary>
    private void UpdateSwimTimers()
    {
        if (swimDashCooldownTimer > 0) swimDashCooldownTimer -= Time.deltaTime;
        if (isSwimDashing)
        {
            swimDashTimer -= Time.deltaTime;
            if (swimDashTimer <= 0) EndSwimDash();
        }
    }
    
    /// <summary>
    /// Updates ledge system timers
    /// </summary>
    private void UpdateLedgeTimers()
    {
        // Ledge release cooldown
        if (ledgeReleaseTimer > 0) ledgeReleaseTimer -= Time.deltaTime;
        
        // Reset ledge grab ability after cooldown
        if (!canGrabLedge && ledgeReleaseTimer <= 0)
        {
            canGrabLedge = true;
        }
        
        // Update grab time while holding ledge
        if (isLedgeGrabbing)
        {
            ledgeGrabTime += Time.deltaTime;
            currentLedgeHoldTime += Time.deltaTime;
        }
        else
        {
            ledgeGrabTime = 0f;
            currentLedgeHoldTime = 0f;
        }
        
        // Ledge climb timer
        if (isLedgeClimbing)
        {
            ledgeClimbTimer -= Time.deltaTime;
            if (ledgeClimbTimer <= 0)
            {
                isLedgeClimbing = false;
                rb.gravityScale = normalGravityScale;
            }
        }
    }

    /// <summary>
    /// Updates combo system timers
    /// </summary>
    private void UpdateComboTimers()
    {
        // Combo window timer (time to continue combo)
        if (comboWindowTimer > 0)
        {
            comboWindowTimer -= Time.deltaTime;
            if (comboWindowTimer <= 0)
            {
                comboWindowTimer = 0;
                
                // If we're not attacking and combo window closed, start reset timer
                if (!isAttacking && currentCombo > 0)
                {
                    comboResetTimer = comboResetTime;
                }
            }
        }
        
        // Combo reset timer (time before combo resets to 0)
        if (comboResetTimer > 0)
        {
            comboResetTimer -= Time.deltaTime;
            if (comboResetTimer <= 0)
            {
                ResetCombo();
            }
        }
    }
    
    /// <summary>
    /// PHASE 3: Checks the environment around the player
    /// Called in Update()
    /// </summary>
    private void CheckEnvironment()
    {
        CheckWall();
        CheckWater();
        
        // Only detect ledges if we can grab them and aren't already grabbing/climbing
        if (canGrabLedge && !isLedgeGrabbing && !isLedgeClimbing)
        {
            ledgeDetected = CheckForLedge();
        }
    }
    
    /// <summary>
    /// PHASE 4: Handles inputs based on current player state
    /// Called in Update()
    /// </summary>
    private void HandleStateBasedInputs()
    {
        // Handle swimming inputs if swimming
        if (isSwimming && !isHardLanding)
        {
            HandleSwimInput();
        }
        
        // Handle other inputs if not in special states
        if (!isHardLanding && !isLedgeGrabbing && !isLedgeClimbing)
        {
            HandleDashInput();
            HandleRunInput();
            HandleJumpInput();
            HandleAttackInput();
        }
        
        // Always handle ledge input (it checks its own state)
        HandleLedgeInput();
    }
    
    /// <summary>
    /// PHASE 5: Manages player state transitions and updates
    /// Called in Update()
    /// </summary>
    private void ManagePlayerStates()
    {
        // Handle wall sliding if unlocked and not in ledge states
        if (wallJumpUnlocked && !isHardLanding && !isLedgeGrabbing && !isLedgeClimbing)
        {
            HandleWallSlide();
        }
        
        // Handle ledge climbing if active
        if (isLedgeClimbing)
        {
            HandleLedgeClimb();
        }
    }
    
    /// <summary>
    /// PHASE 6: Updates animations based on current state
    /// Called in Update()
    /// </summary>
    private void UpdateAnimations()
    {
        SetAnimation(moveInput.x);
    }
    
    // ====================================================================
    // SECTION 6: FIXEDUPDATE PHASE METHODS
    // ====================================================================
    
    /// <summary>
    /// PHASE 1: Updates ground detection and landing logic
    /// Called in FixedUpdate()
    /// </summary>
    private void UpdateGroundCheck()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, environmentLayer);
        
        // Reset ledge grab ability when grounded
        if (isGrounded)
        {
            canGrabLedge = true;
            ledgeReleaseTimer = 0f;
        }
        
        // Release ledge if player becomes grounded while grabbing
        if (isGrounded && (isLedgeGrabbing || isLedgeClimbing))
        {
            ReleaseLedge();
        }
        
        // Track peak height during ascent
        if (!isGrounded && !wasGrounded && rb.linearVelocity.y > 0)
        {
            peakHeight = transform.position.y;
        }
        
        // LANDING DETECTION - Handle transitions from air to ground
        if (!wasGrounded && isGrounded)
        {
            // Calculate fall distance for hard landing detection
            totalFallDistance = fallStartHeight - transform.position.y;
            
            // Check for hard landing (screen-height or velocity based)
            CheckForHardLanding(currentFallSpeed, totalFallDistance);
            
            // Reset fall tracking
            fallStartHeight = transform.position.y;
            totalFallDistance = 0f;
            fellFromOffScreen = false;
        }
        
        // Start tracking fall when beginning descent
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
    /// PHASE 2: Applies physics-based systems like gravity
    /// Called in FixedUpdate()
    /// </summary>
    private void ApplyPhysicsBasedSystems()
    {
        // Skip gravity if ledge grabbing or climbing
        if (!isLedgeGrabbing && !isLedgeClimbing)
        {
            UpdateGravity();
        }
        
        // Prevent wall sticking if not climbing ledge
        if (!isLedgeClimbing)
        {
            PreventWallStick();
        }
    }
    
    /// <summary>
    /// PHASE 3: Executes movement based on current state
    /// Called in FixedUpdate()
    /// </summary>
    private void ExecuteMovement()
    {
        // Special state movement handlers (in priority order)
        if (isLedgeGrabbing || isLedgeClimbing)
        {
            HandleLedgeMovement();
        }
        else if (isSwimming)
        {
            HandleSwimMovement();
        }
        else
        {
            HandleGroundAndAirMovement();
        }
    }
    
    /// <summary>
    /// PHASE 4: Updates player orientation (facing direction)
    /// Called in FixedUpdate()
    /// </summary>
    private void UpdatePlayerOrientation()
    {
        // Only flip if not in special states that lock orientation
        if (!isLedgeGrabbing && !isLedgeClimbing && !isDashing && 
            !isAttacking && !isWallJumping && !isWallSliding && 
            !isHardLanding && !isSwimDashing)
        {
            HandleFlip();
        }
    }
    
    // ====================================================================
    // SECTION 7: INPUT HANDLING METHODS
    // ====================================================================
    
    /// <summary>
    /// Handles dash input with all required checks
    /// </summary>
    private void HandleDashInput()
    {
        if (!dashUnlocked || isHardLanding || isSwimming) return;
        
        if (dashAction.triggered && !isDashing && dashCooldownTimer <= 0 && 
            !isAttacking && !isWallSliding)
        {
            bool canDash = isGrounded || isWallSliding;
            
            // Check air dash availability
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
    /// Determines if player is running based on input and state
    /// </summary>
    private void HandleRunInput()
    {
        if (!runUnlocked || isHardLanding)
        {
            isRunning = false;
            return;
        }
        
        // Running requires holding dash button while grounded
        isRunning = dashAction.IsPressed() && isGrounded && !isDashing && 
                   !isAttacking && !isWallSliding;
    }
    
    /// <summary>
    /// Processes jump input with buffering and coyote time
    /// Prioritizes wall jumps, then double jumps, then normal jumps
    /// </summary>
    private void HandleJumpInput()
    {
        if (isHardLanding || isSwimming) return;
        
        // Buffer jump input
        if (jumpAction.triggered)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        
        // Process buffered jump
        if (jumpBufferCounter > 0)
        {
            // Priority 1: Wall jump
            if (isWallSliding && wallJumpUnlocked)
            {
                WallJump();
                jumpBufferCounter = 0;
                hasDoubleJumped = false;
            }
            // Priority 2: Double jump
            else if (!isGrounded && coyoteTimeCounter <= 0 && doubleJumpUnlocked && 
                    hasDoubleJump && !hasDoubleJumped && !isDashing && !isAttacking)
            {
                DoubleJump();
                jumpBufferCounter = 0;
            }
            // Priority 3: Normal jump (coyote time or grounded)
            else if (coyoteTimeCounter > 0)
            {
                NormalJump();
                jumpBufferCounter = 0;
                coyoteTimeCounter = 0;
            }
        }
    }
    
    /// <summary>
    /// Enhanced attack input handler with combo system
    /// </summary>
    private void HandleAttackInput()
    {
        if (isHardLanding || isLedgeGrabbing || isLedgeClimbing || isInWater || isSwimming) return;
        
        // Check if attack button was pressed this frame
        bool attackPressed = attackAction.triggered;
        
        if (!attackPressed) return; // No attack input this frame
        
        // If we're currently attacking, queue the next attack if within combo window
        if (isAttacking)
        {
            if (comboWindowTimer > 0 && currentCombo > 0 && currentCombo < maxComboHits)
            {
                attackQueued = true;
                comboWindowTimer = comboWindow; // Extend window for queued attack
            }
            return;
        }
        
        // Not currently attacking, check if we can start a new attack
        bool canAttack = attackCooldownTimer <= 0 && !isDashing && comboAvailable;
        
        // Additional checks based on state
        if (!isGrounded && !enableAirCombo)
            canAttack = false;
        if (isWallSliding && !enableWallCombo)
            canAttack = false;
        
        // Check if we need to reset combo first (max combo reached)
        if (currentCombo >= maxComboHits)
        {
            ResetCombo();
        }
        
        if (canAttack)
        {
            StartComboAttack();
        }
    }
    
    /// <summary>
    /// Handles swimming-specific inputs (dash and jump out of water)
    /// </summary>
    private void HandleSwimInput()
    {
        if (!isSwimming || isHardLanding) return;
        
        // Swim dash
        if (dashAction.triggered && !isSwimDashing && swimDashCooldownTimer <= 0)
        {
            StartSwimDash();
        }
        
        // Jump out of water
        if (jumpAction.triggered && !isSwimDashing)
        {
            SwimJump();
        }
    }
    
    /// <summary>
    /// Handles all ledge-related inputs including grab, climb, jump, and release
    /// </summary>
    private void HandleLedgeInput()
    {   
        // AUTO-GRAB: When falling past a detected ledge
        if (ledgeDetected && !isLedgeGrabbing && !isLedgeClimbing && 
            canGrabLedge && rb.linearVelocity.y < 0)
        {
            // Check if player is at appropriate height to grab
            float playerBottom = playerCollider.bounds.min.y;
            float ledgeTop = ledgePosition.y;
            
            // Player should be slightly below the ledge
            if (playerBottom < ledgeTop && playerBottom > ledgeTop - 1.0f)
            {
                GrabLedge();
            }
        }
        
        // LEDGE ACTION HANDLING: When already grabbing a ledge
        if (isLedgeGrabbing)
        {
            // Don't accept climb input until minimum hold time has passed
            bool canAcceptClimbInput = currentLedgeHoldTime >= minLedgeHoldTime;
            
            // Calculate input direction relative to ledge
            float inputDirection = CalculateLedgeInputDirection();
            
            // CLIMB UP: Press Up or towards the ledge (with hold time check)
            if ((moveInput.y > 0.5f || (inputDirection == ledgeSide && Mathf.Abs(moveInput.x) > 0.5f)) && canAcceptClimbInput)
            {
                if (!isLedgeClimbing)
                {
                    ClimbLedge();
                }
            }
            // RELEASE: Press Down or away from ledge (immediate)
            else if (moveInput.y < -0.5f || (inputDirection == -ledgeSide && Mathf.Abs(moveInput.x) > 0.5f))
            {
                ReleaseLedge();
            }
            // JUMP AWAY: Press jump button
            else if (jumpAction.triggered)
            {
                LedgeJump();
                return;
            }
        }
    }
    
    /// <summary>
    /// Calculates the dominant input direction for ledge actions
    /// Prioritizes vertical input over horizontal
    /// </summary>
    private float CalculateLedgeInputDirection()
    {
        if (Mathf.Abs(moveInput.y) > Mathf.Abs(moveInput.x))
        {
            return Mathf.Sign(moveInput.y);
        }
        else
        {
            return Mathf.Sign(moveInput.x);
        }
    }
    
    // ====================================================================
    // SECTION 8: MOVEMENT & PHYSICS METHODS
    // ====================================================================
    
    /// <summary>
    /// Main movement handler for ground and air movement (excluding swimming)
    /// </summary>
    private void HandleGroundAndAirMovement()
    {
        if (isHardLanding)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        
        // Determine current movement speed
        float currentSpeed = isRunning && isGrounded ? runSpeed : moveSpeed;
        
        // STATE-SPECIFIC MOVEMENT HANDLERS
        if (isDashing)
        {
            HandleDashMovement();
        }
        else if (isWallJumping)
        {
            HandleWallJumpMovement();
        }
        else if (isAttacking)
        {
            HandleAttackMovement();
        }
        else if (isWallSliding)
        {
            HandleWallSlideMovement();
        }
        else
        {
            HandleNormalMovement(currentSpeed);
        }
    }
    
    /// <summary>
    /// Handles movement during dash state
    /// </summary>
    private void HandleDashMovement()
    {
        rb.linearVelocity = dashDirection * dashSpeed;
        rb.gravityScale = 0; // No gravity during dash
    }
    
    /// <summary>
    /// Handles movement during wall jump state with limited control
    /// </summary>
    private void HandleWallJumpMovement()
    {
        float controlForce = 5f;
        rb.AddForce(new Vector2(moveInput.x * controlForce, 0));
        
        // Limit maximum wall jump speed
        float maxWallJumpSpeed = 10f;
        if (Mathf.Abs(rb.linearVelocity.x) > maxWallJumpSpeed)
        {
            rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * maxWallJumpSpeed, rb.linearVelocity.y);
        }
    }
    
    /// <summary>
    /// Handles movement during attack state with combo considerations
    /// </summary>
    private void HandleAttackMovement()
    {
        // Allow slight movement during first two combo hits
        if (currentCombo < 3 && comboWindowTimer > 0)
        {
            float moveSpeedMultiplier = 0.3f; // Reduced movement during attack
            Vector2 targetVelocity = new Vector2(moveInput.x * moveSpeed * moveSpeedMultiplier, rb.linearVelocity.y);
            rb.linearVelocity = Vector3.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocity, movementSmoothing);
        }
        else
        {
            // Stop movement for final hit or no combo
            Vector2 targetVelocity = new Vector2(0, rb.linearVelocity.y);
            rb.linearVelocity = Vector3.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocity, movementSmoothing);
        }
    }
    
    /// <summary>
    /// Handles wall slide movement with speed limits
    /// </summary>
    private void HandleWallSlideMovement()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        // Stop upward movement during wall slide
        if (rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }
    }
    
    /// <summary>
    /// Handles normal ground and air movement with all systems applied
    /// </summary>
    private void HandleNormalMovement(float currentSpeed)
    {
        float currentMoveInput = moveInput.x;
        
        // Prevent input into walls
        if (isAgainstWall && !isWallSliding && Mathf.Sign(currentMoveInput) == lastWallSide)
        {
            currentMoveInput = 0;
        }
        
        Vector2 targetVelocity = new Vector2(currentMoveInput * currentSpeed, rb.linearVelocity.y);
        
        // Apply air control if in air
        if (!isGrounded)
        {
            ApplyAirControl();
        }
        
        // Apply wall stick logic if wall sliding
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
        
        // Smooth movement
        rb.linearVelocity = Vector3.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocity, movementSmoothing);
        
        // Slow down when pushing against a wall
        if (!isWallSliding && isAgainstWall && Mathf.Sign(rb.linearVelocity.x) == lastWallSide)
        {
            rb.linearVelocity = new Vector2(
                Mathf.MoveTowards(rb.linearVelocity.x, 0, Time.deltaTime * 20f),
                rb.linearVelocity.y
            );
        }
    }
    
    /// <summary>
    /// Handles movement while grabbing or climbing a ledge
    /// </summary>
    private void HandleLedgeMovement()
    {
        rb.linearVelocity = Vector2.zero;
        
        // Only zero velocity if grabbing, not climbing
        // Climbing movement is handled in HandleLedgeClimb()
        if (!isLedgeClimbing)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
    
    /// <summary>
    /// Controls gravity based on player state
    /// </summary>
    private void UpdateGravity()
    {
        // Skip gravity in these states
        if (isDashing || isWallSliding || isHardLanding || isWallJumping || isSwimming)
            return;
        
        currentFallSpeed = rb.linearVelocity.y;
        
        // FALLING: Apply falling gravity
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = fallingGravityScale;
            
            // Clamp to maximum fall speed
            if (rb.linearVelocity.y < maxFallSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
            }
        }
        // ASCENDING (JUMP RELEASED): Apply quick fall gravity
        else if (rb.linearVelocity.y > 0 && !isJumpButtonHeld)
        {
            rb.gravityScale = fallingGravityScale * quickFallGravityMultiplier;
        }
        // NEUTRAL: Apply normal gravity
        else
        {
            rb.gravityScale = normalGravityScale;
        }
    }
    
    /// <summary>
    /// Applies air control physics for precise aerial movement
    /// </summary>
    private void ApplyAirControl()
    {
        if (isGrounded || isWallSliding || isDashing || isHardLanding) return;
        
        float targetXVelocity = moveInput.x * moveSpeed * airControlMultiplier;
        float velocityDifference = targetXVelocity - rb.linearVelocity.x;
        
        // Apply acceleration force
        rb.AddForce(Vector2.right * velocityDifference * airAcceleration);
        
        // Clamp to maximum air speed
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
    
    /// <summary>
    /// Flips the player sprite horizontally
    /// </summary>
    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }
    
    /// <summary>
    /// Updates attack point position to match facing direction
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
    
    // ====================================================================
    // SECTION 9: ACTION IMPLEMENTATION METHODS
    // ====================================================================
    
    /// <summary>
    /// Executes a normal ground jump
    /// Resets fall tracking for next jump
    /// </summary>
    private void NormalJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        ResetFallTracking();
    }
    
    /// <summary>
    /// Executes a double jump
    /// Tracks that double jump has been used
    /// </summary>
    private void DoubleJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, doubleJumpForce);
        hasDoubleJumped = true;
        ResetFallTracking();
    }
    
    /// <summary>
    /// Executes a wall jump with force applied away from wall
    /// Resets air dash count and fall tracking
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
        
        // Face away from wall after jump
        if ((wallSide == 1 && !facingRight) || (wallSide == -1 && facingRight))
        {
            Flip();
        }
        
        airDashCount = 0;
        ResetFallTracking();
        CancelCombo();
    }
    
    /// <summary>
    /// Resets fall tracking variables after a jump
    /// </summary>
    private void ResetFallTracking()
    {
        peakHeight = transform.position.y;
        fallStartHeight = transform.position.y;
        fellFromOffScreen = CheckIfFellFromOffScreen(fallStartHeight);
    }
    
    /// <summary>
    /// Starts dash movement
    /// Determines dash direction based on input and facing
    /// </summary>
    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        
        // Determine dash direction
        dashDirection = facingRight ? Vector2.right : Vector2.left;
        
        // Use input direction if in air and moving
        if (!isGrounded && Mathf.Abs(moveInput.x) > 0.1f && !isWallSliding)
        {
            dashDirection = new Vector2(Mathf.Sign(moveInput.x), 0);
        }

        CancelCombo();
    }
    
    /// <summary>
    /// Ends dash movement and restores normal physics
    /// </summary>
    private void EndDash()
    {
        isDashing = false;
        rb.gravityScale = normalGravityScale;
        
        // Stop horizontal movement if grounded
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }
    }
    

    /// <summary>
    /// Starts or continues a combo attack
    /// </summary>
    private void StartComboAttack()
    {
        // Determine combo index (0-based)
        int comboIndex = Mathf.Clamp(currentCombo, 0, maxComboHits - 1);
        
        // Start attack
        isAttacking = true;
        attackTimer = comboAttackDurations[comboIndex];
        attackCooldownTimer = comboCooldowns[comboIndex];
        
        // Set combo state
        currentCombo++;
        comboWindowTimer = comboWindow;  // Open window for next attack
        comboResetTimer = comboResetTime; // Reset overall combo timer
        
        // Check if this is the final hit
        isComboFinishing = (currentCombo >= maxComboHits);
        
        // Play appropriate animation
        if (animator != null)
        {
            string animationName = isComboFinishing ? comboAnimations[maxComboHits - 1] : comboAnimations[comboIndex];
            animator.Play(animationName);
        }
        
        // Perform attack based on combo hit
        ExecuteComboAttack(comboIndex);
    }

    /// <summary>
    /// Executes attack logic for specific combo hit
    /// </summary>
    private void ExecuteComboAttack(int comboIndex)
    {
        // Get attack parameters for this combo hit
        float damage = comboDamage[comboIndex];
        float knockback = comboKnockback[comboIndex];
        float range = comboRange[comboIndex];
        
        // Check for enemies in attack range
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, range, enemyLayers);
        
        // Apply damage and knockback to each enemy
        foreach (Collider2D enemy in hitEnemies)
        {
            // Example damage application - adjust based on your enemy system
            // enemy.GetComponent<EnemyHealth>().TakeDamage(damage);
            
            // Apply knockback
            Vector2 knockbackDirection = facingRight ? Vector2.right : Vector2.left;
            // enemy.GetComponent<Rigidbody2D>().AddForce(knockbackDirection * knockback, ForceMode2D.Impulse);
        }
        
        // Apply movement effects based on combo hit
        ApplyComboMovement(comboIndex);
    }

    /// <summary>
    /// Applies movement effects during combo attacks
    /// </summary>
    private void ApplyComboMovement(int comboIndex)
    {
        // Stop horizontal movement during attack
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        // Add slight forward movement for first two hits
        if (comboIndex < 2)
        {
            float forwardForce = 3f + (comboIndex * 2f);
            Vector2 forceDirection = facingRight ? Vector2.right : Vector2.left;
            rb.AddForce(forceDirection * forwardForce, ForceMode2D.Impulse);
        }
        // For final hit, add upward lift if grounded
        else if (comboIndex == 2 && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.3f);
        }
    }

    /// <summary>
    /// Ends attack and manages combo continuation
    /// </summary>
    private void EndAttack()
    {
        isAttacking = false;
        
        // Check if we have a queued attack
        if (attackQueued && comboWindowTimer > 0 && currentCombo < maxComboHits)
        {
            attackQueued = false; // Clear the queue
            StartComboAttack();   // Execute the queued attack
            return;
        }
        
        // No queued attack, check if combo window is closed
        if (comboWindowTimer <= 0 && currentCombo > 0)
        {
            // Start reset timer
            comboResetTimer = comboResetTime;
            
            // Play combo reset animation if available and this was the final hit
            if (animator != null && !string.IsNullOrEmpty(comboResetAnimation) && currentCombo >= maxComboHits)
            {
                animator.Play(comboResetAnimation);
            }
        }
        else if (currentCombo == 0)
        {
            // No combo active, reset immediately
            ResetCombo();
        }
    }

    /// <summary>
    /// Resets combo state
    /// </summary>
    private void ResetCombo()
    {
        currentCombo = 0;
        comboWindowTimer = 0f;
        comboResetTimer = 0f;
        comboAvailable = true;
        isComboFinishing = false;
        attackQueued = false; // NEW: Clear any queued attacks
        
        // Ensure attack state is cleared
        if (!isAttacking)
        {
            attackCooldownTimer = 0f;
        }
    }

    /// <summary>
    /// Cancels combo (called when taking damage, etc.)
    /// </summary>
    public void CancelCombo()
    {
        ResetCombo();
        isAttacking = false;
        attackCooldownTimer = 0f;
        attackQueued = false; // NEW: Clear queued attacks
    }


    
    /// <summary>
    /// Enhanced hard landing detection with screen-height check
    /// Combines velocity and screen-height conditions based on settings
    /// </summary>
    private void CheckForHardLanding(float fallSpeed, float fallDistance)
    {
        bool meetsVelocityCondition = fallSpeed <= hardLandingThreshold;
        bool meetsHeightCondition = CheckScreenHeightFall(fallDistance);
        
        bool shouldHardLand = false;
        
        // Choose detection method based on settings
        if (useScreenHeightForHardLanding)
        {
            if (requireBothConditions)
            {
                // Both conditions must be met
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
            // Fallback to velocity-only detection
            shouldHardLand = meetsVelocityCondition && fallDistance > 2f;
        }
        
        // Additional off-screen check for dramatic falls
        bool fellFromOffScreenPosition = fellFromOffScreen || CheckIfFellFromOffScreen(fallStartHeight);
        if (fellFromOffScreenPosition && fallDistance > screenHeightInUnits * 0.5f)
        {
            shouldHardLand = true;
        }
        
        // Apply hard landing or soft landing
        if (shouldHardLand)
        {
            StartHardLanding();
        }
        else if (fallDistance > 0.5f)
        {
            // Soft landing
            animator.Play("Kalb_land");
        }
    }
    
    /// <summary>
    /// Checks if fall distance is at least one screen height
    /// </summary>
    private bool CheckScreenHeightFall(float fallDistance)
    {
        float requiredDistance = screenHeightInUnits * minScreenHeightForHardLanding;
        return fallDistance >= requiredDistance;
    }
    
    /// <summary>
    /// Checks if fall started from off-screen (above camera view)
    /// </summary>
    private bool CheckIfFellFromOffScreen(float fallStartY)
    {
        if (mainCamera == null) return false;
        
        Bounds screenBounds = GetScreenBounds();
        float adjustedTop = screenBounds.max.y + screenHeightDetectionOffset;
        return fallStartY > adjustedTop;
    }
    
    /// <summary>
    /// Calculates visible screen bounds in world units
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
    /// Initiates hard landing state with screen shake
    /// </summary>
    private void StartHardLanding()
    {
        isHardLanding = true;
        hardLandingTimer = hardLandingStunTime;
        
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        
        animator.Play("Kalb_hard_land");
        
        // Screen shake for hard landing
        TriggerLandingScreenShake(currentFallSpeed, totalFallDistance);
    }
    
    /// <summary>
    /// Triggers screen shake based on fall impact intensity
    /// </summary>
    private void TriggerLandingScreenShake(float fallSpeed, float fallDistance)
    {
        if (!enableScreenShake || cameraShake == null) return;
        
        float normalizedFallSpeed = Mathf.Abs(fallSpeed) / Mathf.Abs(maxFallSpeed);
        float normalizedFallDistance = Mathf.Clamp01(fallDistance / (screenHeightInUnits * 2f));
        float combinedFactor = (normalizedFallSpeed * 0.6f) + (normalizedFallDistance * 0.4f);
        
        float intensity = Mathf.Lerp(0.1f, hardLandingShakeIntensity, combinedFactor);
        float duration = Mathf.Lerp(0.1f, hardLandingShakeDuration, combinedFactor);
        
        cameraShake.Shake(intensity, duration);
    }
    
    /// <summary>
    /// Ends hard landing recovery
    /// </summary>
    private void EndHardLanding()
    {
        isHardLanding = false;
        rb.gravityScale = normalGravityScale;
        animator.Play("Kalb_idle");
    }
    
    // ====================================================================
    // SECTION 10: SWIMMING SYSTEM
    // ====================================================================
    
    /// <summary>
    /// Starts a swim dash in water
    /// Saves current gravity and sets it to zero during dash
    /// </summary>
    private void StartSwimDash()
    {
        isSwimDashing = true;
        swimDashTimer = swimDashDuration;
        swimDashCooldownTimer = swimDashCooldown;
        
        preDashGravityScale = rb.gravityScale;
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
    /// Ends swim dash and restores previous gravity
    /// </summary>
    private void EndSwimDash()
    {
        isSwimDashing = false;
        rb.gravityScale = preDashGravityScale;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, rb.linearVelocity.y * 0.5f);
    }
    
    /// <summary>
    /// Jumps out of water
    /// Exits swimming state and restores normal gravity
    /// </summary>
    private void SwimJump()
    {
        if (!isSwimming || isSwimDashing) return;
        
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, swimJumpForce);
        isSwimming = false;
        rb.gravityScale = normalGravityScale;
        coyoteTimeCounter = coyoteTime; // Allow potential double jump
        CancelCombo();
    }
    
    /// <summary>
    /// Main swimming movement handler with buoyancy and floating
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
        ApplyBuoyancyCorrection();
    }
    
    /// <summary>
    /// Applies buoyancy force to keep player at water surface
    /// </summary>
    private void ApplyBuoyancy()
    {
        if (!isSwimming || currentWaterCollider == null) return;
        
        // Calculate target position (face above water)
        float playerHeight = playerCollider.bounds.extents.y * 2f;
        float targetY = waterSurfaceY + waterSurfaceOffset - (playerHeight * 0.8f);
        
        // Adjust for floating effect
        if (enableFloating && !isSwimDashing)
        {
            targetY += currentFloatOffset;
        }
        
        // Current position and depth difference
        float currentY = transform.position.y;
        float depthDifference = targetY - currentY;
        
        // Calculate buoyancy force with damping
        float buoyancyForce = depthDifference * buoyancyStrength;
        float dampingForce = -rb.linearVelocity.y * buoyancyDamping;
        float totalForce = Mathf.Clamp(buoyancyForce + dampingForce, -maxBuoyancyForce, maxBuoyancyForce);
        
        // Apply force
        rb.AddForce(new Vector2(0, totalForce));
    }
    
    /// <summary>
    /// Applies direct position correction for buoyancy (backup system)
    /// </summary>
    private void ApplyBuoyancyCorrection()
    {
        if (isSwimming && currentWaterCollider != null)
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
    /// Applies floating/bobbing effect when swimming
    /// </summary>
    private void ApplyFloatingEffect()
    {
        if (!isSwimming || isSwimDashing || !enableFloating) return;
        
        // Update timer
        floatTimer += Time.deltaTime * floatFrequency;
        
        // Calculate sine wave for bobbing
        float sineWave = Mathf.Sin(floatTimer * Mathf.PI * 2f);
        
        // Reduce effect when moving horizontally
        float horizontalMovementFactor = Mathf.Clamp01(1f - Mathf.Abs(moveInput.x) * 0.5f);
        targetFloatOffset = sineWave * floatAmplitude * horizontalMovementFactor;
        
        // Smooth interpolation
        currentFloatOffset = Mathf.Lerp(currentFloatOffset, targetFloatOffset, Time.deltaTime * floatSmoothness);
        
        // Update original position reference
        if (Mathf.Abs(currentFloatOffset) > 0.01f && Mathf.Abs(moveInput.x) < 0.1f)
        {
            originalPosition = new Vector3(originalPosition.x, 
                                        transform.position.y - currentFloatOffset, 
                                        originalPosition.z);
        }
        
        // Apply offset
        Vector3 currentPos = transform.position;
        transform.position = new Vector3(currentPos.x, originalPosition.y + currentFloatOffset, currentPos.z);
    }
    
    /// <summary>
    /// Checks if player is in water and handles state transitions
    /// </summary>
    private void CheckWater()
    {
        wasInWater = isInWater;
        
        // Check for water overlap
        Collider2D waterCollider = Physics2D.OverlapCircle(
            transform.position, 
            waterCheckRadius, 
            waterLayer
        );
        
        isInWater = waterCollider != null;
        currentWaterCollider = waterCollider;
        
        if (isInWater && waterCollider != null)
        {
            waterSurfaceY = waterCollider.bounds.max.y;
        }
        
        // Handle state transitions
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
    /// Initializes swimming state and physics
    /// </summary>
    private void OnEnterWater()
    {
        isSwimming = true;
        
        // Initialize floating effect
        floatTimer = Random.Range(0f, Mathf.PI * 2f);
        originalPosition = transform.position;
        currentFloatOffset = 0f;
        targetFloatOffset = 0f;
        
        // Adjust velocity for water entry
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.7f, -3f);
        }
        else
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.7f, Mathf.Min(rb.linearVelocity.y, 2f) * 0.3f);
        }
        
        // Set water physics
        rb.gravityScale = waterEntryGravity;
        
        // Reset other states
        isDashing = false;
        isWallJumping = false;
        wallSlideState = WallSlideState.None;
        isWallSliding = false;
        airDashCount = 0;
        hasDoubleJumped = false;
        CancelCombo();
    }
    
    /// <summary>
    /// Called when player exits water
    /// Restores normal physics
    /// </summary>
    private void OnExitWater()
    {
        isSwimming = false;
        isSwimDashing = false;
        isInWater = false;
        
        rb.gravityScale = normalGravityScale;
        swimDashCooldownTimer = 0f;
        canGrabLedge = true; // Reset ledge ability
    }
    
    // ====================================================================
    // SECTION 11: WALL INTERACTION SYSTEM
    // ====================================================================
    
    /// <summary>
    /// Prevents player from getting stuck on walls
    /// Uses raycasting to detect and push away from walls
    /// </summary>
    private void PreventWallStick()
    {
        if (isGrounded || Mathf.Abs(moveInput.x) < 0.1f || isHardLanding)
        {
            isAgainstWall = false;
            lastWallSide = 0;
            return;
        }
        
        // Skip if wall sliding or clinging (those have their own handling)
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
        
        // Cast multiple rays to detect walls
        for (int i = 0; i < rayCount; i++)
        {
            float height = (i / (float)(rayCount - 1) - 0.5f) * totalHeight;
            Vector2 rayOrigin = origin + new Vector2(0, height);
            
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, 
                new Vector2(direction, 0), 
                rayLength, 
                environmentLayer);
            
            if (hit.collider != null && hit.distance < closestDistance)
            {
                hitWall = true;
                closestDistance = hit.distance;
                
                // Push away from wall if too close
                if (hit.distance < wallNormalDistance)
                {
                    Vector2 pushBack = new Vector2(-direction * (wallNormalDistance - hit.distance), 0);
                    transform.position += (Vector3)pushBack * 0.5f;
                }
            }
        }
        
        // Update wall collision state
        if (hitWall && Mathf.Sign(moveInput.x) == direction)
        {
            if (lastWallSide != direction)
            {
                lastWallSide = (int)direction;
                isAgainstWall = true;
            }
            
            // Prevent input into wall
            if (Mathf.Sign(moveInput.x) == lastWallSide)
            {
                if (!isWallSliding)
                {
                    moveInput = new Vector2(0, moveInput.y);
                    
                    // Slow down velocity into wall
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
    /// Handles wall slide physics and state transitions
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
        
        // Update wall cling timer
        if (wallClingTimer > 0)
        {
            wallClingTimer -= Time.deltaTime;
        }
        
        // Wall sliding logic
        if (isTouchingWall)
        {
            // State transitions
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
            
            // Apply wall slide speed limits
            float currentSlideSpeed = rb.linearVelocity.y;
            
            if (isWallClinging)
            {
                // Slower slide during wall cling
                float clingSpeed = -wallSlideSpeed * wallClingSlowdown;
                if (currentSlideSpeed < clingSpeed)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, clingSpeed);
                }
                
                // Reset air dash when wall clinging
                if (resetAirDashOnGround)
                {
                    airDashCount = 0;
                }
            }
            else
            {
                // Normal wall slide with jump button effect
                if (isJumpButtonHeld && currentSlideSpeed < 0)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentSlideSpeed * 0.7f);
                }
                else if (currentSlideSpeed < -wallSlideSpeed)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
                }
                
                // Reset air dash when wall sliding
                if (resetAirDashOnGround)
                {
                    airDashCount = 0;
                }
            }
        }
        else
        {
            // Not touching wall - reset wall slide state
            wallSlideState = WallSlideState.None;
            isWallSliding = false;
            isWallClinging = false;
        }
    }
    
    /// <summary>
    /// Enhanced wall detection using raycasting
    /// Determines which side the player is touching a wall on
    /// </summary>
    private void CheckWall()
    {
        if (!wallJumpUnlocked || isHardLanding || isLedgeGrabbing || isLedgeClimbing)
        {
            ResetWallState();
            return;
        }
        
        Vector2 wallCheckPos = wallCheck.position;
        Vector2 offset = new Vector2(wallCheckOffset, 0);
        
        // Check both sides for walls
        bool touchingRightWall = Physics2D.Raycast(
            wallCheckPos + offset, 
            Vector2.right, 
            wallCheckDistance, 
            environmentLayer
        ).collider != null && !isGrounded;
        
        bool touchingLeftWall = Physics2D.Raycast(
            wallCheckPos - offset, 
            Vector2.left, 
            wallCheckDistance, 
            environmentLayer
        ).collider != null && !isGrounded;
        
        // Reset wall state
        isTouchingWall = false;
        wallSide = 0;
        
        // Determine which wall is being touched and update state
        if (touchingRightWall && moveInput.x > 0.1f)
        {
            SetWallTouchingState(1);
        }
        else if (touchingLeftWall && moveInput.x < -0.1f)
        {
            SetWallTouchingState(-1);
        }
        else if ((touchingRightWall || touchingLeftWall) && wallClingTimer > 0)
        {
            HandleWallCling(touchingRightWall, touchingLeftWall);
        }
    }
    
    /// <summary>
    /// Resets all wall interaction state variables
    /// </summary>
    private void ResetWallState()
    {
        isTouchingWall = false;
        wallSide = 0;
        isWallSliding = false;
        isWallClinging = false;
        wallSlideState = WallSlideState.None;
    }
    
    /// <summary>
    /// Sets wall touching state for a specific side
    /// </summary>
    private void SetWallTouchingState(int side)
    {
        isTouchingWall = true;
        wallSide = side;
        wallClingTimer = wallClingTime;
        isWallClinging = false;
    }
    
    /// <summary>
    /// Handles wall clinging state when switching directions
    /// </summary>
    private void HandleWallCling(bool touchingRightWall, bool touchingLeftWall)
    {
        // Determine which wall we're on
        if (touchingRightWall)
        {
            wallSide = 1;
        }
        else if (touchingLeftWall)
        {
            wallSide = -1;
        }
        
        // Check if switching direction (wall cling condition)
        bool switchingDirection = (wallSide == 1 && moveInput.x < -0.1f) || 
                                (wallSide == -1 && moveInput.x > 0.1f);
        
        if (switchingDirection)
        {
            isWallClinging = true;
            isTouchingWall = true;
        }
    }
    
    // ====================================================================
    // SECTION 12: LEDGE SYSTEM
    // ====================================================================
    
    /// <summary>
    /// Detects ledges that can be grabbed
    /// Uses raycasting to find exact ledge positions
    /// </summary>
    private bool CheckForLedge()
    {
        // Don't check if in states that prevent ledge grabbing
        if (isGrounded || isDashing || isAttacking || isHardLanding || 
            isSwimming || isWallSliding || rb.linearVelocity.y >= 0)
            return false;

        // Determine check direction based on facing and input
        float checkDirection = facingRight ? 1f : -1f;
        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            checkDirection = Mathf.Sign(moveInput.x);
        }
        
        ledgeSide = (int)checkDirection;
        
        // Calculate wall check position
        Vector2 playerCenter = playerCollider.bounds.center;
        float playerHalfWidth = playerCollider.bounds.extents.x;
        Vector2 wallCheckPos = new Vector2(
            playerCenter.x + (checkDirection * (playerHalfWidth + 0.05f)),
            playerCenter.y
        );
        
        // Cast vertical ray down from above to find ledge corner
        Vector2 verticalCheckStart = new Vector2(
            wallCheckPos.x + (checkDirection * 0.1f),
            playerCenter.y + playerCollider.bounds.extents.y + 1.0f
        );
        
        RaycastHit2D verticalHit = Physics2D.Raycast(
            verticalCheckStart,
            Vector2.down,
            2.0f,
            environmentLayer
        );
        
        if (verticalHit.collider == null) return false;
        
        // Found ledge surface
        ledgePosition = verticalHit.point;
        
        // Verify there's a wall at this position
        Vector2 wallVerificationStart = new Vector2(
            ledgePosition.x - (checkDirection * 0.1f),
            ledgePosition.y - 0.05f
        );
        
        RaycastHit2D wallVerificationHit = Physics2D.Raycast(
            wallVerificationStart,
            Vector2.right * checkDirection,
            0.2f,
            environmentLayer
        );
        
        if (wallVerificationHit.collider == null) return false;
        
        // Adjust to wall surface position
        ledgePosition.x = wallVerificationStart.x + (wallVerificationHit.distance * checkDirection);
        
        return true;
    }
    
    /// <summary>
    /// Grabs onto a detected ledge
    /// Snaps player to correct position and sets up grab state
    /// </summary>
    private void GrabLedge()
    {
        if (!ledgeDetected || isLedgeGrabbing) return;
        
        // Set up ledge grab state
        isLedgeGrabbing = true;
        isLedgeClimbing = false;
        ledgeClimbTimer = 0f;
        ledgeGrabTime = 0f;
        currentLedgeHoldTime = 0f;
        
        // Stop movement and gravity
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        
        // Calculate grab position
        float playerHeight = playerCollider.bounds.size.y;
        float grabX = ledgePosition.x - (ledgeSide * ledgeGrabOffsetX);
        float grabY = ledgePosition.y - (playerHeight * ledgeGrabOffsetY);
        
        // Snap to position
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
        
        // Reset detection to prevent re-grabbing
        ledgeDetected = false;
        
        // Play animation
        animator.Play("Kalb_ledge_grab");
    }
    
    /// <summary>
    /// Releases from the ledge with force applied away from wall
    /// Prevents immediate regrabbing with a cooldown
    /// </summary>
    private void ReleaseLedge()
    {
        if (!isLedgeGrabbing) return;
        
        isLedgeGrabbing = false;
        rb.gravityScale = normalGravityScale;
        
        // Apply release force away from wall
        float releaseDirection = -ledgeSide;
        Vector2 releaseForce = new Vector2(
            releaseDirection * ledgeReleaseForce * 0.5f,
            -ledgeReleaseForce * 0.8f
        );
        
        rb.AddForce(releaseForce, ForceMode2D.Impulse);
        
        // Prevent immediate regrabbing
        canGrabLedge = false;
        ledgeReleaseTimer = ledgeReleaseCooldown;
        ledgeDetected = false;
    }
    
    /// <summary>
    /// Climbs up onto the ledge
    /// Starts climb animation and timer
    /// </summary>
    private void ClimbLedge()
    {
        if (!isLedgeGrabbing || isLedgeClimbing) return;
        
        isLedgeClimbing = true;
        isLedgeGrabbing = false;
        ledgeClimbTimer = ledgeClimbTime;
        
        // Start climb animation
        if (animator != null)
        {
            animator.Play("Kalb_ledge_climb", -1, 0f);
            animator.speed = 1f;
        }
    }
    
    /// <summary>
    /// Handles ledge climbing animation and movement
    /// Lerps player to final position over climb duration
    /// </summary>
    private void HandleLedgeClimb()
    {
        if (!isLedgeClimbing) return;
        
        ledgeClimbTimer -= Time.deltaTime;
        float climbProgress = 1f - (ledgeClimbTimer / ledgeClimbTime);
        
        // Update animation progress
        if (animator != null)
        {
            animator.Play("Kalb_ledge_climb", -1, climbProgress);
            animator.speed = 1f;
        }
        
        // Handle climbing movement
        if (ledgeClimbTimer > 0)
        {
            float playerHeight = playerCollider.bounds.size.y;
            Vector3 climbTarget = new Vector3(
                ledgePosition.x + (ledgeSide * 0.4f),
                ledgePosition.y + (playerHeight * 0.5f),
                transform.position.z
            );
            
            // Smooth movement to climb target
            transform.position = Vector3.Lerp(transform.position, climbTarget, 
                                            (5f * Time.deltaTime) / ledgeClimbTime);
        }
        else
        {
            // Climb finished
            isLedgeClimbing = false;
            rb.gravityScale = normalGravityScale;
            
            // Ensure final position
            float playerHeight = playerCollider.bounds.size.y;
            Vector3 finalPosition = new Vector3(
                ledgePosition.x + (ledgeSide * 0.4f),
                ledgePosition.y + (playerHeight * 0.5f),
                transform.position.z
            );
            transform.position = finalPosition;
            
            // Small hop at the end
            rb.linearVelocity = new Vector2(0, 3f);
            
            // Reset to idle animation
            if (animator != null)
            {
                animator.Play("Kalb_idle");
            }
        }
    }
    
    /// <summary>
    /// Jumps away from the ledge
    /// Applies force away from wall and resets ledge ability
    /// </summary>
    private void LedgeJump()
    {
        if (!isLedgeGrabbing) return;
        
        isLedgeGrabbing = false;
        isLedgeClimbing = false;
        
        // Restore gravity and prevent immediate regrabbing
        rb.gravityScale = normalGravityScale;
        canGrabLedge = false;
        ledgeReleaseTimer = ledgeReleaseCooldown;
        
        // Apply jump force away from wall
        Vector2 jumpDir = new Vector2(-ledgeSide * ledgeJumpAngle.x, ledgeJumpAngle.y).normalized;
        rb.AddForce(jumpDir * ledgeJumpForce, ForceMode2D.Impulse);
        
        // Face away from wall
        if (ledgeSide == 1 && !facingRight)
        {
            Flip();
        }
        else if (ledgeSide == -1 && facingRight)
        {
            Flip();
        }
        
        // Reset air dash and track jump
        airDashCount = 0;
        ResetFallTracking();
    }
    
    // ====================================================================
    // SECTION 13: ANIMATION SYSTEM
    // ====================================================================
    
    /// <summary>
    /// Controls animation states based on player state and input
    /// Priority order: Special states > Movement states > Default states
    /// </summary>
    private void SetAnimation(float horizontalInput)
    {
        // SPECIAL STATES (highest priority)
        if (isLedgeClimbing)
        {
            animator.Play("Kalb_ledge_climb");
        }
        else if (isLedgeGrabbing)
        {
            animator.Play("Kalb_ledge_grab");
        }
        else if (isHardLanding)
        {
            animator.Play("Kalb_hard_land");
        }
        else if (isSwimming)
        {
            HandleSwimmingAnimations(horizontalInput);
        }
        // COMBO ATTACK STATE (new priority - above regular attacks)
        else if (isAttacking && currentCombo > 0)
        {
            // Animator parameters handle this via HandleComboAnimations()
            HandleComboAnimations();
        }
        // ACTION STATES
        else if (isDashing && dashUnlocked)
        {
            animator.Play("Kalb_dash");
        }
        else if (isWallSliding && wallJumpUnlocked)
        {
            animator.Play("Kalb_wallslide");
        }
        else if (isWallJumping && wallJumpUnlocked)
        {
            animator.Play("Kalb_jump");
        }
        // MOVEMENT STATES
        else if (isGrounded)
        {
            HandleGroundAnimations(horizontalInput);
        }
        // AIR STATES (lowest priority)
        else
        {
            HandleAirAnimations();
        }
    }
    
    /// <summary>
    /// Handles swimming-specific animations
    /// </summary>
    private void HandleSwimmingAnimations(float horizontalInput)
    {
        if (isSwimDashing)
        {
            animator.Play("Kalb_dash");
        }
        else if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            if (dashAction.IsPressed() && !isSwimDashing)
            {
                animator.Play("Kalb_swim_fast");
            }
            else
            {
                animator.Play("Kalb_swim");
            }
        }
        else
        {
            animator.Play("Kalb_swim_idle");
        }
    }
    
    /// <summary>
    /// Handles ground movement animations
    /// </summary>
    private void HandleGroundAnimations(float horizontalInput)
    {
        if (horizontalInput == 0)
        {
            animator.Play("Kalb_idle");
        }
        else
        {
            if (isRunning && runUnlocked)
            {
                animator.Play("Kalb_run");
            }
            else
            {
                animator.Play("Kalb_walk");
            }
        }
    }
    
    /// <summary>
    /// Handles air movement animations with fall speed variation
    /// </summary>
    private void HandleAirAnimations()
    {
        if (rb.linearVelocity.y > 0)
        {
            animator.Play("Kalb_jump");
        }
        else
        {
            // Vary fall animation based on speed
            float fallSpeedNormalized = Mathf.Abs(rb.linearVelocity.y) / Mathf.Abs(maxFallSpeed);
            animator.SetFloat("FallSpeed", fallSpeedNormalized);
            animator.Play("Kalb_fall");
        }
    }

    /// <summary>
    /// Handles combo attack animations through Animator parameters
    /// </summary>
    private void HandleComboAnimations()
    {
        if (animator == null) return;
        
        // Set animator parameters
        animator.SetBool("IsAttacking", isAttacking);
        animator.SetInteger("CurrentCombo", currentCombo);
        animator.SetFloat("ComboWindow", comboWindowTimer);
        
        // Adjust animation speed based on combo hit duration
        if (isAttacking && currentCombo > 0)
        {
            int comboIndex = Mathf.Clamp(currentCombo - 1, 0, maxComboHits - 1);
            float attackDuration = comboAttackDurations[comboIndex];
            
            // Calculate speed multiplier (normalize to 1.0 for 0.2s duration)
            float speedMultiplier = 0.2f / attackDuration;
            animator.SetFloat("AttackSpeed", speedMultiplier);
            
            // Ensure the correct animation is playing
            string animationName = isComboFinishing ? comboAnimations[maxComboHits - 1] : comboAnimations[comboIndex];
            
            // Only force play if not already playing this animation
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName(animationName) || stateInfo.normalizedTime > 1.0f)
            {
                animator.Play(animationName, -1, 0f);
            }
        }
        else
        {
            animator.SetFloat("AttackSpeed", 1.0f);
        }
    }
    
    // ====================================================================
    // SECTION 14: PUBLIC API & ABILITY MANAGEMENT
    // ====================================================================
    
    /// <summary>
    /// Unlocks the dash ability
    /// </summary>
    public void UnlockDash()
    {
        dashUnlocked = true;
    }
    
    /// <summary>
    /// Unlocks the run ability
    /// </summary>
    public void UnlockRun()
    {
        runUnlocked = true;
    }
    
    /// <summary>
    /// Unlocks the wall jump ability
    /// </summary>
    public void UnlockWallJump()
    {
        wallJumpUnlocked = true;
    }
    
    /// <summary>
    /// Unlocks the double jump ability
    /// </summary>
    public void UnlockDoubleJump()
    {
        doubleJumpUnlocked = true;
    }
    
    /// <summary>
    /// Unlocks all abilities at once
    /// </summary>
    public void UnlockAllAbilities()
    {
        UnlockDash();
        UnlockRun();
        UnlockWallJump();
        UnlockDoubleJump();
    }
    
    /// <summary>
    /// Resets all abilities to locked state
    /// </summary>
    public void ResetAbilities()
    {
        dashUnlocked = false;
        runUnlocked = false;
        wallJumpUnlocked = false;
        doubleJumpUnlocked = false;
    }

    /// <summary>
    /// Unlocks longer combo chain
    /// </summary>
    public void UpgradeCombo(int newMaxCombo)
    {
        maxComboHits = newMaxCombo;
        // Resize arrays if needed
        if (comboDamage.Length < newMaxCombo)
        {
            System.Array.Resize(ref comboDamage, newMaxCombo);
            System.Array.Resize(ref comboKnockback, newMaxCombo);
            System.Array.Resize(ref comboRange, newMaxCombo);
            System.Array.Resize(ref comboAttackDurations, newMaxCombo);
            System.Array.Resize(ref comboCooldowns, newMaxCombo);
            System.Array.Resize(ref comboAnimations, newMaxCombo);
        }
    }

    /// <summary>
    /// Gets current combo count
    /// </summary>
    public int GetCurrentCombo()
    {
        return currentCombo;
    }

    /// <summary>
    /// Gets max combo hits
    /// </summary>
    public int GetMaxCombo()
    {
        return maxComboHits;
    }

    /// <summary>
    /// Checks if player is in combo finisher state
    /// </summary>
    public bool IsComboFinishing()
    {
        return isComboFinishing;
    }
    
    // ====================================================================
    // SECTION 15: EDITOR & DEBUG VISUALIZATION
    // ====================================================================
    
    /// <summary>
    /// Draws gizmos in the Scene view for debugging
    /// Shows detection ranges, raycasts, and state information
    /// </summary>
    void OnDrawGizmosSelected()
    {
        DrawGroundCheckGizmo();
        DrawWallCheckGizmo();
        DrawAttackRangeGizmo();
        
        if (!Application.isPlaying || mainCamera == null) return;
        
        DrawFallTrackingGizmos();
        DrawScreenBoundsGizmos();
        DrawSwimmingGizmos();
        DrawLedgeGizmos();
    }
    
    /// <summary>
    /// Draws ground check sphere
    /// </summary>
    private void DrawGroundCheckGizmo()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
    
    /// <summary>
    /// Draws wall check rays
    /// </summary>
    private void DrawWallCheckGizmo()
    {
        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            Vector2 offset = new Vector2(wallCheckOffset, 0);
            Gizmos.DrawLine(wallCheck.position + (Vector3)offset, 
                          wallCheck.position + (Vector3)offset + Vector3.right * wallCheckDistance);
            Gizmos.DrawLine(wallCheck.position - (Vector3)offset, 
                          wallCheck.position - (Vector3)offset + Vector3.left * wallCheckDistance);
        }
    }
    
    /// <summary>
    /// Draws attack range sphere
    /// </summary>
    private void DrawAttackRangeGizmo()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
    
    /// <summary>
    /// Draws fall tracking visualizations
    /// </summary>
    private void DrawFallTrackingGizmos()
    {
        // Fall start height
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            new Vector3(transform.position.x - 1f, fallStartHeight, 0),
            new Vector3(transform.position.x + 1f, fallStartHeight, 0)
        );
        
        // Screen height threshold
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
    }
    
    /// <summary>
    /// Draws screen bounds and off-screen detection area
    /// </summary>
    private void DrawScreenBoundsGizmos()
    {
        Bounds screenBounds = GetScreenBounds();
        
        // Screen bounds
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
    
    /// <summary>
    /// Draws swimming system visualizations
    /// </summary>
    private void DrawSwimmingGizmos()
    {
        if (isSwimming && currentWaterCollider != null)
        {
            // Target surface position
            float targetY = waterSurfaceY + waterSurfaceOffset;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                new Vector3(transform.position.x - 0.5f, targetY, 0),
                new Vector3(transform.position.x + 0.5f, targetY, 0)
            );
            
            // Current position
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                new Vector3(transform.position.x - 0.5f, transform.position.y, 0),
                new Vector3(transform.position.x + 0.5f, transform.position.y, 0)
            );
        }

        if (isSwimming && enableFloating)
        {
            // Floating effect range
            Gizmos.color = Color.cyan;
            float floatRange = floatAmplitude * 2f;
            Gizmos.DrawWireCube(
                new Vector3(transform.position.x, originalPosition.y, 0),
                new Vector3(0.5f, floatRange, 0)
            );
            
            // Current float offset
            Gizmos.color = Color.white;
            Gizmos.DrawLine(
                new Vector3(transform.position.x - 0.25f, originalPosition.y + currentFloatOffset, 0),
                new Vector3(transform.position.x + 0.25f, originalPosition.y + currentFloatOffset, 0)
            );
        }
        // Draw combo attack range if attacking
        if (isAttacking && currentCombo > 0)
        {
            int comboIndex = (currentCombo - 1) % maxComboHits;
            float range = comboRange[comboIndex];
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, range);
            
            // Draw combo number
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
                $"Combo: {currentCombo}/{maxComboHits}");
        }
    }
    
    /// <summary>
    /// Draws ledge system visualizations
    /// </summary>
    private void DrawLedgeGizmos()
    {
        if (ledgeCheckPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(ledgeCheckPoint.position, 0.1f);
            
            if (ledgeDetected)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(ledgePosition, new Vector3(0.3f, 0.1f, 0));
            }
        }

        if (isLedgeGrabbing)
        {
            // Ledge position
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(ledgePosition, 0.1f);
            
            // Player position
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
            
            // Connection line
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, ledgePosition);
            
            // Platform surface
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(
                new Vector3(ledgePosition.x - 0.5f, ledgePosition.y, 0),
                new Vector3(ledgePosition.x + 0.5f, ledgePosition.y, 0)
            );
        }
    }
}

// ====================================================================
// SECTION 16: CAMERA SHAKE COMPONENT (SEPARATE CLASS)
// ====================================================================

/// <summary>
/// Simple camera shake effect component
/// Attach to main camera for screen shake effects
/// </summary>
public class CameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    public float shakeIntensity = 0.1f;
    public float shakeDuration = 0.2f;
    public float dampingSpeed = 1.0f;
    
    private Vector3 initialPosition;
    private float shakeTimer = 0f;
    private float currentIntensity = 0f;
    
    /// <summary>
    /// Initializes camera shake component
    /// Stores the camera's original position
    /// </summary>
    void Start()
    {
        initialPosition = transform.localPosition;
    }
    
    /// <summary>
    /// Updates shake effect if active
    /// Applies random offset based on intensity and timer
    /// </summary>
    void Update()
    {
        if (shakeTimer > 0)
        {
            // Apply random offset for shake effect
            transform.localPosition = initialPosition + Random.insideUnitSphere * currentIntensity;
            
            // Reduce shake over time
            shakeTimer -= Time.deltaTime * dampingSpeed;
            currentIntensity = Mathf.Lerp(0f, currentIntensity, shakeTimer / shakeDuration);
        }
        else
        {
            // Reset to original position when shake ends
            shakeTimer = 0f;
            transform.localPosition = initialPosition;
        }
    }
    
    /// <summary>
    /// Triggers a camera shake with specified intensity and duration
    /// If called during an existing shake, uses the stronger values
    /// </summary>
    /// <param name="intensity">How strong the shake should be</param>
    /// <param name="duration">How long the shake should last</param>
    public void Shake(float intensity, float duration)
    {
        // Use the higher intensity if already shaking
        if (intensity > currentIntensity)
        {
            currentIntensity = intensity;
        }
        
        // Use the longer duration if already shaking
        if (duration > shakeTimer)
        {
            shakeTimer = duration;
            this.shakeDuration = duration;
        }
    }
    
    /// <summary>
    /// Stops camera shake immediately
    /// Returns camera to original position
    /// </summary>
    public void StopShake()
    {
        shakeTimer = 0f;
        transform.localPosition = initialPosition;
    }
}