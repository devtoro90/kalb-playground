using UnityEngine;
using UnityEngine.InputSystem;

public class Kalb : MonoBehaviour
{
    // ====================================================================
    // SECTION 1: INSPECTOR CONFIGURATION VARIABLES
    // ====================================================================
    // These are public variables that can be configured in the Unity Inspector
    // Organized by system for easy editing and balancing
    
    [Header("Ability Unlocks")]
    public bool runUnlocked = false;        // Can the player run/sprint?
    public bool dashUnlocked = false;       // Can the player dash horizontally?
    public bool wallJumpUnlocked = false;   // Can the player jump off walls?
    public bool doubleJumpUnlocked = false; // Can the player jump a second time in mid-air?
    public bool wallLockUnlocked = false;   // Can the player lock onto walls to stop sliding?
    public bool pogoUnlocked = true;        // Can the player perform pogo attacks downward?
    
    [Header("Basic Movement")]
    public float moveSpeed = 5f;            // Normal walking speed
    public float runSpeed = 8f;             // Speed when running/sprinting
    public float jumpForce = 12f;           // Force applied for normal jumps
    [Range(0, 0.3f)] public float movementSmoothing = 0.05f; // Smoothing factor for movement transitions
    public bool facingRight = true;         // Is the player currently facing right?
    
    [Header("Jump & Air Movement")]
    public float coyoteTime = 0.15f;        // Time after leaving ground where player can still jump
    public float jumpBufferTime = 0.1f;     // Time window where jump input is remembered before landing
    public float jumpCutMultiplier = 0.5f;  // Multiplier applied to jump when button is released early
    public bool hasDoubleJump = true;       // Does the player have access to double jump mechanic?
    public float doubleJumpForce = 10f;     // Force applied for double jumps
    public float airControlMultiplier = 0.5f; // How much control player has in air (0-1)
    public float maxAirSpeed = 10f;         // Maximum horizontal speed while in air
    public float airAcceleration = 15f;     // How quickly player accelerates horizontally in air
    
    [Header("Dash Settings")]
    public float dashSpeed = 20f;           // Speed during dash
    public float dashDuration = 0.2f;       // How long dash lasts
    public float dashCooldown = 0.5f;       // Time before player can dash again
    public bool canAirDash = true;          // Can player dash while in air?
    public bool resetAirDashOnGround = true; // Reset air dash count when landing?
    public int maxAirDashes = 1;            // Maximum number of air dashes before landing
    
    [Header("Wall Interaction")]
    public float wallSlideSpeed = 6f;       // Maximum downward speed while sliding on wall
    public float wallJumpForce = 11f;       // Force applied when jumping off wall
    public Vector2 wallJumpAngle = new Vector2(1, 2); // Direction of wall jump (x=horizontal, y=vertical)
    public float wallJumpDuration = 0.2f;   // How long wall jump state lasts
    public float wallStickTime = 0.25f;     // Time player sticks to wall before sliding
    public float wallClingTime = 0.2f;      // Time player clings to wall when changing direction
    public float wallClingSlowdown = 0.3f;  // Speed reduction while wall clinging

