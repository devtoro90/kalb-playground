using UnityEngine;

public class KalbWallJump : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KalbController controller;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private KalbCollisionDetector collisionDetector;
    [SerializeField] private KalbMovement movement;
    [SerializeField] private KalbPhysics physics;
    [SerializeField] private Transform wallCheckMiddle;
    [SerializeField] private KalbSettings settings;
    [SerializeField] private KalbAbilitySystem abilitySystem;

    // State
    private bool isTouchingWall = false;
    private bool isWallSliding = false; // This is the STICKY wall slide state
    private int wallSide = 0; // -1 = left, 1 = right, 0 = none
    
    // Wall sliding acceleration system
    private float currentSlideSpeed = 0f;
    private float wallStickTimer = 0f;
    private bool isStickingToWall = false;
    private float neutralSlideMultiplier = 1f;
    private float wallJumpHorizontalLockTimer = 0f;
    private bool justWallJumped = false;
    
    // Tap boost system
    private float lastTapTime = 0f;
    private int tapCount = 0;
    private float currentTapBoost = 0f;
    private float tapBoostDecayTimer = 0f;
    
    // Wall engage buffer
    private float wallEngageBufferTimer = 0f;
    private int bufferedWallSide = 0;
    private const float WALL_ENGAGE_BUFFER_TIME = 0.1f;
    
    // Away input grace period
    private float awayInputTimer = 0f;
    private bool isPressingAwayFromWall = false;
    
    // Cooldown to prevent immediate re-engagement after ledge release
    private float wallSlideCooldownTimer = 0f;
    private const float WALL_SLIDE_COOLDOWN = 0.2f;
    
    // Properties
    public bool IsTouchingWall => isTouchingWall;
    public bool IsWallSliding => isWallSliding;
    public int WallSide => wallSide;
    public bool JustWallJumped => justWallJumped;
    public float CooldownRemaining => wallSlideCooldownTimer;
    public float CurrentSlideSpeed => currentSlideSpeed;
    public float SlideSpeedRatio => Mathf.Abs(currentSlideSpeed / settings.wallSlideMaxSpeed);
    
    private void Awake()
    {
        if (controller == null) controller = GetComponent<KalbController>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (collisionDetector == null) collisionDetector = GetComponent<KalbCollisionDetector>();
        if (movement == null) movement = GetComponent<KalbMovement>();
        if (physics == null) physics = GetComponent<KalbPhysics>();
        if (abilitySystem == null) abilitySystem = GetComponent<KalbAbilitySystem>();
        
        // Create wall check point if not assigned
        if (wallCheckMiddle == null)
        {
            GameObject obj = new GameObject("WallCheckMiddle");
            obj.transform.parent = transform;
            obj.transform.localPosition = new Vector3(0, 0, 0);
            wallCheckMiddle = obj.transform;
        }
    }
    
    private void Update()
    {
        CheckWall();
        UpdateTimers();
        UpdateWallSlideEngagement();
        UpdateAwayInputGracePeriod();
        UpdateTapBoostSystem();
        UpdateWallStick();
    }
    
    private void FixedUpdate()
    {
        ApplyWallSlidePhysics();
    }
    
    private void CheckWall()
    {
        // Check if wall jump/slide ability is unlocked
        if (abilitySystem != null && !abilitySystem.CanWallJump())
        {
            ClearWallStates();
            return;
        }
        
        // Don't check for walls if swimming
        if (controller.Swimming.IsSwimming)
        {
            ClearWallStates();
            return;
        }
        
        // Don't check during dash or attacking
        if (controller.DashState.IsDashing || controller.ComboSystem.IsAttacking)
        {
            ClearWallStates();
            return;
        }
        
        // CRITICAL: Don't check walls during or immediately after ledge release
        if (controller.LedgeDetector.IsOnCooldown || 
            (controller.LedgeDetector.CooldownRemaining > 0))
        {
            ClearWallStates();
            return;
        }
        
        // Reset wall check
        bool wasTouchingWall = isTouchingWall;
        isTouchingWall = false;
        wallSide = 0;
        
        // Check both sides
        CheckWallSide(1);  // Right
        CheckWallSide(-1); // Left
    }
    
    private void ClearWallStates()
    {
        isTouchingWall = false;
        isWallSliding = false;
        wallSide = 0;
        awayInputTimer = 0f;
        isPressingAwayFromWall = false;
        currentSlideSpeed = 0f;
        isStickingToWall = false;
        wallStickTimer = 0f;
    }
    
    private void CheckWallSide(int direction)
    {
        Vector2 checkPosition = wallCheckMiddle.position;
        
        RaycastHit2D hit = Physics2D.Raycast(
            checkPosition,
            Vector2.right * direction,
            settings.wallCheckDistance,
            settings.wallLayer
        );
        
        if (hit.collider != null)
        {
            isTouchingWall = true;
            wallSide = direction;
        }
        else if (IsPushingTowardWall() && direction == Mathf.RoundToInt(Mathf.Sign(controller.InputHandler.MoveInput.x)))
        {
            // Not touching wall but pressing toward it - buffer the input
            bufferedWallSide = direction;
            wallEngageBufferTimer = WALL_ENGAGE_BUFFER_TIME;
        }
    }
    
    private bool HasBufferedWallEngagement()
    {
        if (wallEngageBufferTimer <= 0 || bufferedWallSide == 0)
            return false;
        
        // Check if we're now touching the wall on the buffered side
        return isTouchingWall && wallSide == bufferedWallSide;
    }
    
    public void UpdateTimers()
    {
        // Update wall jump horizontal lock timer
        if (wallJumpHorizontalLockTimer > 0)
        {
            wallJumpHorizontalLockTimer -= Time.deltaTime;
            if (wallJumpHorizontalLockTimer <= 0)
            {
                justWallJumped = false;
            }
        }
        
        // Update wall slide cooldown
        if (wallSlideCooldownTimer > 0)
        {
            wallSlideCooldownTimer -= Time.deltaTime;
        }
        
        // Update wall engage buffer timer
        if (wallEngageBufferTimer > 0)
        {
            wallEngageBufferTimer -= Time.deltaTime;
            if (wallEngageBufferTimer <= 0)
            {
                bufferedWallSide = 0;
            }
        }
        
        // Update wall stick timer
        if (wallStickTimer > 0)
        {
            wallStickTimer -= Time.deltaTime;
            if (wallStickTimer <= 0)
            {
                isStickingToWall = false;
            }
        }
    }
    
    private void UpdateWallStick()
    {
        // Handle initial stickiness when grabbing wall
        if (isWallSliding && isStickingToWall)
        {
            // During stick period, we don't slide
            currentSlideSpeed = 0f;
            
            // Apply extra force toward wall during stick
            if (isTouchingWall)
            {
                Vector2 stickForce = new Vector2(wallSide * settings.wallStickForce * 2f, 0);
                rb.AddForce(stickForce);
            }
        }
    }
    
    private void UpdateAwayInputGracePeriod()
    {
        if (!isWallSliding) 
        {
            awayInputTimer = 0f;
            isPressingAwayFromWall = false;
            return;
        }
        
        // Check if player is pressing away from wall
        bool nowPressingAway = IsPressingAwayFromWall();
        
        if (nowPressingAway && !isPressingAwayFromWall)
        {
            // Just started pressing away - start grace period
            awayInputTimer = settings.awayInputGracePeriod;
            isPressingAwayFromWall = true;
        }
        else if (!nowPressingAway && isPressingAwayFromWall)
        {
            // Stopped pressing away - reset
            awayInputTimer = 0f;
            isPressingAwayFromWall = false;
        }
        else if (isPressingAwayFromWall && awayInputTimer > 0)
        {
            // Count down grace period
            awayInputTimer -= Time.deltaTime;
            
            // If grace period expires, disengage wall slide
            if (awayInputTimer <= 0)
            {
                DisengageWallSlide();
            }
        }
    }
    
    private void UpdateTapBoostSystem()
    {
        if (!isWallSliding) 
        {
            // Reset tap boost when not sliding
            tapCount = 0;
            currentTapBoost = 0f;
            tapBoostDecayTimer = 0f;
            return;
        }
        
        // Decay tap boost over time
        if (tapBoostDecayTimer > 0)
        {
            tapBoostDecayTimer -= Time.deltaTime;
            if (tapBoostDecayTimer <= 0)
            {
                currentTapBoost = 0f;
                tapCount = 0;
            }
        }
    }
    
    public void RegisterTapTowardWall()
    {
        if (!isWallSliding) return;
        
        float currentTime = Time.time;
        
        // Check if this tap is within the boost window
        if (currentTime - lastTapTime <= settings.wallTapBoostWindow)
        {
            tapCount = Mathf.Min(tapCount + 1, settings.maxWallTapBoosts);
            currentTapBoost = tapCount * settings.wallTapBoostAmount;
        }
        else
        {
            // Start new tap sequence
            tapCount = 1;
            currentTapBoost = settings.wallTapBoostAmount;
        }
        
        lastTapTime = currentTime;
        tapBoostDecayTimer = settings.wallTapBoostWindow * 2f; // Keep boost for twice the window
        
        // Cap at max speed
        float maxTotalSpeed = Mathf.Abs(settings.wallSlideMaxSpeed);
        float currentTotal = Mathf.Abs(currentSlideSpeed) + currentTapBoost;
        if (currentTotal > maxTotalSpeed)
        {
            currentTapBoost = maxTotalSpeed - Mathf.Abs(currentSlideSpeed);
        }
    }

    private bool IsPressingAwayFromWall()
    {
        if (controller == null || controller.InputHandler == null)
            return false;
        
        float inputDirection = Mathf.Sign(controller.InputHandler.MoveInput.x);
        return Mathf.Abs(controller.InputHandler.MoveInput.x) > 0.1f && 
            Mathf.Approximately(inputDirection, -wallSide);
    }

    private bool IsPushingTowardWall()
    {
        if (controller == null || controller.InputHandler == null)
            return false;
        
        float inputDirection = Mathf.Sign(controller.InputHandler.MoveInput.x);
        return Mathf.Abs(controller.InputHandler.MoveInput.x) > 0.1f && 
            Mathf.Approximately(inputDirection, wallSide);
    }

    private bool IsHoldingTowardWall()
    {
        if (controller == null || controller.InputHandler == null)
            return false;
        
        float inputDirection = Mathf.Sign(controller.InputHandler.MoveInput.x);
        return Mathf.Abs(controller.InputHandler.MoveInput.x) > 0.1f && 
            Mathf.Approximately(inputDirection, wallSide);
    }

    private void UpdateWallSlideEngagement()
    {
        // If on cooldown (from ledge release), don't engage wall slide
        if (wallSlideCooldownTimer > 0)
        {
            isWallSliding = false;
            return;
        }
        
        // Don't engage if we just wall jumped
        if (justWallJumped && wallJumpHorizontalLockTimer > 0)
        {
            isWallSliding = false;
            return;
        }
        
        // Don't engage if we're in a ledge state
        if (controller.IsInLedgeState())
        {
            isWallSliding = false;
            return;
        }
        
        // ENGAGEMENT RULE: Can engage when moving in ANY vertical direction (up or down)
        // as long as we're touching a wall (or have buffered wall touch) and pushing toward it
        if (!isWallSliding)
        {
            bool shouldEngage = (isTouchingWall || HasBufferedWallEngagement()) && 
                            IsPushingTowardWall() &&
                            !controller.IsEffectivelyGrounded();
            
            if (shouldEngage)
            {
                EngageWallSlide();
                
                // Clear buffer on successful engagement
                wallEngageBufferTimer = 0f;
                bufferedWallSide = 0;
            }
        }
        else
        {
            // DISENGAGEMENT RULES (STICKY - only disengage when specific conditions met)
            bool shouldDisengage = false;
            
            // Rule 1: No longer touching wall
            if (!isTouchingWall)
                shouldDisengage = true;
            
            // Rule 2: Reached ground
            else if (controller.IsEffectivelyGrounded())
                shouldDisengage = true;
            
            if (shouldDisengage)
            {
                DisengageWallSlide();
            }
        }
    }

    public void EngageWallSlide()
    {
        if (isWallSliding) return;
        
        isWallSliding = true;
        isStickingToWall = true;
        wallStickTimer = settings.wallStickDuration;
        
        // FIX: Always initialize to min speed
        currentSlideSpeed = settings.wallSlideMinSpeed;  // Make sure this is set
        
        awayInputTimer = 0f;
        isPressingAwayFromWall = false;
        
        // Reset tap boost system
        tapCount = 0;
        currentTapBoost = 0f;
        
        // If we were moving upward when engaging, zero out the upward velocity
        // to prevent sliding up the wall
        if (rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }
        else
        {
            // Apply initial stick to wall
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    public void DisengageWallSlide()
    {
        if (!isWallSliding) return;
        
        isWallSliding = false;
        isStickingToWall = false;
        currentSlideSpeed = 0f;
        awayInputTimer = 0f;
        isPressingAwayFromWall = false;
    }

    public void ForceDisengageForWallJump()
    {
        isWallSliding = false;
        isStickingToWall = false;
        awayInputTimer = 0f;
        isPressingAwayFromWall = false;
        justWallJumped = true;
        currentTapBoost = 0f; // Reset tap boost on wall jump
    }

    public void SetWallSlideCooldown()
    {
        wallSlideCooldownTimer = WALL_SLIDE_COOLDOWN;
    }

    public bool CanWallJumpDuringGracePeriod()
    {
        return isPressingAwayFromWall && awayInputTimer > 0;
    }
    
    private void ApplyWallSlidePhysics()
    {
        if (!isWallSliding) return;
        
        // If we're still in stick period, don't slide
        if (isStickingToWall)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            ApplyWallStickForce();
            return;
        }
        
        bool holdingAway = IsPressingAwayFromWall();
        
        float acceleration;
        float targetSpeed;
        
        if (holdingAway && awayInputTimer > 0)
        {
            // In grace period when pressing away - decelerate
            acceleration = settings.wallSlideDeceleration;
            targetSpeed = 0f;
        }
        else
        {
            acceleration = settings.wallSlideAcceleration;
            targetSpeed = settings.wallSlideMaxSpeed - currentTapBoost;
            
            if (IsHoldingTowardWall() && controller.InputHandler.DashPressed)
            {
                RegisterTapTowardWall();
            }
        }
        
        // Apply acceleration to slide speed (negative values = downward)
        float currentAbsSpeed = Mathf.Abs(currentSlideSpeed);
        float targetAbsSpeed = Mathf.Abs(targetSpeed);
        
        if (currentAbsSpeed < targetAbsSpeed)
        {
            // Accelerating
            currentAbsSpeed += acceleration * Time.fixedDeltaTime;
            currentAbsSpeed = Mathf.Min(currentAbsSpeed, targetAbsSpeed);
        }
        else if (currentAbsSpeed > targetAbsSpeed)
        {
            // Decelerating
            currentAbsSpeed -= settings.wallSlideDeceleration * Time.fixedDeltaTime;
            currentAbsSpeed = Mathf.Max(currentAbsSpeed, targetAbsSpeed);
        }
        
        // FIX: Ensure we never go below min speed when accelerating
        // This prevents dropping below min speed during transition
        if (!holdingAway && currentAbsSpeed < Mathf.Abs(settings.wallSlideMinSpeed))
        {
            currentAbsSpeed = Mathf.Abs(settings.wallSlideMinSpeed);
        }
        
        // Apply sign (always downward)
        currentSlideSpeed = -currentAbsSpeed;
        
        // Apply the slide velocity
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentSlideSpeed);
        
        // Apply force toward the wall to prevent drifting
        ApplyWallStickForce();
    }
    
    private void ApplyWallStickForce()
    {
        if (!isTouchingWall) return;
        
        float currentDistanceToWall = GetDistanceToWall();
        
        if (currentDistanceToWall > settings.wallStickTolerance)
        {
            float forceMultiplier = Mathf.Clamp((currentDistanceToWall - settings.wallStickTolerance) * 10f, 0, 10f);
            Vector2 wallForce = new Vector2(wallSide * settings.wallStickForce * forceMultiplier * Time.fixedDeltaTime, 0);
            rb.AddForce(wallForce);
        }
        
        // Clamp horizontal velocity to prevent drifting
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
        {
            rb.linearVelocity = new Vector2(
                Mathf.MoveTowards(rb.linearVelocity.x, 0, 20f * Time.fixedDeltaTime),
                rb.linearVelocity.y
            );
        }
    }

    public float GetDistanceToWall()
    {
        if (!isTouchingWall) return Mathf.Infinity;
        
        Vector2 checkPosition = wallCheckMiddle.position;
        RaycastHit2D hit = Physics2D.Raycast(
            checkPosition,
            Vector2.right * wallSide,
            settings.wallCheckDistance,
            settings.wallLayer
        );
        
        return hit.collider != null ? hit.distance : Mathf.Infinity;
    }
    
    public bool CanWallJump()
    {
        return abilitySystem != null && abilitySystem.CanWallJump() &&
               isTouchingWall &&
               !controller.IsEffectivelyGrounded() && 
               !controller.Swimming.IsSwimming &&
               !controller.DashState.IsDashing &&
               wallJumpHorizontalLockTimer <= 0;
    }
    
    public void ExecuteWallJump()
    {
        bool isInGracePeriod = isPressingAwayFromWall && awayInputTimer > 0;
        
        if (!CanWallJump() && !isInGracePeriod) return;
        
        // Store current slide speed for momentum preservation
        float slideMomentum = currentSlideSpeed * settings.wallJumpMomentumRetention;
        
        Vector2 jumpDirection = new Vector2(
            -wallSide * settings.wallJumpAngle.x,
            settings.wallJumpAngle.y
        ).normalized;
        
        // Apply jump force with slide momentum
        Vector2 jumpVelocity = jumpDirection * settings.wallJumpForce;
        jumpVelocity.y += Mathf.Abs(slideMomentum); // Add downward momentum as upward boost
        
        rb.linearVelocity = jumpVelocity;
        
        bool shouldFaceRight = wallSide == -1;
        movement.ForceFlip(shouldFaceRight);
        
        justWallJumped = true;
        wallJumpHorizontalLockTimer = settings.wallJumpHorizontalLockDuration;
        
        ForceDisengageForWallJump();
        
        physics.ResetJumpState();
        
        if (controller.AbilitySystem != null && controller.AbilitySystem.CanDoubleJump())
        {
            physics.ResetDoubleJump();
            physics.SetCanDoubleJump(true);
        }
        
        controller.DashState.ResetAirDash();
    }
    
    public float GetHorizontalInputLock(float rawInput)
    {
        if (wallJumpHorizontalLockTimer <= 0 || !justWallJumped) return rawInput;
        
        if (Mathf.Sign(rawInput) == wallSide)
        {
            return rawInput * 0.3f;
        }
        
        return rawInput;
    }

    public void ResetSlideSpeed()
    {
        if (isWallSliding)
        {
            // Reset to minimum slide speed when coming from wall lock
            currentSlideSpeed = settings.wallSlideMinSpeed;
            
            // Also reset stick timer if you want the initial stickiness
            isStickingToWall = true;
            wallStickTimer = settings.wallStickDuration;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (wallCheckMiddle != null)
        {
            Gizmos.color = isTouchingWall ? Color.green : Color.yellow;
            Gizmos.DrawRay(wallCheckMiddle.position, Vector2.right * settings.wallCheckDistance);
            Gizmos.DrawRay(wallCheckMiddle.position, Vector2.left * settings.wallCheckDistance);
            
            if (isWallSliding)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 1f, 0.3f);
                
                // Draw slide speed indicator
                float speedRatio = Mathf.Abs(currentSlideSpeed / settings.wallSlideMaxSpeed);
                Gizmos.color = Color.Lerp(Color.green, Color.red, speedRatio);
                Gizmos.DrawLine(transform.position, transform.position + Vector3.down * speedRatio * 2f);
            }
            
            if (wallJumpHorizontalLockTimer > 0)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.2f);
            }
        }
    }
}