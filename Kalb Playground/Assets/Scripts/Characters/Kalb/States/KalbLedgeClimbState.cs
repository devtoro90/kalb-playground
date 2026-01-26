using UnityEngine;

public class KalbLedgeClimbState : KalbState
{
    private KalbLedgeDetector ledgeDetector;
    private Rigidbody2D rb;
    private KalbPhysics physics;
    private Collider2D playerCollider;
    
    // Climb state
    private float ledgeClimbTimer = 0f;
    private int ledgeSide = 0;
    private bool isFinishing = false;
    
    public bool IsLedgeClimbing { get; private set; }
    public float LedgeClimbTimer => ledgeClimbTimer;
    
    public KalbLedgeClimbState(KalbController controller, KalbStateMachine stateMachine) 
        : base(controller, stateMachine)
    {
        ledgeDetector = controller.LedgeDetector;
        rb = controller.Rb;
        physics = controller.Physics;
        playerCollider = controller.GetComponent<Collider2D>();
    }
    
    public override void Enter()
    {
        IsLedgeClimbing = true;
        isFinishing = false;
        ledgeClimbTimer = controller.Settings.ledgeClimbTime;
        ledgeSide = ledgeDetector.LedgeSide;
        
        // CRITICAL: IMMEDIATELY position player above platform (like original script)
        // This is the key fix - position first, then animate
        Vector3 climbTargetPosition = CalculateSimpleClimbPosition();
        
        // Position player immediately (no smooth transition)
        controller.transform.position = climbTargetPosition;
        
        // DISABLE PHYSICS during climb
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        
        // Reset physics state
        physics.ResetJumpState();
        
        // Play climb animation - animation will handle the visual climb
        controller.AnimationController.PlayAnimation("Kalb_ledge_climb");
        
        
    }
    
    public override void Exit()
    {
        IsLedgeClimbing = false;
        isFinishing = false;
        
        // RESTORE PHYSICS PROPERLY
        rb.gravityScale = controller.Settings.normalGravityScale;
        
        // Clear ledge detection after successful climb
        if (ledgeDetector != null)
        {
            ledgeDetector.ClearDetection();
            ledgeDetector.StopCooldown(); // No cooldown for successful climb
        }
        
       
    }
    
    public override void Update()
    {
        // Update climb timer
        if (ledgeClimbTimer > 0)
        {
            ledgeClimbTimer -= Time.deltaTime;
            
            // Check if climb is finished
            if (ledgeClimbTimer <= 0 && !isFinishing)
            {
                FinishClimb();
            }
        }
    }
    
    public override void FixedUpdate()
    {
        // NO POSITION SMOOTHING - player is already positioned
        // Just ensure physics is disabled
        if (IsLedgeClimbing)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }
    }
    
    public override void HandleInput()
    {
        // No input during climb animation - it's automatic
    }
    
    private Vector3 CalculateSimpleClimbPosition()
    {
        // SIMPLE POSITION: Just put player above the ledge
        float playerHeight = playerCollider.bounds.size.y;
        
        // Move slightly away from wall and above ledge
        float targetX = ledgeDetector.LedgePosition.x + (ledgeSide * 0.4f);
        float targetY = ledgeDetector.LedgePosition.y + (playerHeight * 0.5f);
        
        return new Vector3(targetX, targetY, controller.transform.position.z);
    }
    
    private void FinishClimb()
    {
        if (isFinishing) return;
        
        isFinishing = true;
       
        
        // Restore physics
        rb.gravityScale = controller.Settings.normalGravityScale;
        
        // Small upward hop at the end for smoother transition (matches original)
        rb.linearVelocity = new Vector2(0, 2f);
        
        // Reset physics state
        physics.ResetJumpState();
        
        // Transition immediately to appropriate state
        TransitionAfterClimb();
    }
    
    private void TransitionAfterClimb()
    {
        // Check input to decide next state (matches original logic)
        if (Mathf.Abs(controller.InputHandler.MoveInput.x) > 0.1f)
        {
            stateMachine.ChangeState(controller.WalkState);
        }
        else
        {
            stateMachine.ChangeState(controller.IdleState);
        }
        
       
    }
}