    [Header("Wall Slide Acceleration")]
    public float wallSlideAccelerationTime = 2f;        // Time to reach max slide speed from standstill
    public float wallSlideDecelerationTime = 0.2f;      // Time to slow down when changing walls
    public AnimationCurve wallSlideAccelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);  // Acceleration curve
    public bool enableWallSlideAcceleration = true;     // Enable progressive wall slide speed?

    [Header("Wall Lock Ability")]
    public float wallLockSpeed = 0.5f;      // Speed while wall locked (should be very slow)
    public float wallLockReleaseSpeed = 2f; // Speed when releasing from wall lock
    public float wallLockInputThreshold = 0.7f; // How much input needed to engage wall lock
    public bool resetAirDashOnWallLock = true; // Reset air dash when engaging wall lock?
    
    [Header("Falling & Landing")]
    public float maxFallSpeed = -20f;       // Maximum downward speed (terminal velocity)
    public float hardLandingThreshold = -15f; // Fall speed that triggers hard landing
    public float hardLandingStunTime = 0.3f; // Time player is stunned after hard landing
    public float fallingGravityScale = 2.5f; // Gravity multiplier when falling
    public float normalGravityScale = 2f;   // Normal gravity multiplier
    public float quickFallGravityMultiplier = 1.2f; // Extra gravity when jump button released
    
    [Header("Screen-Height Hard Landing")]
    public bool useScreenHeightForHardLanding = true; // Use screen height for hard landing detection?
    public float minScreenHeightForHardLanding = 0.8f; // Minimum screen height to trigger hard landing
    public float screenHeightDetectionOffset = 1.0f;   // Offset for detecting off-screen falls
    public bool requireBothConditions = true; // Require both velocity AND height for hard landing?
    
    [Header("Screen Shake")]
    public bool enableScreenShake = true;   // Enable screen shake effects?
    public float hardLandingShakeIntensity = 0.15f; // Screen shake intensity for hard landings
    public float hardLandingShakeDuration = 0.25f; // Screen shake duration for hard landings
    
    [Header("Attack")]
    public float attackCooldown = 0.1f;     // Base cooldown between attacks
    public float attackDuration = 0.2f;     // Base duration of attack animation
    public Transform attackPoint;           // Position where attack originates
    public float attackRange = 0.5f;        // Base range of attacks
    public LayerMask enemyLayers;           // Which layers can be damaged by attacks
    public int attackDamage = 20;           // Base attack damage

    [Header("Combo Attack System")]
    public int maxComboHits = 3;            // Maximum number of hits in a combo chain
    public float comboWindow = 0.2f;        // Time window to continue combo
    public float comboResetTime = 0.6f;     // Time before combo resets completely
    public bool enableAirCombo = true;      // Can player combo while in air?
    public bool enableWallCombo = true;     // Can player combo while wall sliding?

    [Header("Combo Attack Settings")]
    public float[] comboDamage = new float[] { 20f, 25f, 35f };     // Damage for each combo hit
    public float[] comboKnockback = new float[] { 5f, 7f, 12f };    // Knockback for each combo hit
    public float[] comboRange = new float[] { 0.2f, 0.2f, 0.2f };   // Range for each combo hit
    public float[] comboAttackDurations = new float[] { 0.2f, 0.2f, 0.2f };  // Duration for each attack
    public float[] comboCooldowns = new float[] { 0.3f, 0.4f, 0.6f };        // Cooldown after each attack

    [Header("Combo Animation Names")]
    public string[] comboAnimations = new string[] { "Kalb_attack1", "Kalb_attack2", "Kalb_attack3" };
    public string comboResetAnimation = "Kalb_attack_reset"; // Animation when combo resets
    
    [Header("Environment Detection")]
    public Transform groundCheck;           // Position to check for ground
    public float groundCheckRadius = 0.2f;  // Radius for ground detection
    public Transform wallCheck;             // Position to check for walls
    public float wallCheckDistance = 0.05f; // Distance to check for walls
    public float wallCheckOffset = 0.02f;   // Offset for wall checks
    public LayerMask environmentLayer;      // Which layers count as environment
    
    [Header("Swimming")]
    public float swimSpeed = 3f;            // Normal swimming speed
    public float swimFastSpeed = 5f;        // Fast swimming speed (when holding dash)
    public float swimDashSpeed = 10f;       // Speed during swim dash
    public float swimJumpForce = 8f;        // Force when jumping out of water
    public float waterSurfaceOffset = 1.20f; // How far above water surface player floats
    public float waterEntrySpeedReduction = 0.5f; // Speed reduction when entering water
    public LayerMask waterLayer;            // Which layers count as water
    public float waterCheckRadius = 0.5f;   // Radius for water detection
    public Transform waterCheckPoint;       // Position to check for water
    public float waterEntryGravity = 0.5f;  // Gravity while in water
    public float buoyancyStrength = 50f;    // Strength of buoyancy force
    public float buoyancyDamping = 10f;     // Damping to prevent buoyancy oscillations
    public float maxBuoyancyForce = 20f;    // Maximum buoyancy force applied
    
    [Header("Floating Effect")]
    public float floatAmplitude = 0.05f;    // How much player bobs up/down in water
    public float floatFrequency = 1f;       // How fast player bobs in water
    public float floatSmoothness = 5f;      // Smoothness of floating transition
    public bool enableFloating = true;      // Enable water bobbing effect?
    
    [Header("Ledge System")]
    public float ledgeDetectionDistance = 0.5f; // How far to check for ledges
    public float ledgeGrabOffsetY = 0.15f;  // Vertical offset when grabbing ledge
    public float ledgeGrabOffsetX = 0.55f;  // Horizontal offset when grabbing ledge
    public float ledgeClimbTime = 0.5f;     // Time it takes to climb onto ledge
    public float ledgeJumpForce = 12f;      // Force when jumping from ledge
    public Vector2 ledgeJumpAngle = new Vector2(1, 2); // Direction of ledge jump
    public float ledgeClimbCheckRadius = 0.2f; // Radius for checking if ledge is climbable
    public Transform ledgeCheckPoint;       // Position to check for ledges
    public float minLedgeHoldTime = 0.3f;   // Minimum time player must hold ledge before climbing
    public float ledgeReleaseForce = 5f;    // Force applied when releasing from ledge
    public float ledgeReleaseCooldown = 0.2f; // Time before player can grab ledge again

    [Header("Pogo Attack Settings")]
    public float pogoAttackDuration = 0.2f; // How long pogo attack lasts
    public float pogoAttackCooldown = 0.1f; // Cooldown between pogo attacks
    public float pogoBounceForce = 15f;     // Force applied when pogo bouncing
    public float pogoDownwardForce = 5f;    // Force applied downward during pogo attack
    public float pogoDetectionRange = 1f;   // How far below player to detect pogo-able objects
    public LayerMask pogoLayers;            // Which layers can be pogo'd on
    public float pogoDamage = 25f;          // Damage dealt by pogo attack
    public float pogoKnockback = 8f;        // Knockback dealt by pogo attack
    public bool enablePogoOnSpikes = true;  // Can player pogo on spike tiles?
    public bool resetJumpOnPogo = true;     // Reset jump/dash abilities after pogo bounce?
    public bool canPogoInAir = true;        // Can player pogo while in air?
    public int maxPogoChain = 3;            // Maximum consecutive pogo bounces
    public float pogoChainWindow = 0.5f;    // Time window to continue pogo chain

    [Header("Pogo Visual Feedback")]
    public GameObject pogoEffectPrefab;     // Visual effect when pogo hits something
    public float pogoEffectDuration = 0.3f; // How long pogo effect lasts
    public Color pogoFlashColor = Color.yellow; // Color to flash player when pogo hits
    public float pogoFlashDuration = 0.1f;  // How long pogo flash lasts

    [Header("Health & Damage System")]
    public int maxHealth = 100;             // Player's maximum health
    public int currentHealth = 100;         // Player's current health
    public float invincibilityTime = 1f;    // Time player is invincible after taking damage
    public float hitFlashDuration = 0.1f;   // How long damage flash lasts per pulse
    public float hitFlashIntensity = 0.7f;  // Intensity of damage flash
    public Color hurtFlashColor = Color.red; // Color to flash when hurt
    
    [Header("Knockback Settings")]
    public float knockbackForce = 10f;      // Base knockback force when hit
    public float knockbackDuration = 0.3f;  // How long knockback lasts
    public float horizontalKnockbackMultiplier = 1.5f; // Multiplier for horizontal knockback
    public float verticalKnockbackMultiplier = 0.8f;   // Multiplier for vertical knockback
    
    [Header("Death & Respawn")]
    public float deathAnimationTime = 1.5f; // How long death animation plays
    public float respawnInvincibilityTime = 3f; // Time player is invincible after respawning
    public GameObject deathEffectPrefab;    // Visual effect when player dies
    
    [Header("Health Visual Feedback")]
    public GameObject damageNumberPrefab;   // Prefab for showing damage numbers
    public float damageNumberYOffset = 1.5f; // Height above player to show damage numbers
    public bool showDamageNumbers = true;   // Show floating damage numbers?
    
    [Header("Debug/Developer Options")]
    public bool godMode = false;            // Player takes no damage
    public bool infiniteJumps = false;      // Player can jump infinitely
    public bool showDebugInfo = true;       // Show debug information in editor
    
    // ====================================================================
    // SECTION 2: PRIVATE STATE VARIABLES
    // ====================================================================
    // These variables track the player's current state and are not exposed in inspector
    
    // Component References - cached for performance
    private Rigidbody2D rb;                 // Physics body component
    private PlayerInput playerInput;        // Input system component
    private InputAction moveAction;         // Action for movement input
    private InputAction jumpAction;         // Action for jump input
    private InputAction dashAction;         // Action for dash/run input
    private InputAction attackAction;       // Action for attack input
    private Animator animator;              // Animation controller
    private Camera mainCamera;              // Main camera reference
    private Collider2D playerCollider;      // Player's collider
    private MetroidvaniaCamera metroidvaniaCamera; // Custom camera controller
    private SpriteRenderer playerSprite;    // For visual effects like flashing
    private Color originalSpriteColor;      // Original sprite color for resetting
    
    // Input State - tracks current input values
    public Vector2 moveInput;               // Current movement input (-1 to 1)
    private bool isJumpButtonHeld = false;  // Is jump button currently held?
    private bool isRunning = false;         // Is player running/sprinting?
    private bool attackQueued = false;      // Is an attack queued for combo?
    
    // Movement State - tracks physics and movement
    private Vector3 velocity = Vector3.zero; // Used for movement smoothing
    private bool isGrounded;                // Is player touching ground?
    private float currentFallSpeed = 0f;    // Current vertical speed (negative = falling)
    
    // Action State Flags - tracks which actions player is performing
    public bool isDashing = false;          // Is player currently dashing?
    private bool isAttacking = false;       // Is player currently attacking?
    public bool isWallJumping = false;      // Is player currently wall jumping?
    private bool isHardLanding = false;     // Is player stunned from hard landing?
    private bool isSwimming = false;        // Is player swimming in water?
    private bool isSwimDashing = false;     // Is player dashing in water?
    private bool isLedgeGrabbing = false;   // Is player currently grabbing a ledge?
    private bool isLedgeClimbing = false;   // Is player currently climbing onto ledge?
    private bool isWallSliding;             // Is player sliding down a wall?
    private bool isTouchingWall;            // Is player touching a wall?
    private bool isWallClinging = false;    // Is player clinging to wall (changing direction)?
    private bool isAgainstWall = false;     // Is player pressed against a wall (not sliding)?
    public int wallSide = 0;                // Which side wall is on (-1=left, 1=right, 0=none)
    private int lastWallSide = 0;           // Last wall side touched
    
    // Wall Interaction State
    private float wallNormalDistance = 0.05f; // Distance to maintain from wall to prevent sticking
    
    // Wall Slide State Machine - tracks wall slide progression
    private enum WallSlideState { None, Starting, Sliding, Jumping }
    private WallSlideState wallSlideState = WallSlideState.None;
    private bool isWallSlideEngaged = false; // Has wall slide been properly engaged?
    private float wallSlideDisengageTimer = 0f; // Timer for wall slide disengagement
    private float wallSlideDisengageDelay = 0.15f; // Delay before disengaging wall slide
    
    // Wall Slide Acceleration State - for progressive wall slide speed
    private float currentWallSlideSpeed = 0f;        // Current actual slide speed
    private float wallSlideAccelerationTimer = 0f;   // Timer for acceleration curve
    private bool isAcceleratingWallSlide = false;    // Is player currently accelerating slide?
    private float wallSlideAccelerationDirection = 1f; // Direction of acceleration (1=down, -1=up)
    
    // Wall Lock State - for wall locking ability
    private bool isWallLocked = false;              // Is player fully locked to wall?
    private bool isWallLockEngaging = false;        // Is player engaging wall lock?
    private bool isWallLockDisengaging = false;     // Is player disengaging wall lock?
    private float wallLockTimer = 0f;               // Timer for wall lock transitions
    private float wallLockEngageTime = 0.1f;        // Time to fully engage wall lock
    private float wallLockDisengageTime = 0.05f;    // Time to disengage wall lock
    private float originalWallSlideSpeed = 0f;      // Stores original speed before lock
    
    // Jump & Air Movement State
    private float coyoteTimeCounter = 0f;           // Timer for coyote time
    private float jumpBufferCounter = 0f;           // Timer for jump buffering
    private bool hasDoubleJumped = false;           // Has player used double jump this jump?
    private int airDashCount = 0;                   // How many air dashes used this jump?
    
    // Falling & Landing Tracking - for hard landing detection
    private float peakHeight = 0f;                  // Highest point reached this jump
    private float fallStartHeight = 0f;             // Height where fall started
    private float totalFallDistance = 0f;           // Total distance fallen
    private float screenHeightInUnits = 0f;         // Height of screen in world units
    private bool fellFromOffScreen = false;         // Did fall start off-screen?
    
    // Timers & Cooldowns - track durations of various actions
    private float attackCooldownTimer = 0f;         // Time until next attack
    private float attackTimer = 0f;                 // Time left in current attack
    private float dashCooldownTimer = 0f;           // Time until next dash
    private float dashTimer = 0f;                   // Time left in current dash
    private float hardLandingTimer = 0f;            // Time left in hard landing stun
    private float wallClingTimer = 0f;              // Time left in wall cling
    private float wallJumpTimer = 0f;               // Time left in wall jump state
    private float wallStickTimer = 0f;              // Time left in wall stick
    private float comboWindowTimer = 0f;            // Time left to continue combo
    private float comboResetTimer = 0f;             // Time before combo resets
    
    // Dash Variables
    private Vector2 dashDirection = Vector2.right;  // Direction player is dashing
    
    // Swimming State
    private bool isInWater = false;                 // Is player currently in water?
    private bool wasInWater = false;                // Was player in water last frame?
    private float swimDashTimer = 0f;               // Time left in swim dash
    private float swimDashCooldownTimer = 0f;       // Time until next swim dash
    private float swimDashDuration = 0.15f;         // How long swim dash lasts
    private float swimDashCooldown = 0.3f;          // Cooldown between swim dashes
    private Vector2 swimDashDirection = Vector2.right; // Direction of swim dash
    private float waterSurfaceY = 0f;               // Y position of water surface
    private Collider2D currentWaterCollider = null; // Reference to current water collider
    private float preDashGravityScale;              // Gravity before dash (to restore)
    
    // Floating Effect - for water bobbing
    private float floatTimer = 0f;                  // Timer for floating animation
    private float currentFloatOffset = 0f;          // Current vertical offset from floating
    private float targetFloatOffset = 0f;           // Target vertical offset for smooth transition
    private Vector3 originalPosition;               // Original position before floating offset
    
    // Ledge System State
    private bool ledgeDetected = false;             // Is a ledge currently detected?
    private Vector2 ledgePosition;                  // Position of detected ledge
    private float ledgeClimbTimer = 0f;             // Time left in ledge climb animation
    private int ledgeSide = 0;                      // Which side ledge is on (-1=left, 1=right)
    private float currentLedgeHoldTime = 0f;        // How long player has held ledge
    private float ledgeReleaseTimer = 0f;           // Time until player can grab ledge again
    private bool canGrabLedge = true;               // Can player currently grab ledges?
    
    // Pogo Attack State
    private bool isPogoAttacking = false;           // Is player performing pogo attack?
    private bool isPogoBouncing = false;            // Is player bouncing from pogo hit?
    private float pogoAttackTimer = 0f;             // Time left in pogo attack
    private float pogoCooldownTimer = 0f;           // Time until next pogo attack
    private int currentPogoChain = 0;               // Current number of consecutive pogo bounces
    private float pogoChainTimer = 0f;              // Time left to continue pogo chain
    private float lastPogoTime = 0f;                // Time of last pogo bounce
    private Vector2 pogoDirection = Vector2.down;   // Direction of pogo attack
    private bool hasPogoedThisJump = false;         // Has player used pogo this jump?
    
    // Combo System State
    private int currentCombo = 0;                   // Current combo count (0 = no combo)
    private bool comboAvailable = true;             // Can player start/continue combo?
    private bool isComboFinishing = false;          // Is player performing final combo attack?
    
    // Health & Damage State
    private bool isInvincible = false;              // Is player currently invincible?
    private bool isTakingDamage = false;            // Is player currently being knocked back?
    private bool isDead = false;                    // Is player dead?
    private float invincibilityTimer = 0f;          // Time left of invincibility
    private float knockbackTimer = 0f;              // Time left of knockback
    private Vector2 knockbackDirection = Vector2.right; // Direction of current knockback
    
    // Death & Respawn
    private Vector3 lastCheckpointPosition;         // Position to respawn at
    private float deathTimer = 0f;                  // Time left in death animation
    private bool isRespawning = false;              // Is player currently respawning?
    private float respawnTimer = 0f;                // Time left of respawn invincibility
    
    // Animation State
    private bool isAnimationLocked = false;         // Is animation locked (preventing changes)?
    private float animationLockTimer = 0f;          // Time left of animation lock
    
    // ====================================================================
    // SECTION 3: UNITY LIFE CYCLE METHODS
    // ====================================================================
    
    /// <summary>
    /// Called once when the script instance is loaded
    /// Initializes all components and systems
    /// </summary>
    void Start()
    {
        InitializeComponents();
        CalculateScreenHeightInUnits();
        SetupMissingObjects();
    }
    
    /// <summary>
    /// Called every frame
    /// Handles input reading, timer updates, and non-physics logic
    /// </summary>
    void Update()
    {
        // Read all player inputs
        ReadInputs();
        
        // Update all active timers and cooldowns
        UpdateAllTimers();
        
        // Check environment (walls, water, ledges, etc.)
        CheckEnvironment();
        
        // Process inputs based on current state
        HandleStateBasedInputs();
        
        // Manage state transitions and updates
        ManagePlayerStates();
        
        // Update animation states
        UpdateAnimations();
    }
    
    /// <summary>
    /// Called at fixed time intervals (for physics)
    /// Handles movement, collisions, and physics calculations
    /// </summary>
    void FixedUpdate()
    {
        // Update ground detection and landing logic
        UpdateGroundCheck();
        
        // Apply physics systems (gravity, knockback, etc.)
        ApplyPhysicsBasedSystems();
        
        // Execute movement based on current state
        ExecuteMovement();
        
        // Update player orientation (facing direction)
        UpdatePlayerOrientation();
    }
    
    // ====================================================================
    // SECTION 4: INITIALIZATION METHODS
    // ====================================================================
    
    /// <summary>
    /// Gets references to all required components and sets up input system
    /// </summary>
    private void InitializeComponents()
    {
        // Get physics body and configure it
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true; // Prevent rotation from physics
        
        // Set up input system actions
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        dashAction = playerInput.actions["Dash/Run"];
        attackAction = playerInput.actions["Attack"];
        
        // Get animation and collider components
        animator = GetComponent<Animator>();
        playerCollider = GetComponent<Collider2D>();
        
        // Store original position for floating effect
        originalPosition = transform.position;
        
        // Get camera references
        mainCamera = Camera.main;
        metroidvaniaCamera = FindFirstObjectByType<MetroidvaniaCamera>();
        if (metroidvaniaCamera == null && Camera.main != null)
        {
            metroidvaniaCamera = Camera.main.gameObject.AddComponent<MetroidvaniaCamera>();
        }
        
        // Initialize specialized systems
        InitializeHealthSystem();
        InitializeWallSlideAcceleration();
    }
    
    /// <summary>
    /// Calculates screen height in world units for fall detection system
    /// </summary>
    private void CalculateScreenHeightInUnits()
    {
        if (mainCamera != null)
        {
            // Convert screen top and bottom to world positions
            Vector3 topOfScreen = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 1f, 0));
            Vector3 bottomOfScreen = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, 0));
            
            // Calculate distance between them
            screenHeightInUnits = Vector3.Distance(topOfScreen, bottomOfScreen);
        }
        else
        {
            // Fallback value if no camera found
            screenHeightInUnits = 10f;
        }
    }
    
    /// <summary>
    /// Creates required child GameObjects if not assigned in Inspector
    /// Prevents null reference errors at runtime
    /// </summary>
    private void SetupMissingObjects()
    {
        // Create all necessary check points if missing
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
    
    /// <summary>
    /// Initializes health system components and state
    /// </summary>
    private void InitializeHealthSystem()
    {
        // Get sprite renderer for damage flash effects
        playerSprite = GetComponent<SpriteRenderer>();
        if (playerSprite != null)
        {
            originalSpriteColor = playerSprite.color;
        }
        
        // Ensure health is within valid range
        currentHealth = Mathf.Clamp(currentHealth, 1, maxHealth);
        
        // Set initial checkpoint to starting position
        lastCheckpointPosition = transform.position;
        
        // Initialize player as alive and vulnerable
        isDead = false;
        isInvincible = false;
        isTakingDamage = false;
    }
    
    /// <summary>
    /// Initializes wall slide acceleration system with default values
    /// </summary>
    private void InitializeWallSlideAcceleration()
    {
        currentWallSlideSpeed = 0f;
        wallSlideAccelerationTimer = 0f;
        isAcceleratingWallSlide = false;
        wallSlideAccelerationDirection = 1f;
        
        // Set default acceleration curve if none provided
        if (wallSlideAccelerationCurve == null || wallSlideAccelerationCurve.keys.Length == 0)
        {
            wallSlideAccelerationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        }
    }
    
    /// <summary>
    /// Resets wall slide acceleration to start from zero
    /// Called when starting new wall slide
    /// </summary>
    public void ResetWallSlideAcceleration()
    {
        if (!enableWallSlideAcceleration) return;
        
        currentWallSlideSpeed = 0f;
        wallSlideAccelerationTimer = 0f;
        isAcceleratingWallSlide = true;
        wallSlideAccelerationDirection = 1f;  // Always start accelerating downward
    }
    
    /// <summary>
    /// Updates wall slide acceleration over time
    /// Controls progressive speed buildup during wall slide
    /// </summary>
    private void UpdateWallSlideAcceleration()
    {
        if (!enableWallSlideAcceleration || !isWallSliding || isWallLocked) return;
        
        // Reset acceleration when starting new wall slide
        if (wallSlideState == WallSlideState.Starting && !isAcceleratingWallSlide)
        {
            ResetWallSlideAcceleration();
        }
        
        // Update acceleration timer and calculate speed
        if (isAcceleratingWallSlide && wallSlideAccelerationTimer < 1f)
        {
            // Increase timer based on acceleration time
            wallSlideAccelerationTimer += Time.deltaTime / wallSlideAccelerationTime;
            wallSlideAccelerationTimer = Mathf.Clamp01(wallSlideAccelerationTimer);
            
            // Get speed from acceleration curve
            float curveValue = wallSlideAccelerationCurve.Evaluate(wallSlideAccelerationTimer);
            currentWallSlideSpeed = Mathf.Lerp(0f, wallSlideSpeed, curveValue);
            
            // Stop accelerating when at max speed
            if (wallSlideAccelerationTimer >= 1f)
            {
                isAcceleratingWallSlide = false;
                currentWallSlideSpeed = wallSlideSpeed;  // Ensure exact max speed
            }
        }
        // Maintain max speed if not accelerating
        else if (!isAcceleratingWallSlide && currentWallSlideSpeed < wallSlideSpeed)
        {
            currentWallSlideSpeed = wallSlideSpeed;
        }
        
        // Handle deceleration when needed
        UpdateWallSlideDeceleration();
    }
    
    /// <summary>
    /// Handles deceleration when changing walls or slowing down
    /// </summary>
    private void UpdateWallSlideDeceleration()
    {
        if (!enableWallSlideAcceleration) return;
        
        // Check if player is trying to change wall sides
        bool changingWalls = false;
        if (isTouchingWall && Mathf.Abs(moveInput.x) > 0.1f)
        {
            float inputDirection = Mathf.Sign(moveInput.x);
            changingWalls = inputDirection == -wallSide && !isWallLockEngaging;
        }
        
        // Decelerate when changing walls or engaging wall lock
        if (changingWalls || isWallLockEngaging)
        {
            if (wallSlideAccelerationDirection > 0)  // Only if we were accelerating downward
            {
                wallSlideAccelerationDirection = -1f;  // Switch to deceleration
                wallSlideAccelerationTimer = 1f - (currentWallSlideSpeed / wallSlideSpeed);
            }
            
            // Update deceleration
            if (wallSlideAccelerationDirection < 0)
            {
                wallSlideAccelerationTimer -= Time.deltaTime / wallSlideDecelerationTime;
                wallSlideAccelerationTimer = Mathf.Clamp01(wallSlideAccelerationTimer);
                
                float curveValue = wallSlideAccelerationCurve.Evaluate(wallSlideAccelerationTimer);
                currentWallSlideSpeed = Mathf.Lerp(0f, wallSlideSpeed, curveValue);
                
                // Reset when fully decelerated
                if (wallSlideAccelerationTimer <= 0f)
                {
                    wallSlideAccelerationDirection = 1f;
                    isAcceleratingWallSlide = false;
                }
            }
        }
        // Switch back to acceleration when conditions change
        else if (wallSlideAccelerationDirection < 0 && !changingWalls && !isWallLockEngaging)
        {
            wallSlideAccelerationDirection = 1f;
            isAcceleratingWallSlide = true;
            wallSlideAccelerationTimer = currentWallSlideSpeed / wallSlideSpeed;
        }
    }
    
    /// <summary>
    /// Applies the current wall slide speed to movement
    /// Controls actual downward velocity during wall slide
    /// </summary>
    private void ApplyWallSlideAcceleration()
    {
        if (!enableWallSlideAcceleration || !isWallSliding || isWallLocked) return;
        
        // Get target speed (accelerated or normal)
        float targetSpeed = enableWallSlideAcceleration ? 
            Mathf.Min(-currentWallSlideSpeed, rb.linearVelocity.y) : 
            -wallSlideSpeed;
        
        // Only apply if falling (negative velocity)
        if (rb.linearVelocity.y < 0)
        {
            // Apply jump button effect (slower fall when holding jump)
            if (isJumpButtonHeld && rb.linearVelocity.y < targetSpeed * 0.3f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, targetSpeed * 0.3f);
            }
            // Apply accelerated speed
            else if (rb.linearVelocity.y < targetSpeed)
            {
                // Smoothly approach target speed
                float newYSpeed = Mathf.MoveTowards(rb.linearVelocity.y, targetSpeed, 
                    Time.fixedDeltaTime * wallSlideSpeed * 2f);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, newYSpeed);
            }
        }
    }
    
    // ====================================================================
    // SECTION 5: UPDATE PHASE METHODS
    // ====================================================================
    // These methods are called in Update() in a specific order
    
    /// <summary>
    /// Phase 1: Reads all player inputs from input system
    /// </summary>
    private void ReadInputs()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        UpdateJumpButtonState();
    }
    
    /// <summary>
    /// Tracks jump button state for variable jump height
    /// Also handles jump cut when button is released early
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
    /// Phase 2: Updates all active timers and cooldowns
    /// Called every frame to decrement timers
    /// </summary>
    private void UpdateAllTimers()
    {
        // Update timers for actions and cooldowns
        UpdateActionTimers();
        
        // Update jump-related timers (coyote time, buffer)
        UpdateJumpTimers();
        
        // Update swimming system timers
        UpdateSwimTimers();
        
        // Update ledge system timers
        UpdateLedgeTimers();
        
        // Update combo system timers
        UpdateComboTimers();
        
        // Update pogo attack timers
        UpdatePogoTimers();

        // Update health and damage timers
        UpdateHealthTimers();
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
    /// Includes coyote time and jump buffering
    /// </summary>
    private void UpdateJumpTimers()
    {
        // Coyote time: Allows jumping briefly after leaving ground
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            hasDoubleJumped = false;
            hasPogoedThisJump = false;
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
    /// Updates swimming-related timers (dash duration and cooldown)
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
    /// Updates ledge system timers (grab time, climb time, cooldowns)
    /// </summary>
    private void UpdateLedgeTimers()
    {
        // Ledge release cooldown timer
        if (ledgeReleaseTimer > 0) ledgeReleaseTimer -= Time.deltaTime;
        
        // Reset ledge grab ability after cooldown
        if (!canGrabLedge && ledgeReleaseTimer <= 0)
        {
            canGrabLedge = true;
        }
        
        // Track how long player has been holding ledge
        if (isLedgeGrabbing)
        {
            currentLedgeHoldTime += Time.deltaTime;
        }
        else
        {
            currentLedgeHoldTime = 0f;
        }
        
        // Ledge climb animation timer
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
    /// Updates combo system timers (window to continue combo, reset timer)
    /// </summary>
    private void UpdateComboTimers()
    {
        // Combo window timer - time to continue combo
        if (comboWindowTimer > 0)
        {
            comboWindowTimer -= Time.deltaTime;
            if (comboWindowTimer <= 0)
            {
                comboWindowTimer = 0;
                
                // If not attacking and window closed, start reset timer
                if (!isAttacking && currentCombo > 0)
                {
                    comboResetTimer = comboResetTime;
                }
            }
        }
        
        // Combo reset timer - time before combo resets to 0
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
    /// Updates pogo attack timers (attack duration, cooldown, chain window)
    /// </summary>
    private void UpdatePogoTimers()
    {
        // Pogo attack duration timer
        if (isPogoAttacking)
        {
            pogoAttackTimer -= Time.deltaTime;
            if (pogoAttackTimer <= 0)
            {
                EndPogoAttack();
            }
        }
        
        // Pogo cooldown timer
        if (pogoCooldownTimer > 0)
        {
            pogoCooldownTimer -= Time.deltaTime;
        }
        
        // Pogo chain window timer
        if (pogoChainTimer > 0)
        {
            pogoChainTimer -= Time.deltaTime;
            if (pogoChainTimer <= 0)
            {
                ResetPogoChain();
            }
        }
        
        // Auto-cancel bounce state after timeout
        if (isPogoBouncing && Time.time - lastPogoTime > 0.3f)
        {
            isPogoBouncing = false;
        }
    }
    
    /// <summary>
    /// Updates health-related timers (invincibility, knockback, death, respawn)
    /// </summary>
    private void UpdateHealthTimers()
    {
        // Invincibility timer after taking damage
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0)
            {
                EndInvincibility();
            }
        }
        
        // Knockback duration timer
        if (isTakingDamage && knockbackTimer > 0)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0)
            {
                EndKnockback();
            }
        }

        // Animation lock timer (prevents animation changes)
        if (isAnimationLocked)
        {
            animationLockTimer -= Time.deltaTime;
            if (animationLockTimer <= 0)
            {
                isAnimationLocked = false;
            }
        }
        
        // Death animation timer
        if (isDead)
        {
            deathTimer -= Time.deltaTime;
            if (deathTimer <= 0)
            {
                Respawn();
            }
        }
        
        // Respawn invincibility timer
        if (isRespawning)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0)
            {
                EndRespawnInvincibility();
            }
        }
    }
    
    /// <summary>
    /// Phase 3: Checks the environment around the player
    /// Detects walls, water, ledges, and death boundaries
    /// </summary>
    private void CheckEnvironment()
    {
        // Check if player has fallen out of bounds
        CheckDeathBoundary();

        // Detect walls for sliding and wall jumps
        CheckWall();
        
        // Detect water for swimming
        CheckWater();
        
        // Detect ledges if able to grab them
        if (canGrabLedge && !isLedgeGrabbing && !isLedgeClimbing)
        {
            ledgeDetected = CheckForLedge();
        }
    }
    
    /// <summary>
    /// Phase 4: Handles inputs based on current player state
    /// Different inputs are processed depending on state (swimming, attacking, etc.)
    /// </summary>
    private void HandleStateBasedInputs()
    {
        // Block most inputs if dead, taking damage, or hard landing
        if (isDead || isTakingDamage || isHardLanding)
        {
            HandleMinimalInputDuringDamage();
            return;
        }

        // Handle swimming-specific inputs
        if (isSwimming && !isHardLanding)
        {
            HandleSwimInput();
        }
        
        // Handle normal inputs if not in special states
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
    /// Phase 5: Manages player state transitions and updates
    /// Handles ongoing state behaviors like wall sliding and climbing
    /// </summary>
    private void ManagePlayerStates()
    {
        // Handle wall sliding if unlocked and not in ledge states
        if (wallJumpUnlocked && !isHardLanding && !isLedgeGrabbing && !isLedgeClimbing)
        {
            HandleWallSlide();
            HandleWallSlideDisengagement();
        }
        
        // Handle ledge climbing if active
        if (isLedgeClimbing)
        {
            HandleLedgeClimb();
        }

        // Handle pogo attack detection during attack
        if (isPogoAttacking && !isPogoBouncing)
        {
            CheckForPogoTargets();
        }
    }
    
    /// <summary>
    /// Phase 6: Updates animations based on current state
    /// Controls which animation plays based on player actions
    /// </summary>
    private void UpdateAnimations()
    {
        SetAnimation(moveInput.x);
    }
    
    // ====================================================================
    // SECTION 6: FIXEDUPDATE PHASE METHODS
    // ====================================================================
    // These methods are called in FixedUpdate() in a specific order
    
    /// <summary>
    /// Phase 1: Updates ground detection and landing logic
    /// Called in FixedUpdate for physics accuracy
    /// </summary>
    private void UpdateGroundCheck()
    {
        bool wasGrounded = isGrounded;
        
        // Check if player is grounded using circle cast
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, environmentLayer);

        // Reset wall slide when landing
        if (isGrounded && isWallSlideEngaged)
        {
            isWallSlideEngaged = false;
            wallSlideDisengageTimer = 0f;

            // Reset acceleration when landing
            if (enableWallSlideAcceleration)
            {
                ResetWallSlideAcceleration();
            }
        }
        
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
        
        // Track peak height during ascent for fall distance calculation
        if (!isGrounded && !wasGrounded && rb.linearVelocity.y > 0)
        {
            peakHeight = transform.position.y;
        }
        
        // LANDING DETECTION - Handle transitions from air to ground
        if (!wasGrounded && isGrounded)
        {
            // Calculate fall distance for hard landing detection
            totalFallDistance = fallStartHeight - transform.position.y;
            
            // Check if landing was hard enough to stun player
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
    /// Phase 2: Applies physics-based systems like gravity
    /// Controls gravity based on player state
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

        // Apply reduced gravity during pogo bounce for better control
        if (isPogoBouncing)
        {
            rb.gravityScale = normalGravityScale * 0.7f;
        }
    }
    
    /// <summary>
    /// Phase 3: Executes movement based on current state
    /// Applies forces and velocities to the rigidbody
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
    /// Phase 4: Updates player orientation (facing direction)
    /// Flips sprite based on movement direction
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
    /// Validates ability unlock, cooldowns, and state conditions
    /// </summary>
    private void HandleDashInput()
    {
        // Check if dash is available
        if (!dashUnlocked || isHardLanding || isSwimming) return;
        
        // Check if dash button was pressed and conditions are met
        if (dashAction.triggered && !isDashing && dashCooldownTimer <= 0 && 
            !isAttacking && !isWallSliding)
        {
            bool canDash = isGrounded || isWallSliding;
            
            // Check air dash availability
            if (!isGrounded && !isWallSliding && canAirDash)
            {
                canDash = airDashCount < maxAirDashes;
            }
            
            // Execute dash if allowed
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
    /// Running requires holding dash button while grounded
    /// </summary>
    private void HandleRunInput()
    {
        if (!runUnlocked || isHardLanding)
        {
            isRunning = false;
            return;
        }
        
        // Running requires holding dash button while grounded and not in other actions
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
        
        // Buffer jump input (store input for later processing)
        if (jumpAction.triggered)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        
        // Process buffered jump if available
        if (jumpBufferCounter > 0)
        {
            // Priority 1: Wall jump (highest priority)
            if (isWallSliding && wallJumpUnlocked)
            {
                WallJump();
                jumpBufferCounter = 0;
                hasDoubleJumped = false;
            }
            // Priority 2: Double jump (if in air and haven't double jumped)
            else if (!isGrounded && coyoteTimeCounter <= 0 && doubleJumpUnlocked && 
                    hasDoubleJump && !hasDoubleJumped && !isDashing && !isAttacking)
            {
                DoubleJump();
                jumpBufferCounter = 0;
            }
            // Priority 3: Normal jump (using coyote time or grounded)
            else if (coyoteTimeCounter > 0)
            {
                NormalJump();
                jumpBufferCounter = 0;
                coyoteTimeCounter = 0;
            }
        }
    }
    
    /// <summary>
    /// Enhanced attack input handler with combo system and pogo attack
    /// Handles regular attacks, combos, and downward pogo attacks
    /// </summary>
    private void HandleAttackInput()
    {
        // Don't allow attacks in these states
        if (isHardLanding || isLedgeGrabbing || isLedgeClimbing || isInWater || isSwimming) return;

        // Check for pogo attack (down + attack)
        if (moveInput.y < -0.7f && attackAction.triggered && pogoUnlocked)
        {
            // Only allow pogo in air, not on ground
            if (!isGrounded && !isWallSliding && canPogoInAir)
            {
                // Only allow one pogo per jump unless we've bounced
                if (!isPogoAttacking && pogoCooldownTimer <= 0 && !isDashing && !hasPogoedThisJump)
                {
                    StartPogoAttack();
                    return; // Exit to prevent regular attack
                }
            }
        }
        
        // Check if attack button was pressed this frame
        bool attackPressed = attackAction.triggered;
        
        if (!attackPressed) return; // No attack input this frame
        
        // If currently attacking, queue next attack if within combo window
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
        
        // Start attack if allowed
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
        
        // Swim dash input
        if (dashAction.triggered && !isSwimDashing && swimDashCooldownTimer <= 0)
        {
            StartSwimDash();
        }
        
        // Jump out of water input
        if (jumpAction.triggered && !isSwimDashing)
        {
            SwimJump();
        }
    }
    
    /// <summary>
    /// Handles all ledge-related inputs including grab, climb, jump, and release
    /// Includes auto-grab when falling past ledges
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
    /// Prioritizes vertical input over horizontal for ledge climbing decisions
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

    /// <summary>
    /// Handles minimal input allowed during damage/death states
    /// Currently allows pause menu input (to be implemented)
    /// </summary>
    private void HandleMinimalInputDuringDamage()
    {
        // Allow pause menu input even when dead/taking damage
        // Example: if (Input.GetKeyDown(KeyCode.Escape)) PauseGame();
    }
    
    // ====================================================================
    // SECTION 8: MOVEMENT & PHYSICS METHODS
    // ====================================================================
    
    /// <summary>
    /// Main movement handler for ground and air movement (excluding swimming)
    /// Routes to appropriate movement handler based on current state
    /// </summary>
    private void HandleGroundAndAirMovement()
    {
        // PRIORITY 1: Knockback movement (highest priority - overrides everything)
        if (isTakingDamage && knockbackTimer > 0)
        {
            HandleKnockbackMovement();
            return;
        }
        
        // PRIORITY 2: Hard landing (player is stunned)
        if (isHardLanding)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        
        // Determine current movement speed (running or walking)
        float currentSpeed = isRunning && isGrounded ? runSpeed : moveSpeed;
        
        // STATE-SPECIFIC MOVEMENT HANDLERS (in priority order)
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
        else if (isPogoAttacking)
        {
            HandlePogoMovement();
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
    /// Applies constant velocity in dash direction with no gravity
    /// </summary>
    private void HandleDashMovement()
    {
        rb.linearVelocity = dashDirection * dashSpeed;
        rb.gravityScale = 0; // No gravity during dash
    }
    
    /// <summary>
    /// Handles movement during wall jump state with limited control
    /// Allows some horizontal influence during wall jump
    /// </summary>
    private void HandleWallJumpMovement()
    {
        // Allow limited horizontal control during wall jump
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
    /// Allows slight movement during first two combo hits, stops for final hit
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
    /// Handles wall slide movement with speed limits and acceleration
    /// Controls downward velocity while sliding on walls
    /// </summary>
    private void HandleWallSlideMovement()
    {
        // Don't apply wall slide movement if wall locked
        if (isWallLocked || isWallLockEngaging)
        {
            return;
        }
        
        // CRITICAL: Only apply wall slide movement if actually touching a wall
        if (!isTouchingWall && isWallSliding)
        {
            // Lost wall contact - let normal falling physics take over
            return;
        }
        
        // Stop horizontal movement while wall sliding
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        // Stop upward movement during wall slide
        if (rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }

        // Apply acceleration-based movement if enabled
        if (enableWallSlideAcceleration && isWallSliding && !isWallLocked)
        {
            ApplyWallSlideAcceleration();
        }
        // Original wall slide speed limiting (fallback)
        else if (isWallSliding)
        {
            float currentSlideSpeed = rb.linearVelocity.y;
            
            if (isWallClinging)
            {
                // Slower slide during wall cling
                float clingSpeed = -wallSlideSpeed * wallClingSlowdown;
                if (currentSlideSpeed < clingSpeed)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, clingSpeed);
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
            }
        }
    }
    
    /// <summary>
    /// Handles normal ground and air movement with all systems applied
    /// Main movement logic for walking, running, and air control
    /// </summary>
    private void HandleNormalMovement(float currentSpeed)
    {
        float currentMoveInput = moveInput.x;
        
        // Don't allow input into walls unless wall sliding is engaged
        if (isAgainstWall && !isWallSliding && Mathf.Sign(currentMoveInput) == lastWallSide && !isWallSlideEngaged)
        {
            currentMoveInput = 0;
        }
        
        // Calculate target velocity based on input
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
                // Allow movement away from wall to disengage
                if (Mathf.Sign(moveInput.x) == -wallSide)
                {
                    // Don't stick - allow movement away
                }
                else if (moveInput.x == 0 || Mathf.Sign(moveInput.x) == wallSide)
                {
                    // Stick when not moving or moving into wall
                    targetVelocity.x = 0;
                }
            }
        }
        
        // Smooth movement using velocity smoothing
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
    /// Handles movement during pogo attack
    /// Allows limited horizontal control and applies downward force
    /// </summary>
    private void HandlePogoMovement()
    {
        // During pogo attack, allow limited horizontal control
        float controlMultiplier = isPogoBouncing ? 0.3f : 0.1f;
        float targetXVelocity = moveInput.x * moveSpeed * controlMultiplier;
        
        // Apply horizontal movement with smoothing
        Vector2 targetVelocity = new Vector2(targetXVelocity, rb.linearVelocity.y);
        rb.linearVelocity = Vector3.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocity, movementSmoothing * 2f);
        
        // Apply additional downward force during active pogo (not bouncing)
        if (!isPogoBouncing && moveInput.y < -0.5f)
        {
            rb.AddForce(Vector2.down * pogoDownwardForce * 0.5f);
        }
    }
    
    /// <summary>
    /// Handles movement while grabbing or climbing a ledge
    /// Freezes movement during grab, handles climbing animation
    /// </summary>
    private void HandleLedgeMovement()
    {
        rb.linearVelocity = Vector2.zero;
        
        // Only zero velocity if grabbing, not climbing
        // Climbing movement is handled separately in HandleLedgeClimb()
        if (!isLedgeClimbing)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    /// <summary>
    /// Handles movement during knockback with priority over other forces
    /// Applies knockback force and prevents player control during knockback
    /// </summary>
    private void HandleKnockbackMovement()
    {
        // During knockback, NO player control - just physics
        Vector2 currentVelocity = rb.linearVelocity;
        
        // Calculate knockback decay over time (stronger at start, weaker at end)
        float knockbackProgress = 1f - (knockbackTimer / knockbackDuration);
        float currentKnockbackForce = knockbackForce * (1f - knockbackProgress);
        
        // Apply knockback direction with decay
        Vector2 knockbackVelocity = knockbackDirection * currentKnockbackForce;
        
        // Immediately set velocity (no smoothing during knockback)
        rb.linearVelocity = knockbackVelocity;
        
        // Apply reduced gravity during knockback
        if (!isGrounded)
        {
            rb.gravityScale = normalGravityScale * 0.5f;
        }
        else
        {
            // If grounded, stop vertical knockback
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }
    }
    
    /// <summary>
    /// Controls gravity based on player state
    /// Applies different gravity scales for falling, ascending, and normal states
    /// </summary>
    private void UpdateGravity()
    {
        // Skip gravity in these states (they handle gravity separately)
        if (isDashing || isWallSliding || isHardLanding || isWallJumping || isSwimming || isWallLocked || isWallLockEngaging)
            return;
        
        currentFallSpeed = rb.linearVelocity.y;
        
        // FALLING: Apply increased falling gravity
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = fallingGravityScale;
            
            // Clamp to maximum fall speed (terminal velocity)
            if (rb.linearVelocity.y < maxFallSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
            }
        }
        // ASCENDING (JUMP RELEASED): Apply quick fall gravity for faster descent
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
    /// Allows limited horizontal control while in air
    /// </summary>
    private void ApplyAirControl()
    {
        if (isGrounded || isWallSliding || isDashing || isHardLanding) return;
        
        // Calculate target velocity based on input
        float targetXVelocity = moveInput.x * moveSpeed * airControlMultiplier;
        float velocityDifference = targetXVelocity - rb.linearVelocity.x;
        
        // Apply acceleration force toward target velocity
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
    /// Controls which way the character is facing
    /// </summary>
    private void HandleFlip()
    {
        if (Mathf.Abs(moveInput.x) < 0.1f || isHardLanding) return;
            
        // Flip to face movement direction
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
    /// Flips the player sprite horizontally by inverting X scale
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
    /// Ensures attacks come from correct side of character
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
    // These methods execute specific player actions
    
    /// <summary>
    /// Executes a normal ground jump
    /// Applies upward force and resets fall tracking
    /// </summary>
    private void NormalJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        ResetFallTracking();
        hasPogoedThisJump = false;
    }
    
    /// <summary>
    /// Executes a double jump
    /// Tracks that double jump has been used for this jump sequence
    /// </summary>
    private void DoubleJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, doubleJumpForce);
        hasDoubleJumped = true;
        ResetFallTracking();
    }
    
    /// <summary>
    /// Executes a wall jump with force applied away from wall
    /// Resets various states and applies wall jump force
    /// </summary>
    private void WallJump()
    {
        // Set wall jump state
        isWallJumping = true;
        isWallSliding = false;
        isWallClinging = false;
        wallSlideState = WallSlideState.Jumping;
        wallClingTimer = 0f;
        wallJumpTimer = wallJumpDuration;

        // Disengage wall slide when jumping off
        isWallSlideEngaged = false;
        wallSlideDisengageTimer = 0f;

        // Reset wall lock when jumping
        ResetWallLockState();

        // Reset acceleration when wall jumping
        if (enableWallSlideAcceleration)
        {
            ResetWallSlideAcceleration();
        }
        
        // Stop current velocity and apply wall jump force
        rb.linearVelocity = Vector2.zero;
        Vector2 jumpDir = new Vector2(-wallSide * wallJumpAngle.x, wallJumpAngle.y).normalized;
        rb.AddForce(jumpDir * wallJumpForce, ForceMode2D.Impulse);
        
        // Face away from wall after jump
        if ((wallSide == 1 && !facingRight) || (wallSide == -1 && facingRight))
        {
            Flip();
        }
        
        // Reset air abilities
        airDashCount = 0;
        ResetFallTracking();
        hasPogoedThisJump = false; 
        CancelCombo();
    }
    
    /// <summary>
    /// Resets fall tracking variables after a jump
    /// Prepares for new fall distance measurement
    /// </summary>
    private void ResetFallTracking()
    {
        peakHeight = transform.position.y;
        fallStartHeight = transform.position.y;
        fellFromOffScreen = CheckIfFellFromOffScreen(fallStartHeight);
    }
    
    /// <summary>
    /// Starts dash movement
    /// Determines dash direction based on input and facing direction
    /// </summary>
    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        // Disengage wall slide when dashing
        if (isWallSlideEngaged)
        {
            DisengageWallSlide();
        }
        
        // Determine dash direction (default to facing direction)
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
    /// Called when dash timer expires
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
    /// Manages combo state and executes appropriate attack
    /// </summary>
    private void StartComboAttack()
    {
        // Determine combo index (0-based)
        int comboIndex = Mathf.Clamp(currentCombo, 0, maxComboHits - 1);
        
        // Start attack state
        isAttacking = true;
        attackTimer = comboAttackDurations[comboIndex];
        attackCooldownTimer = comboCooldowns[comboIndex];
        
        // Update combo state
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
    /// Checks for enemies in range and applies damage/knockback
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
            // Enemy damage logic would go here
            // Example: enemy.GetComponent<EnemyHealth>().TakeDamage(damage);
            
            // Apply knockback
            Vector2 knockbackDirection = facingRight ? Vector2.right : Vector2.left;
            // enemy.GetComponent<Rigidbody2D>().AddForce(knockbackDirection * knockback, ForceMode2D.Impulse);
        }
        
        // Apply movement effects based on combo hit
        ApplyComboMovement(comboIndex);
    }

    /// <summary>
    /// Applies movement effects during combo attacks
    /// Adds forward momentum for first two hits, upward lift for final hit
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
    /// Checks for queued attacks and manages combo reset
    /// </summary>
    private void EndAttack()
    {
        isAttacking = false;
        
        // Check if we have a queued attack to execute
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
    /// Resets combo state completely
    /// Called when combo times out or is cancelled
    /// </summary>
    private void ResetCombo()
    {
        currentCombo = 0;
        comboWindowTimer = 0f;
        comboResetTimer = 0f;
        comboAvailable = true;
        isComboFinishing = false;
        attackQueued = false; // Clear any queued attacks
        
        // Ensure attack state is cleared
        if (!isAttacking)
        {
            attackCooldownTimer = 0f;
        }
    }

    /// <summary>
    /// Cancels combo (called when taking damage, etc.)
    /// Immediately resets combo and stops attacking
    /// </summary>
    public void CancelCombo()
    {
        ResetCombo();
        isAttacking = false;
        attackCooldownTimer = 0f;
        attackQueued = false; // Clear queued attacks
    }

    /// <summary>
    /// Core method for taking damage - call this from enemies, traps, etc.
    /// Handles damage application, knockback, invincibility, and death
    /// </summary>
    /// <param name="damageAmount">Amount of damage to take</param>
    /// <param name="damageSource">Position where damage came from (for knockback direction)</param>
    /// <param name="overrideKnockback">Optional custom knockback force</param>
    public void TakeDamage(int damageAmount, Vector3 damageSource, float overrideKnockback = 0f)
    {
        // Don't take damage if invincible, dead, or in certain states
        if (isInvincible || isDead || isRespawning || godMode) 
            return;
        
        // Check for i-frame states (dash, wall jump, etc.)
        if (CheckDamageImmunityStates()) 
            return;
        
        // Apply damage
        int actualDamage = Mathf.Clamp(damageAmount, 1, currentHealth);
        currentHealth -= actualDamage;
        
        // Cancel any ongoing actions
        CancelPlayerActions();
        
        // Calculate knockback direction away from damage source
        CalculateKnockbackDirection(damageSource);
        
        // Apply knockback force
        float knockbackForceToUse = overrideKnockback > 0 ? overrideKnockback : knockbackForce;
        ApplyKnockback(knockbackForceToUse);
        
        // Start invincibility frames
        StartInvincibility();
        
        // Visual and audio feedback
        TriggerDamageFeedback(actualDamage);
        
        // Camera effects (screen shake)
        TriggerDamageCameraEffects();
        
        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Checks if player is in a state that grants damage immunity
    /// Some actions provide temporary invincibility
    /// </summary>
    private bool CheckDamageImmunityStates()
    {
        // States that grant temporary invincibility
        if (isDashing) return true;
        if (isWallJumping && wallJumpTimer > wallJumpDuration * 0.5f) return true;
        if (isAttacking && currentCombo >= 2) return true; // Later combo hits might have armor
        
        return false;
    }
    
    /// <summary>
    /// Cancels all player actions when taking damage
    /// Stops dashes, attacks, wall slides, etc.
    /// </summary>
    private void CancelPlayerActions()
    {
        // Cancel dash
        if (isDashing) EndDash();
        
        // Cancel attacks
        CancelCombo();
        
        // Cancel pogo
        if (isPogoAttacking) EndPogoAttack();
        
        // Cancel wall slide
        isWallSliding = false;
        wallSlideState = WallSlideState.None;
        
        // Cancel ledge grab
        if (isLedgeGrabbing || isLedgeClimbing)
            ReleaseLedge();
    }
    
    /// <summary>
    /// Calculates direction for knockback based on damage source
    /// Knocks player away from where damage came from
    /// </summary>
    private void CalculateKnockbackDirection(Vector3 damageSource)
    {
        // Direction away from damage source
        Vector2 direction = (transform.position - damageSource).normalized;
        
        // Ensure minimum vertical/horizontal components for consistent knockback
        if (Mathf.Abs(direction.x) < 0.3f) direction.x = Mathf.Sign(direction.x) * 0.3f;
        if (Mathf.Abs(direction.y) < 0.1f) direction.y = 0.1f;
        
        // Normalize and apply multipliers
        direction.Normalize();
        knockbackDirection = new Vector2(
            direction.x * horizontalKnockbackMultiplier,
            direction.y * verticalKnockbackMultiplier
        ).normalized;
    }
    
    /// <summary>
    /// Applies knockback force to the player
    /// Sets up knockback state and animation lock
    /// </summary>
    private void ApplyKnockback(float force)
    {
        isTakingDamage = true;
        knockbackTimer = knockbackDuration;
        
        // Lock animation to prevent interruption during knockback
        isAnimationLocked = true;
        animationLockTimer = knockbackDuration;
        
        // Stop current velocity
        rb.linearVelocity = Vector2.zero;
        
        // Apply knockback force as impulse
        rb.AddForce(knockbackDirection * force, ForceMode2D.Impulse);
        
        // Cap maximum knockback velocity
        float maxKnockbackSpeed = force * 1.5f;
        if (rb.linearVelocity.magnitude > maxKnockbackSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxKnockbackSpeed;
        }
        
        // Force play hurt animation immediately
        if (animator != null)
        {
            animator.Play("Kalb_hurt", -1, 0f);
            animator.Update(0f); // Force immediate update
        }
    }
    
    /// <summary>
    /// Starts invincibility frames after taking damage
    /// Sets up invincibility timer and starts flashing effect
    /// </summary>
    private void StartInvincibility()
    {
        isInvincible = true;
        invincibilityTimer = invincibilityTime;
        
        // Start flashing effect coroutine
        StartCoroutine(DamageFlashRoutine());
    }
    
    /// <summary>
    /// Ends invincibility frames
    /// Restores normal vulnerability and sprite color
    /// </summary>
    private void EndInvincibility()
    {
        isInvincible = false;
        invincibilityTimer = 0f;
        
        // Restore sprite color
        if (playerSprite != null)
        {
            playerSprite.color = originalSpriteColor;
        }
    }
    
    /// <summary>
    /// Ends knockback state
    /// Restores player control and returns to appropriate animation
    /// </summary>
    private void EndKnockback()
    {
        isTakingDamage = false;
        knockbackTimer = 0f;
        isAnimationLocked = false; 
        
        // Smoothly stop horizontal movement
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, rb.linearVelocity.y);
        }

        // Return to idle animation if grounded
        if (isGrounded && animator != null)
        {
            animator.Play("Kalb_idle", -1, 0f);
        }
    }

    /// <summary>
    /// Triggers visual and audio feedback for damage
    /// Shows damage numbers, plays sounds, triggers animations
    /// </summary>
    private void TriggerDamageFeedback(int damageAmount)
    {
        // Damage number popup
        if (showDamageNumbers && damageNumberPrefab != null)
        {
            Vector3 spawnPosition = transform.position + Vector3.up * damageNumberYOffset;
            GameObject damageNumber = Instantiate(damageNumberPrefab, spawnPosition, Quaternion.identity);
            
            // Damage number script would set the text here
            // damageNumber.GetComponent<DamageNumber>().SetDamage(damageAmount);
        }
        
        // Play hit sound (commented out - implement as needed)
        // if (audioSource != null && hitSound != null)
        //     audioSource.PlayOneShot(hitSound);
        
        // Play hurt animation
        if (animator != null && !isDead)
        {
            animator.Play("Kalb_hurt", -1, 0f);
        }
    }
    
    /// <summary>
    /// Coroutine for damage flash effect
    /// Flashes sprite between hurt color and original color
    /// </summary>
    private System.Collections.IEnumerator DamageFlashRoutine()
    {
        if (playerSprite == null) yield break;
        
        float flashInterval = 0.1f;
        int flashCount = Mathf.FloorToInt(invincibilityTime / flashInterval);
        
        // Alternate colors for flashing effect
        for (int i = 0; i < flashCount; i++)
        {
            playerSprite.color = (i % 2 == 0) ? hurtFlashColor : originalSpriteColor;
            yield return new WaitForSeconds(flashInterval);
        }
        
        // Ensure original color at the end
        playerSprite.color = originalSpriteColor;
    }
    
    /// <summary>
    /// Triggers camera effects for damage (screen shake)
    /// </summary>
    private void TriggerDamageCameraEffects()
    {
        if (metroidvaniaCamera == null) return;
        
        // Calculate screen shake based on damage taken
        float shakeIntensity = Mathf.Clamp(knockbackForce * 0.01f, 0.05f, 0.2f);
        float shakeDuration = Mathf.Clamp(knockbackDuration, 0.1f, 0.3f);
        
        // Trigger screen shake in knockback direction
        metroidvaniaCamera.TriggerScreenShake(shakeIntensity, shakeDuration, knockbackDirection);
    }

    /// <summary>
    /// Handles player death
    /// Stops physics, plays death animation, triggers death effects
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        deathTimer = deathAnimationTime;
        
        // Disable physics and stop movement
        DisablePlayerPhysics();
        
        // Cancel all active states
        CancelPlayerActions();
        
        // Play death animation
        if (animator != null)
        {
            animator.Play("Kalb_death", -1, 0f);
            animator.Update(0f);
            animator.SetBool("IsDead", true);
        }
        
        // Spawn death visual effect
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }
    }

    /// <summary>
    /// Disables player physics for death animation
    /// Stops movement and disables collisions
    /// </summary>
    private void DisablePlayerPhysics()
    {
        // Stop all movement
        rb.linearVelocity = Vector2.zero;
        
        // Disable physics simulation but keep body dynamic
        rb.simulated = false;
        
        // Add slight upward force for death animation
        rb.linearVelocity = new Vector2(0, 3f);
        
        // Disable collider for death animation
        if (playerCollider != null)
            playerCollider.enabled = false;
    }

    /// <summary>
    /// Restores player physics after respawn
    /// Re-enables all physics components
    /// </summary>
    private void RestorePlayerPhysics()
    {
        // Re-enable everything in Respawn()
        rb.simulated = true;
        if (playerCollider != null)
            playerCollider.enabled = true;
        
        // Reset velocity and gravity
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = normalGravityScale;
    }
    
    /// <summary>
    /// Respawns player at last checkpoint
    /// Restores health, physics, and sets up respawn invincibility
    /// </summary>
    private void Respawn()
    {
        isDead = false;
        isRespawning = true;
        respawnTimer = respawnInvincibilityTime;
        
        // RESTORE PHYSICS PROPERLY
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Only freeze rotation
        rb.simulated = true; // Re-enable physics simulation
        rb.gravityScale = normalGravityScale;
        rb.linearVelocity = Vector2.zero;
        
        // Restore collisions
        if (playerCollider != null)
            playerCollider.enabled = true;
        
        // Reset animator death state
        if (animator != null)
        {
            animator.SetBool("IsDead", false);
        }
        
        // Reset health to full
        currentHealth = maxHealth;
        
        // Teleport to checkpoint with safety offset
        Vector3 safeSpawnPosition = lastCheckpointPosition + Vector3.up * 0.5f;
        transform.position = safeSpawnPosition;
        
        // Reset input
        moveInput = Vector2.zero;
        
        // Start respawn invincibility flashing
        StartCoroutine(RespawnFlashRoutine());
        
        // Play respawn animation
        if (animator != null)
        {
            animator.Play("Kalb_respawn", -1, 0f);
        }
    }
    
    /// <summary>
    /// Sets a new checkpoint position for respawning
    /// Called when player touches checkpoints
    /// </summary>
    public void SetCheckpoint(Vector3 checkpointPosition)
    {
        lastCheckpointPosition = checkpointPosition;
    }
    
    /// <summary>
    /// Ends respawn invincibility
    /// Restores normal vulnerability and sprite appearance
    /// </summary>
    private void EndRespawnInvincibility()
    {
        isRespawning = false;
        respawnTimer = 0f;
        
        // Restore sprite color
        if (playerSprite != null)
        {
            playerSprite.color = originalSpriteColor;
        }
    }
    
    /// <summary>
    /// Coroutine for respawn flash effect
    /// Flashes player between semi-transparent and normal during respawn invincibility
    /// </summary>
    private System.Collections.IEnumerator RespawnFlashRoutine()
    {
        if (playerSprite == null) yield break;
        
        float flashInterval = 0.15f;
        int flashCount = Mathf.FloorToInt(respawnInvincibilityTime / flashInterval);
        
        // Alternate between semi-transparent and normal
        for (int i = 0; i < flashCount; i++)
        {
            Color flashColor = originalSpriteColor;
            flashColor.a = (i % 2 == 0) ? 0.3f : 1f;
            playerSprite.color = flashColor;
            yield return new WaitForSeconds(flashInterval);
        }
        
        // Ensure full opacity at the end
        playerSprite.color = originalSpriteColor;
    }

    /// <summary>
    /// Checks if player has fallen out of bounds and kills them
    /// Detects falling below death boundary
    /// </summary>
    private void CheckDeathBoundary()
    {
        if (isDead || godMode) return;
        
        // Define death boundary (adjust based on your game)
        float deathY = -20f; // Fall below this Y position = death
        
        if (transform.position.y < deathY)
        {
            // Instant death from falling
            TakeDamage(currentHealth, transform.position + Vector3.up * 2f, 0f);
        }
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
            // Soft landing animation
            animator.Play("Kalb_land");
        }
    }
    
    /// <summary>
    /// Checks if fall distance is at least one screen height
    /// Used for screen-height based hard landing detection
    /// </summary>
    private bool CheckScreenHeightFall(float fallDistance)
    {
        float requiredDistance = screenHeightInUnits * minScreenHeightForHardLanding;
        return fallDistance >= requiredDistance;
    }
    
    /// <summary>
    /// Checks if fall started from off-screen (above camera view)
    /// Used for detecting dramatic falls from off-screen platforms
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
    /// Used for off-screen fall detection
    /// </summary>
    private Bounds GetScreenBounds()
    {
        if (mainCamera == null) return new Bounds(transform.position, new Vector3(20, 10, 0));
        
        // Convert screen corners to world positions
        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));
        Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, mainCamera.nearClipPlane));
        
        // Create bounds from corners
        Bounds bounds = new Bounds();
        bounds.SetMinMax(bottomLeft, topRight);
        return bounds;
    }
    
    /// <summary>
    /// Initiates hard landing state with screen shake
    /// Stuns player and triggers camera effects
    /// </summary>
    private void StartHardLanding()
    {
        isHardLanding = true;
        hardLandingTimer = hardLandingStunTime;
        
        // Stop all movement
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        
        // Play hard landing animation
        animator.Play("Kalb_hard_land");
        
        // Trigger camera effects (screen shake)
        TriggerCameraEffects();
    }

    /// <summary>
    /// Triggers camera effects based on player actions
    /// Handles screen shake for hard landings, dashes, and wall jumps
    /// </summary>
    private void TriggerCameraEffects()
    {
        if (metroidvaniaCamera == null) return;
        
        // Hard landing camera shake
        if (isHardLanding)
        {
            // Use enhanced hard landing shake with optional time freeze
            metroidvaniaCamera.TriggerHardLandingWithFreeze(currentFallSpeed, totalFallDistance);
        }
        
        // Dash camera shake
        if (isDashing && dashUnlocked)
        {
            // Directional shake in dash direction
            Vector3 dashDir = facingRight ? Vector3.right : Vector3.left;
            metroidvaniaCamera.TriggerScreenShake(0.08f, 0.1f, dashDir);
        }
        
        // Wall jump camera effect
        if (isWallJumping)
        {
            Vector3 wallJumpDir = new Vector3(-wallSide * 0.7f, 0.3f, 0);
            metroidvaniaCamera.TriggerScreenShake(0.12f, 0.15f, wallJumpDir);
        }
    }
    
    /// <summary>
    /// Ends hard landing recovery
    /// Restores movement and returns to idle animation
    /// </summary>
    private void EndHardLanding()
    {
        isHardLanding = false;
        rb.gravityScale = normalGravityScale;
        animator.Play("Kalb_idle");
    }

    /// <summary>
    /// Starts a pogo attack (downward attack that can bounce off enemies/spikes)
    /// Sets up pogo state and applies initial downward force
    /// </summary>
    private void StartPogoAttack()
    {
        isPogoAttacking = true;
        isAttacking = true; // Also set regular attacking flag for compatibility
        pogoAttackTimer = pogoAttackDuration;
        pogoCooldownTimer = pogoAttackCooldown;
        pogoDirection = moveInput.y < -0.7f ? Vector2.down : (Vector2)(attackPoint.position - transform.position).normalized;

        // Mark that we've used our pogo for this jump
        hasPogoedThisJump = true;
        
        // Apply initial downward force for pogo
        if (moveInput.y < -0.7f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -pogoDownwardForce);
        }
        
        // Play pogo animation
        if (animator != null)
        {
            animator.Play("Kalb_pogo_attack");
        }
        
        // Cancel combo if active
        CancelCombo();
        
        // Detect pogo-able objects immediately
        CheckForPogoTargets();
    }

    /// <summary>
    /// Checks for pogo-able targets below the player
    /// Uses raycasting and sphere casting for detection
    /// </summary>
    private void CheckForPogoTargets()
    {
        Vector2 detectionPoint = transform.position;
        float detectionRange = pogoDetectionRange;
        
        // Raycast downward for spikes/enemies
        RaycastHit2D[] hits = Physics2D.RaycastAll(detectionPoint, Vector2.down, detectionRange, pogoLayers);
        
        // Process raycast hits
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null)
            {
                HandlePogoHit(hit.collider.gameObject, hit.point);
                break; // Only pogo on first valid target
            }
        }
        
        // Also check sphere cast for better detection of wider targets
        Collider2D[] sphereHits = Physics2D.OverlapCircleAll(detectionPoint + (Vector2.down * (detectionRange / 2f)), 
                                                            detectionRange / 2f, pogoLayers);
        
        // Process sphere cast hits (skip if already handled by raycast)
        foreach (Collider2D collider in sphereHits)
        {
            // Skip if already handled by raycast
            bool alreadyHit = false;
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == collider)
                {
                    alreadyHit = true;
                    break;
                }
            }
            
            if (!alreadyHit)
            {
                HandlePogoHit(collider.gameObject, collider.bounds.center);
                break;
            }
        }
    }

    /// <summary>
    /// Handles hitting a pogo-able target
    /// Processes spike tiles and enemies, triggers bounce and effects
    /// </summary>
    private void HandlePogoHit(GameObject target, Vector2 hitPoint)
    {
        // Check if it's a spike tile
        SpikeTile spikeTile = target.GetComponent<SpikeTile>();
        if (spikeTile != null && enablePogoOnSpikes)
        {
            if (spikeTile.CanBePogoed())
            {
                PerformPogoBounce();
                currentPogoChain++;
                pogoChainTimer = pogoChainWindow;
                lastPogoTime = Time.time;
                
                // Spawn pogo visual effect
                if (pogoEffectPrefab != null)
                {
                    GameObject effect = Instantiate(pogoEffectPrefab, hitPoint, Quaternion.identity);
                    Destroy(effect, pogoEffectDuration);
                }
                
                // Flash player sprite for visual feedback
                StartCoroutine(PogoFlashEffect());
                
                return;
            }
        }
        
        // Check if it's an enemy (you'll need to adapt this to your enemy system)
        // Example:
        // EnemyHealth enemy = target.GetComponent<EnemyHealth>();
        // if (enemy != null)
        // {
        //     enemy.TakeDamage(pogoDamage);
        //     PerformPogoBounce();
        //     currentPogoChain++;
        //     pogoChainTimer = pogoChainWindow;
        //     return;
        // }
    }

    /// <summary>
    /// Performs the pogo bounce
    /// Applies upward force and resets jump/dash abilities
    /// </summary>
    private void PerformPogoBounce()
    {
        isPogoBouncing = true;
        lastPogoTime = Time.time;
        
        // Apply bounce force upward
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); // Reset vertical velocity
        rb.AddForce(Vector2.up * pogoBounceForce, ForceMode2D.Impulse);
        
        // Reset jump states for additional mobility after bounce
        if (resetJumpOnPogo)
        {
            hasDoubleJumped = false;
            airDashCount = 0;
            coyoteTimeCounter = coyoteTime;
        }

        // Reset pogo count so you can pogo again after successful bounce
        hasPogoedThisJump = false; 
        
        // Apply slight horizontal movement based on input for control during bounce
        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            rb.AddForce(new Vector2(moveInput.x * 3f, 0), ForceMode2D.Impulse);
        }
    }

    /// <summary>
    /// Applies pogo bounce from external sources (like SpikeTile)
    /// Allows external objects to trigger pogo bounce
    /// </summary>
    public void ApplyPogoBounce(float force)
    {
        if (!isPogoAttacking) return;
        
        isPogoBouncing = true;
        lastPogoTime = Time.time;
        
        // Apply bounce force
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        
        // Reset jump states
        if (resetJumpOnPogo)
        {
            hasDoubleJumped = false;
            airDashCount = 0;
            coyoteTimeCounter = coyoteTime;
        }

        // Reset pogo count so you can pogo again after successful bounce
        hasPogoedThisJump = false;
    }

    /// <summary>
    /// Ends pogo attack
    /// Cleans up pogo state and manages bounce continuation
    /// </summary>
    private void EndPogoAttack()
    {
        isPogoAttacking = false;
        isAttacking = false;
        
        // If we're still in bounce state, maintain it briefly
        if (isPogoBouncing && rb.linearVelocity.y > 0)
        {
            // Let bounce continue naturally
        }
        else
        {
            isPogoBouncing = false;
        }

        // If pogo attack ended without bouncing, we've still used our pogo for this jump
        // This prevents spamming pogo attacks in air without hitting anything
        if (!isPogoBouncing)
        {
            hasPogoedThisJump = true; 
        }
    }

    /// <summary>
    /// Resets pogo chain count
    /// Called when chain window expires
    /// </summary>
    private void ResetPogoChain()
    {
        currentPogoChain = 0;
        pogoChainTimer = 0f;
    }

    /// <summary>
    /// Coroutine for pogo flash effect
    /// Flashes player sprite when pogo hits something
    /// </summary>
    private System.Collections.IEnumerator PogoFlashEffect()
    {
        SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();
        if (playerSprite != null)
        {
            Color original = playerSprite.color;
            playerSprite.color = pogoFlashColor;
            yield return new WaitForSeconds(pogoFlashDuration);
            playerSprite.color = original;
        }
    }

    /// <summary>
    /// Checks if player is currently pogo attacking
    /// Public method for other scripts to check pogo state
    /// </summary>
    public bool IsPogoAttacking()
    {
        return isPogoAttacking;
    }

    /// <summary>
    /// Gets current pogo chain count
    /// Public method for tracking consecutive pogo bounces
    /// </summary>
    public int GetCurrentPogoChain()
    {
        return currentPogoChain;
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
        
        // Save and disable gravity during swim dash
        preDashGravityScale = rb.gravityScale;
        rb.gravityScale = 0f;

        // Disengage wall slide when dashing
        if (isWallSlideEngaged)
        {
            DisengageWallSlide();
        }
        
        // Determine dash direction based on input or facing direction
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
    /// Applies slowdown to prevent abrupt stops
    /// </summary>
    private void EndSwimDash()
    {
        isSwimDashing = false;
        rb.gravityScale = preDashGravityScale;
        
        // Slow down gradually after dash
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, rb.linearVelocity.y * 0.5f);
    }
    
    /// <summary>
    /// Jumps out of water
    /// Exits swimming state and restores normal gravity and jump abilities
    /// </summary>
    private void SwimJump()
    {
        if (!isSwimming || isSwimDashing) return;
        
        // Apply upward force for water jump
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, swimJumpForce);
        isSwimming = false;
        rb.gravityScale = normalGravityScale;
        
        // Allow potential double jump after water jump
        coyoteTimeCounter = coyoteTime;
        hasPogoedThisJump = false; 
        CancelCombo();
    }
    
    /// <summary>
    /// Main swimming movement handler with buoyancy and floating
    /// Handles horizontal movement, buoyancy, and bobbing effects
    /// </summary>
    private void HandleSwimMovement()
    {
        if (!isSwimming || isHardLanding) return;
        
        // Handle swim dash first (highest priority)
        if (isSwimDashing)
        {
            rb.linearVelocity = swimDashDirection * swimDashSpeed;
            rb.gravityScale = 0f; // No gravity during dash
            return;
        }
        
        // Apply fast buoyancy FIRST (most important) - always active
        ApplyBuoyancy();
        
        // Calculate horizontal movement speed
        float currentSwimSpeed = swimSpeed;
        
        // Check for fast swimming (when holding dash button)
        if (dashAction.IsPressed() && !isSwimDashing)
        {
            currentSwimSpeed = swimFastSpeed;
        }
        
        // Apply horizontal movement with velocity control
        float targetXVelocity = moveInput.x * currentSwimSpeed;
        float currentXVelocity = rb.linearVelocity.x;
        
        // Smooth horizontal movement but prioritize buoyancy
        float horizontalAcceleration = isInWater ? 25f : 15f; // Faster acceleration in water
        float newXVelocity = Mathf.MoveTowards(currentXVelocity, targetXVelocity, 
                                            Time.fixedDeltaTime * horizontalAcceleration);
        
        // Apply floating effect when not actively moving horizontally
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
            
            // Apply the floating offset to position
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
    /// Uses spring physics to maintain position at water surface
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
        
        // Calculate buoyancy force with damping (spring physics)
        float buoyancyForce = depthDifference * buoyancyStrength;
        float dampingForce = -rb.linearVelocity.y * buoyancyDamping;
        float totalForce = Mathf.Clamp(buoyancyForce + dampingForce, -maxBuoyancyForce, maxBuoyancyForce);
        
        // Apply force
        rb.AddForce(new Vector2(0, totalForce));
    }
    
    /// <summary>
    /// Applies direct position correction for buoyancy (backup system)
    /// Used when spring physics isn't enough to keep player at surface
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
                
                // OR use direct position adjustment as fallback for extreme cases
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
    /// Creates sine wave motion for natural water bobbing
    /// </summary>
    private void ApplyFloatingEffect()
    {
        if (!isSwimming || isSwimDashing || !enableFloating) return;
        
        // Update timer for sine wave
        floatTimer += Time.deltaTime * floatFrequency;
        
        // Calculate sine wave for bobbing
        float sineWave = Mathf.Sin(floatTimer * Mathf.PI * 2f);
        
        // Reduce effect when moving horizontally
        float horizontalMovementFactor = Mathf.Clamp01(1f - Mathf.Abs(moveInput.x) * 0.5f);
        targetFloatOffset = sineWave * floatAmplitude * horizontalMovementFactor;
        
        // Smooth interpolation between current and target offset
        currentFloatOffset = Mathf.Lerp(currentFloatOffset, targetFloatOffset, Time.deltaTime * floatSmoothness);
        
        // Update original position reference
        if (Mathf.Abs(currentFloatOffset) > 0.01f && Mathf.Abs(moveInput.x) < 0.1f)
        {
            originalPosition = new Vector3(originalPosition.x, 
                                        transform.position.y - currentFloatOffset, 
                                        originalPosition.z);
        }
        
        // Apply offset to position
        Vector3 currentPos = transform.position;
        transform.position = new Vector3(currentPos.x, originalPosition.y + currentFloatOffset, currentPos.z);
    }
    
    /// <summary>
    /// Checks if player is in water and handles state transitions
    /// Uses overlap circle to detect water colliders
    /// </summary>
    private void CheckWater()
    {
        wasInWater = isInWater;
        
        // Check for water overlap using circle cast
        Collider2D waterCollider = Physics2D.OverlapCircle(
            transform.position, 
            waterCheckRadius, 
            waterLayer
        );
        
        // Update water state
        isInWater = waterCollider != null;
        currentWaterCollider = waterCollider;
        
        // Store water surface position if in water
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
        
        // Initialize floating effect with random phase
        floatTimer = Random.Range(0f, Mathf.PI * 2f);
        originalPosition = transform.position;
        currentFloatOffset = 0f;
        targetFloatOffset = 0f;
        
        // Adjust velocity for water entry
        if (rb.linearVelocity.y < 0)
        {
            // Slow down downward momentum when entering water
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.7f, -3f);
        }
        else
        {
            // Reduce upward momentum when entering water
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.7f, Mathf.Min(rb.linearVelocity.y, 2f) * 0.3f);
        }
        
        // Set water physics (reduced gravity)
        rb.gravityScale = waterEntryGravity;
        
        // Reset other states that don't work in water
        isDashing = false;
        isWallJumping = false;
        wallSlideState = WallSlideState.None;
        isWallSliding = false;
        airDashCount = 0;
        hasDoubleJumped = false;
        hasPogoedThisJump = false;
        CancelCombo();
    }
    
    /// <summary>
    /// Called when player exits water
    /// Restores normal physics and resets swimming state
    /// </summary>
    private void OnExitWater()
    {
        isSwimming = false;
        isSwimDashing = false;
        isInWater = false;
        
        // Restore normal gravity
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
        // Don't check if grounded, not moving, or hard landing
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
        
        // Cast multiple rays at different heights to detect walls
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
            
            // Prevent input into wall, but allow movement away
            if (Mathf.Sign(moveInput.x) == lastWallSide)
            {
                if (!isWallSliding || !isWallSlideEngaged)
                {
                    moveInput = new Vector2(0, moveInput.y);
                    
                    // Slow down velocity into wall
                    if (Mathf.Sign(rb.linearVelocity.x) == lastWallSide && Mathf.Abs(rb.linearVelocity.x) > 0.1f)
                    {
                        float reduction = Mathf.Lerp(rb.linearVelocity.x, 0, Time.deltaTime * 15f);
                        rb.linearVelocity = new Vector2(reduction, rb.linearVelocity.y);
                    }
                }
                // If wall sliding and engaged, allow some movement away detection
                else if (isWallSliding && isWallSlideEngaged)
                {
                    // Check if trying to move away (but input is still into wall due to smoothing)
                    // This helps with the transition
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
    /// Manages wall slide engagement, acceleration, and wall lock
    /// </summary>
    private void HandleWallSlide()
    {
        // Don't wall slide if ability not unlocked or in certain states
        if (!wallJumpUnlocked || isHardLanding || isLedgeGrabbing || isLedgeClimbing) 
        {
            isWallSliding = false;
            isWallClinging = false;
            wallSlideState = WallSlideState.None;

            ResetWallLockState();
            // Reset acceleration when leaving wall slide
            if (enableWallSlideAcceleration)
            {
                currentWallSlideSpeed = 0f;
                wallSlideAccelerationTimer = 0f;
                isAcceleratingWallSlide = false;
            }
            return;
        }

        // Update wall slide acceleration if enabled
        if (enableWallSlideAcceleration && isWallSliding)
        {
            UpdateWallSlideAcceleration();
        }

        // Handle wall lock if ability is unlocked
        if (wallLockUnlocked && isWallSliding)
        {
            HandleWallLock();
        }

        // CRITICAL FIX: Check if wall slide should end due to lost wall contact
        if (isWallSliding && !isTouchingWall && !isWallSlideEngaged)
        {
            // Platform ended - transition to falling
            EndWallSlide();
            return;
        }

        // Force wall slide when engaged, even without input
        if (isWallSlideEngaged && isTouchingWall && !isWallSliding)
        {
            isWallSliding = true;
            wallSlideState = WallSlideState.Sliding;

            ResetHardLandingTracking();
        }

        // Check if we should automatically disengage wall slide
        if (isWallSlideEngaged && !isTouchingWall && wallSlideDisengageTimer <= 0)
        {
            isWallSlideEngaged = false;
            isWallSliding = false;
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
            // CRITICAL FIX: Ensure wall slide state is properly set when touching wall
            if (wallSlideState == WallSlideState.None)
            {
                wallSlideState = WallSlideState.Starting;
                if (rb.linearVelocity.y > 0)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                }

                // Engage wall slide automatically
                if (!isWallSlideEngaged)
                {
                    isWallSlideEngaged = true;
                    ResetHardLandingTracking();
                }
            }
            else if (wallSlideState == WallSlideState.Starting)
            {
                wallSlideState = WallSlideState.Sliding;
            }
            
            //Automatically slide if engaged, regardless of input
            if (isWallSlideEngaged)
            {
                isWallSliding = true;
            }
            
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
            // CRITICAL FIX: When not touching wall, only reset if not engaged 
            // AND after disengage timer expires
            if (!isWallSlideEngaged || wallSlideDisengageTimer <= 0)
            {
                // Actually losing wall contact - transition properly
                if (isWallSliding)
                {
                    EndWallSlide();
                }
                else
                {
                    wallSlideState = WallSlideState.None;
                    isWallSliding = false;
                    isWallClinging = false;
                }
            }
        }
    }

    /// <summary>
    /// Ends wall slide and transitions to falling state
    /// Resets all wall slide related states
    /// </summary>
    private void EndWallSlide()
    {
        // Reset all wall slide states
        isWallSliding = false;
        isWallClinging = false;
        isWallSlideEngaged = false;
        wallSlideState = WallSlideState.None;
        wallSlideDisengageTimer = 0f;
        
        // Reset wall lock when ending wall slide
        ResetWallLockState();
        
        // Reset acceleration
        if (enableWallSlideAcceleration)
        {
            currentWallSlideSpeed = 0f;
            wallSlideAccelerationTimer = 0f;
            isAcceleratingWallSlide = false;
        }
        
        // Allow normal falling physics to apply
        // Don't zero out velocity - let gravity take over naturally
        // This ensures smooth transition to falling animation
        
        // Reset wall contact state
        isTouchingWall = false;
        wallSide = 0;
        
        // CRITICAL: Restore normal gravity immediately
        if (!isDashing && !isWallJumping && !isHardLanding)
        {
            rb.gravityScale = normalGravityScale;
        }
    }

    /// <summary>
    /// Handles wall lock ability - locks player to wall position while holding input
    /// Allows player to stop sliding and hold position on wall
    /// </summary>
    private void HandleWallLock()
    {
        if (!wallLockUnlocked || !isWallSliding || !isWallSlideEngaged) return;
        
        // Calculate input direction relative to wall
        float inputDirection = Mathf.Sign(moveInput.x);
        bool pressingIntoWall = Mathf.Abs(moveInput.x) > wallLockInputThreshold && 
                            inputDirection == wallSide;
        
        // Handle wall lock engagement
        if (pressingIntoWall && !isWallLocked && !isWallLockEngaging)
        {
            StartWallLock();
        }
        // Handle wall lock disengagement (releasing input)
        else if (!pressingIntoWall && isWallLocked && !isWallLockDisengaging)
        {
            EndWallLock();
        }
        
        // Update wall lock timers
        UpdateWallLockTimers();
        
        // Apply wall lock movement
        ApplyWallLockMovement();
    }

    /// <summary>
    /// Starts the wall lock process
    /// Begins transition to locked state on wall
    /// </summary>
    private void StartWallLock()
    {
        if (isWallLocked || isWallLockEngaging) return;
        
        isWallLockEngaging = true;
        isWallLockDisengaging = false;
        wallLockTimer = wallLockEngageTime;
        
        // Store original wall slide speed
        originalWallSlideSpeed = wallSlideSpeed;

        // Start deceleration for wall lock
        if (enableWallSlideAcceleration)
        {
            // Set deceleration direction
            wallSlideAccelerationDirection = -1f;
            wallSlideAccelerationTimer = currentWallSlideSpeed / wallSlideSpeed;
        }
        
        // Reset air dash if enabled
        if (resetAirDashOnWallLock)
        {
            airDashCount = 0;
        }
        
        // Reduce gravity to zero for smooth lock
        rb.gravityScale = 0f;
        
        // Stop vertical movement immediately
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
    }

    /// <summary>
    /// Ends the wall lock and resumes normal wall slide
    /// Begins transition back to sliding state
    /// </summary>
    private void EndWallLock()
    {
        if (!isWallLocked || isWallLockDisengaging) return;
        
        isWallLockDisengaging = true;
        isWallLockEngaging = false;
        wallLockTimer = wallLockDisengageTime;
        
        // Restore gravity
        rb.gravityScale = normalGravityScale;
    }

    /// <summary>
    /// Updates wall lock state timers
    /// Handles transitions between locked/unlocked states
    /// </summary>
    private void UpdateWallLockTimers()
    {
        if (wallLockTimer > 0)
        {
            wallLockTimer -= Time.deltaTime;
            
            // Timer finished - complete the state transition
            if (wallLockTimer <= 0)
            {
                if (isWallLockEngaging)
                {
                    CompleteWallLockEngagement();
                }
                else if (isWallLockDisengaging)
                {
                    CompleteWallLockDisengagement();
                }
            }
        }
    }

    /// <summary>
    /// Completes the wall lock engagement process
    /// Finalizes transition to fully locked state
    /// </summary>
    private void CompleteWallLockEngagement()
    {
        isWallLocked = true;
        isWallLockEngaging = false;
        
        // Completely stop movement
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        
        // Set to wall lock speed (very slow or stopped)
        wallSlideSpeed = wallLockSpeed;
    }

    /// <summary>
    /// Completes the wall lock disengagement process
    /// Finalizes transition back to sliding state
    /// </summary>
    private void CompleteWallLockDisengagement()
    {
        isWallLocked = false;
        isWallLockDisengaging = false;
        
        // Restore original wall slide speed
        wallSlideSpeed = originalWallSlideSpeed;

        // Reset acceleration when leaving wall lock
        if (enableWallSlideAcceleration)
        {
            ResetWallSlideAcceleration();
        }
        
        // Restore gravity
        rb.gravityScale = normalGravityScale;
        
        // Give a tiny downward nudge to resume sliding
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -0.1f);
    }

    /// <summary>
    /// Applies movement during wall lock states
    /// Handles smooth transitions between sliding and locked states
    /// </summary>
    private void ApplyWallLockMovement()
    {
        if (!isWallLocked && !isWallLockEngaging && !isWallLockDisengaging) return;
        
        // During engagement: smoothly slow down to stop
        if (isWallLockEngaging)
        {
            float progress = 1f - (wallLockTimer / wallLockEngageTime);
            float currentSpeed = Mathf.Lerp(originalWallSlideSpeed, wallLockSpeed, progress);
            
            // Apply slowing down movement
            rb.linearVelocity = new Vector2(0, -currentSpeed);
        }
        // During locked state: stay completely still
        else if (isWallLocked)
        {
            rb.linearVelocity = Vector2.zero;
            
            // Optional: Apply a tiny force into the wall to prevent drifting
            Vector2 wallPushForce = new Vector2(wallSide * 0.1f, 0);
            rb.AddForce(wallPushForce);
        }
        // During disengagement: smoothly resume sliding
        else if (isWallLockDisengaging)
        {
            float progress = 1f - (wallLockTimer / wallLockDisengageTime);
            float currentSpeed = Mathf.Lerp(wallLockSpeed, originalWallSlideSpeed, progress);
            
            // Apply speeding up movement
            rb.linearVelocity = new Vector2(0, -currentSpeed);
        }
    }

    /// <summary>
    /// Resets all wall lock state variables
    /// Cleans up wall lock state when leaving wall
    /// </summary>
    private void ResetWallLockState()
    {
        isWallLocked = false;
        isWallLockEngaging = false;
        isWallLockDisengaging = false;
        wallLockTimer = 0f;
        
        // Restore original wall slide speed if it was changed
        if (originalWallSlideSpeed > 0)
        {
            wallSlideSpeed = originalWallSlideSpeed;
        }

        // Reset acceleration
        if (enableWallSlideAcceleration)
        {
            currentWallSlideSpeed = 0f;
            wallSlideAccelerationTimer = 0f;
            isAcceleratingWallSlide = false;
        }
    }

    /// <summary>
    /// Handles input that should disengage wall slide
    /// Detects when player is trying to move away from wall
    /// </summary>
    private void HandleWallSlideDisengagement()
    {
        if (!isWallSlideEngaged || !isWallSliding) return;
        
        // Check if player is trying to move away from wall
        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            float inputDirection = Mathf.Sign(moveInput.x);
            
            // If pressing opposite direction from wall, disengage
            if (inputDirection == -wallSide)
            {
                DisengageWallSlide();
                
                // Apply a small force away from wall for responsiveness
                rb.AddForce(new Vector2(inputDirection * 2f, 0), ForceMode2D.Impulse);
            }
        }
        
        // Also disengage if jumping (but not wall jumping - that's handled elsewhere)
        if (jumpAction.triggered && !isWallJumping)
        {
            // Small delay to prevent accidental re-engagement
            wallSlideDisengageTimer = wallSlideDisengageDelay;
        }
    }
    
    /// <summary>
    /// Enhanced wall detection using raycasting
    /// Determines which side the player is touching a wall on
    /// </summary>
    private void CheckWall()
    {
        // Don't check walls if ability not unlocked or in certain states
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

        // Store previous wall touching state
        bool wasTouchingWall = isTouchingWall;
        
        //Check if we should disengage wall slide
        if (isWallSlideEngaged && wallSlideDisengageTimer > 0)
        {
            wallSlideDisengageTimer -= Time.deltaTime;
            
            // If timer runs out and we're not touching wall, disengage
            if (wallSlideDisengageTimer <= 0 && !touchingRightWall && !touchingLeftWall)
            {
                isWallSlideEngaged = false;
                
                // CRITICAL FIX: Use EndWallSlide() instead of direct state changes
                if (isWallSliding)
                {
                    EndWallSlide();
                }
                else
                {
                    wallSlideState = WallSlideState.None;
                }
            }
        }
        
        // Reset wall state
        isTouchingWall = false;
        wallSide = 0;
        
        // Determine which wall is being touched and update state
        if (touchingRightWall)
        {
            // If we're already engaged in wall slide, OR if we're pressing into wall
            if (isWallSlideEngaged || moveInput.x > 0.1f || isWallClinging)
            {
                SetWallTouchingState(1);
            }
            else if (wallClingTimer > 0)
            {
                HandleWallCling(true, false);  // Right wall only
            }
        }
        else if (touchingLeftWall)
        {
            // If we're already engaged in wall slide, OR if we're pressing into wall
            if (isWallSlideEngaged || moveInput.x < -0.1f || isWallClinging)
            {
                SetWallTouchingState(-1);
            }
            else if (wallClingTimer > 0)
            {
                HandleWallCling(false, true);  // Left wall only
            }
        }
        else
        {
            // CRITICAL FIX: When losing wall contact while wall sliding
            if (isWallSliding && wasTouchingWall)
            {
                // Start disengage timer to give a small grace period
                wallSlideDisengageTimer = wallSlideDisengageDelay * 0.3f; // Shorter grace period
            }
            else if (isWallSlideEngaged && wallSlideState == WallSlideState.Sliding)
            {
                wallSlideDisengageTimer = wallSlideDisengageDelay;
            }
        }
        
        // CRITICAL FIX: Additional check for losing wall contact
        if (wasTouchingWall && !isTouchingWall && isWallSliding)
        {
            // If we're falling and not trying to re-engage, end wall slide
            if (rb.linearVelocity.y < 0 && Mathf.Abs(moveInput.x) < 0.1f)
            {
                // Small delay before ending to prevent flickering on uneven walls
                if (wallSlideDisengageTimer <= 0)
                {
                    EndWallSlide();
                }
            }
        }
    }
    
    /// <summary>
    /// Resets all wall interaction state variables
    /// Cleans up wall state when leaving walls
    /// </summary>
    private void ResetWallState()
    {
        isTouchingWall = false;
        wallSide = 0;
        isWallSliding = false;
        isWallClinging = false;
        wallSlideState = WallSlideState.None;
        isWallSlideEngaged = false;
        wallSlideDisengageTimer = 0f; 
    }
    
    /// <summary>
    /// Sets wall touching state for a specific side
    /// Updates wall side and resets relevant timers
    /// </summary>
    private void SetWallTouchingState(int side)
    {
        isTouchingWall = true;
        wallSide = side;
        wallClingTimer = wallClingTime;
        isWallClinging = false;

        //Reset disengage timer when we establish wall contact
        if (isWallSlideEngaged)
        {
            wallSlideDisengageTimer = 0f;
        }
        ResetHardLandingTracking();
    }

    /// <summary>
    /// Resets all hard landing tracking variables
    /// Called when starting wall slide to measure fall from that point
    /// </summary>
    private void ResetHardLandingTracking()
    {
        // Reset fall start height to current position so any fall from wall slide
        // is measured from when we started sliding, not from original jump height
        fallStartHeight = transform.position.y;
        totalFallDistance = 0f;
        peakHeight = transform.position.y;
        fellFromOffScreen = false;
        
        // Also reset any active hard landing
        if (isHardLanding)
        {
            EndHardLanding();
        }
    }
    
    /// <summary>
    /// Handles wall clinging state when switching directions
    /// Allows brief wall hold when changing direction against wall
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

            ResetHardLandingTracking();
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
        currentLedgeHoldTime = 0f;
        
        // Stop movement and gravity
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        
        // Calculate grab position with offsets
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
        
        // Reset other states that conflict with ledge grabbing
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
        
        // Prevent immediate regrabbing with cooldown
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
        
        // Update climb progress
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
            
            // Small hop at the end for smoother transition
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
        coyoteTimeCounter = coyoteTime; // Allow potential double jump
        hasPogoedThisJump = false; 
        CancelCombo();
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
        // Death state (highest priority)
        if (isDead)
        {
            // Keep playing death animation without interruption
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Kalb_death"))
            {
                animator.Play("Kalb_death", -1, 0f);
            }
            return;
        }
        // Respawn state
        else if (isRespawning)
        {
            // Keep playing respawn animation without interruption
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Kalb_respawn"))
            {
                animator.Play("Kalb_respawn", -1, 0f);
            }
            return;
        }
        // Damage state
        else if (isTakingDamage && knockbackTimer > 0)
        {
            // FORCE hurt animation to play completely
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Kalb_hurt"))
            {
                animator.Play("Kalb_hurt", -1, 0f);
                // Lock the animation to prevent interruption
                LockAnimationForDuration(0.3f);
            }
            return;
        }
        // SPECIAL STATES (high priority)
        else if (isLedgeClimbing)
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
        else if (isPogoAttacking && pogoUnlocked)
        {
            HandlePogoAnimations();
        }
        // COMBO ATTACK STATE
        else if (isAttacking && currentCombo > 0)
        {
            HandleComboAnimations();
        }
        // ACTION STATES
        else if (isDashing && dashUnlocked)
        {
            animator.Play("Kalb_dash");
        }
        else if (isWallSliding && wallJumpUnlocked && isTouchingWall)
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
    /// Locks animation for a specific duration to prevent interruption
    /// Used for attacks and damage animations
    /// </summary>
    private void LockAnimationForDuration(float duration)
    {
        isAnimationLocked = true;
        animationLockTimer = duration;
    }
    
    /// <summary>
    /// Handles swimming-specific animations
    /// Different animations for swimming, fast swimming, and dashing
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
    /// Controls idle, walk, and run animations
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
    /// Controls jump and fall animations with speed variation
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
    /// Controls combo progression and animation speeds
    /// </summary>
    private void HandleComboAnimations()
    {
        if (animator == null) return;
        
        // Set animator parameters for combo system
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

    /// <summary>
    /// Handles pogo attack animations
    /// Controls pogo attack and bounce animations
    /// </summary>
    private void HandlePogoAnimations()
    {
        if (isPogoBouncing)
        {
            // Use attack reset animation for bounce (or create specific bounce animation)
            animator.Play("Kalb_attack_reset");
        }
        else
        {
            animator.Play("Kalb_pogo_attack");
        }
    }
    
    // ====================================================================
    // SECTION 14: PUBLIC API & ABILITY MANAGEMENT
    // ====================================================================
    // Public methods for other scripts to interact with the player
    
    /// <summary>
    /// Unlocks the dash ability
    /// </summary>
    public void UnlockDash() => dashUnlocked = true;
    
    /// <summary>
    /// Unlocks the run ability
    /// </summary>
    public void UnlockRun() => runUnlocked = true;
    
    /// <summary>
    /// Unlocks the wall jump ability
    /// </summary>
    public void UnlockWallJump() => wallJumpUnlocked = true;
    
    /// <summary>
    /// Unlocks the double jump ability
    /// </summary>
    public void UnlockDoubleJump() => doubleJumpUnlocked = true;
    
    /// <summary>
    /// Unlocks the pogo attack ability
    /// </summary>
    public void UnlockPogo() => pogoUnlocked = true;
    
    /// <summary>
    /// Unlocks the wall lock ability
    /// </summary>
    public void UnlockWallLock() => wallLockUnlocked = true;
    
    /// <summary>
    /// Unlocks all abilities at once
    /// </summary>
    public void UnlockAllAbilities()
    {
        UnlockDash();
        UnlockRun();
        UnlockWallJump();
        UnlockDoubleJump();
        UnlockPogo();
        UnlockWallLock();
    }
    
    /// <summary>
    /// Resets all abilities to locked state
    /// Useful for game resets or difficulty modes
    /// </summary>
    public void ResetAbilities()
    {
        dashUnlocked = false;
        runUnlocked = false;
        wallJumpUnlocked = false;
        doubleJumpUnlocked = false;
        pogoUnlocked = false;
        wallLockUnlocked = false;
    }

    /// <summary>
    /// Upgrades combo chain to allow more hits
    /// Resizes arrays if needed for new combo length
    /// </summary>
    /// <param name="newMaxCombo">New maximum combo hits</param>
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
    /// <returns>Current combo hit count (0 = no combo)</returns>
    public int GetCurrentCombo() => currentCombo;
    
    /// <summary>
    /// Gets max combo hits
    /// </summary>
    /// <returns>Maximum number of hits in combo chain</returns>
    public int GetMaxCombo() => maxComboHits;
    
    /// <summary>
    /// Checks if player is in combo finisher state
    /// </summary>
    /// <returns>True if performing final combo hit</returns>
    public bool IsComboFinishing() => isComboFinishing;
    
    /// <summary>
    /// Checks if pogo is unlocked
    /// </summary>
    /// <returns>True if pogo attack is available</returns>
    public bool IsPogoUnlocked() => pogoUnlocked;
    
    /// <summary>
    /// Checks if wall lock is currently active
    /// </summary>
    /// <returns>True if player is locked to wall</returns>
    public bool IsWallLocked() => isWallLocked;
    
    /// <summary>
    /// Checks if wall lock is engaging (transitioning to locked state)
    /// </summary>
    /// <returns>True if wall lock is engaging</returns>
    public bool IsWallLockEngaging() => isWallLockEngaging;
    
    /// <summary>
    /// Heals the player by specified amount
    /// </summary>
    /// <param name="amount">Health points to restore</param>
    public void Heal(int amount)
    {
        if (isDead) return;
        
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        
        // Visual feedback for healing could be added here
        // StartCoroutine(HealFlashRoutine());
    }
    
    /// <summary>
    /// Sets player health to specific value
    /// </summary>
    /// <param name="health">New health value</param>
    public void SetHealth(int health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
    }
    
    /// <summary>
    /// Increases max health and restores some health
    /// </summary>
    /// <param name="increaseAmount">Amount to increase max health by</param>
    public void IncreaseMaxHealth(int increaseAmount)
    {
        maxHealth += increaseAmount;
        currentHealth = Mathf.Clamp(currentHealth + increaseAmount, 0, maxHealth);
    }
    
    /// <summary>
    /// DEBUG: Test damage from right side
    /// </summary>
    /// <param name="damage">Amount of damage to test (default 10)</param>
    public void TestTakeDamage(int damage = 10)
    {
        Vector3 testDamageSource = transform.position + Vector3.right * 2f;
        TakeDamage(damage, testDamageSource);
    }
    
    /// <summary>
    /// DEBUG: Test damage from left side
    /// </summary>
    /// <param name="damage">Amount of damage to test (default 10)</param>
    public void TestTakeDamageLeft(int damage = 10)
    {
        Vector3 testDamageSource = transform.position + Vector3.left * 2f;
        TakeDamage(damage, testDamageSource);
    }
    
    /// <summary>
    /// DEBUG: Kill player for testing
    /// </summary>
    public void TestKill()
    {
        TakeDamage(currentHealth, transform.position + Vector3.up * 2f);
    }
    
    /// <summary>
    /// DEBUG: Toggle god mode (invincibility)
    /// </summary>
    public void ToggleGodMode()
    {
        godMode = !godMode;
        
        // Visual feedback for god mode
        if (playerSprite != null)
        {
            if (godMode)
            {
                // Golden tint for god mode
                playerSprite.color = new Color(1f, 0.92f, 0.016f, 0.7f);
            }
            else
            {
                playerSprite.color = originalSpriteColor;
            }
        }
    }

    /// <summary>
    /// Manually disengages wall slide (useful for external systems)
    /// Forces player to leave wall slide state
    /// </summary>
    public void DisengageWallSlide()
    {
        isWallSlideEngaged = false;
        isWallSliding = false;
        isWallClinging = false;
        wallSlideState = WallSlideState.None;
        wallSlideDisengageTimer = 0f;

        // Also reset wall lock
        ResetWallLockState();
        
        // Reset acceleration
        if (enableWallSlideAcceleration)
        {
            ResetWallSlideAcceleration();
        }
    
        // Reset wall contact state
        isTouchingWall = false;
        wallSide = 0;
    }

    /// <summary>
    /// Gets the current wall slide speed (with acceleration applied)
    /// </summary>
    /// <returns>Current wall slide speed</returns>
    public float GetCurrentWallSlideSpeed()
    {
        return enableWallSlideAcceleration ? currentWallSlideSpeed : wallSlideSpeed;
    }
    
    // ====================================================================
    // SECTION 15: EDITOR & DEBUG VISUALIZATION
    // ====================================================================
    
    /// <summary>
    /// Draws gizmos in the Scene view for debugging
    /// Shows detection ranges, raycasts, and state information
    /// Only visible when GameObject is selected in Unity Editor
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Draw basic detection gizmos
        DrawGroundCheckGizmo();
        DrawWallCheckGizmo();
        DrawAttackRangeGizmo();
        
        // Only draw runtime gizmos when playing
        if (!Application.isPlaying || mainCamera == null) return;
        
        // Draw system-specific debug visuals
        DrawFallTrackingGizmos();
        DrawScreenBoundsGizmos();
        DrawSwimmingGizmos();
        DrawLedgeGizmos();
        DrawPogoGizmos();
        DrawWallLockGizmos();
        DrawWallSlideAccelerationGizmos();
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
    /// Shows fall start height and hard landing threshold
    /// </summary>
    private void DrawFallTrackingGizmos()
    {
        // Fall start height line
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            new Vector3(transform.position.x - 1f, fallStartHeight, 0),
            new Vector3(transform.position.x + 1f, fallStartHeight, 0)
        );
        
        // Screen height threshold line
        float requiredDistance = screenHeightInUnits * minScreenHeightForHardLanding;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            new Vector3(transform.position.x - 1f, fallStartHeight - requiredDistance, 0),
            new Vector3(transform.position.x + 1f, fallStartHeight - requiredDistance, 0)
        );
        
        // Current position line
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
        
        // Screen bounds rectangle
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(screenBounds.center, screenBounds.size);
        
        // Off-screen detection area line
        Gizmos.color = Color.magenta;
        Vector3 offScreenTop = new Vector3(screenBounds.center.x, screenBounds.max.y + screenHeightDetectionOffset, 0);
        Gizmos.DrawLine(
            new Vector3(screenBounds.min.x, offScreenTop.y, 0),
            new Vector3(screenBounds.max.x, offScreenTop.y, 0)
        );
    }
    
    /// <summary>
    /// Draws swimming system visualizations
    /// Shows water surface target and floating range
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

        // Floating effect visualization
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
        
        // Combo attack range visualization
        if (isAttacking && currentCombo > 0)
        {
            int comboIndex = (currentCombo - 1) % maxComboHits;
            float range = comboRange[comboIndex];
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, range);
            
            // Draw combo number label
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
                $"Combo: {currentCombo}/{maxComboHits}");
        }
    }
    
    /// <summary>
    /// Draws ledge system visualizations
    /// Shows ledge detection and grab positions
    /// </summary>
    private void DrawLedgeGizmos()
    {
        // Ledge check point
        if (ledgeCheckPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(ledgeCheckPoint.position, 0.1f);
            
            // Detected ledge position
            if (ledgeDetected)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(ledgePosition, new Vector3(0.3f, 0.1f, 0));
            }
        }

        // Ledge grab visualization
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

    /// <summary>
    /// Draws pogo attack detection gizmos
    /// Shows pogo detection range and direction
    /// </summary>
    private void DrawPogoGizmos()
    {
        if (pogoUnlocked)
        {
            // Pogo detection range sphere
            Gizmos.color = Color.magenta;
            Vector2 detectionCenter = transform.position + (Vector3.down * (pogoDetectionRange / 2f));
            Gizmos.DrawWireSphere(detectionCenter, pogoDetectionRange / 2f);
            
            // Pogo direction indicator
            if (isPogoAttacking)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, pogoDirection * pogoDetectionRange);
            }
            
            // Pogo chain indicator label
            if (currentPogoChain > 0)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, 
                    $"Pogo Chain: {currentPogoChain}/{maxPogoChain}");
            }
        }
    }

    /// <summary>
    /// Draws wall lock visualization gizmos
    /// Shows wall lock state with colored indicators
    /// </summary>
    private void DrawWallLockGizmos()
    {
        if (wallLockUnlocked && isWallSliding)
        {
            if (isWallLocked)
            {
                // Green circle when fully locked
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, 0.3f);
                
                // Text label
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, 
                    "WALL LOCKED");
            }
            else if (isWallLockEngaging)
            {
                // Yellow circle when engaging
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, 0.25f);
                
                // Progress indicator
                float progress = 1f - (wallLockTimer / wallLockEngageTime);
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, 
                    $"Locking: {progress:P0}");
            }
            else if (isWallLockDisengaging)
            {
                // Blue circle when disengaging
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, 0.25f);
            }
        }
    }

    /// <summary>
    /// Draws wall slide acceleration visualization gizmos
    /// Shows acceleration progress and current speed
    /// </summary>
    private void DrawWallSlideAccelerationGizmos()
    {
        if (!enableWallSlideAcceleration || !isWallSliding) return;
        
        // Draw acceleration meter bar
        Vector3 startPos = transform.position + new Vector3(-0.5f, 0.8f, 0);
        Vector3 endPos = transform.position + new Vector3(0.5f, 0.8f, 0);
        
        // Background bar
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(startPos, endPos);
        
        // Current speed indicator
        float speedRatio = currentWallSlideSpeed / wallSlideSpeed;
        Vector3 speedPos = Vector3.Lerp(startPos, endPos, speedRatio);
        
        // Color based on acceleration state
        if (isAcceleratingWallSlide)
        {
            Gizmos.color = Color.Lerp(Color.yellow, Color.green, speedRatio);
        }
        else if (wallSlideAccelerationDirection < 0)
        {
            Gizmos.color = Color.blue;
        }
        else
        {
            Gizmos.color = Color.green;
        }
        
        Gizmos.DrawLine(startPos, speedPos);
        
        // Text labels for speed and acceleration
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, 
            $"Slide Speed: {currentWallSlideSpeed:F1}/{wallSlideSpeed:F1}");
        
        if (isAcceleratingWallSlide)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f, 
                $"Accelerating: {wallSlideAccelerationTimer:P0}");
        }
    }
}