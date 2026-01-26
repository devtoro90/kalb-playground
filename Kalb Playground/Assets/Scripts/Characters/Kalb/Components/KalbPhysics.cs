using UnityEngine;

public class KalbPhysics : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private KalbSettings settings;
    [SerializeField] private KalbCollisionDetector collisionDetector;
    [SerializeField] private KalbSwimming swimming;
    
    // Jump state
    private bool isJumpButtonHeld = false;
    private float coyoteTimeCounter = 0f;
    private float jumpBufferCounter = 0f;
    
    public bool IsJumpButtonHeld => isJumpButtonHeld;
    public float CoyoteTimeCounter => coyoteTimeCounter;
    public float JumpBufferCounter => jumpBufferCounter;
    
    private void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (collisionDetector == null) collisionDetector = GetComponent<KalbCollisionDetector>();
        if (swimming == null) swimming = GetComponent<KalbSwimming>();
        if (settings == null)
        {
            Debug.LogWarning("KalbPhysics: Settings not assigned!");
        }
    }
    
    private void Update()
    {
        UpdateTimers();
    }
    
    private void FixedUpdate()
    {
        ApplyGravity();
        ApplyWallFriction(); // NEW: Apply wall friction only when appropriate
    }
    
    private void ApplyWallFriction()
    {
        if (collisionDetector == null || rb == null) return;
        
        // Skip wall friction if swimming
        if (swimming != null && swimming.IsSwimming) return;
        
        // Get ability system to check if wall abilities are unlocked
        KalbAbilitySystem abilitySystem = GetComponent<KalbAbilitySystem>();
        if (abilitySystem == null) abilitySystem = GetComponentInParent<KalbAbilitySystem>();
        
        // CRITICAL: If wall slide/jump/lock abilities are NOT unlocked
        // AND we're touching a wall while in air, COMPLETELY disable wall friction
        if (!abilitySystem.CanWallJump() && !abilitySystem.CanWallLock())
        {
            // We're in air, touching a wall, but wall abilities are locked
            if (!collisionDetector.IsGrounded && collisionDetector.IsTouchingWall)
            {
                // 1. Set physics material to frictionless (no friction)
                // 2. Apply constant velocity based on input (bypassing physics)
                
                // Apply NO friction - let player fall/jump normally
                // The key is to NOT let Unity's physics apply any wall friction
                
                // Option A: Set velocity directly based on input
                KalbController controller = GetComponent<KalbController>();
                if (controller != null && controller.InputHandler != null)
                {
                    float moveInput = controller.InputHandler.MoveInput.x;
                    
                    // If pressing into wall, ignore that input for physics
                    // Only apply air control perpendicular to wall
                    if (collisionDetector.WallSide != 0)
                    {
                        // If input is into the wall, ignore it for physics
                        if (Mathf.Sign(moveInput) == collisionDetector.WallSide)
                        {
                            // Don't apply force into the wall
                            moveInput = 0;
                        }
                    }
                    
                    // Apply air control with adjusted input
                    ApplyAirControlPhysics(moveInput);
                }
                
                return; // Skip any other wall friction
            }
        }
        
        // Original wall slide logic for when abilities ARE unlocked
        // NO FRICTION when just touching a wall - only when actually wall sliding
        if (collisionDetector.IsWallSliding)
        {
            // When wall sliding, we want to limit fall speed but not stop horizontal movement
            float currentSlideSpeed = rb.linearVelocity.y;
            
            // Limit maximum wall slide speed (optional - adjust as needed)
            float maxWallSlideSpeed = -5f;
            if (currentSlideSpeed < maxWallSlideSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxWallSlideSpeed);
            }
            
            // DO NOT apply horizontal friction when wall sliding
            // This allows player to push away from wall or continue falling naturally
        }
        // COMPLETELY REMOVE any other wall friction cases
    }

        private void ApplyAirControlPhysics(float moveInput)
    {
        if (rb == null || settings == null) return;
        
        // If no input, allow natural drift (no artificial slowdown)
        if (moveInput == 0)
        {
            // Don't slow down - let momentum carry
            return;
        }
        
        // Calculate target velocity based on input
        float targetXVelocity = moveInput * settings.moveSpeed * settings.airControlMultiplier;
        float velocityDifference = targetXVelocity - rb.linearVelocity.x;
        
        // Apply acceleration force toward target velocity
        rb.AddForce(Vector2.right * velocityDifference * settings.airAcceleration);
        
        // Clamp to maximum air speed
        if (Mathf.Abs(rb.linearVelocity.x) > settings.maxAirSpeed)
        {
            rb.linearVelocity = new Vector2(
                Mathf.Sign(rb.linearVelocity.x) * settings.maxAirSpeed,
                rb.linearVelocity.y
            );
        }
    }
    
    private void UpdateTimers()
    {
        // Coyote time
        if (coyoteTimeCounter > 0)
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
        
        // Jump buffer
        if (jumpBufferCounter > 0)
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }
    
    private void ApplyGravity()
    {
        if (settings == null || rb == null) return;
        
        // If we're swimming, swimming system handles buoyancy - skip gravity
        if (swimming != null && swimming.IsSwimming)
        {
            // Only apply minimal gravity when swimming (if needed for fall through water)
            // The buoyancy system in KalbSwimming handles vertical movement
            return;
        }
        
        // CRITICAL FIX: Check if we're ACTIVELY wall sliding, not just touching wall
        // Wall sliding requires specific conditions: touching wall + falling + pressing into wall
        if (collisionDetector != null && collisionDetector.IsWallSliding)
        {
            // Only apply reduced gravity when ACTIVELY wall sliding
            rb.gravityScale = settings.normalGravityScale * 0.5f;
            return;
        }
        
        // FALLING: Apply increased falling gravity
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = settings.fallingGravityScale;
            
            // Clamp to maximum fall speed (terminal velocity)
            if (rb.linearVelocity.y < settings.maxFallSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, settings.maxFallSpeed);
            }
        }
        // ASCENDING (JUMP RELEASED): Apply quick fall gravity for faster descent
        else if (rb.linearVelocity.y > 0 && !isJumpButtonHeld)
        {
            rb.gravityScale = settings.fallingGravityScale * settings.quickFallGravityMultiplier;
        }
        // NEUTRAL: Apply normal gravity
        else
        {
            rb.gravityScale = settings.normalGravityScale;
        }
    }
    
    public void SetJumpButtonState(bool isHeld)
    {
        isJumpButtonHeld = isHeld;
    }
    
    public void ApplyJumpCut()
    {
        if (rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * settings.jumpCutMultiplier);
        }
    }
    
    public void Jump(float jumpForce)
    {
        if (rb == null) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;
    }
    
    public void SetCoyoteTime()
    {
        if (settings == null) return;
        coyoteTimeCounter = settings.coyoteTime;
    }
    
    public void SetJumpBuffer()
    {
        if (settings == null) return;
        jumpBufferCounter = settings.jumpBufferTime;
    }
    
    public void ResetJumpState()
    {
        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;
        isJumpButtonHeld = false;
    }
}