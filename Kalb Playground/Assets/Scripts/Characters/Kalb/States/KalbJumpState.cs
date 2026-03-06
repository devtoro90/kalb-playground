using UnityEngine;

public class KalbJumpState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbCollisionDetector collisionDetector;
    private KalbMovement movement;
    private KalbPhysics physics;
    private KalbSwimming swimming;
    private KalbSettings settings;
    
    // Fall tracking
    private float fallStartY = 0f;
    private float maxFallDistance = 0f;
    private bool hasStartedFalling = false;
    
    public KalbJumpState(KalbController controller, KalbStateMachine stateMachine) 
        : base(controller, stateMachine)
    {
        inputHandler = controller.InputHandler;
        collisionDetector = controller.CollisionDetector;
        movement = controller.Movement;
        physics = controller.Physics;
        swimming = controller.Swimming;
        settings = controller.Settings;
    }
    
    public override void Enter()
    {
        
        
        // Initialize fall tracking
        fallStartY = controller.transform.position.y;
        maxFallDistance = 0f;
        hasStartedFalling = false;
        
        // Ensure gravity is properly set
        if (controller.Rb.gravityScale != controller.Settings.normalGravityScale)
        {
            controller.Rb.gravityScale = controller.Settings.normalGravityScale;
        }

        controller.GravityManager.SetNormalGravity();
        controller.AnimationController.PlayAnimation("Kalb_jump");
        controller.ComboSystem?.CancelCombo();
        
        // Apply jump
        physics.Jump(controller.Settings.jumpForce);
        physics.SetJumpButtonState(true);

        // Enable double jump if unlocked
        if (controller.AbilitySystem != null && controller.AbilitySystem.CanDoubleJump())
        {
            physics.SetCanDoubleJump(true);
        }

        controller.InputBuffer?.ClearBufferedInput("Jump");
        controller.InputBuffer?.ClearBufferedInput("Dash");
        controller.InputBuffer?.ClearBufferedInput("Attack");
    }
    
    public override void Exit()
    {
        physics.SetJumpButtonState(false);

        if (controller.FloatFallState != null)
        {
            controller.FloatFallState.ResetFloat();
        }
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
                
            }
            
            // Update maximum fall distance
            float currentFallDistance = fallStartY - currentY;
            if (currentFallDistance > maxFallDistance)
            {
                maxFallDistance = currentFallDistance;
            }
        }
        
        // Check for ledge state
        if (controller.AbilitySystem.CanLedgeGrab() && controller.LedgeDetector.LedgeDetected && !controller.IsEffectivelyGrounded() && 
            controller.Rb.linearVelocity.y < 0 && controller.Settings.ledgeGrabUnlocked)
        {
            float playerBottom = controller.GetComponent<Collider2D>().bounds.min.y;
            float ledgeTop = controller.LedgeDetector.LedgePosition.y;
            
            if (playerBottom < ledgeTop && playerBottom > ledgeTop - 1.0f)
            {
                
                stateMachine.ChangeState(controller.LedgeState);
                return;
            }
        }

        // Check for wall slide transition
        if (controller.WallJump != null && controller.WallJump.IsWallSliding &&
            controller.AbilitySystem != null && controller.AbilitySystem.CanWallJump())
        {
            
            stateMachine.ChangeState(controller.WallSlideState);
            return;
        }
        
        // Check for swimming transition
        if (swimming != null && swimming.IsInWater)
        {
            
            stateMachine.ChangeState(controller.SwimState);
            return;
        }
        
        // CRITICAL: When landing, check if we should trigger hard landing
        if (controller.IsEffectivelyGrounded())
        {
            
            
            // Check if this qualifies as a hard landing
            if (settings.enableHardLanding && 
                maxFallDistance >= settings.hardLandingFallThreshold)
            {
                
                
                // Pass the fall distance to hard landing state and transition
                if (controller.HardLandState != null)
                {
                    controller.HardLandState.SetFallDistance(maxFallDistance);
                    stateMachine.ChangeState(controller.HardLandState);
                    return;
                }
            }
            
            // Normal landing
            
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
        
        // Check vertical velocity for animation
        if (controller.Rb.linearVelocity.y < 0)
        {
            controller.AnimationController.PlayAnimation("Kalb_fall");
            // Transition to air state when falling
            if (!(stateMachine.CurrentState is KalbAirState))
            {
                stateMachine.ChangeState(controller.AirState);
            }
        }
    }
    
    public override void FixedUpdate()
    {
        movement.ApplyAirControl(inputHandler.MoveInput.x);
    }
    
    public override void HandleInput()
    {
        if (inputHandler.JumpHeld)
        {
            physics.SetJumpButtonState(true);
        }
        else if (inputHandler.JumpReleased)
        {
            physics.SetJumpButtonState(false);
            physics.ApplyJumpCut();
        }

        if (controller.WallJump != null && controller.WallJump.IsWallSliding &&
            controller.AbilitySystem != null && controller.AbilitySystem.CanWallJump())
        {
            stateMachine.ChangeState(controller.WallSlideState);
        }
    }
}