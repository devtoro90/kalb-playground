using UnityEngine;

public class KalbPhysics : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private KalbController controller;
    [SerializeField] private KalbSettings settings;
    [SerializeField] private KalbCollisionDetector collisionDetector;
    [SerializeField] private KalbSwimming swimming;
    [SerializeField] private KalbGravityManager gravityManager;

// Jump state
    private bool isJumpButtonHeld = false;
    private float coyoteTimeCounter = 0f;
    private float jumpBufferCounter = 0f;
    private bool canDoubleJump = false;
    private bool hasDoubleJumped = false;
    
    public bool IsJumpButtonHeld => isJumpButtonHeld;
    public float CoyoteTimeCounter => coyoteTimeCounter;
    public float JumpBufferCounter => jumpBufferCounter;
    public bool CanDoubleJump => canDoubleJump && !hasDoubleJumped;
    
    private void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (controller == null) controller = GetComponent<KalbController>();
        if (collisionDetector == null) collisionDetector = GetComponent<KalbCollisionDetector>();
        if (swimming == null) swimming = GetComponent<KalbSwimming>();
        if (gravityManager == null) gravityManager = GetComponent<KalbGravityManager>();
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
        ApplyAirFriction();
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
        
        // If we're swimming, swimming system handles buoyancy
        if (swimming != null && swimming.IsSwimming)
        {
            return;
        }
        
        // Let gravity manager handle gravity scaling
        // The gravity manager will have the float override if active
        
        // FALLING: Apply increased falling gravity (if not floating)
        if (rb.linearVelocity.y < 0)
        {
            // Check if we're in float state - gravity manager handles this
            if (gravityManager != null && gravityManager.OverrideSource != "FloatingFall")
            {
                gravityManager?.SetFallingGravity();
            }
            
            // Clamp to maximum fall speed (but respect float speed if floating)
            float maxFall = settings.maxFallSpeed;
            
            // If floating, use float fall speed as limit
            if (gravityManager != null && gravityManager.OverrideSource == "FloatingFall")
            {
                maxFall = settings.floatFallSpeed;
            }
            
            if (rb.linearVelocity.y < maxFall)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFall);
            }
        }
        // ASCENDING (JUMP RELEASED): Apply quick fall gravity
        else if (rb.linearVelocity.y > 0 && !isJumpButtonHeld)
        {
            gravityManager?.SetQuickFallGravity();
        }
        // NEUTRAL: Apply normal gravity
        else
        {
            gravityManager?.SetNormalGravity();
        }
    }

    private void ApplyAirFriction()
    {
        if (settings == null || rb == null) return;
        if (controller.IsEffectivelyGrounded()) return;
        if (swimming != null && swimming.IsSwimming) return;
        
        // Apply air friction (slows down when no input)
        float currentXVelocity = rb.linearVelocity.x;
        
        if (Mathf.Abs(currentXVelocity) > 0.1f && Mathf.Abs(controller.InputHandler.MoveInput.x) < 0.1f)
        {
            // Gradually slow down in air (HK style - not too fast)
            float frictionForce = -Mathf.Sign(currentXVelocity) * settings.airFriction;
            rb.AddForce(new Vector2(frictionForce, 0));
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
        
        // CRITICAL FIX: Only modify the Y velocity, preserve X completely
        // Don't even read rb.linearVelocity.x here - just set Y
        Vector2 currentVelocity = rb.linearVelocity;

        rb.linearVelocity = new Vector2(currentVelocity.x, jumpForce);

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

    public void SetCanDoubleJump(bool value)
    {
        canDoubleJump = value;
        if (value) hasDoubleJumped = false; // Reset double jump when enabled
    }

    public void ResetDoubleJump()
    {
        canDoubleJump = false;
        hasDoubleJumped = false;
    }
}