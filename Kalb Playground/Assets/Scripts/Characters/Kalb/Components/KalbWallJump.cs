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
    private bool isWallSliding = false;
    private bool isWallSlidingActive = false;
    private int wallSide = 0; // -1 = left, 1 = right, 0 = none
    //private float wallStickTimer = 0f;
    private float wallJumpHorizontalLockTimer = 0f;
    private bool justWallJumped = false;
    private float awayInputTimer = 0f;
    private bool isPressingAwayFromWall = false;
    
    // Properties
    public bool IsTouchingWall => isTouchingWall;
    public bool IsWallSliding => isWallSliding;
    public int WallSide => wallSide;
    public bool JustWallJumped => justWallJumped;
    
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
            isTouchingWall = false;
            isWallSliding = false;
            isWallSlidingActive = false;
            return;
        }
        
        // Don't check for walls if grounded or swimming
        if (controller.IsEffectivelyGrounded() || controller.Swimming.IsSwimming)
        {
            isTouchingWall = false;
            isWallSliding = false;
            isWallSlidingActive = false;
            return;
        }
        
        // Don't check during dash or attacking
        if (controller.DashState.IsDashing || controller.ComboSystem.IsAttacking)
        {
            isTouchingWall = false;
            isWallSliding = false;
            isWallSlidingActive = false;
            return;
        }
        
        // Reset wall check
        bool wasTouchingWall = isTouchingWall;
        isTouchingWall = false;
        wallSide = 0;
        
        // Check both sides
        CheckWallSide(1);  // Right
        CheckWallSide(-1); // Left
        
        // NEW: Update wall slide state with input requirement
        if (isTouchingWall && rb.linearVelocity.y < 0)
        {
            // Only set wall sliding state if not requiring input OR pushing toward wall
            if (!settings.requireInputForWallSlide || IsPushingTowardWall())
            {
                isWallSliding = true;
            }
        }
        else
        {
            isWallSliding = false;
        }
        
        // If we just lost wall contact, disengage active slide
        if (wasTouchingWall && !isTouchingWall)
        {
            isWallSlidingActive = false;
        }
    }
    
    private void CheckWallSide(int direction)
    {
        Vector2 checkPosition = wallCheckMiddle.position;
        
        // Raycast to check for wall
        RaycastHit2D hit = Physics2D.Raycast(
            checkPosition,
            Vector2.right * direction,
            settings.wallCheckDistance,
            settings.wallLayer
        );
        
        if (hit.collider != null)
        {
            // Found a wall
            isTouchingWall = true;
            wallSide = direction;
            
        }
        
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
    }

    private void UpdateAwayInputGracePeriod()
    {
        if (!isWallSlidingActive) 
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
            
            // Early disengagement if we've drifted too far from wall
            float distanceToWall = GetDistanceToWall();
            if (distanceToWall > settings.awayInputDisengageDistance)
            {
                // Drifted too far - disengage immediately
                ForceDisengageWallSlide();
            }
            else if (awayInputTimer <= 0)
            {
                // Grace period expired - disengage wall slide
                ForceDisengageWallSlide();
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
        if (!isTouchingWall || !settings.requireInputForWallSlide)
        {
            isWallSlidingActive = isWallSliding;
            return;
        }
        
        // Check if we should engage wall slide
        if (!isWallSlidingActive)
        {
            // Only engage if: falling AND (not requiring input OR pushing against wall)
            bool shouldEngage = rb.linearVelocity.y < 0 && 
                            (!settings.requireInputForWallSlide || IsPushingTowardWall());
            
            if (shouldEngage)
            {
                isWallSlidingActive = true;
                awayInputTimer = 0f; // Reset grace period
                isPressingAwayFromWall = false;
            }
        }
        else
        {
            // Once engaged, only disengage if:
            // 1. No longer touching wall
            // 2. No longer falling (going up)
            // 3. Not wall sliding state anymore
            // 4. Grace period expired (handled in UpdateAwayInputGracePeriod)
            bool shouldDisengage = !isTouchingWall || 
                                rb.linearVelocity.y >= 0 || 
                                !isWallSliding;
            
            if (shouldDisengage)
            {
                ForceDisengageWallSlide();
            }
        }
    }

    public void ForceDisengageWallSlide()
    {
        isWallSlidingActive = false;
        isWallSliding = false;
        awayInputTimer = 0f;
        isPressingAwayFromWall = false;
    }

    public void ForceDisengageForWallJump()
    {
        // Called when executing a wall jump to immediately disengage
        isWallSlidingActive = false;
        isWallSliding = false;
        awayInputTimer = 0f;
        isPressingAwayFromWall = false;
        justWallJumped = true; // This will help prevent immediate re-engagement
    }

    public bool CanWallJumpDuringGracePeriod()
    {
        // Can wall jump during grace period even if pressing away
        return isPressingAwayFromWall && awayInputTimer > 0;
    }

    public float GetAwayInputTimerRemaining()
    {
        return awayInputTimer;
    }
    
    private void ApplyWallSlide()
    {
        if (!isWallSlidingActive) return;
        
        // Apply wall slide speed (clamp downward velocity)
        if (rb.linearVelocity.y < settings.wallSlideSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, settings.wallSlideSpeed);
        }

        // Apply force toward the wall to prevent drifting
        float currentDistanceToWall = GetDistanceToWall();
        
        if (currentDistanceToWall > settings.wallStickTolerance)
        {
            // Apply force toward the wall
            float forceMultiplier = Mathf.Clamp((currentDistanceToWall - settings.wallStickTolerance) * 10f, 0, 10f);
            Vector2 wallForce = new Vector2(wallSide * settings.wallStickForce * forceMultiplier * Time.fixedDeltaTime, 0);
            rb.AddForce(wallForce);
        }
        
        // If no horizontal input, clamp horizontal velocity
        if (controller.InputHandler == null || Mathf.Abs(controller.InputHandler.MoveInput.x) < 0.1f)
        {
            // Allow very slight movement but prevent drifting
            float maxDrift = 0.05f;
            if (Mathf.Abs(rb.linearVelocity.x) > maxDrift)
            {
                rb.linearVelocity = new Vector2(
                    Mathf.MoveTowards(rb.linearVelocity.x, 0, 20f * Time.fixedDeltaTime),
                    rb.linearVelocity.y
                );
            }
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
        // Can wall jump if:
        // 1. Touching a wall
        // 2. Not grounded
        // 3. Not swimming
        // 4. Not dashing
        // 5. Not in horizontal lock from previous wall jump
        // 6. Wall jump is unlocked
        return abilitySystem != null && abilitySystem.CanWallJump() &&
               isTouchingWall &&
               !controller.IsEffectivelyGrounded() && 
               !controller.Swimming.IsSwimming &&
               !controller.DashState.IsDashing &&
               wallJumpHorizontalLockTimer <= 0;
    }
    
    public void ExecuteWallJump()
    {
        // NEW: Check if we're in grace period
        bool isInGracePeriod = isPressingAwayFromWall && awayInputTimer > 0;
        
        // Can wall jump if: normal conditions OR in grace period
        if (!CanWallJump() && !isInGracePeriod) return;
        
        // Store initial jump direction
        Vector2 jumpDirection = new Vector2(
            -wallSide * settings.wallJumpAngle.x, // Away from wall
            settings.wallJumpAngle.y              // Upward
        ).normalized;
        
        // Apply wall jump force
        rb.linearVelocity = jumpDirection * settings.wallJumpForce;
        
        // Face away from wall
        bool shouldFaceRight = wallSide == -1;
        movement.ForceFlip(shouldFaceRight);
        
        // SHORTER horizontal lock (0.1s instead of 0.2s)
        justWallJumped = true;
        wallJumpHorizontalLockTimer = settings.wallJumpHorizontalLockDuration;
        
        // Allow SOME input during wall jump (not completely locked)
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x * 0.7f, // Reduce initial push to allow player control
            rb.linearVelocity.y
        );
        
        // NEW: Force disengage wall slide for wall jump
        ForceDisengageForWallJump();
        
        // Reset physics state
        physics.ResetJumpState();
        
        // Enable double jump after wall jump
        if (controller.AbilitySystem != null && controller.AbilitySystem.CanDoubleJump())
        {
            physics.ResetDoubleJump();
            physics.SetCanDoubleJump(true);
        }
        
        // Reset dash count
        controller.DashState.ResetAirDash();
    }
    
    // Call this to prevent input toward wall after wall jump
    public float GetHorizontalInputLock(float rawInput)
    {
        if (wallJumpHorizontalLockTimer <= 0 || !justWallJumped) return rawInput;
        
        // Allow 30% input toward wall (for fine control)
        if (Mathf.Sign(rawInput) == wallSide)
        {
            return rawInput * 0.3f; // Reduced but not zero
        }
        
        // Full control away from wall
        return rawInput;
    }

    public void ResetWallSlide()
    {
        isWallSliding = false;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (wallCheckMiddle != null)
        {
            // Draw wall check rays
            Gizmos.color = isTouchingWall ? Color.green : Color.yellow;
            
            // Right check
            Gizmos.DrawRay(wallCheckMiddle.position, Vector2.right * settings.wallCheckDistance);
            
            // Left check
            Gizmos.DrawRay(wallCheckMiddle.position, Vector2.left * settings.wallCheckDistance);
            
            // Draw wall slide indicator
            if (isWallSliding)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 1f, 0.3f);
            }
            
            // Draw wall jump lock indicator
            if (wallJumpHorizontalLockTimer > 0)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.2f);
            }
        }
    }
}