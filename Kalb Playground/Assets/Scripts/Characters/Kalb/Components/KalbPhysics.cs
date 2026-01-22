using UnityEngine;

public class KalbPhysics : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private KalbSettings settings;
    [SerializeField] private KalbCollisionDetector collisionDetector;
    
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
        
        // Only apply wall friction if we're actually wall sliding (touching wall + falling)
        if (collisionDetector.ShouldApplyWallFriction())
        {
            // When wall sliding, we want to limit fall speed but not stop horizontal movement
            // This creates the classic wall slide feel
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
        // If we're touching a wall but NOT wall sliding (e.g., jumping into wall), 
        // we might want to apply some friction to prevent sticking
        else if (collisionDetector.IsTouchingWall && !collisionDetector.IsGrounded)
        {
            // Minimal friction when touching wall but not sliding
            // This helps prevent getting "stuck" on walls when jumping
            float wallTouchFriction = 2f;
            float newXVelocity = Mathf.MoveTowards(rb.linearVelocity.x, 0, wallTouchFriction * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newXVelocity, rb.linearVelocity.y);
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