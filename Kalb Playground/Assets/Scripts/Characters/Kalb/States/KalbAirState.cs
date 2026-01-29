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
        // Check for jump input (for coyote time or double jump)
        if (inputHandler.JumpPressed)
        {
            controller.Physics.SetJumpBuffer();
            
            // Check for double jump
            if (!collisionDetector.IsGrounded && 
                controller.Physics.CanDoubleJump &&
                controller.AbilitySystem != null && 
                controller.AbilitySystem.CanDoubleJump())
            {
                // Execute double jump
                ExecuteDoubleJump();
            }
        }
        
        if (inputHandler.JumpReleased)
        {
            controller.Physics.ApplyJumpCut();
        }

        // Check for attack input in air state
        if (inputHandler.AttackPressed && comboSystem != null && comboSystem.CanAttack)
        {
            // Don't check swimming state here - let controller handle it
            // This allows attacks in air after water jumps
        }
    }

    private void ExecuteDoubleJump()
    {
        // Mark as double jumped
        
        controller.Physics.ResetDoubleJump(); // This sets hasDoubleJumped = true
        
        // Apply double jump force
        float jumpForce = controller.Settings.doubleJumpForce;
        
        // Optionally maintain horizontal momentum
        if (controller.Settings.doubleJumpMaintainsMomentum)
        {
            // Keep current horizontal velocity or boost it
            float currentXVelocity = controller.Rb.linearVelocity.x;
            float boostedXVelocity = currentXVelocity * controller.Settings.doubleJumpHorizontalBoost;
            
            controller.Rb.linearVelocity = new Vector2(
                boostedXVelocity,
                jumpForce
            );
        }
        else
        {
            // Standard double jump
            controller.Physics.Jump(jumpForce);
        }
        
        controller.Physics.SetJumpButtonState(true);
        
        // Play double jump animation/sound
        controller.AnimationController.PlayAnimation("Kalb_jump"); // You'll need to create this animation
        
        // Reset jump buffer to prevent chaining
        controller.Physics.SetJumpBuffer(); // This clears the buffer
        inputHandler.ResetJumpInput();
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