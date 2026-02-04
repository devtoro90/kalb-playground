using UnityEngine;

public class KalbMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KalbController controller;
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
        if (controller == null) controller = GetComponent<KalbController>();
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
    
    public void ApplyAirControl(float moveInput, bool isTurningAround = false)
    {
        if (collisionDetector == null || rb == null || settings == null) return;
        if (controller.IsEffectivelyGrounded()) return;
        
        // Skip if swimming
        if (swimming != null && swimming.IsSwimming) return;
        
        // Skip during wall slide
        if (controller.WallJump != null && controller.WallJump.IsWallSliding)
        {
            return;
        }
        
        // Get wall jump input lock if applicable
        if (controller.WallJump != null)
        {
            moveInput = controller.WallJump.GetHorizontalInputLock(moveInput);
        }
        
        // Determine control context for different acceleration values
        float currentXVelocity = rb.linearVelocity.x;
        float currentSpeed = Mathf.Abs(currentXVelocity);
        float inputDirection = Mathf.Sign(moveInput);
        float currentDirection = Mathf.Sign(currentXVelocity);
        
        bool isTurning = Mathf.Abs(moveInput) > 0.1f && 
                        Mathf.Abs(inputDirection - currentDirection) > 1.5f && 
                        currentSpeed > 0.5f;
        
        // Calculate acceleration based on context (HK-style)
        float acceleration;
        if (isTurning)
        {
            // Quick turnaround in air (like HK)
            acceleration = settings.airTurnAcceleration; // NEW: Add to settings
        }
        else if (Mathf.Abs(moveInput) > 0.1f)
        {
            // Normal air acceleration
            acceleration = settings.airAcceleration;
        }
        else
        {
            // No input - air deceleration
            acceleration = settings.airDeceleration; // NEW: Add to settings
        }
        
        // Calculate target velocity
        float targetXVelocity = moveInput * settings.maxAirSpeed;
        
        // Apply acceleration toward target velocity
        float velocityDifference = targetXVelocity - currentXVelocity;
        float forceMagnitude = velocityDifference * acceleration;
        
        // Clamp force to prevent overshooting
        float maxForce = Mathf.Abs(targetXVelocity - currentXVelocity) * 50f;
        forceMagnitude = Mathf.Clamp(forceMagnitude, -maxForce, maxForce);
        
        // Apply force
        rb.AddForce(Vector2.right * forceMagnitude);
        
        // Clamp to maximum air speed
        if (Mathf.Abs(rb.linearVelocity.x) > settings.maxAirSpeed)
        {
            rb.linearVelocity = new Vector2(
                Mathf.Sign(rb.linearVelocity.x) * settings.maxAirSpeed,
                rb.linearVelocity.y
            );
        }
        
        // Flip sprite if needed
        if (flipInAir && Mathf.Abs(moveInput) > 0.1f)
        {
            bool shouldFaceRight = moveInput > 0;
            if (shouldFaceRight != facingRight)
            {
                Flip(moveInput);
            }
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