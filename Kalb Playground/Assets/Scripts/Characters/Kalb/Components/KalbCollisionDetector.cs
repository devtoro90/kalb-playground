using UnityEngine;

public class KalbCollisionDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheck;
    
    [Header("Settings")]
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private float wallCheckDistance = 0.05f;
    [SerializeField] private LayerMask environmentLayer;
    [SerializeField] private float wallSlideDetectionHeight = 0.5f; // Height range to check for walls
    
    // State
    private bool isGrounded;
    private bool isTouchingWall;
    private bool isWallSliding; // NEW: Track if we're actually wall sliding (not just touching)
    private int wallSide = 0; // -1 = left, 1 = right, 0 = none
    private Rigidbody2D rb;
    
    public bool IsGrounded => isGrounded;
    public bool IsTouchingWall => isTouchingWall;
    public bool IsWallSliding => isWallSliding; // NEW: Expose wall sliding state
    public int WallSide => wallSide;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    private void Update()
    {
        CheckGround();
        CheckWall();
    }
    
    private void CheckGround()
    {
        if (groundCheck == null) return;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, environmentLayer);
    }
    
    private void CheckWall()
    {
        if (wallCheck == null || rb == null) return;
        
        // Reset wall states
        isTouchingWall = false;
        isWallSliding = false; // Reset wall sliding state
        wallSide = 0;
        
        // Only check for walls if we're falling (negative velocity) 
        // This prevents wall friction when ascending/jumping
        bool isFalling = rb.linearVelocity.y < -0.1f;
        
        if (!isFalling)
        {
            return; // Don't detect walls when not falling
        }
        
        // Check right wall with multiple rays for better detection
        bool touchingRightWall = false;
        for (float yOffset = -wallSlideDetectionHeight/2; yOffset <= wallSlideDetectionHeight/2; yOffset += wallSlideDetectionHeight/3)
        {
            Vector2 checkPosition = (Vector2)wallCheck.position + new Vector2(0, yOffset);
            RaycastHit2D hit = Physics2D.Raycast(checkPosition, Vector2.right, wallCheckDistance, environmentLayer);
            
            if (hit.collider != null && !isGrounded)
            {
                touchingRightWall = true;
                break;
            }
        }
        
        // Check left wall with multiple rays
        bool touchingLeftWall = false;
        for (float yOffset = -wallSlideDetectionHeight/2; yOffset <= wallSlideDetectionHeight/2; yOffset += wallSlideDetectionHeight/3)
        {
            Vector2 checkPosition = (Vector2)wallCheck.position + new Vector2(0, yOffset);
            RaycastHit2D hit = Physics2D.Raycast(checkPosition, Vector2.left, wallCheckDistance, environmentLayer);
            
            if (hit.collider != null && !isGrounded)
            {
                touchingLeftWall = true;
                break;
            }
        }
        
        isTouchingWall = touchingRightWall || touchingLeftWall;
        wallSide = touchingRightWall ? 1 : (touchingLeftWall ? -1 : 0);
        
        // We're wall sliding if we're touching a wall AND falling down
        isWallSliding = isTouchingWall && isFalling;
    }
    
    // NEW: Method to check if we should apply wall friction
    public bool ShouldApplyWallFriction()
    {
        // Only apply wall friction if we're actively wall sliding (touching wall + falling)
        return isWallSliding;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        
        if (wallCheck != null && rb != null)
        {
            // Draw wall check rays at different heights
            for (float yOffset = -wallSlideDetectionHeight/2; yOffset <= wallSlideDetectionHeight/2; yOffset += wallSlideDetectionHeight/3)
            {
                Vector2 checkPosition = (Vector2)wallCheck.position + new Vector2(0, yOffset);
                
                // Right wall check
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(checkPosition, Vector2.right * wallCheckDistance);
                
                // Left wall check
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(checkPosition, Vector2.left * wallCheckDistance);
            }
        }
    }
}