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
    private bool wasWallSliding = false; // Track previous frame for transitions
    private int wallSide = 0; // -1 = left, 1 = right, 0 = none
    private float wallJumpHorizontalLockTimer = 0f;
    private bool justWallJumped = false;
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
        
        wasWallSliding = isWallSliding;
    }
    
    private void FixedUpdate()
    {
        ApplyWallSlide();
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
                
                // If we were moving upward, clamp upward velocity to prevent flying up the wall
                if (rb.linearVelocity.y > 0)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                }
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
            
            // Rule 3: Pressing away after grace period (handled in UpdateAwayInputGracePeriod)
            
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
        awayInputTimer = 0f;
        isPressingAwayFromWall = false;
        
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
        awayInputTimer = 0f;
        isPressingAwayFromWall = false;
    }

    public void ForceDisengageForWallJump()
    {
        isWallSliding = false;
        awayInputTimer = 0f;
        isPressingAwayFromWall = false;
        justWallJumped = true;
    }

    public void SetWallSlideCooldown()
    {
        wallSlideCooldownTimer = WALL_SLIDE_COOLDOWN;
    }

    public bool CanWallJumpDuringGracePeriod()
    {
        return isPressingAwayFromWall && awayInputTimer > 0;
    }
    
    private void ApplyWallSlide()
    {
        if (!isWallSliding) return;
        
        // Apply wall slide speed (clamp downward velocity)
        if (rb.linearVelocity.y < settings.wallSlideSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, settings.wallSlideSpeed);
        }

        // Apply force toward the wall to prevent drifting
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
        
        Vector2 jumpDirection = new Vector2(
            -wallSide * settings.wallJumpAngle.x,
            settings.wallJumpAngle.y
        ).normalized;
        
        rb.linearVelocity = jumpDirection * settings.wallJumpForce;
        
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
            }
            
            if (wallJumpHorizontalLockTimer > 0)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.2f);
            }
        }
    }
}