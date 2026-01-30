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
        
        // CRITICAL FIX: If we have jump momentum, PRESERVE IT by not applying air control
        if (jumpMomentumTimer > 0)
        {
            // During jump momentum phase, DO NOT modify horizontal velocity at all
            // This preserves the running jump momentum
            Debug.Log($"Jump momentum active: timer={jumpMomentumTimer:F2}, X velocity={rb.linearVelocity.x:F2}");
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
        jumpMomentumTimer = duration; // Longer duration for better momentum preservation
        Debug.Log($"StartJumpMomentum: {duration}s, current X velocity = {rb.linearVelocity.x:F2}");
    }
    
    // Check if jump momentum is active
    public bool HasJumpMomentum()
    {
        return jumpMomentumTimer > 0;
    }
}