using UnityEngine;

public class KalbCombatState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbComboSystem comboSystem;
    private KalbMovement movement;
    private KalbPhysics physics;
    private KalbSwimming swimming;
    private KalbCollisionDetector collisionDetector;

    public KalbCombatState(KalbController controller, KalbStateMachine stateMachine) 
        : base(controller, stateMachine)
    {
        inputHandler = controller.InputHandler;
        comboSystem = controller.ComboSystem;
        movement = controller.Movement;
        physics = controller.Physics;
        swimming = controller.Swimming;
        collisionDetector = controller.CollisionDetector;
    }
    
    public override void Enter()
    {
        // Start the combo attack - combo system will determine if it's upward
        comboSystem.StartAttack();
        controller.InputBuffer?.ClearBufferedInput("Jump");
        controller.InputBuffer?.ClearBufferedInput("Dash");
        controller.InputBuffer?.ClearBufferedInput("Attack");
    }
    
    public override void Exit()
    {
        // Clean up if needed
    }
    
    public override void Update()
    {
        // Check for swimming transition (cancel combo)
        if (swimming != null && swimming.IsSwimming)
        {
            comboSystem.CancelCombo();
            stateMachine.ChangeState(controller.SwimState);
            return;
        }
        
        // If attack is finished, transition to appropriate state
        if (!comboSystem.IsAttacking && !comboSystem.IsUpwardAttacking && !comboSystem.IsWallAttacking)
        {
            TransitionToNextState();
        }
    }
    
    public override void FixedUpdate()
    {
        // MODIFIED: Different movement handling for upward attacks
        if (comboSystem.IsWallAttacking)
        {
            // During wall attack, maintain wall position
            // Don't apply movement
            if (controller.WallJump != null && controller.WallJump.IsTouchingWall)
            {
                // Stay attached to wall
                controller.Rb.linearVelocity = Vector2.zero;
                
                // Apply stick force to wall
                Vector2 wallStickForce = new Vector2(controller.WallJump.WallSide * 10f, 0);
                controller.Rb.AddForce(wallStickForce);
            }
        }
        else if (comboSystem.IsUpwardAttacking)
        {
            // During upward attack, minimal horizontal movement
            // The upward attack already applies its own upward force
            movement.StopHorizontalMovement();
        }
        else if (comboSystem.IsAttacking)
        {
            // Limited movement during normal combo attacks
            if (comboSystem.CurrentCombo < 3 && comboSystem.IsInComboWindow)
            {
                // Slow horizontal movement during attack
                float moveInput = inputHandler.MoveInput.x * 0.3f; // Reduced control
                movement.ApplyAirControl(moveInput);
            }
            else
            {
                // Stop movement for final hit
                movement.StopHorizontalMovement();
            }
        }
    }
    
    public override void HandleInput()
    {
        // Queue next attack if button pressed during combo window
        if (inputHandler.AttackPressed && comboSystem.IsAttacking && comboSystem.IsInComboWindow)
        {
            // The combo system will handle the queued attack
        }
        
        // NEW: Allow upward attack during normal combo if conditions met
        if (inputHandler.AttackPressed && comboSystem.IsAttacking && comboSystem.ShouldPerformUpwardAttack())
        {
            // Cancel current combo and perform upward attack
            comboSystem.CancelCombo();
            comboSystem.StartAttack(); // This will trigger upward attack
        }
        
        // Allow jump input (will cancel combo)
        if (inputHandler.JumpPressed)
        {
            physics.SetJumpBuffer();
            comboSystem.CancelCombo();
        }
    }
    
    private void TransitionToNextState()
    {
        // If we were wall attacking, return to appropriate wall state
        if (comboSystem.IsWallAttacking)
        {
            if (controller.WallJump != null && controller.WallJump.IsTouchingWall)
            {
                if (controller.WallLockState != null && Mathf.Abs(inputHandler.MoveInput.x) > controller.Settings.wallLockInputThreshold)
                {
                    stateMachine.ChangeState(controller.WallLockState);
                }
                else
                {
                    stateMachine.ChangeState(controller.WallSlideState);
                }
                return;
            }
        }
        
        if (controller.IsEffectivelyGrounded())
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
        else
        {
            if (swimming.IsJumpingFromWater && controller.Rb.linearVelocity.y > 0)
            {
                // Stay in air state but allow jumping
                stateMachine.ChangeState(controller.AirState);
            }
            else
            {
                stateMachine.ChangeState(controller.AirState);
            }
        }
    }
}