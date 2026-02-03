using UnityEngine;

public class KalbWallSlideState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbMovement movement;
    private KalbWallJump wallJump;
    private Rigidbody2D rb;
    private KalbPhysics physics;
    private KalbCollisionDetector collisionDetector;
    private KalbSwimming swimming;
    
    public KalbWallSlideState(KalbController controller, KalbStateMachine stateMachine) 
        : base(controller, stateMachine)
    {
        inputHandler = controller.InputHandler;
        movement = controller.Movement;
        wallJump = controller.WallJump;
        rb = controller.Rb;
        physics = controller.Physics;
        collisionDetector = controller.CollisionDetector;
        swimming = controller.Swimming;
    }
    
    public override void Enter()
    {
        // Play wall slide animation
        controller.AnimationController.PlayAnimation("Kalb_wallslide");
        
        // Stop horizontal movement when starting wall slide
        movement.StopHorizontalMovement();

        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        // Set gravity for wall slide
        controller.GravityManager.SetNormalGravity();
        
        // Reset input buffer
        controller.InputBuffer?.ClearBufferedInput("Jump");
        controller.InputBuffer?.ClearBufferedInput("Dash");
        controller.InputBuffer?.ClearBufferedInput("Attack");
    }
    
    public override void Exit()
    {
        // Reset any wall slide specific state if needed
    }
    
    public override void Update()
    {
        // Check if we should exit wall slide
        if (!ShouldContinueWallSliding())
        {
            ExitToAppropriateState();
            return;
        }
        
        // Check for ledge (higher priority than wall slide)
        if (controller.LedgeDetector.LedgeDetected && !controller.IsEffectivelyGrounded() &&
            rb.linearVelocity.y < 0 && controller.Settings.ledgeGrabUnlocked)
        {
            float playerBottom = controller.GetComponent<Collider2D>().bounds.min.y;
            float ledgeTop = controller.LedgeDetector.LedgePosition.y;
            
            if (playerBottom < ledgeTop && playerBottom > ledgeTop - 1.0f)
            {
                stateMachine.ChangeState(controller.LedgeState);
                return;
            }
        }
        
        // Check for swimming
        if (swimming != null && swimming.IsInWater)
        {
            stateMachine.ChangeState(controller.SwimState);
            return;
        }
        
        // Update animation
        controller.AnimationController.PlayAnimation("Kalb_wallslide");
    }
    
    public override void FixedUpdate()
    {
        // Wall sliding physics is handled by KalbWallJump component
        // This state just ensures we're in the right state for animation
    }
    
    public override void HandleInput()
    {
        // Check for wall jump input
        if (inputHandler.JumpPressed && wallJump != null && wallJump.CanWallJump())
        {
            controller.Physics.SetJumpBuffer();
            
            if (controller.InputBuffer?.ConsumeBufferedInput("Jump") == true)
            {
                ExecuteWallJump();
                return;
            }
        }
        
        // Check for dash input (only if ability unlocked)
        if (inputHandler.DashPressed && controller.AbilitySystem.CanDash())
        {
            if (controller.CanDashFromCurrentState() && controller.DashCooldownTimer <= 0)
            {
                controller.InputBuffer.BufferDash();
                
                if (controller.InputBuffer.ConsumeBufferedInput("Dash"))
                {
                    stateMachine.ChangeState(controller.DashState);
                    inputHandler.ResetDashInput();
                }
            }
        }
        
        // Check for attack input (optional - can be disabled if you prefer)
        if (inputHandler.AttackPressed && controller.ComboSystem.CanAttack)
        {
            // Allow attacking while wall sliding
            if (controller.CanAttackFromCurrentState())
            {
                controller.InputBuffer.BufferAttack();
                
                if (controller.InputBuffer.ConsumeBufferedInput("Attack"))
                {
                    stateMachine.ChangeState(controller.CombatState);
                    inputHandler.ResetAttackInput();
                }
            }
        }
    }
    
    private bool ShouldContinueWallSliding()
    {
        // Must meet all wall slide conditions:
        // 1. Must be touching wall
        if (!wallJump.IsTouchingWall) return false;
        
        // 2. Must be wall sliding (not just touching)
        if (!wallJump.IsWallSliding) return false;
        
        // 3. Must not be grounded
        if (controller.IsEffectivelyGrounded()) return false;
        
        // 4. Must not be swimming
        if (swimming != null && swimming.IsSwimming) return false;
        
        // 5. Must not be dashing
        if (controller.DashState.IsDashing) return false;
        
        // 6. Check if wall jump/slide ability is unlocked
        if (controller.AbilitySystem != null && !controller.AbilitySystem.CanWallJump()) // NEW
        {
            return false;
        }
        // 7. Must not be attacking (if you want to disable during attack)
        // if (controller.ComboSystem.IsAttacking) return false;

return true;
    }
    
    private void ExitToAppropriateState()
    {
        // Check if we're grounded
        if (controller.IsEffectivelyGrounded())
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
        
        // Check if we're swimming
        if (swimming != null && swimming.IsInWater)
        {
            stateMachine.ChangeState(controller.SwimState);
            return;
        }
        
        // Otherwise, go to air state
        stateMachine.ChangeState(controller.AirState);
    }
    
    private void ExecuteWallJump()
    {
        if (wallJump == null) return;
        
        wallJump.ExecuteWallJump();
        
        // Play jump animation
        controller.AnimationController.PlayAnimation("Kalb_jump");
        
        // Reset jump buffer
        controller.Physics.SetJumpBuffer();
        inputHandler.ResetJumpInput();
        
        // Transition to air state after wall jump
        stateMachine.ChangeState(controller.AirState);
    }
}