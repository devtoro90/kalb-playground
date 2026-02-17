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

        controller.InputBuffer?.ClearBufferedInput("Jump");
        controller.InputBuffer?.ClearBufferedInput("Dash");
        controller.InputBuffer?.ClearBufferedInput("Attack");
    }
    
    public override void Update()
    {
        // Check for looking up input first (highest priority in idle)
        float verticalInput = inputHandler.MoveInput.y;
        bool isLookingUp = verticalInput > 0.5f;
        
        // Check for ledge state
        if (controller.AbilitySystem.CanLedgeGrab() && controller.LedgeDetector.LedgeDetected && !controller.IsEffectivelyGrounded() &&
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
        
        if (!controller.IsEffectivelyGrounded())
        {
            stateMachine.ChangeState(controller.AirState);
            return;
        }
        
        // Only transition to movement states if not looking up
        // OR if looking up but input is very strong (prioritize movement)
        if (Mathf.Abs(inputHandler.MoveInput.x) > 0.1f)
        {
            // If we were looking up, we might want a quick transition or just go to walk/run
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
        movement.Move(0, controller.IsEffectivelyGrounded()); // CHANGED
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