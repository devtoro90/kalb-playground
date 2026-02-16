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
        
        // Set gravity for wall slide
        controller.GravityManager.SetNormalGravity();

        // Apply immediate force toward the wall to ensure contact
        if (wallJump != null && wallJump.WallSide != 0)
        {
            Vector2 wallPush = new Vector2(wallJump.WallSide * 2f, 0);
            rb.AddForce(wallPush, ForceMode2D.Impulse);
        }
        
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
        // Check if we should transition to wall lock
        if (controller.AbilitySystem.CanWallLock() && 
            controller.WallLockCooldownTimer <= 0 &&
            IsPushingTowardWall() && 
            !(stateMachine.CurrentState is KalbWallLockState))
        {
            stateMachine.ChangeState(controller.WallLockState);
            return;
        }
        
        // Check if we should exit wall slide
        if (!ShouldContinueWallSliding())
        {
            float currentDistanceToWall = wallJump.GetDistanceToWall();
            if (currentDistanceToWall > 0.3f)
            {
                ExitToAppropriateState();
                return;
            }
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
        
        // Update animation with slide speed parameter
        UpdateAnimation();
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
        
        // Check for attack input
        if (inputHandler.AttackPressed && controller.ComboSystem.CanAttack)
        {
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
        if (!wallJump.IsTouchingWall) return false;
        if (!wallJump.IsWallSliding) return false;
        if (controller.IsEffectivelyGrounded()) return false;
        if (swimming != null && swimming.IsSwimming) return false;
        if (controller.DashState.IsDashing) return false;
        if (controller.AbilitySystem != null && !controller.AbilitySystem.CanWallJump()) return false;
        
        return true;
    }
    
    private bool IsPushingTowardWall()
    {
        if (!wallJump.IsTouchingWall)
            return false;
        
        float inputDirection = Mathf.Sign(inputHandler.MoveInput.x);
        float wallSide = wallJump.WallSide;
        
        return Mathf.Abs(inputHandler.MoveInput.x) > controller.Settings.wallLockInputThreshold && 
            Mathf.Approximately(inputDirection, wallSide);
    }
    
    private void ExitToAppropriateState()
    {
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
        
        if (swimming != null && swimming.IsInWater)
        {
            stateMachine.ChangeState(controller.SwimState);
            return;
        }
        
        stateMachine.ChangeState(controller.AirState);
    }
    
    private void ExecuteWallJump()
    {
        if (wallJump == null) return;
        
        wallJump.ExecuteWallJump();
        controller.AnimationController.PlayAnimation("Kalb_jump");
        controller.Physics.SetJumpBuffer();
        inputHandler.ResetJumpInput();
        stateMachine.ChangeState(controller.AirState);
    }
    
    private void UpdateAnimation()
    {
        // Get slide speed ratio for animation blending
        float speedRatio = wallJump.SlideSpeedRatio;
        
        // You can use this to blend between different wall slide animations
        // or to control the speed of the slide animation
        controller.AnimationController.PlayAnimation("Kalb_wallslide");
        
        // Optional: Set animator parameter for slide speed
        // animator.SetFloat("SlideSpeed", speedRatio);
    }
}