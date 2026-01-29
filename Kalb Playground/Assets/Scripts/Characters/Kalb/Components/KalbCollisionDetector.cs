using UnityEngine;

public class KalbCollisionDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform ceilingCheck; 
    
    [Header("Settings")]
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private float ceilingCheckRadius = 0.15f;
    [SerializeField] private LayerMask environmentLayer;
    
    // State
    private bool isGrounded;
    private bool isTouchingCeiling;
    private int wallSide = 0; // -1 = left, 1 = right, 0 = none
    private Rigidbody2D rb;
    
    public bool IsGrounded => isGrounded;
    public bool IsTouchingCeiling => isTouchingCeiling;
    public int WallSide => wallSide;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    private void Update()
    {
        CheckGround();
        CheckCeiling();
    }
    
    private void CheckGround()
    {
        if (groundCheck == null) return;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, environmentLayer);
    }

    private void CheckCeiling()
    {
        if (ceilingCheck == null) return;
        isTouchingCeiling = Physics2D.OverlapCircle(ceilingCheck.position, ceilingCheckRadius, environmentLayer);
    }
    
    
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (ceilingCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(ceilingCheck.position, ceilingCheckRadius);
        }
    }
}