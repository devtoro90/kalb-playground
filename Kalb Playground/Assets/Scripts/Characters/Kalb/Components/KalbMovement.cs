using UnityEngine;

public class KalbMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private KalbCollisionDetector collisionDetector;
    [SerializeField] private KalbSettings settings;
    [SerializeField] private KalbSwimming swimming;
    
    [Header("Movement Settings")]
    [SerializeField] private bool instantStop = true;
    [SerializeField] private bool flipInAir = true;
    
    // Movement state
    private Vector3 velocity = Vector3.zero;
    private bool facingRight = true;
    
    public bool FacingRight => facingRight;
    
    private void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (collisionDetector == null) collisionDetector = GetComponent<KalbCollisionDetector>();
        if (swimming == null) swimming = GetComponent<KalbSwimming>();
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
        
        // If no input in air, allow some drift
        if (moveInput == 0)
        {
            // Slow down gradually in air
            float newXVelocity = Mathf.MoveTowards(rb.linearVelocity.x, 0, 5f * Time.fixedDeltaTime);
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
    
    // REMOVED: ApplySwimMovement method - it's now in KalbSwimming
    
    private void Flip(float moveInput)
    {
        // Only flip if direction actually changes
        if ((moveInput > 0 && !facingRight) || (moveInput < 0 && facingRight))
        {
            facingRight = !facingRight;
            Vector3 theScale = transform.localScale;
            theScale.x *= -1;
            transform.localScale = theScale;
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
        }
    }
}