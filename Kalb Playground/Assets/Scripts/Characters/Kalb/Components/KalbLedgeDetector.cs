using UnityEngine;

public class KalbLedgeDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KalbController controller;
    [SerializeField] private Transform ledgeCheckPoint;
    [SerializeField] private Collider2D playerCollider;
    [SerializeField] private KalbCollisionDetector collisionDetector;
    
    [Header("Settings")]
    [SerializeField] private float ledgeGrabOffsetY = 0.15f;
    [SerializeField] private float ledgeGrabOffsetX = 0.55f;
    [SerializeField] private LayerMask environmentLayer;
    
    [Header("Cooldown Settings")]
    [SerializeField] private float ledgeGrabCooldown = 0.5f;
    [SerializeField] private float verticalReleaseThreshold = -2f;
    
    [Header("State")]
    private bool ledgeDetected = false;
    private Vector2 ledgePosition;
    private int ledgeSide = 0;
    private float lastLedgeReleaseTime = 0f;
    private bool isOnCooldown = false;
    
    public bool LedgeDetected => ledgeDetected && !isOnCooldown;
    public Vector2 LedgePosition => ledgePosition;
    public int LedgeSide => ledgeSide;
    public bool IsOnCooldown => isOnCooldown;
    public float CooldownRemaining => Mathf.Max(0, (lastLedgeReleaseTime + ledgeGrabCooldown) - Time.time);
    
    private void Awake()
    {
        if (controller == null) controller = GetComponent<KalbController>();
        if (playerCollider == null) playerCollider = GetComponent<Collider2D>();
        if (collisionDetector == null) collisionDetector = GetComponent<KalbCollisionDetector>();
    }
    
    private void Start()
    {
        // Create ledge check point if not assigned
        if (ledgeCheckPoint == null)
        {
            GameObject obj = new GameObject("LedgeCheck");
            obj.transform.parent = transform;
            obj.transform.localPosition = new Vector3(0, 0.5f, 0);
            ledgeCheckPoint = obj.transform;
        }
    }
    
    private void Update()
    {
        // Update cooldown
        if (isOnCooldown && Time.time > lastLedgeReleaseTime + ledgeGrabCooldown)
        {
            isOnCooldown = false;
        }
    }
    
    public bool CheckForLedge(KalbController controller)
    {
        // CRITICAL: Check if ledge grab ability is unlocked
        if (controller.AbilitySystem != null && !controller.AbilitySystem.CanLedgeGrab())
        {
            ledgeDetected = false;
            return false;
        }

        // FIXED: Don't check for ledge if in water exit grace period
        if (controller.Swimming.IsInWaterExitGracePeriod)
        {
            ledgeDetected = false;
            return false;
        }
        
        // Don't check if on cooldown
        if (isOnCooldown)
        {
            ledgeDetected = false;
            return false;
        }
        
        // Don't check if grounded or moving upward
        if (controller.IsEffectivelyGrounded() || controller.Rb.linearVelocity.y >= 0)
        {
            ledgeDetected = false;
            return false;
        }
        
        // Don't check if not falling fast enough (prevents instant re-grab)
        if (controller.Rb.linearVelocity.y > verticalReleaseThreshold)
        {
            ledgeDetected = false;
            return false;
        }
        
        // Don't check during certain states
        if (controller.DashState.IsDashing || controller.ComboSystem.IsAttacking || 
            controller.Swimming.IsSwimming || controller.Swimming.IsInWaterExitGracePeriod)
        {
            ledgeDetected = false;
            return false;
        }
        
        // Determine check direction based on facing and input
        float checkDirection = controller.FacingRight ? 1f : -1f;
        if (Mathf.Abs(controller.InputHandler.MoveInput.x) > 0.1f)
        {
            checkDirection = Mathf.Sign(controller.InputHandler.MoveInput.x);
        }
        
        ledgeSide = (int)checkDirection;
        
        // Calculate wall check position
        Vector2 playerCenter = playerCollider.bounds.center;
        float playerHalfWidth = playerCollider.bounds.extents.x;
        Vector2 wallCheckPos = new Vector2(
            playerCenter.x + (checkDirection * (playerHalfWidth + 0.05f)),
            playerCenter.y
        );
        
        // Cast vertical ray down from above to find ledge corner
        Vector2 verticalCheckStart = new Vector2(
            wallCheckPos.x + (checkDirection * 0.1f),
            playerCenter.y + playerCollider.bounds.extents.y + 1.0f
        );
        
        RaycastHit2D verticalHit = Physics2D.Raycast(
            verticalCheckStart,
            Vector2.down,
            2.0f,
            environmentLayer
        );
        
        if (verticalHit.collider == null)
        {
            ledgeDetected = false;
            return false;
        }
        
        // Found ledge surface
        ledgePosition = verticalHit.point;
        
        // Verify there's a wall at this position
        Vector2 wallVerificationStart = new Vector2(
            ledgePosition.x - (checkDirection * 0.1f),
            ledgePosition.y - 0.05f
        );
        
        RaycastHit2D wallVerificationHit = Physics2D.Raycast(
            wallVerificationStart,
            Vector2.right * checkDirection,
            0.2f,
            environmentLayer
        );
        
        if (wallVerificationHit.collider == null)
        {
            ledgeDetected = false;
            return false;
        }
        
        // Adjust to wall surface position
        ledgePosition.x = wallVerificationStart.x + (wallVerificationHit.distance * checkDirection);
        
        ledgeDetected = true;
        return true;
    }
    
    public Vector3 CalculateGrabPosition()
    {
        if (!ledgeDetected) return Vector3.zero;
        
        float playerHeight = playerCollider.bounds.size.y;
        float grabX = ledgePosition.x - (ledgeSide * ledgeGrabOffsetX);
        float grabY = ledgePosition.y - (playerHeight * ledgeGrabOffsetY);
        
        return new Vector3(grabX, grabY, transform.position.z);
    }
    
    public Vector3 CalculateClimbTarget()
    {
        if (!ledgeDetected) return Vector3.zero;
        
        float playerHeight = playerCollider.bounds.size.y;
        
        Vector2 rayStart = new Vector2(ledgePosition.x, ledgePosition.y + 0.1f);
        float rayLength = controller.Settings.climbSurfaceCheckDistance;
        
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.up, rayLength, environmentLayer);
        
        if (hit.collider != null)
        {
            float surfaceY = hit.point.y;
            float targetX = ledgePosition.x + (ledgeSide * controller.Settings.climbHorizontalBuffer);
            float feetOffset = playerHeight * 0.5f;
            
            return new Vector3(targetX, surfaceY + feetOffset, transform.position.z);
        }
        
        return new Vector3(
            ledgePosition.x + (ledgeSide * 0.3f),
            ledgePosition.y + playerHeight * 0.8f,
            transform.position.z
        );
    }
    
    // Call this when player releases from ledge
    public void StartCooldown()
    {
        isOnCooldown = true;
        lastLedgeReleaseTime = Time.time;
        ledgeDetected = false;
    }
    
    // Call this when player climbs successfully (no cooldown needed)
    public void ClearDetection()
    {
        ledgeDetected = false;
    }
    
    // Force stop cooldown (if needed)
    public void StopCooldown()
    {
        isOnCooldown = false;
    }

    public bool CanClimb()
    {
        if (!ledgeDetected) return false;
        return true;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (ledgeCheckPoint != null)
        {
            Gizmos.color = isOnCooldown ? Color.red : Color.magenta;
            Gizmos.DrawWireSphere(ledgeCheckPoint.position, 0.1f);
            
            if (ledgeDetected && Application.isPlaying)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(ledgePosition, new Vector3(0.3f, 0.1f, 0));
                
                Vector3 grabPos = CalculateGrabPosition();
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(grabPos, 0.15f);
            }
            
            if (isOnCooldown && Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.2f);
            }

            if (ledgeDetected && Application.isPlaying)
            {
                Vector3 climbTarget = CalculateClimbTarget();
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(climbTarget, 0.2f);
            }
        }
    }
}