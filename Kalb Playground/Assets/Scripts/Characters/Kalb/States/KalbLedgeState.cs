using UnityEngine;

public class KalbLedgeState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbLedgeDetector ledgeDetector;
    private Rigidbody2D rb;
    private KalbMovement movement;
    private KalbCollisionDetector collisionDetector;
    private KalbPhysics physics;
    
    // Ledge state
    private Vector2 ledgePosition;
    private int ledgeSide = 0;
    private float currentLedgeHoldTime = 0f;
    private const float MIN_LEDGE_HOLD_TIME = 0.3f;
    private Vector3 grabPosition;
    private bool isReleasing = false;
    
    public bool IsLedgeGrabbing { get; private set; }
    public float CurrentLedgeHoldTime => currentLedgeHoldTime;
    
    public KalbLedgeState(KalbController controller, KalbStateMachine stateMachine) 
        : base(controller, stateMachine)
    {
        inputHandler = controller.InputHandler;
        ledgeDetector = controller.LedgeDetector;
        rb = controller.Rb;
        movement = controller.Movement;
        collisionDetector = controller.CollisionDetector;
        physics = controller.Physics;
    }
    
    public override void Enter()
    {
        IsLedgeGrabbing = true;
        isReleasing = false;
        
        // Get ledge data from detector
        ledgePosition = ledgeDetector.LedgePosition;
        ledgeSide = ledgeDetector.LedgeSide;
        currentLedgeHoldTime = 0f;
        
        // COMPLETELY STOP ALL PHYSICS
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f; // CRITICAL: Zero gravity
        
        // Calculate exact grab position
        grabPosition = ledgeDetector.CalculateGrabPosition();
        
        // SNAP INSTANTLY to grab position - NO LERP, NO SMOOTHING
        controller.transform.position = grabPosition;
        
        // Face the wall
        bool shouldFaceRight = ledgeSide == 1;
        if (shouldFaceRight != movement.FacingRight)
        {
            movement.ForceFlip(shouldFaceRight);
        }
        
        // Cancel any ongoing actions
        controller.ComboSystem.CancelCombo();
        physics.ResetJumpState(); // Reset jump buffers
        
        // Reset input to prevent carry-over
        inputHandler.ResetJumpInput();
        
        // Play grab animation
        controller.AnimationController.PlayAnimation("Kalb_ledge_grab");
        
        
    }
    
    public override void Exit()
    {
        IsLedgeGrabbing = false;
        isReleasing = false;
        
        // RESTORE PHYSICS PROPERLY
        rb.gravityScale = controller.Settings.normalGravityScale;
        
        currentLedgeHoldTime = 0f;
        
       
    }
    
    public override void Update()
    {
        // Update hold time
        currentLedgeHoldTime += Time.deltaTime;
        
        // If already releasing, skip other checks
        if (isReleasing) return;
        
        // Check if we should release (e.g., fell off or grounded)
        if (!ledgeDetector.LedgeDetected || collisionDetector.IsGrounded)
        {
            // Only auto-release if we're significantly away from ledge
            float distanceToLedge = Vector2.Distance(controller.transform.position, grabPosition);
            if (distanceToLedge > 0.3f || collisionDetector.IsGrounded)
            {
                ReleaseLedge();
                return;
            }
        }
    }
    
    public override void FixedUpdate()
    {
        // If releasing, let physics take over
        if (isReleasing) return;
        
        // CRITICAL: Keep position absolutely locked during grab
        rb.linearVelocity = Vector2.zero;
        rb.MovePosition(grabPosition); // Force position in physics update too
    }
    
    public override void HandleInput()
    {
        // If already releasing, skip input
        if (isReleasing) return;
        
        // Don't accept climb input until minimum hold time has passed
        bool canAcceptClimbInput = currentLedgeHoldTime >= MIN_LEDGE_HOLD_TIME;
        
        // Get raw input for more responsive release
        float horizontalInput = inputHandler.MoveInput.x;
        float verticalInput = inputHandler.MoveInput.y;
        
        // DEBUG: Log input for troubleshooting
        if (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f)
        {
            
        }
        
        // CLIMB UP: Press Up or towards the ledge
        if (canAcceptClimbInput && (verticalInput > 0.5f || (Mathf.Sign(horizontalInput) == ledgeSide && Mathf.Abs(horizontalInput) > 0.5f)))
        {
            ClimbLedge();
            return;
        }
        
        // RELEASE: Press Down or away from ledge - MORE PERMISSIVE THRESHOLD
        bool pressingDown = verticalInput < -0.3f; // Reduced threshold
        bool pressingAway = Mathf.Sign(horizontalInput) == -ledgeSide && Mathf.Abs(horizontalInput) > 0.3f; // Reduced threshold
        
        if (pressingDown || pressingAway)
        {
            
            ReleaseLedge();
            return;
        }
        
        // JUMP AWAY: Press jump button
        if (inputHandler.JumpPressed)
        {
            LedgeJump();
            return;
        }
    }
    
    private void ClimbLedge()
    {
       
        
        // Clear detection but NO COOLDOWN for successful climb
        if (ledgeDetector != null)
        {
            ledgeDetector.ClearDetection();
        }
        
        stateMachine.ChangeState(controller.LedgeClimbState);
    }
    
    private void ReleaseLedge()
    {
        if (isReleasing) return;
        
        isReleasing = true;
       
        
        // START COOLDOWN to prevent immediate re-grab
        if (ledgeDetector != null)
        {
            ledgeDetector.StartCooldown();
        }
        
        // Restore gravity IMMEDIATELY
        rb.gravityScale = controller.Settings.normalGravityScale;
        
        // Calculate release velocity - push away from wall and down
        float releaseHorizontal = -ledgeSide * controller.Settings.ledgeReleaseForce * 0.5f;
        float releaseVertical = -controller.Settings.ledgeReleaseForce * 0.8f;
        
        
        
        // Apply release force
        rb.linearVelocity = new Vector2(releaseHorizontal, releaseVertical);
        
        // Force state change to AirState
        stateMachine.ChangeState(controller.AirState);
    }

    private void LedgeJump()
    {
        if (isReleasing) return;
        
        isReleasing = true;
        
        // START COOLDOWN to prevent immediate re-grab
        if (ledgeDetector != null)
        {
            ledgeDetector.StartCooldown();
        }
        
        // Restore gravity IMMEDIATELY
        rb.gravityScale = controller.Settings.normalGravityScale;
        
        // Apply jump force away from wall
        Vector2 jumpDir = new Vector2(
            -ledgeSide * controller.Settings.ledgeJumpAngle.x,
            controller.Settings.ledgeJumpAngle.y
        ).normalized;
        
        float jumpForce = controller.Settings.ledgeJumpForce;
        rb.linearVelocity = jumpDir * jumpForce;
        
        // Face away from wall
        bool shouldFaceRight = ledgeSide == -1;
        if (shouldFaceRight != movement.FacingRight)
        {
            movement.ForceFlip(shouldFaceRight);
        }
        
        // Reset abilities
        controller.DashState.ResetAirDash();
        physics.ResetJumpState();
        
        // Enable double jump after ledge jump
        if (controller.AbilitySystem != null && controller.AbilitySystem.CanDoubleJump())
        {
            
            physics.ResetDoubleJump(); // Clear any previous double jump
            physics.SetCanDoubleJump(true); // Enable for next jump
        }
        
        // Force state change to AirState
        stateMachine.ChangeState(controller.AirState);
    }
}