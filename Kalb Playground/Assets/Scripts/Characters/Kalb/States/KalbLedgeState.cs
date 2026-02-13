using UnityEngine;

public class KalbLedgeState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbLedgeDetector ledgeDetector;
    private Rigidbody2D rb;
    private KalbMovement movement;
    private KalbPhysics physics;
    
    // Ledge state
    private Vector2 ledgePosition;
    private int ledgeSide = 0;
    private float currentLedgeHoldTime = 0f;
    private const float MIN_LEDGE_HOLD_TIME = 0.3f;
    private Vector3 grabPosition;
    private bool isReleasing = false;
    
    public bool IsLedgeGrabbing { get; private set; }
    
    public KalbLedgeState(KalbController controller, KalbStateMachine stateMachine) 
        : base(controller, stateMachine)
    {
        inputHandler = controller.InputHandler;
        ledgeDetector = controller.LedgeDetector;
        rb = controller.Rb;
        movement = controller.Movement;
        physics = controller.Physics;
    }
    
    public override void Enter()
    {
        IsLedgeGrabbing = true;
        isReleasing = false;
        
        ledgePosition = ledgeDetector.LedgePosition;
        ledgeSide = ledgeDetector.LedgeSide;
        currentLedgeHoldTime = 0f;
        
        rb.linearVelocity = Vector2.zero;
        controller.GravityManager.SetZeroGravity("LedgeGrab");
        
        grabPosition = ledgeDetector.CalculateGrabPosition();
        controller.transform.position = grabPosition;
        
        bool shouldFaceRight = ledgeSide == 1;
        if (shouldFaceRight != movement.FacingRight)
        {
            movement.ForceFlip(shouldFaceRight);
        }
        
        controller.ComboSystem.CancelCombo();
        physics.ResetJumpState();
        inputHandler.ResetJumpInput();
        
        controller.AnimationController.PlayAnimation("Kalb_ledge_grab");

        controller.InputBuffer?.ClearBufferedInput("Jump");
        controller.InputBuffer?.ClearBufferedInput("Dash");
        controller.InputBuffer?.ClearBufferedInput("Attack");
    }
    
    public override void Exit()
    {
        IsLedgeGrabbing = false;
        isReleasing = false;
        
        rb.gravityScale = controller.Settings.normalGravityScale;
        currentLedgeHoldTime = 0f;
    }
    
    public override void Update()
    {
        currentLedgeHoldTime += Time.deltaTime;
        
        if (isReleasing) return;
        
        if (!ledgeDetector.LedgeDetected || controller.IsEffectivelyGrounded())
        {
            float distanceToLedge = Vector2.Distance(controller.transform.position, grabPosition);
            if (distanceToLedge > 0.3f || controller.IsEffectivelyGrounded())
            {
                ReleaseLedge();
                return;
            }
        }
    }
    
    public override void FixedUpdate()
    {
        if (isReleasing) return;
        
        rb.linearVelocity = Vector2.zero;
        rb.MovePosition(grabPosition);
    }
    
    public override void HandleInput()
    {
        if (isReleasing) return;
        
        bool canAcceptClimbInput = currentLedgeHoldTime >= MIN_LEDGE_HOLD_TIME;
        float horizontalInput = inputHandler.MoveInput.x;
        float verticalInput = inputHandler.MoveInput.y;
        
        if (canAcceptClimbInput && (verticalInput > 0.5f || (Mathf.Sign(horizontalInput) == ledgeSide && Mathf.Abs(horizontalInput) > 0.5f)))
        {
            ClimbLedge();
            return;
        }
        
        bool pressingDown = verticalInput < -0.3f;
        bool pressingAway = Mathf.Sign(horizontalInput) == -ledgeSide && Mathf.Abs(horizontalInput) > 0.3f;
        
        if (pressingDown || pressingAway)
        {
            ReleaseLedge();
            return;
        }
        
        if (inputHandler.JumpPressed)
        {
            LedgeJump();
            return;
        }
    }
    
    private void ClimbLedge()
    {
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

        if (ledgeDetector != null)
        {
            ledgeDetector.StartCooldown();
        }
        
        // CRITICAL: Set wall slide cooldown to prevent immediate wall slide
        if (controller.WallJump != null)
        {
            controller.WallJump.SetWallSlideCooldown();
        }
        
        controller.GravityManager.ClearOverride("LedgeGrab");
        
        // Apply release force - ALWAYS go to fall, never to wall slide
        float releaseHorizontal = -ledgeSide * controller.Settings.ledgeReleaseForce * 0.5f;
        float releaseVertical = -controller.Settings.ledgeReleaseForce * 0.8f;
        
        rb.linearVelocity = new Vector2(releaseHorizontal, releaseVertical);
        
        // Force state change to AirState (fall)
        stateMachine.ChangeState(controller.AirState);
    }

    private void LedgeJump()
    {
        if (isReleasing) return;
        
        isReleasing = true;
        
        if (ledgeDetector != null)
        {
            ledgeDetector.StartCooldown();
        }
        
        // Set wall slide cooldown
        if (controller.WallJump != null)
        {
            controller.WallJump.SetWallSlideCooldown();
        }
        
        rb.gravityScale = controller.Settings.normalGravityScale;
        
        Vector2 jumpDir = new Vector2(
            -ledgeSide * controller.Settings.ledgeJumpAngle.x,
            controller.Settings.ledgeJumpAngle.y
        ).normalized;
        
        float jumpForce = controller.Settings.ledgeJumpForce;
        rb.linearVelocity = jumpDir * jumpForce;
        
        bool shouldFaceRight = ledgeSide == -1;
        if (shouldFaceRight != movement.FacingRight)
        {
            movement.ForceFlip(shouldFaceRight);
        }

        controller.DashState.ResetAirDash();
        physics.ResetJumpState();
        
        if (controller.AbilitySystem != null && controller.AbilitySystem.CanDoubleJump())
        {
            physics.ResetDoubleJump();
            physics.SetCanDoubleJump(true);
        }
        
        stateMachine.ChangeState(controller.AirState);
    }
}