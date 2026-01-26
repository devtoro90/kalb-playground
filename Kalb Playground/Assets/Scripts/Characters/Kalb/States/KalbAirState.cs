using UnityEngine;

public class KalbAirState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbCollisionDetector collisionDetector;
    private KalbMovement movement;
    private KalbSwimming swimming;
    private KalbComboSystem comboSystem;
    
    public KalbAirState(KalbController controller, KalbStateMachine stateMachine) 
        : base(controller, stateMachine)
    {
        inputHandler = controller.InputHandler;
        collisionDetector = controller.CollisionDetector;
        movement = controller.Movement;
        swimming = controller.Swimming;
        comboSystem = controller.ComboSystem;
    }
    
    public override void Enter()
    {
        UpdateAnimation();
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
            // Cancel combo when entering swim state
            comboSystem?.CancelCombo(); 
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

        // NEW: Check for attack input in air state
        // This allows attacking while jumping out of water
        if (inputHandler.AttackPressed && comboSystem != null && comboSystem.CanAttack)
        {
            // Don't check swimming state here - let controller handle it
            // This allows attacks in air after water jumps
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