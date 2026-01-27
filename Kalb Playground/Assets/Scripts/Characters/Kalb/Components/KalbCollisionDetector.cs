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
    
    // State
    private bool isGrounded;
    private bool isTouchingWall;
    private int wallSide = 0; // -1 = left, 1 = right, 0 = none
    private Rigidbody2D rb;
    
    public bool IsGrounded => isGrounded;
    public bool IsTouchingWall => isTouchingWall;
    public int WallSide => wallSide;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    private void Update()
    {
        CheckGround();
    }
    
    private void CheckGround()
    {
        if (groundCheck == null) return;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, environmentLayer);
    }
    
    
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        
        if (wallCheck != null)
        {
            // Draw wall check rays
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(wallCheck.position, Vector2.right * wallCheckDistance);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(wallCheck.position, Vector2.left * wallCheckDistance);
        }
    }
}