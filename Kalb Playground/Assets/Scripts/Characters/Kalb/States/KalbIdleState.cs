using UnityEngine;

public class KalbIdleState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbCollisionDetector collisionDetector;
    private KalbMovement movement;
    private KalbSwimming swimming;
    private KalbAbilitySystem abilitySystem; 
    
    public KalbIdleState(KalbController controller, KalbStateMachine stateMachine) 
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
        controller.AnimationController.PlayAnimation("Kalb_idle");
        movement.ResetSmoothing(); // Reset smoothing when entering idle
    }
    
    public override void Update()
    {
        // Check for ledge state
        if (controller.LedgeDetector.LedgeDetected && !collisionDetector.IsGrounded && 
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
        
        if (!collisionDetector.IsGrounded)
        {
            stateMachine.ChangeState(controller.AirState);
            return;
        }
        
        if (Mathf.Abs(inputHandler.MoveInput.x) > 0.1f)
        {
            if (abilitySystem != null && abilitySystem.CanRun() && inputHandler.DashHeld)
            {
                stateMachine.ChangeState(controller.RunState);
            }
            else
            {
                stateMachine.ChangeState(controller.WalkState);
            }
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