using UnityEngine;

public class KalbAirState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbCollisionDetector collisionDetector;
    private KalbMovement movement;
    private KalbSwimming swimming;
    private KalbComboSystem comboSystem;
    private KalbSettings settings;
    
    // Fall tracking
    private float fallStartY = 0f;
    private float maxFallDistance = 0f;
    private bool hasStartedFalling = false;
    private bool wasFalling = false;
    
    public KalbAirState(KalbController controller, KalbStateMachine stateMachine) 
        : base(controller, stateMachine)
    {
        inputHandler = controller.InputHandler;
        collisionDetector = controller.CollisionDetector;
        movement = controller.Movement;
        swimming = controller.Swimming;
        comboSystem = controller.ComboSystem;
        settings = controller.Settings;
    }
    
    public override void Enter()
    {
        Debug.Log("[AirState] ENTER");
        
        // Initialize fall tracking
        fallStartY = controller.transform.position.y;
        maxFallDistance = 0f;
        hasStartedFalling = false;
        wasFalling = false;
        
        UpdateAnimation();
        controller.GravityManager.SetNormalGravity();
        controller.InputBuffer?.ClearBufferedInput("Jump");
        controller.InputBuffer?.ClearBufferedInput("Dash");
        controller.InputBuffer?.ClearBufferedInput("Attack");
    }
    
    public override void Update()
    {
        // Track fall distance
        float currentY = controller.transform.position.y;
        float verticalVelocity = controller.Rb.linearVelocity.y;
        
        // If we're moving downward, track the maximum fall distance
        if (verticalVelocity < -0.5f)
        {
            if (!hasStartedFalling)
            {
                // Just started falling
                fallStartY = currentY;
                hasStartedFalling = true;
                wasFalling = true;
                Debug.Log($"[AirState] Started falling from Y: {fallStartY}");
            }
            
            // Update maximum fall distance
            float currentFallDistance = fallStartY - currentY;
            if (currentFallDistance > maxFallDistance)
            {
                maxFallDistance = currentFallDistance;
            }
        }
        else if (verticalVelocity >= 0 && wasFalling)
        {
            // We're moving up again (like after a bounce) - reset fall tracking
            wasFalling = false;
            hasStartedFalling = false;
        }
        
        // Check for ledge state
        if (controller.AbilitySystem.CanLedgeGrab() && controller.LedgeDetector.LedgeDetected && !controller.IsEffectivelyGrounded() &&
            controller.Rb.linearVelocity.y < 0 && controller.Settings.ledgeGrabUnlocked)
        {
            float playerBottom = controller.GetComponent<Collider2D>().bounds.min.y;
            float ledgeTop = controller.LedgeDetector.LedgePosition.y;
            
            if (playerBottom < ledgeTop && playerBottom > ledgeTop - 1.0f)
            {
                Debug.Log("[AirState] Transition to LedgeState");
                stateMachine.ChangeState(controller.LedgeState);
                return;
            }
        }
        
        // Check for swimming transition
        if (swimming != null && swimming.IsInWater)
        {
            comboSystem?.CancelCombo();
            Debug.Log("[AirState] Transition to SwimState");
            stateMachine.ChangeState(controller.SwimState);
            return;
        }
        
        // CRITICAL: When landing, check if we should trigger hard landing
        if (controller.IsEffectivelyGrounded())
        {
            Debug.Log($"[AirState] Landed! Max fall distance: {maxFallDistance}, Threshold: {settings.hardLandingFallThreshold}");
            
            // Check if this qualifies as a hard landing
            if (settings.enableHardLanding && 
                maxFallDistance >= settings.hardLandingFallThreshold)
            {
                Debug.Log($"[AirState] HARD LANDING triggered! Distance: {maxFallDistance}");
                
                // Pass the fall distance to hard landing state and transition
                if (controller.HardLandState != null)
                {
                    controller.HardLandState.SetFallDistance(maxFallDistance);
                    stateMachine.ChangeState(controller.HardLandState);
                    return;
                }
            }
            
            // Normal landing
            Debug.Log("[AirState] Normal landing");
            if (Mathf.Abs(inputHandler.MoveInput.x) > 0.1f)
            {
                stateMachine.ChangeState(controller.WalkState);
            }
            else
            {
                stateMachine.ChangeState(controller.IdleState);
            }
            return;
        }
        
        UpdateAnimation();
    }
    
    public override void FixedUpdate()
    {
        // Apply air control
        movement.ApplyAirControl(inputHandler.MoveInput.x);
    }
    
    public override void HandleInput()
    {
        // Check for wall slide state
        if (!controller.Swimming.IsInWaterExitGracePeriod && controller.WallJump != null && controller.WallJump.IsWallSliding &&
            controller.AbilitySystem != null && controller.AbilitySystem.CanWallJump())
        {
            stateMachine.ChangeState(controller.WallSlideState);
            return;
        }

        // Check for pogo attack
        if (inputHandler.AttackPressed && inputHandler.IsDownHeld && 
            controller.Settings.enablePogoAttack && 
            controller.PogoAttackState != null && 
            controller.PogoAttackState.CanPogo)
        {
            controller.InputBuffer.BufferAttack();
            
            if (controller.InputBuffer.ConsumeBufferedInput("Attack"))
            {
                stateMachine.ChangeState(controller.PogoAttackState);
                inputHandler.ResetAttackInput();
                return;
            }
        }
        
        // Check for jump input (for coyote time or double jump)
        if (inputHandler.JumpPressed)
        {
            controller.Physics.SetJumpBuffer();
            
            // Check for double jump
            if (!controller.IsEffectivelyGrounded() &&
                controller.Physics.CanDoubleJump &&
                controller.AbilitySystem != null && 
                controller.AbilitySystem.CanDoubleJump())
            {
                ExecuteDoubleJump();
            }
        }
        
        if (inputHandler.JumpReleased)
        {
            controller.Physics.ApplyJumpCut();
        }

        // Check for float fall
        if (inputHandler.JumpHeld && controller.Rb.linearVelocity.y < -1f &&
            controller.FloatFallState != null && controller.FloatFallState.CanFloat)
        {
            if (!(stateMachine.CurrentState is KalbFloatFallState))
            {
                stateMachine.ChangeState(controller.FloatFallState);
                return;
            }
        }

        // Check for attack input
        if (inputHandler.AttackPressed && comboSystem != null && comboSystem.CanAttack)
        {
            // Will be handled by controller
        }
    }

    private void ExecuteDoubleJump()
    {
        controller.Physics.ResetDoubleJump();
        
        float currentXVelocity = controller.Rb.linearVelocity.x;
        float jumpForce = controller.Settings.doubleJumpForce;
        
        if (controller.Settings.doubleJumpMaintainsMomentum)
        {
            float speedRatio = Mathf.Clamp01(Mathf.Abs(currentXVelocity) / controller.Settings.moveSpeed);
            float momentumPreservation = Mathf.Lerp(0.7f, 1.0f, speedRatio);
            float preservedXVelocity = currentXVelocity * momentumPreservation;
            float playerControl = inputHandler.MoveInput.x * controller.Settings.moveSpeed * 0.3f;
            
            controller.Rb.linearVelocity = new Vector2(
                preservedXVelocity + playerControl,
                jumpForce
            );
        }
        else
        {
            float targetXVelocity = inputHandler.MoveInput.x * controller.Settings.moveSpeed * 0.7f;
            controller.Rb.linearVelocity = new Vector2(targetXVelocity, jumpForce);
        }
        
        controller.Physics.SetJumpButtonState(true);
        controller.AnimationController.PlayAnimation("Kalb_jump");
        controller.Physics.SetJumpBuffer();
        inputHandler.ResetJumpInput();
    }
    
    private void UpdateAnimation()
    {
        if (controller.Rb.linearVelocity.y > 0)
        {
            controller.AnimationController.PlayAnimation("Kalb_jump");
        }
        else
        {
            controller.AnimationController.PlayAnimation("Kalb_fall");
        }
    }
}