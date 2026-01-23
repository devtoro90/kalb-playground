using UnityEngine;

public class KalbAirState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbCollisionDetector collisionDetector;
    private KalbMovement movement;
    private KalbSwimming swimming;
    
    public KalbAirState(KalbController controller, KalbStateMachine stateMachine) 
        : base(controller, stateMachine)
    {
        inputHandler = controller.InputHandler;
        collisionDetector = controller.CollisionDetector;
        movement = controller.Movement;
        swimming = controller.Swimming;
    }
    
    public override void Enter()
    {
        UpdateAnimation();
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
        // Check for jump input (for coyote time or double jump later)
        if (inputHandler.JumpPressed)
        {
            controller.Physics.SetJumpBuffer();
        }
        
        if (inputHandler.JumpReleased)
        {
            controller.Physics.ApplyJumpCut();
        }
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