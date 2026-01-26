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
        
        // If we're wall sliding, reduce gravity for that classic wall slide feel
        if (collisionDetector != null && collisionDetector.IsWallSliding)
        {
            rb.gravityScale = settings.normalGravityScale * 0.5f; // Reduced gravity while wall sliding
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