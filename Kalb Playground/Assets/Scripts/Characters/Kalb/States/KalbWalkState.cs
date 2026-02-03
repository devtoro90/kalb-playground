using UnityEngine;

public class KalbWalkState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbCollisionDetector collisionDetector;
    private KalbMovement movement;
    private KalbSwimming swimming;
    private KalbAbilitySystem abilitySystem; 
    
    public KalbWalkState(KalbController controller, KalbStateMachine stateMachine) 
        : base(controller, stateMachine)
    {
        inputHandler = controller.InputHandler;
        collisionDetector = controller.CollisionDetector;
        movement = controller.Movement;
        swimming = controller.Swimming;
        abilitySystem = controller.AbilitySystem; 
    }
    
    public override void Enter()
    {
        controller.AnimationController.PlayAnimation("Kalb_walk");

        controller.InputBuffer?.ClearBufferedInput("Jump");
        controller.InputBuffer?.ClearBufferedInput("Dash");
        controller.InputBuffer?.ClearBufferedInput("Attack");
    }
    
    public override void Update()
    {
        // Check for ledge state
        if (controller.LedgeDetector.LedgeDetected && !controller.IsEffectivelyGrounded() && // CHANGED
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
            stateMachine.ChangeState(controller.SwimState);
            return;
        }
        
        if (!controller.IsEffectivelyGrounded()) // CHANGED
        {
            stateMachine.ChangeState(controller.AirState);
            return;
        }
        
        if (Mathf.Abs(inputHandler.MoveInput.x) < 0.1f)
        {
            stateMachine.ChangeState(controller.IdleState);
        }
        
        if (abilitySystem != null && abilitySystem.CanRun() && inputHandler.DashHeld)
        {
            stateMachine.ChangeState(controller.RunState);
            return;
        }
    }
    
    public override void FixedUpdate()
    {
        movement.Move(inputHandler.MoveInput.x, controller.IsEffectivelyGrounded()); // CHANGED
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