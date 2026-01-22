using UnityEngine;

public class KalbWalkState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbCollisionDetector collisionDetector;
    private KalbMovement movement;
    
    public KalbWalkState(KalbController controller, KalbStateMachine stateMachine) 
        : base(controller, stateMachine)
    {
        inputHandler = controller.InputHandler;
        collisionDetector = controller.CollisionDetector;
        movement = controller.Movement;
    }
    
    public override void Enter()
    {
        controller.AnimationController.PlayAnimation("Kalb_walk");
    }
    
    public override void Update()
    {
        if (!collisionDetector.IsGrounded)
        {
            stateMachine.ChangeState(controller.AirState);
            return;
        }
        
        if (Mathf.Abs(inputHandler.MoveInput.x) < 0.1f)
        {
            stateMachine.ChangeState(controller.IdleState);
        }
    }
    
    public override void FixedUpdate()
    {
        movement.Move(inputHandler.MoveInput.x, collisionDetector.IsGrounded);
    }
    
    public override void HandleInput()
    {
        if (inputHandler.JumpPressed)
        {
            controller.Physics.SetJumpBuffer();
        }
        
        if (inputHandler.JumpReleased)
        {
            controller.Physics.ApplyJumpCut();
        }
    }
}