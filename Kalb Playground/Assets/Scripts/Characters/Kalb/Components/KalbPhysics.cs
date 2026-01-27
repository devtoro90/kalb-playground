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