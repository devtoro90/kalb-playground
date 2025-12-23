using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.5f;
    public float jumpForce = 13f;
    [Range(0, 0.3f)] public float movementSmoothing = 0.05f;
    
    [Header("Environment Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public Transform wallCheck;
    public float wallCheckDistance = 0.5f;
    public float wallCheckOffset = 0.2f; // Offset from center to detect walls better
    public LayerMask environmentLayer;
    
    [Header("Wall Slide & Jump Settings")]
    public float wallSlideSpeed = 2f;
    public float wallJumpForce = 13f;
    public Vector2 wallJumpAngle = new Vector2(1, 2);
    public float wallJumpDuration = 0.2f;
    public float wallStickTime = 0.25f;
    public float wallSlideGravity = 0.5f; // Reduced gravity while sliding

    [Header("Wall Cling Settings")]
    public float wallClingTime = 0.2f; // Time you can switch direction before falling
    public float wallClingSlowdown = 0.3f; // How much to slow down when clinging
    
    [Header("Dash Settings")]
    public bool canAirDash = true;
    public bool resetAirDashOnGround = true;
    public int maxAirDashes = 1;
    
    [Header("Attack Settings")]
    public float attackCooldown = 0.5f;
    public float attackDuration = 0.2f;
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;
    public int attackDamage = 20;

    [Header("Jump Timing")]
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.1f;
    public float jumpCutMultiplier = 0.5f;

    [Header("Double Jump")]
    public bool hasDoubleJump = true;
    public float doubleJumpForce = 12f;
    
    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction attackAction;
    private Vector2 moveInput;
    private Vector3 velocity = Vector3.zero;
    private bool facingRight = true;
    private bool isGrounded;
    private bool isWallSliding;
    private bool jumpPressed;
    private Animator animator;
    
    // Dash variables
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector2 dashDirection = Vector2.right;
    private int airDashCount = 0;
    
    // Run variables
    private bool isRunning = false;
    
    // Attack variables
    private bool isAttacking = false;
    private float attackTimer = 0f;
    private float attackCooldownTimer = 0f;
    
    // Wall variables
    private bool isWallJumping = false;
    private float wallJumpTimer = 0f;
    private float wallStickTimer = 0f;
    private bool isTouchingWall;
    private int wallSide = 0; // 1 = right, -1 = left, 0 = no wall
    private bool isJumpButtonHeld = false; // Track if jump button is held
    private float wallClingTimer = 0f;
    private bool isWallClinging = false;

    private enum WallSlideState { None, Starting, Sliding, Jumping }
    private WallSlideState wallSlideState = WallSlideState.None;
    private float coyoteTimeCounter = 0f;
    private float jumpBufferCounter = 0f;
    private bool hasDoubleJumped = false;

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
        
        // Set up ground check if not assigned
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.parent = transform;
            groundCheckObj.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = groundCheckObj.transform;
        }
        
        // Set up wall check if not assigned
        if (wallCheck == null)
        {
            GameObject wallCheckObj = new GameObject("WallCheck");
            wallCheckObj.transform.parent = transform;
            wallCheckObj.transform.localPosition = new Vector3(0.5f, 0, 0);
            wallCheck = wallCheckObj.transform;
        }
        
        // Set up attack point if not assigned
        if (attackPoint == null)
        {
            GameObject attackPointObj = new GameObject("AttackPoint");
            attackPointObj.transform.parent = transform;
            attackPointObj.transform.localPosition = new Vector3(0.5f, 0, 0);
            attackPoint = attackPointObj.transform;
        }
    }

    void Update()
    {
        // Read input in Update
        moveInput = moveAction.ReadValue<Vector2>();
        
        // Track jump button state
        if (jumpAction.IsPressed())
        {
            isJumpButtonHeld = true;
        }
        else if (jumpAction.WasReleasedThisFrame())
        {
            isJumpButtonHeld = false;
            if (rb.linearVelocity.y > 0 && !isDashing && !isWallJumping && !isWallSliding)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }
        }
        
        // Update timers
        UpdateTimers();
        
        // Check wall status
        CheckWall();
        
        // Check for dash input
        HandleDashInput();
        
        // Check for run input
        HandleRunInput();
        
        // Check for jump input
        HandleJumpInput();
        
        // Check for attack input
        HandleAttackInput();
        
        // Handle wall sliding
        HandleWallSlide();
        
        // Update animations
        SetAnimation(moveInput.x);
    }

    void FixedUpdate()
    {
        // Check if player is grounded
        UpdateGroundCheck();
        
        // Handle movement based on state
        HandleMovement();
        
        // Handle flipping (but not during certain states)
        if (!isDashing && !isAttacking && !isWallJumping && !isWallSliding)
        {
            HandleFlip();
        }
    }

    private void UpdateTimers()
    {
        // Update dash timers
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
        
        // Update attack timers
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
        
        // Update wall jump timer
        if (isWallJumping)
        {
            wallJumpTimer -= Time.deltaTime;
            if (wallJumpTimer <= 0)
            {
                isWallJumping = false;
            }
        }
        
        // Update wall stick timer
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

        // COYOTE TIME: Update after existing timers
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            hasDoubleJumped = false;
        }
        else if (coyoteTimeCounter > 0)
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
        
        // JUMP BUFFER: Update after coyote time
        if (jumpBufferCounter > 0)
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void CheckWall()
    {
        // Check both directions for walls with offset for better detection
        Vector2 wallCheckPos = wallCheck.position;
        Vector2 offset = new Vector2(wallCheckOffset, 0);
        
        RaycastHit2D rightHit = Physics2D.Raycast(wallCheckPos + offset, Vector2.right, 
            wallCheckDistance, environmentLayer);
        RaycastHit2D leftHit = Physics2D.Raycast(wallCheckPos - offset, Vector2.left, 
            wallCheckDistance, environmentLayer);
        
        // Check if we're touching walls in either direction
        bool touchingRightWall = rightHit.collider != null && !isGrounded;
        bool touchingLeftWall = leftHit.collider != null && !isGrounded;
        
        // Reset wall detection
        isTouchingWall = false;
        wallSide = 0;
        
        // WALL SLIDE LOGIC: Only slide when actively pressing toward the wall
        if (touchingRightWall && moveInput.x > 0.1f)
        {
            // Touching right wall AND pressing right = wall slide right
            isTouchingWall = true;
            wallSide = 1;
            wallClingTimer = wallClingTime; // Reset cling timer when sliding properly
            isWallClinging = false;
        }
        else if (touchingLeftWall && moveInput.x < -0.1f)
        {
            // Touching left wall AND pressing left = wall slide left
            isTouchingWall = true;
            wallSide = -1;
            wallClingTimer = wallClingTime; // Reset cling timer when sliding properly
            isWallClinging = false;
        }
        // WALL CLING: Check if we should cling when switching directions
        else if ((touchingRightWall || touchingLeftWall) && wallClingTimer > 0)
        {
            // Determine which wall we're on for clinging
            if (touchingRightWall)
            {
                wallSide = 1;
            }
            else if (touchingLeftWall)
            {
                wallSide = -1;
            }
            
            // Check if we're switching direction (pressing away from wall)
            bool switchingDirection = (wallSide == 1 && moveInput.x < -0.1f) || 
                                    (wallSide == -1 && moveInput.x > 0.1f);
            
            if (switchingDirection)
            {
                isWallClinging = true;
                isTouchingWall = true; // Allow wall slide during cling time
            }
        }
    }

    private void HandleWallSlide()
    {
        // Update wall cling timer
        if (wallClingTimer > 0)
        {
            wallClingTimer -= Time.deltaTime;
        }
        
        // Check if we should be wall sliding OR wall clinging
        if (isTouchingWall)
        {
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
            wallSlideState = WallSlideState.None;
            isWallSliding = false;
            isWallClinging = false;
        }
    }

    private void SnapToWall()
    {
        // Snap player closer to the wall for better visual alignment
        Vector2 snapDirection = wallSide == 1 ? Vector2.right : Vector2.left;
        //float snapDistance = 0.05f; // Small snap distance
        
        // Move player slightly toward the wall
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
            float desiredDistance = wallCheckDistance * 0.8f; // Keep some distance
            
            if (distanceToWall > desiredDistance)
            {
                // Move slightly closer to wall
                Vector2 newPosition = transform.position + (Vector3)(-snapDirection * (distanceToWall - desiredDistance) * 0.1f);
                rb.MovePosition(newPosition);
            }
        }
    }

    private void HandleDashInput()
    {
        // Check for dash input (only when not already dashing and cooldown is ready)
        if (dashAction.triggered && !isDashing && dashCooldownTimer <= 0 && !isAttacking && !isWallSliding)
        {
            // Check if we can dash
            bool canDash = isGrounded || isWallSliding;
            
            if (!isGrounded && !isWallSliding && canAirDash)
            {
                canDash = airDashCount < maxAirDashes;
            }
            
            if (canDash)
            {
                StartDash();
                
                // Track air dashes
                if (!isGrounded && !isWallSliding)
                {
                    airDashCount++;
                }
            }
        }
    }

    private void HandleRunInput()
    {
        // Check if E is being held for running (only when grounded)
        if (dashAction.IsPressed() && isGrounded && !isDashing && !isAttacking && !isWallSliding)
        {
            isRunning = true;
        }
        else
        {
            isRunning = false;
        }
    }

    private void HandleJumpInput()
    {
        // Store jump input in buffer
        if (jumpAction.triggered)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        
        // Process buffered jump
        if (jumpBufferCounter > 0)
        {
            // Wall jump
            if (isWallSliding)
            {
                WallJump();
                jumpBufferCounter = 0;
                hasDoubleJumped = false; // Reset on wall jump
            }
            // Double jump (only if coyote time expired)
            else if (!isGrounded && coyoteTimeCounter <= 0 && hasDoubleJump && !hasDoubleJumped && !isDashing && !isAttacking)
            {
                DoubleJump();
                jumpBufferCounter = 0;
            }
            // Normal jump (with coyote time)
            else if (coyoteTimeCounter > 0)
            {
                NormalJump();
                jumpBufferCounter = 0;
                coyoteTimeCounter = 0;
            }
        }
    }

    private void NormalJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    private void DoubleJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, doubleJumpForce);
        hasDoubleJumped = true;
    }

    private void WallJump()
    {
        isWallJumping = true;
        isWallSliding = false;
        isWallClinging = false;
        wallSlideState = WallSlideState.Jumping;
        wallClingTimer = 0f; // Reset cling timer
        wallJumpTimer = wallJumpDuration;
        
        // Clear velocity and apply jump
        rb.linearVelocity = Vector2.zero;
        Vector2 jumpDir = new Vector2(-wallSide * wallJumpAngle.x, wallJumpAngle.y).normalized;
        rb.AddForce(jumpDir * wallJumpForce, ForceMode2D.Impulse);
        
        // Flip player to face away from wall
        if (wallSide == 1 && !facingRight)
        {
            Flip();
        }
        else if (wallSide == -1 && facingRight)
        {
            Flip();
        }
        
        // Reset air dash count
        airDashCount = 0;
    }

    private void HandleAttackInput()
    {
        // Check for attack input
        if (attackAction.triggered && !isAttacking && attackCooldownTimer <= 0 && !isDashing)
        {
            StartAttack();
        }
    }

    private void UpdateGroundCheck()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, environmentLayer);
        
        // Reset air dash count when grounded
        if (isGrounded && resetAirDashOnGround)
        {
            airDashCount = 0;
            hasDoubleJumped = false; 
        }
        
        // Reset wall sliding when grounded
        if (isGrounded)
        {
            isWallSliding = false;
            isTouchingWall = false;
        }
    }

    private void HandleMovement()
    {
        // Calculate current speed based on state
        float currentSpeed = moveSpeed;
        
        if (isRunning && isGrounded)
        {
            currentSpeed = runSpeed;
        }
        
        if (isDashing)
        {
            // During dash, maintain dash velocity
            rb.linearVelocity = dashDirection * dashSpeed;
            
            // Cancel gravity during dash
            rb.gravityScale = 0;
        }
        else if (isWallJumping)
        {
            // During wall jump, allow limited control
            float controlForce = 5f;
            rb.AddForce(new Vector2(moveInput.x * controlForce, 0));
            
            // Clamp horizontal speed during wall jump
            float maxWallJumpSpeed = 10f;
            if (Mathf.Abs(rb.linearVelocity.x) > maxWallJumpSpeed)
            {
                rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * maxWallJumpSpeed, rb.linearVelocity.y);
            }
            
            // IMPORTANT: Allow gravity during wall jump so player can go up
            rb.gravityScale = 2;
        }
        else if (isAttacking)
        {
            // Slow down or stop movement during attack
            Vector2 targetVelocity = new Vector2(0, rb.linearVelocity.y);
            rb.linearVelocity = Vector3.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocity, movementSmoothing);
            rb.gravityScale = 2;
        }
        else if (isWallSliding)
        {
            // Wall slide physics - slow descent
            // Keep horizontal position fixed to wall
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            rb.gravityScale = wallSlideGravity; // Reduced gravity while sliding
            
            // IMPORTANT: Only prevent upward movement if we're actively sliding
            // (not during the transition out of wall slide)
            if (rb.linearVelocity.y > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            }
        }
        else
        {
            // Normal movement
            rb.gravityScale = 2;
            
            Vector2 targetVelocity = new Vector2(moveInput.x * currentSpeed, rb.linearVelocity.y);
            
            // If wall stick timer is active, limit movement away from wall
            if (wallStickTimer > 0 && isTouchingWall)
            {
                if (Mathf.Sign(moveInput.x) == -wallSide || moveInput.x == 0)
                {
                    targetVelocity.x = 0;
                }
            }
            
            rb.linearVelocity = Vector3.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocity, movementSmoothing);
        }
    }

    private void HandleFlip()
    {
        // Don't flip if there's no horizontal input
        if (Mathf.Abs(moveInput.x) < 0.1f)
            return;
            
        // Flip character based on movement direction
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

    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        
        // Set dash direction based on facing direction
        dashDirection = facingRight ? Vector2.right : Vector2.left;
        
        // If in air and moving, dash in movement direction
        if (!isGrounded && Mathf.Abs(moveInput.x) > 0.1f && !isWallSliding)
        {
            dashDirection = new Vector2(Mathf.Sign(moveInput.x), 0);
        }
    }

    private void EndDash()
    {
        isDashing = false;
        
        // Restore gravity
        rb.gravityScale = 2;
        
        // Optional: Reset vertical velocity to prevent flying after dash
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }
    }

    private void StartAttack()
    {
        isAttacking = true;
        attackTimer = attackDuration;
        attackCooldownTimer = attackCooldown;
        
        // Detect enemies in range
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        // TO DO: Damage enemies
        /*foreach (Collider2D enemy in hitEnemies)
        {
            Enemy enemyScript = enemy.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                enemyScript.TakeDamage(attackDamage);
            }
        }*/
    }

    private void EndAttack()
    {
        isAttacking = false;
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }

    private void UpdateAttackPointPosition()
    {
        // Position attack point based on facing direction
        if (attackPoint != null)
        {
            attackPoint.localPosition = new Vector3(
                facingRight ? Mathf.Abs(attackPoint.localPosition.x) : -Mathf.Abs(attackPoint.localPosition.x),
                attackPoint.localPosition.y,
                attackPoint.localPosition.z
            );
        }
    }

    // Visualize checks in editor
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
            // Show both directions with offset
            Vector2 offset = new Vector2(wallCheckOffset, 0);
            Gizmos.DrawLine(wallCheck.position + (Vector3)offset, wallCheck.position + (Vector3)offset + Vector3.right * wallCheckDistance);
            Gizmos.DrawLine(wallCheck.position - (Vector3)offset, wallCheck.position - (Vector3)offset + Vector3.left * wallCheckDistance);
        }
        
        if (attackPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
    
    private void SetAnimation(float moveInput)
    {
        if (isAttacking)
        {
            animator.Play("Player_attack");
        }
        else if (isDashing)
        {
            animator.Play("Player_dash");
        }
        else if (isWallSliding)
        {
            animator.Play("Player_wallslide");
        }
        else if (isWallJumping)
        {
            animator.Play("Player_jump");
        }
        else if (isGrounded)
        {
            if (moveInput == 0)
            {
                animator.Play("Player_idle");
            }
            else
            {
                if (isRunning)
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
                animator.Play("Player_fall");
            }
        }
    }
}