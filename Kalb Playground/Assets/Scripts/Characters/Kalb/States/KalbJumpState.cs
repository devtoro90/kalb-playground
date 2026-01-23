using UnityEngine;

public class KalbJumpState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbCollisionDetector collisionDetector;
    private KalbMovement movement;
    private KalbPhysics physics;
    private KalbSwimming swimming;
    
    public KalbJumpState(KalbController controller, KalbStateMachine stateMachine) 
        : base(controller, stateMachine)
    {
        inputHandler = controller.InputHandler;
        collisionDetector = controller.CollisionDetector;
        movement = controller.Movement;
        physics = controller.Physics;
        swimming = controller.Swimming;
    }
    
    public override void Enter()
    {
        controller.AnimationController.PlayAnimation("Kalb_jump");
        
        // Perform jump
        physics.Jump(controller.Settings.jumpForce);
        physics.SetJumpButtonState(true);
    }
    
    public override void Exit()
    {
        physics.SetJumpButtonState(false);
    }
    
    public override void Update()
    {
        // Check for swimming transition
        if (swimming != null && swimming.IsInWater)
        {
            stateMachine.ChangeState(controller.SwimState);
            return;
        }
        
        if (collisionDetector.IsGrounded)
        {
            if (Mathf.Abs(inputHandler.MoveInput.x) > 0.1f)
            {
                stateMachine.ChangeState(controller.WalkState);
            }
            else
            {
                stateMachine.ChangeState(controller.IdleState);
            }
        }
        
        // Check vertical velocity for animation
        if (controller.Rb.linearVelocity.y < 0)
        {
            controller.AnimationController.PlayAnimation("Kalb_fall");
            // Transition to air state when falling
            stateMachine.ChangeState(controller.AirState);
        }
    }
    
    public override void FixedUpdate()
    {
        // Use ApplyAirControl to allow flipping in air during jump
        movement.ApplyAirControl(inputHandler.MoveInput.x);
    }
    
    public override void HandleInput()
    {
        // Track jump button state
        if (inputHandler.JumpHeld)
        {
            physics.SetJumpButtonState(true);
        }
        else if (inputHandler.JumpReleased)
        {
            physics.SetJumpButtonState(false);
            physics.ApplyJumpCut();
        }
    }
}