using UnityEngine;

public class KalbAirState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbCollisionDetector collisionDetector;
    private KalbMovement movement;
    private KalbSwimming swimming;
    private KalbComboSystem comboSystem;
    
    public KalbAirState(KalbController controller, KalbStateMachine stateMachine) 
        : base(controller, stateMachine)
    {
        inputHandler = controller.InputHandler;
        collisionDetector = controller.CollisionDetector;
        movement = controller.Movement;
        swimming = controller.Swimming;
        comboSystem = controller.ComboSystem;
    }
    
    public override void Enter()
    {
        UpdateAnimation();
        controller.GravityManager.SetNormalGravity();
        controller.InputBuffer?.ClearBufferedInput("Jump");
        controller.InputBuffer?.ClearBufferedInput("Dash");
        controller.InputBuffer?.ClearBufferedInput("Attack");
    }
    
    public override void Update()
    {
        // Check for ledge state
        if (controller.AbilitySystem.CanLedgeGrab() && controller.LedgeDetector.LedgeDetected && !controller.IsEffectivelyGrounded() && // CHANGED
            controller.Rb.linearVelocity.y < 0 && controller.Settings.ledgeGrabUnlocked)
        {
            // Check if we should auto-grab
            float playerBottom = controller.GetComponent<Collider2D>().bounds.min.y;
            float ledgeTop = controller.LedgeDetector.LedgePosition.y;
            
            if (playerBottom < ledgeTop && playerBottom > ledgeTop - 1.0f)
            {
                stateMachine.ChangeState(controller.LedgeState);
                return;
            }
        }
        
        // Check for swimming transition
        if (swimming != null && swimming.IsInWater)
        {
            // Cancel combo when entering swim state
            comboSystem?.CancelCombo(); 
            stateMachine.ChangeState(controller.SwimState);
            return;
        }
        
        if (controller.IsEffectivelyGrounded()) // CHANGED
        {
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
        // Apply air control - this will handle flipping if enabled
        movement.ApplyAirControl(inputHandler.MoveInput.x);
    }
    
    public override void HandleInput()
    {
        // Check for wall slide state (ADD THIS at the beginning)
        if (controller.WallJump != null && controller.WallJump.IsWallSliding &&
        controller.AbilitySystem != null && controller.AbilitySystem.CanWallJump())
        {
            stateMachine.ChangeState(controller.WallSlideState);
            return;
        }

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
            if (!controller.IsEffectivelyGrounded() && // CHANGED
                controller.Physics.CanDoubleJump &&
                controller.AbilitySystem != null && 
                controller.AbilitySystem.CanDoubleJump())
            {
                
                // Execute double jump
                ExecuteDoubleJump();
            }
        }
        
        if (inputHandler.JumpReleased)
        {
            
            controller.Physics.ApplyJumpCut();
        }

        if (inputHandler.JumpHeld && controller.Rb.linearVelocity.y < -1f &&
        controller.FloatFallState != null && controller.FloatFallState.CanFloat)
        {
            // Only enter if we're not already in float state
            if (!(stateMachine.CurrentState is KalbFloatFallState))
            {
                
                stateMachine.ChangeState(controller.FloatFallState);
                return;
            }
        }

        // Check for attack input in air state
        if (inputHandler.AttackPressed && comboSystem != null && comboSystem.CanAttack)
        {
            // Don't check swimming state here - let controller handle it
            // This allows attacks in air after water jumps
        }
    }

    private void ExecuteDoubleJump()
    {
        // Mark as double jumped
        controller.Physics.ResetDoubleJump();
        
        // Get current velocity and preserve momentum
        float currentXVelocity = controller.Rb.linearVelocity.x;
        float jumpForce = controller.Settings.doubleJumpForce;
        
        // Hollow Knight-style double jump: preserves momentum but allows redirection
        if (controller.Settings.doubleJumpMaintainsMomentum)
        {
            // Current speed ratio (0-1)
            float speedRatio = Mathf.Clamp01(Mathf.Abs(currentXVelocity) / controller.Settings.moveSpeed);
            
            // Preserve 70-100% of horizontal momentum based on speed
            float momentumPreservation = Mathf.Lerp(0.7f, 1.0f, speedRatio);
            float preservedXVelocity = currentXVelocity * momentumPreservation;
            
            // Allow player to add up to 30% new direction
            float playerControl = inputHandler.MoveInput.x * controller.Settings.moveSpeed * 0.3f;
            
            controller.Rb.linearVelocity = new Vector2(
                preservedXVelocity + playerControl,
                jumpForce
            );
        }
        else
        {
            // Standard double jump with full player control
            float targetXVelocity = inputHandler.MoveInput.x * controller.Settings.moveSpeed * 0.7f;
            controller.Rb.linearVelocity = new Vector2(targetXVelocity, jumpForce);
        }
        
        controller.Physics.SetJumpButtonState(true);
        
        // Play double jump animation
        controller.AnimationController.PlayAnimation("Kalb_jump");
        
        // Reset jump buffer
        controller.Physics.SetJumpBuffer();
        inputHandler.ResetJumpInput();
    }
    
    private void UpdateAnimation()
    {
        // Update animation based on vertical velocity
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