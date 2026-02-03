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
    private int wallSide = 0; // -1 = left, 1 = right, 0 = none
    //private float wallStickTimer = 0f;
    private float wallJumpHorizontalLockTimer = 0f;
    private bool justWallJumped = false;
    
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
    }
    
    private void FixedUpdate()
    {
        ApplyWallSlide();
    }
    
    private void CheckWall()
    {
        // Check if wall jump/slide ability is unlocked
        if (abilitySystem != null && !abilitySystem.CanWallJump()) // NEW
        {
            return;
        }
        // Don't check for walls if grounded or swimming
        if (controller.IsEffectivelyGrounded() || controller.Swimming.IsSwimming)
        {
            isTouchingWall = false;
            isWallSliding = false;
            return;
        }
        
        // Don't check during dash or attacking
        if (controller.DashState.IsDashing || controller.ComboSystem.IsAttacking)
        {
            isTouchingWall = false;
            isWallSliding = false;
            return;
        }
        
        // Reset wall check
        isTouchingWall = false;
        wallSide = 0;
        
        // Check both sides
        CheckWallSide(1);  // Right
        CheckWallSide(-1); // Left

// Update wall slide state
        if (isTouchingWall && rb.linearVelocity.y < 0)
        {
                isWallSliding = true;
        }
        else
        {
            isWallSliding = false;
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
        if (!CanWallJump()) return;
        
        // Calculate jump direction (away from wall)
        Vector2 jumpDirection = new Vector2(
            -wallSide * settings.wallJumpAngle.x, // Away from wall
            settings.wallJumpAngle.y              // Upward
        ).normalized;
        
        // Apply wall jump force
        rb.linearVelocity = jumpDirection * settings.wallJumpForce;
        
        // Face away from wall (flip)
        bool shouldFaceRight = wallSide == -1; // If wall is on left, face right
        movement.ForceFlip(shouldFaceRight);
        
        // Set timers and flags
        justWallJumped = true;
        wallJumpHorizontalLockTimer = settings.wallJumpHorizontalLockDuration;
        
        // Reset wall state
        isTouchingWall = false;
        isWallSliding = false;
        
        // Reset physics state
        physics.ResetJumpState();
        
        // Enable double jump after wall jump
        if (controller.AbilitySystem != null && controller.AbilitySystem.CanDoubleJump())
        {
            physics.ResetDoubleJump(); // Clear any previous double jump
            physics.SetCanDoubleJump(true); // Enable for next jump
        }
        
        // Reset dash count
        controller.DashState.ResetAirDash();
    }
    
    // Call this to prevent input toward wall after wall jump
    public float GetHorizontalInputLock(float rawInput)
    {
        if (wallJumpHorizontalLockTimer <= 0 || !justWallJumped) return rawInput;
        
        // If player tries to move back toward wall, block the input
        if (Mathf.Sign(rawInput) == wallSide)
        {
            return 0f;
        }
        
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