using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    // ====================================================================
    // PUBLIC VARIABLES - Inspector Configuration
    // ====================================================================
    
    // -------------------------------------------------
    // [Header] groups organize related settings in Inspector
    // -------------------------------------------------

    [Header("Ability Unlocks")]
    public bool runUnlocked = false;
    public bool dashUnlocked = false;
    public bool wallJumpUnlocked = false;
    public bool doubleJumpUnlocked = false;
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f;                // Base walking speed
    public float runSpeed = 8f;                 // Speed while running
    public float jumpForce = 12f;               // Initial jump velocity
    [Range(0, 0.3f)] public float movementSmoothing = 0.05f; // Movement interpolation smoothness
    
    [Header("Jump Timing")]
    public float coyoteTime = 0.15f;            // Grace period after leaving ground to still jump
    public float jumpBufferTime = 0.1f;         // Input buffer time for jump commands
    public float jumpCutMultiplier = 0.5f;      // Multiplier for early jump release (variable height)
    
    [Header("Double Jump")]
    public bool hasDoubleJump = true;           // Enable/disable double jump ability
    public float doubleJumpForce = 10f;         // Force applied for double jump
    
    [Header("Air Control")]
    public float airControlMultiplier = 0.5f;   // How much control player has in air (0-1)
    public float maxAirSpeed = 10f;             // Maximum horizontal speed while airborne
    public float airAcceleration = 15f;         // How quickly player accelerates in air
    
    [Header("Dash Settings")]
    public float dashSpeed = 20f;               // Speed while dashing
    public float dashDuration = 0.2f;           // How long dash lasts in seconds
    public float dashCooldown = 0.5f;           // Time between dashes
    public bool canAirDash = true;              // Can dash while in the air
    public bool resetAirDashOnGround = true;    // Reset air dash count when landing
    public int maxAirDashes = 1;                // Maximum number of air dashes before landing
    
    [Header("Wall Slide & Jump Settings")]
    public float wallSlideSpeed = 2f;           // Maximum downward speed while wall sliding
    public float wallJumpForce = 11f;           // Force applied when jumping from wall
    public Vector2 wallJumpAngle = new Vector2(1, 2); // Direction vector for wall jumps (x=horizontal, y=vertical)
    public float wallJumpDuration = 0.2f;       // How long wall jump state lasts
    public float wallStickTime = 0.25f;         // Time player sticks to wall before sliding off
    public float wallSlideGravity = 0.5f;       // Gravity multiplier while wall sliding
    
    [Header("Wall Cling Settings")]
    public float wallClingTime = 0.2f;          // Time player can switch direction before falling off wall
    public float wallClingSlowdown = 0.3f;      // Speed reduction multiplier when wall clinging
    
    [Header("Attack Settings")]
    public float attackCooldown = 0.5f;         // Time between attacks
    public float attackDuration = 0.2f;         // How long attack state lasts
    public Transform attackPoint;               // Position where attack hitbox originates
    public float attackRange = 0.5f;            // Radius of attack hitbox
    public LayerMask enemyLayers;               // Which layers enemies are on
    public int attackDamage = 20;               // Damage dealt per attack
    
    [Header("Environment Check")]
    public Transform groundCheck;               // Transform positioned at player's feet for ground detection
    public float groundCheckRadius = 0.2f;      // Radius of ground detection circle
    public Transform wallCheck;                 // Transform positioned at player's side for wall detection
    public float wallCheckDistance = 0.05f;      // How far to check for walls
    public float wallCheckOffset = 0.02f;        // Vertical offset for wall detection rays
    public LayerMask environmentLayer;          // Which layers count as environment (ground/walls)
    
    // ====================================================================
    // PRIVATE VARIABLES - Internal State Management
    // ====================================================================
    
    // -------------------------------------------------
    // Component References
    // -------------------------------------------------
    private Rigidbody2D rb;                     // Physics body component
    private PlayerInput playerInput;            // Input system handler
    private InputAction moveAction;             // Reference to Move input action
    private InputAction jumpAction;             // Reference to Jump input action
    private InputAction dashAction;             // Reference to Dash/Run input action
    private InputAction attackAction;           // Reference to Attack input action
    private Animator animator;                  // Animation controller
    
    // -------------------------------------------------
    // Input & Movement State
    // -------------------------------------------------
    private Vector2 moveInput;                  // Current movement direction from input
    private Vector3 velocity = Vector3.zero;    // Reference velocity for smoothing calculations
    private bool facingRight = true;            // Which direction player is facing (true = right)
    private bool isGrounded;                    // Whether player is touching ground
    private bool isRunning = false;             // Whether run button is held while grounded
    
    // -------------------------------------------------
    // Action State Flags
    // -------------------------------------------------
    private bool isWallSliding;                 // Whether player is sliding down a wall
    private bool isDashing = false;             // Whether player is currently dashing
    private bool isAttacking = false;           // Whether player is currently attacking
    private bool isWallJumping = false;         // Whether player is in wall jump state
    private bool isTouchingWall;                // Whether player is touching a wall
    private bool isWallClinging = false;        // Whether player is clinging to wall (direction switch)
    
    // -------------------------------------------------
    // Timers & Cooldowns
    // -------------------------------------------------
    private float dashTimer = 0f;               // Countdown timer for dash duration
    private float dashCooldownTimer = 0f;       // Countdown timer for dash cooldown
    private float attackTimer = 0f;             // Countdown timer for attack duration
    private float attackCooldownTimer = 0f;     // Countdown timer for attack cooldown
    private float wallJumpTimer = 0f;           // Countdown timer for wall jump state
    private float wallStickTimer = 0f;          // Countdown timer for wall stick grace period
    private float wallClingTimer = 0f;          // Countdown timer for wall cling grace period
    
    // -------------------------------------------------
    // Jump & Air Movement Variables
    // -------------------------------------------------
    private float coyoteTimeCounter = 0f;       // Current coyote time countdown
    private float jumpBufferCounter = 0f;       // Current jump buffer countdown
    private bool isJumpButtonHeld = false;      // Whether jump button is currently held down
    private bool hasDoubleJumped = false;       // Whether player has used double jump this airtime

    // -------------------------------------------------
    // Wall Collision Prevention (when wall jump disabled)
    // -------------------------------------------------
    private bool isAgainstWall = false;            // Whether player is pressing against a wall
    private float wallNormalDistance = 0.05f;      // Minimum distance to maintain from wall
    private bool wasAgainstWallLastFrame = false;  // Track wall state for smooth transitions
    private int lastWallSide = 0;                // Which side wall we last hit (1 = right, -1 = left, 0 = none)
    private float blockedMoveInput = 0f;         // The input value that was blocked due to wall collision
    private bool isInputBlocked = false;         // Whether input is currently being blocked
    private float wallDetectionHeight = 1.0f;  // How tall the wall detection area should be
    private float wallDetectionStep = 0.2f;    // Distance between wall detection rays
    private bool isAgainstWallAnywhere = false; // Whether player is touching wall anywhere on 
    
    // -------------------------------------------------
    // Wall Interaction Variables
    // -------------------------------------------------
    private int wallSide = 0;                   // Which side wall is on (1 = right, -1 = left, 0 = none)
    private enum WallSlideState { None, Starting, Sliding, Jumping } // FSM for wall slide behavior
    private WallSlideState wallSlideState = WallSlideState.None; // Current wall slide state
    
    // -------------------------------------------------
    // Dash Variables
    // -------------------------------------------------
    private Vector2 dashDirection = Vector2.right; // Direction player is dashing in
    private int airDashCount = 0;               // Number of air dashes used since last ground touch
    
    // ====================================================================
    // UNITY LIFE CYCLE METHODS
    // ====================================================================
    
    /// <summary>
    /// Initializes component references and sets up required objects
    /// Called once when the script instance is being loaded
    /// </summary>
    void Start()
    {
        // Get component references
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true; // Prevent unwanted rotation
        
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        dashAction = playerInput.actions["Dash/Run"];
        attackAction = playerInput.actions["Attack"];
        
        animator = GetComponent<Animator>();
        
        // Create missing child objects if not assigned in inspector
        SetupMissingObjects();
    }
    
    /// <summary>
    /// Handles input processing, state updates, and non-physics logic
    /// Called once per frame
    /// </summary>
    void Update()
    {
        // -------------------------------------------------
        // 1. INPUT READING
        // -------------------------------------------------
        moveInput = moveAction.ReadValue<Vector2>(); // Get current movement direction
        
        // Track jump button state for variable jump height
        UpdateJumpButtonState();
        
        // -------------------------------------------------
        // 2. TIMER UPDATES
        // -------------------------------------------------
        UpdateTimers(); // Update all active cooldown and duration timers
        
        // -------------------------------------------------
        // 3. ENVIRONMENT CHECKS
        // -------------------------------------------------
        CheckWall(); // Detect wall collisions and determine wall slide state
        
        // -------------------------------------------------
        // 4. INPUT HANDLING
        // -------------------------------------------------
        HandleDashInput();  // Process dash/run button presses
        HandleRunInput();   // Determine if player is running
        HandleJumpInput();  // Process jump input with buffering
        HandleAttackInput(); // Process attack input
        
        // -------------------------------------------------
        // 5. STATE MANAGEMENT
        // -------------------------------------------------
        HandleWallSlide(); // Apply wall slide physics and state transitions
        
        // -------------------------------------------------
        // 6. VISUAL FEEDBACK
        // -------------------------------------------------
        SetAnimation(moveInput.x); // Update animation based on current state
    }
    
    /// <summary>
    /// Handles physics-based movement and collision checks
    /// Called at fixed time intervals (default 50 times per second)
    /// </summary>
    void FixedUpdate()
    {
        // -------------------------------------------------
        // 1. ENVIRONMENT DETECTION
        // -------------------------------------------------
        UpdateGroundCheck(); // Check if player is grounded

        // -------------------------------------------------
        // 2. WALL COLLISION PREVENTION (always check this)
        // -------------------------------------------------
        PreventWallStick();

        // -------------------------------------------------
        // 3. MOVEMENT EXECUTION
        // -------------------------------------------------
        HandleMovement(); // Apply movement based on current state
        
        // -------------------------------------------------
        // 4. SPRITE ORIENTATION
        // -------------------------------------------------
        // Only flip sprite when not in special states that lock orientation
        if (!isDashing && !isAttacking && !isWallJumping && !isWallSliding)
        {
            HandleFlip();
        }
    }
    
    // ====================================================================
    // INITIALIZATION METHODS
    // ====================================================================
    
    /// <summary>
    /// Creates necessary child objects if they're not assigned in the inspector
    /// Prevents null reference errors by ensuring all required transforms exist
    /// </summary>
    private void SetupMissingObjects()
    {
        // Ground Check setup
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.parent = transform;
            groundCheckObj.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = groundCheckObj.transform;
        }
        
        // Wall Check setup
        if (wallCheck == null)
        {
            GameObject wallCheckObj = new GameObject("WallCheck");
            wallCheckObj.transform.parent = transform;
            wallCheckObj.transform.localPosition = new Vector3(0.5f, 0, 0);
            wallCheck = wallCheckObj.transform;
        }
        
        // Attack Point setup
        if (attackPoint == null)
        {
            GameObject attackPointObj = new GameObject("AttackPoint");
            attackPointObj.transform.parent = transform;
            attackPointObj.transform.localPosition = new Vector3(0.5f, 0, 0);
            attackPoint = attackPointObj.transform;
        }
    }
    
    // ====================================================================
    // INPUT PROCESSING METHODS
    // ====================================================================
    
    /// <summary>
    /// Tracks the jump button state for variable jump height implementation
    /// Called every frame in Update()
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
            // Apply variable jump height: reduce upward velocity if jump released early
            if (rb.linearVelocity.y > 0 && !isDashing && !isWallJumping && !isWallSliding)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }
        }
    }
    
    /// <summary>
    /// Processes dash/run input and manages dash state transitions
    /// </summary>
    private void HandleDashInput()
    {
         // Check if dash is unlocked
        if (!dashUnlocked) return;

        // Check for dash input (only when not already dashing and cooldown is ready)
        if (dashAction.triggered && !isDashing && dashCooldownTimer <= 0 && !isAttacking && !isWallSliding)
        {
            // Check if we can dash based on current state
            bool canDash = isGrounded || isWallSliding;
            
            // Check for air dash availability
            if (!isGrounded && !isWallSliding && canAirDash)
            {
                canDash = airDashCount < maxAirDashes;
            }
            
            if (canDash)
            {
                StartDash();
                
                // Track air dashes separately from ground dashes
                if (!isGrounded && !isWallSliding)
                {
                    airDashCount++;
                }
            }
        }
    }
    
    /// <summary>
    /// Determines if player is running based on dash/run button hold state
    /// Running is only possible while grounded
    /// </summary>
    private void HandleRunInput()
    {
        // Check if run is unlocked
        if (!runUnlocked)
        {
            isRunning = false;
            return;
        }
        // Check if dash/run button is being held for running (only when grounded)
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
    /// Handles normal jumps, double jumps, and wall jumps
    /// </summary>
    private void HandleJumpInput()
    {
        // Store jump input in buffer when button is pressed
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
                hasDoubleJumped = false; // Reset double jump on wall jump
            }
            // Priority 2: Double jump (only if coyote time expired)
            else if (!isGrounded && coyoteTimeCounter <= 0 && doubleJumpUnlocked && hasDoubleJump && !hasDoubleJumped && !isDashing && !isAttacking)
            {
                DoubleJump();
                jumpBufferCounter = 0;
            }
            // Priority 3: Normal jump (with coyote time)
            else if (coyoteTimeCounter > 0)
            {
                NormalJump();
                jumpBufferCounter = 0;
                coyoteTimeCounter = 0; // Consume coyote time
            }
        }
    }
    
    /// <summary>
    /// Processes attack input and manages attack state
    /// </summary>
    private void HandleAttackInput()
    {
        // Check for attack input
        if (attackAction.triggered && !isAttacking && attackCooldownTimer <= 0 && !isDashing)
        {
            StartAttack();
        }
    }
    
    // ====================================================================
    // MOVEMENT & PHYSICS METHODS
    // ====================================================================
    
    /// <summary>
    /// Main movement handler that applies different movement logic based on current state
    /// Called every physics frame in FixedUpdate()
    /// </summary>
    private void HandleMovement()
    {
        // Calculate current speed based on state
        float currentSpeed = moveSpeed;
        
        if (isRunning && isGrounded)
        {
            currentSpeed = runSpeed;
        }
        
        // -------------------------------------------------
        // STATE-BASED MOVEMENT HANDLING
        // -------------------------------------------------
        
        // DASHING STATE
        if (isDashing)
        {
            rb.linearVelocity = dashDirection * dashSpeed;
            rb.gravityScale = 0;
        }
        // WALL JUMPING STATE
        else if (isWallJumping)
        {
            float controlForce = 5f;
            rb.AddForce(new Vector2(moveInput.x * controlForce, 0));
            
            float maxWallJumpSpeed = 10f;
            if (Mathf.Abs(rb.linearVelocity.x) > maxWallJumpSpeed)
            {
                rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * maxWallJumpSpeed, rb.linearVelocity.y);
            }
            
            rb.gravityScale = 2;
        }
        // ATTACKING STATE
        else if (isAttacking)
        {
            Vector2 targetVelocity = new Vector2(0, rb.linearVelocity.y);
            rb.linearVelocity = Vector3.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocity, movementSmoothing);
            rb.gravityScale = 2;
        }
        // WALL SLIDING STATE
        else if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            rb.gravityScale = wallSlideGravity;
            
            if (rb.linearVelocity.y > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            }
        }
        // NORMAL MOVEMENT STATE (default)
        else
        {
            // Standard movement with full gravity
            rb.gravityScale = 2;
            
            // Get the current movement input
            float currentMoveInput = moveInput.x;
            
            // Apply wall collision prevention
            if (isAgainstWall && !isWallSliding)
            {
                // If we're against a wall and trying to move into it, block input
                if (Mathf.Sign(currentMoveInput) == lastWallSide)
                {
                    currentMoveInput = 0;
                }
            }
            
            // Calculate target velocity based on input
            Vector2 targetVelocity = new Vector2(currentMoveInput * currentSpeed, rb.linearVelocity.y);
            
            // Apply air control if not grounded
            if (!isGrounded)
            {
                ApplyAirControl();
            }
            
            // Apply wall stick logic only when wall jump is unlocked AND we're wall sliding
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
            
            // Smoothly interpolate to target velocity
            rb.linearVelocity = Vector3.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocity, movementSmoothing);
            
            // Final safety: If we're moving into a wall (not sliding), reduce velocity
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
    /// Applies air control physics for smoother aerial movement
    /// Reduces control in air compared to ground movement
    /// </summary>
    private void ApplyAirControl()
    {
        // Don't apply air control during these states
        if (isGrounded || isWallSliding || isDashing) return;
        
        // Calculate desired velocity with air control multiplier
        float targetXVelocity = moveInput.x * moveSpeed * airControlMultiplier;
        float velocityDifference = targetXVelocity - rb.linearVelocity.x;
        
        // Apply acceleration force proportional to velocity difference
        rb.AddForce(Vector2.right * velocityDifference * airAcceleration);
        
        // Clamp maximum air speed to prevent excessive velocity
        if (Mathf.Abs(rb.linearVelocity.x) > maxAirSpeed)
        {
            rb.linearVelocity = new Vector2(
                Mathf.Sign(rb.linearVelocity.x) * maxAirSpeed,
                rb.linearVelocity.y
            );
        }
    }
    
    /// <summary>
    /// Flips the player sprite to face movement direction
    /// Only called when not in special states that lock orientation
    /// </summary>
    private void HandleFlip()
    {
        // Don't flip if there's no significant horizontal input
        if (Mathf.Abs(moveInput.x) < 0.1f)
            return;
            
        // Flip character to face movement direction
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
    /// Executes a normal jump from ground
    /// </summary>
    private void NormalJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }
    
    /// <summary>
    /// Executes a double jump while airborne
    /// </summary>
    private void DoubleJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, doubleJumpForce);
        hasDoubleJumped = true;
    }
    
    /// <summary>
    /// Executes a wall jump from wall slide state
    /// Applies angled force away from wall
    /// </summary>
    private void WallJump()
    {
        isWallJumping = true;
        isWallSliding = false;
        isWallClinging = false;
        wallSlideState = WallSlideState.Jumping;
        wallClingTimer = 0f; // Reset cling timer
        wallJumpTimer = wallJumpDuration;
        
        // Clear existing velocity and apply wall jump force
        rb.linearVelocity = Vector2.zero;
        Vector2 jumpDir = new Vector2(-wallSide * wallJumpAngle.x, wallJumpAngle.y).normalized;
        rb.AddForce(jumpDir * wallJumpForce, ForceMode2D.Impulse);
        
        // Flip player to face away from wall for better visual feedback
        if (wallSide == 1 && !facingRight)
        {
            Flip();
        }
        else if (wallSide == -1 && facingRight)
        {
            Flip();
        }
        
        // Reset air dash count - wall jump counts as regaining control
        airDashCount = 0;
    }
    
    /// <summary>
    /// Starts dash movement and sets up dash state
    /// </summary>
    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        
        // Set dash direction based on facing direction by default
        dashDirection = facingRight ? Vector2.right : Vector2.left;
        
        // If in air and moving, dash in movement direction for better control
        if (!isGrounded && Mathf.Abs(moveInput.x) > 0.1f && !isWallSliding)
        {
            dashDirection = new Vector2(Mathf.Sign(moveInput.x), 0);
        }
    }
    
    /// <summary>
    /// Ends dash movement and restores normal physics
    /// </summary>
    private void EndDash()
    {
        isDashing = false;
        
        // Restore normal gravity
        rb.gravityScale = 2;
        
        // Optional: Reset vertical velocity to prevent flying after dash
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }
    }
    
    /// <summary>
    /// Starts attack animation and hit detection
    /// </summary>
    private void StartAttack()
    {
        isAttacking = true;
        attackTimer = attackDuration;
        attackCooldownTimer = attackCooldown;
        
        // Detect enemies in range using circle overlap
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        
        // TO DO: Implement enemy damage system
        /*foreach (Collider2D enemy in hitEnemies)
        {
            Enemy enemyScript = enemy.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                enemyScript.TakeDamage(attackDamage);
            }
        }*/
    }
    
    /// <summary>
    /// Ends attack state
    /// </summary>
    private void EndAttack()
    {
        isAttacking = false;
    }
    
    // ====================================================================
    // WALL INTERACTION METHODS
    // ====================================================================

    /// <summary>
    /// Prevents player from getting stuck on walls when not wall sliding
    /// Works for both wall jump enabled and disabled states
    /// </summary>
    private void PreventWallStick()
    {
        // Reset if grounded or not trying to move
        if (isGrounded || Mathf.Abs(moveInput.x) < 0.1f)
        {
            isAgainstWall = false;
            lastWallSide = 0;
            return;
        }
        
        // If wall jump is enabled AND we're actively wall sliding, don't prevent stick
        if (wallJumpUnlocked && (isWallSliding || isWallClinging))
        {
            return; // Allow normal wall slide behavior
        }
        
        // Enhanced wall collision detection
        float direction = Mathf.Sign(moveInput.x);
        Vector2 origin = transform.position;
        
        // Cast multiple rays for better coverage
        float rayLength = 0.6f;
        float playerHalfWidth = 0.5f;
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
                
                // If we're too close to the wall, push away slightly
                if (hit.distance < wallNormalDistance)
                {
                    Vector2 pushBack = new Vector2(-direction * (wallNormalDistance - hit.distance), 0);
                    transform.position += (Vector3)pushBack * 0.5f; // More gentle push
                }
            }
        }
        
        // Handle wall collision
        if (hitWall && Mathf.Sign(moveInput.x) == direction)
        {
            // Check if this is the same wall we were hitting last frame
            if (lastWallSide != direction)
            {
                lastWallSide = (int)direction;
                isAgainstWall = true;
            }
            
            // Block movement into the wall
            if (Mathf.Sign(moveInput.x) == lastWallSide)
            {
                // Only block if we're not actively wall sliding
                if (!isWallSliding)
                {
                    // Reduce input force against wall
                    moveInput = new Vector2(0, moveInput.y);
                    
                    // Also reduce existing velocity against wall
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
            
            // Reset wall side tracking
            if (Mathf.Abs(moveInput.x) < 0.1f || Mathf.Sign(moveInput.x) != lastWallSide)
            {
                lastWallSide = 0;
            }
        }
    }

    /// <summary>
    /// Checks if player is colliding with a wall anywhere on their body
    /// Uses box cast for more reliable detection
    /// </summary>
    private bool IsTouchingWallAnywhere(float direction)
    {
        // Create a box that represents the player's collider
        Vector2 boxSize = new Vector2(0.1f, 1.0f); // Thin box for detection
        Vector2 origin = (Vector2)transform.position + new Vector2(direction * 0.3f, 0);
        float distance = 0.1f;
        
        RaycastHit2D hit = Physics2D.BoxCast(
            origin,
            boxSize,
            0f,
            new Vector2(direction, 0),
            distance,
            environmentLayer
        );
        
        // Debug visualization
        Debug.DrawLine(origin, origin + new Vector2(direction * distance, 0), 
            hit.collider != null ? Color.magenta : Color.cyan);
        
        return hit.collider != null;
    }
    
    /// <summary>
    /// Handles wall slide physics and state transitions
    /// Called every frame to update wall slide behavior
    /// </summary>
    private void HandleWallSlide()
    {
        // Check if wall jump is unlocked
        if (!wallJumpUnlocked) 
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
        
        // Check if we should be wall sliding OR wall clinging
        if (isTouchingWall)
        {
            // State machine for wall slide transitions
            if (wallSlideState == WallSlideState.None)
            {
                // First frame of wall slide - stop upward momentum
                wallSlideState = WallSlideState.Starting;
                if (rb.linearVelocity.y > 0)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                }
            }
            else if (wallSlideState == WallSlideState.Starting)
            {
                // Second frame onward - normal wall slide
                wallSlideState = WallSlideState.Sliding;
            }
            
            isWallSliding = true;
            
            // Apply different physics based on whether we're sliding or clinging
            float currentSlideSpeed = rb.linearVelocity.y;
            
            if (isWallClinging)
            {
                // WALL CLING: Slow descent when switching directions
                float clingSpeed = -wallSlideSpeed * wallClingSlowdown;
                
                if (currentSlideSpeed < clingSpeed)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, clingSpeed);
                }
                
                // Reset air dash when wall clinging too
                if (resetAirDashOnGround)
                {
                    airDashCount = 0;
                }
            }
            else
            {
                // NORMAL WALL SLIDE
                // If jump button is held, apply less slide speed (allows "clinging" effect)
                if (isJumpButtonHeld && currentSlideSpeed < 0)
                {
                    // Reduce slide speed when holding jump (simulates clinging)
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentSlideSpeed * 0.7f);
                }
                else if (currentSlideSpeed < -wallSlideSpeed)
                {
                    // Clamp to maximum slide speed
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
                }
                
                // Reset air dash when wall sliding
                if (resetAirDashOnGround)
                {
                    airDashCount = 0;
                }
            }
            
            // Snap player closer to wall for better visual alignment
            SnapToWall();
        }
        else
        {
            // Reset wall slide state when not touching wall
            wallSlideState = WallSlideState.None;
            isWallSliding = false;
            isWallClinging = false;
        }
    }
    
    /// <summary>
    /// Enhanced wall detection that checks multiple points along the player's height
    /// </summary>
    private void CheckWall()
    {
        // If wall jump is not unlocked, use simple wall collision prevention instead
        if (!wallJumpUnlocked)
        {
            // Use the enhanced wall detection for collision prevention
            isTouchingWall = false;
            wallSide = 0;
            isWallSliding = false;
            isWallClinging = false;
            wallSlideState = WallSlideState.None;
            return;
        }
        
        // Enhanced wall detection with multiple rays
        Vector2 wallCheckPos = wallCheck.position;
        Vector2 offset = new Vector2(wallCheckOffset, 0);
        
        // Track if we're touching any wall for general collision prevention
        isAgainstWallAnywhere = false;
        int detectedWallSide = 0;
        
        // Cast multiple rays at different heights
        int rayCount = Mathf.FloorToInt(wallDetectionHeight / wallDetectionStep) + 1;
        int rightHits = 0;
        int leftHits = 0;
        
        for (int i = 0; i < rayCount; i++)
        {
            float heightOffset = (i - rayCount / 2) * wallDetectionStep;
            Vector2 heightVector = new Vector2(0, heightOffset);
            
            // Check right side
            RaycastHit2D rightHit = Physics2D.Raycast(
                wallCheckPos + heightVector + offset, 
                Vector2.right, 
                wallCheckDistance, 
                environmentLayer
            );
            
            // Check left side
            RaycastHit2D leftHit = Physics2D.Raycast(
                wallCheckPos + heightVector - offset, 
                Vector2.left, 
                wallCheckDistance, 
                environmentLayer
            );
            
            // Debug visualization
            Debug.DrawRay(wallCheckPos + heightVector + offset, 
                        Vector2.right * wallCheckDistance, 
                        rightHit.collider != null ? Color.red : Color.green);
            Debug.DrawRay(wallCheckPos + heightVector - offset, 
                        Vector2.left * wallCheckDistance, 
                        leftHit.collider != null ? Color.red : Color.green);
            
            if (rightHit.collider != null)
            {
                rightHits++;
                isAgainstWallAnywhere = true;
                if (detectedWallSide == 0) detectedWallSide = 1;
            }
            
            if (leftHit.collider != null)
            {
                leftHits++;
                isAgainstWallAnywhere = true;
                if (detectedWallSide == 0) detectedWallSide = -1;
            }
        }
        
        // For wall sliding logic, only consider walls that are near the middle of the player
        bool touchingRightWall = false;
        bool touchingLeftWall = false;
        
        // Check middle rays specifically for wall slide eligibility
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
        
        // Original wall slide logic (only uses middle rays)
        isTouchingWall = false;
        wallSide = 0;
        
        // WALL SLIDE LOGIC: Only slide when actively pressing toward the wall
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
        // WALL CLING: Check if we should cling when switching directions
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
    /// Adjusts player position to maintain consistent distance from wall during slide
    /// Creates a "sticky" feeling when wall sliding
    /// </summary>
    private void SnapToWall()
    {
        // Determine which direction the wall is on
        Vector2 snapDirection = wallSide == 1 ? Vector2.right : Vector2.left;
        
        // Cast ray to find exact wall distance
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position, 
            -snapDirection, 
            wallCheckDistance * 1.5f, 
            environmentLayer
        );
        
        if (hit.collider != null)
        {
            // Calculate distance to wall
            float distanceToWall = hit.distance;
            float desiredDistance = wallCheckDistance * 0.8f; // Keep some distance from wall
            
            // If too far from wall, move slightly closer
            if (distanceToWall > desiredDistance)
            {
                Vector2 newPosition = transform.position + (Vector3)(-snapDirection * (distanceToWall - desiredDistance) * 0.1f);
                rb.MovePosition(newPosition);
            }
        }
    }
    
    // ====================================================================
    // UTILITY METHODS
    // ====================================================================
    
    /// <summary>
    /// Updates all active timers and cooldowns
    /// Called every frame to manage action states
    /// </summary>
    private void UpdateTimers()
    {
        // -------------------------------------------------
        // DASH TIMERS
        // -------------------------------------------------
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
        
        // -------------------------------------------------
        // ATTACK TIMERS
        // -------------------------------------------------
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
        
        // -------------------------------------------------
        // WALL JUMP TIMER
        // -------------------------------------------------
        if (isWallJumping)
        {
            wallJumpTimer -= Time.deltaTime;
            if (wallJumpTimer <= 0)
            {
                isWallJumping = false;
            }
        }
        
        // -------------------------------------------------
        // WALL STICK TIMER
        // -------------------------------------------------
        if (isTouchingWall && !isGrounded)
        {
            wallStickTimer -= Time.deltaTime;
        }
        else
        {
            wallStickTimer = wallStickTime;
        }
        
        // -------------------------------------------------
        // WALL CLING RESET
        // -------------------------------------------------
        if (isGrounded)
        {
            wallClingTimer = 0f;
            isWallClinging = false;
        }
        
        // -------------------------------------------------
        // COYOTE TIME: Grace period after leaving ground
        // -------------------------------------------------
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            hasDoubleJumped = false; // Reset double jump when grounded
        }
        else if (coyoteTimeCounter > 0)
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
        
        // -------------------------------------------------
        // JUMP BUFFER: Input buffer for jump commands
        // -------------------------------------------------
        if (jumpBufferCounter > 0)
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }
    
    /// <summary>
    /// Checks if player is grounded using circle overlap detection
    /// Called every physics frame
    /// </summary>
    private void UpdateGroundCheck()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, environmentLayer);
        
        // Reset air dash count when grounded (if enabled)
        if (isGrounded && resetAirDashOnGround)
        {
            airDashCount = 0;
            hasDoubleJumped = false; // Reset double jump flag
        }
        
        // Reset wall sliding when grounded (can't wall slide on ground)
        if (isGrounded)
        {
            isWallSliding = false;
            isTouchingWall = false;
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
    /// Updates attack point position based on facing direction
    /// Ensures attack hitbox is always in front of player
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
    /// Controls animation states based on player movement and action states
    /// </summary>
    /// <param name="horizontalInput">Current horizontal movement input</param>
    private void SetAnimation(float horizontalInput)
    {
        // Animation priority: Special actions > Ground states > Air states
        
        if (isAttacking)
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
            // Ground animations
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
            // Air animations
            if (rb.linearVelocity.y > 0)
            {
                animator.Play("Player_jump");
            }
            else
            {
                animator.Play("Player_fall");
            }
        }
    }
    
    // ====================================================================
    // EDITOR VISUALIZATION
    // ====================================================================
    
    /// <summary>
    /// Draws debug gizmos in editor for visualization of detection areas
    /// Helps with level design and debugging
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Ground check visualization
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        
        // Wall check visualization
        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            // Show both directions with offset
            Vector2 offset = new Vector2(wallCheckOffset, 0);
            Gizmos.DrawLine(wallCheck.position + (Vector3)offset, 
                          wallCheck.position + (Vector3)offset + Vector3.right * wallCheckDistance);
            Gizmos.DrawLine(wallCheck.position - (Vector3)offset, 
                          wallCheck.position - (Vector3)offset + Vector3.left * wallCheckDistance);
        }
        
        // Attack range visualization
        if (attackPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }

    // ====================================================================
    // ABILITY UNLOCK METHODS
    // ====================================================================

    /// <summary>
    /// Unlocks the dash ability
    /// </summary>
    public void UnlockDash()
    {
        dashUnlocked = true;
        Debug.Log("Dash unlocked!");
    }

    /// <summary>
    /// Unlocks the run ability
    /// </summary>
    public void UnlockRun()
    {
        runUnlocked = true;
        Debug.Log("Run unlocked!");
    }

    /// <summary>
    /// Unlocks the wall jump ability
    /// </summary>
    public void UnlockWallJump()
    {
        wallJumpUnlocked = true;
        Debug.Log("Wall jump unlocked!");
    }

    /// <summary>
    /// Unlocks the double jump ability
    /// </summary>
    public void UnlockDoubleJump()
    {
        doubleJumpUnlocked = true;
        Debug.Log("Double jump unlocked!");
    }

    /// <summary>
    /// Unlocks all abilities (for testing)
    /// </summary>
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
    
}