using UnityEngine;

public class KalbIdleState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbCollisionDetector collisionDetector;
    private KalbMovement movement;
    
    public KalbIdleState(KalbController controller, KalbStateMachine stateMachine) 
        : base(controller, stateMachine)
    {
        inputHandler = controller.InputHandler;
        collisionDetector = controller.CollisionDetector;
        movement = controller.Movement;
    }
    
    public override void Enter()
    {
        controller.AnimationController.PlayAnimation("Kalb_idle");
        movement.ResetSmoothing(); // Reset smoothing when entering idle
    }
    
    public override void Update()
    {
        if (!collisionDetector.IsGrounded)
        {
            stateMachine.ChangeState(controller.AirState);
            return;
        }
        
        if (Mathf.Abs(inputHandler.MoveInput.x) > 0.1f)
        {
            stateMachine.ChangeState(controller.WalkState);
        }
    }
    
    public override void FixedUpdate()
    {
        // Apply friction even in idle state to stop any residual movement
        movement.Move(0, collisionDetector.IsGrounded);
    }
    
    public override void HandleInput()
    {
        // Check for jump input
        if (inputHandler.JumpPressed)
        {
            controller.Physics.SetJumpBuffer();
        }
        
        // Check for jump release
        if (inputHandler.JumpReleased)
        {
            controller.Physics.ApplyJumpCut();
        }
    }
}