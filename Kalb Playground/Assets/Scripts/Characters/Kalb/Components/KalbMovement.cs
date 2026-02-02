using UnityEngine;

public class KalbMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private KalbCollisionDetector collisionDetector;
    [SerializeField] private KalbSettings settings;
    [SerializeField] private KalbSwimming swimming;
    [SerializeField] private KalbComboSystem comboSystem;
    
    [Header("Movement Settings")]
    [SerializeField] private bool instantStop = true;
    [SerializeField] private bool flipInAir = true;
    
    // Movement state
    protected internal Vector3 velocity = Vector3.zero;
    private bool facingRight = true;
    private float jumpMomentumTimer = 0f;
    
    public Vector3 Velocity 
    { 
        get => velocity; 
        set => velocity = value; 
    }
    public bool FacingRight => facingRight;
    
    private void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (collisionDetector == null) collisionDetector = GetComponent<KalbCollisionDetector>();
        if (swimming == null) swimming = GetComponent<KalbSwimming>();
        if (comboSystem == null) comboSystem = GetComponent<KalbComboSystem>();
    }
    
    private void Update()
    {
        // Update jump momentum timer
        if (jumpMomentumTimer > 0)
        {
            jumpMomentumTimer -= Time.deltaTime;
        }
    }
    
    public void Move(float moveInput, bool isGrounded)
    {
        if (collisionDetector == null || rb == null || settings == null) return;
        
        // Skip if swimming - swimming state handles its own movement
        if (swimming != null && swimming.IsSwimming)
        {
            // Don't apply regular movement when swimming
            // Swimming movement is handled in KalbSwimState
            return;
        }
        
        // Calculate target velocity
        float targetSpeed = moveInput * settings.moveSpeed;
        
        // Instant stop when no input and grounded
        if (instantStop && moveInput == 0 && isGrounded)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            velocity = Vector3.zero;
            return;
        }
        
        Vector2 targetVelocity = new Vector2(targetSpeed, rb.linearVelocity.y);
        
        // Smooth movement for non-instant stopping or in air
        rb.linearVelocity = Vector3.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocity, settings.movementSmoothing);
        
        // Flip sprite if needed
        if (moveInput != 0)
        {
            Flip(moveInput);
        }
    }
    
    public void ApplyAirControl(float moveInput)
    {
        if (collisionDetector == null || rb == null || settings == null) return;
        if (collisionDetector.IsGrounded) return;
        
        // Skip if swimming
        if (swimming != null && swimming.IsSwimming) return;
        
        // FIXED: Preserve momentum but allow directional control
        if (jumpMomentumTimer > 0)
        {
            // Calculate momentum preservation factor (decays over time)
            float momentumPreservation = Mathf.Clamp01(jumpMomentumTimer / 0.3f);
            
            // Get current velocity
            float currentXVelocity = rb.linearVelocity.x;
            float currentSpeed = Mathf.Abs(currentXVelocity);
            
            // Calculate target velocity based on input
            float targetXVelocity = moveInput * settings.moveSpeed * settings.airControlMultiplier;
            
            // Only apply control if:
            // 1. Player is actively providing input, AND
            // 2. They're trying to go opposite direction OR current speed is below max air speed
            if (Mathf.Abs(moveInput) > 0.1f && 
                (Mathf.Sign(moveInput) != Mathf.Sign(currentXVelocity) || currentSpeed < settings.maxAirSpeed))
            {
                // Blend between momentum preservation and player control
                float blendedVelocity = Mathf.Lerp(
                    currentXVelocity,
                    targetXVelocity,
                    (1f - momentumPreservation) * 0.5f // Reduced control during momentum phase
                );
                
                rb.linearVelocity = new Vector2(blendedVelocity, rb.linearVelocity.y);
            }
            
            // Still update flip for consistency
            if (flipInAir && moveInput != 0)
            {
                Flip(moveInput);
            }
            
            return;
        }
        
        // If no input in air, allow some drift but don't slow down too quickly
        if (moveInput == 0)
        {
            // Very gradual slowdown in air (Silksong style - momentum carries)
            float newXVelocity = Mathf.MoveTowards(rb.linearVelocity.x, 0, 2f * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newXVelocity, rb.linearVelocity.y);
            
            // Don't flip when no input
            return;
        }
        
        // Flip in air if enabled
        if (flipInAir)
        {
            Flip(moveInput);
        }
        
        // Calculate target velocity based on input
        float targetSpeed = moveInput * settings.moveSpeed * settings.airControlMultiplier;
        float velocityDifference = targetSpeed - rb.linearVelocity.x;
        
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
    
    private void Flip(float moveInput)
    {
        // Only flip if direction actually changes
        if ((moveInput > 0 && !facingRight) || (moveInput < 0 && facingRight))
        {
            facingRight = !facingRight;
            Vector3 theScale = transform.localScale;
            theScale.x *= -1;
            transform.localScale = theScale;

            // Update attack point position in combo system
            if (comboSystem != null)
            {
                comboSystem.UpdateAttackPointWithFacing(facingRight);
            }
        }
    }
    
    public void StopHorizontalMovement()
    {
        if (rb == null) return;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        velocity = Vector3.zero;
    }
    
    public void ResetSmoothing()
    {
        velocity = Vector3.zero;
    }
    
    // Public method to force flip if needed
    public void ForceFlip(bool faceRight)
    {
        if (faceRight != facingRight)
        {
            facingRight = faceRight;
            Vector3 theScale = transform.localScale;
            theScale.x *= -1;
            transform.localScale = theScale;

            // Update attack point position in combo system
            if (comboSystem != null)
            {
                comboSystem.UpdateAttackPointWithFacing(facingRight);
            }
        }
    }
    
    // Method to set jump momentum timer
    public void StartJumpMomentum(float duration = 0.3f)
    {
        jumpMomentumTimer = duration;
        
        // Store current velocity for momentum preservation
        if (rb != null)
        {
            float currentSpeed = Mathf.Abs(rb.linearVelocity.x);
            
            // If speed is below walk speed, boost it for better jump feel
            if (currentSpeed < settings.moveSpeed * 0.5f && settings != null)
            {
                float direction = facingRight ? 1f : -1f;
                float boostAmount = settings.moveSpeed * 0.3f;
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x + (direction * boostAmount),
                    rb.linearVelocity.y
                );
            }
        }
    }
    
    // Check if jump momentum is active
    public bool HasJumpMomentum()
    {
        return jumpMomentumTimer > 0;
    }
}