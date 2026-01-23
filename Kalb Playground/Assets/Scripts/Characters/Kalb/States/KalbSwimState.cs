using UnityEngine;

public class KalbSwimState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbMovement movement;
    private KalbSwimming swimming;
    private Rigidbody2D rb;
    private KalbPhysics physics;
    private KalbAbilitySystem abilitySystem;
    
    // Jump buffer to ensure jump is processed
    private bool jumpBuffered = false;
    private float jumpBufferTime = 0.1f;
    private float jumpBufferTimer = 0f;
    
    public KalbSwimState(KalbController controller, KalbStateMachine stateMachine) 
        : base(controller, stateMachine)
    {
        inputHandler = controller.InputHandler;
        movement = controller.Movement;
        swimming = controller.Swimming;
        rb = controller.Rb;
        physics = controller.Physics;
        abilitySystem = controller.AbilitySystem;
    }
    
    public override void Enter()
    {
        swimming.EnterSwim();
        controller.AnimationController.PlayAnimation("Kalb_swim_idle");
        jumpBuffered = false;
        jumpBufferTimer = 0f;

        // Cancel combo when entering swim state
        controller.ComboSystem?.CancelCombo();
    }
    
    public override void Exit()
    {
        swimming.ExitSwim();
    }
    
    public override void Update()
    {
        // Update jump buffer timer
        if (jumpBuffered)
        {
            jumpBufferTimer -= Time.deltaTime;
            if (jumpBufferTimer <= 0)
            {
                jumpBuffered = false;
            }
        }
        
        // Check if we should exit swimming
        if (!swimming.IsInWater && !swimming.IsJumpingFromWater)
        {
            ExitToAppropriateState();
            return;
        }
        
        swimming.UpdateSwim();
        UpdateAnimation();
        
        // Handle flip based on input
        HandleFlip();
        
        // Process buffered jump
        if (jumpBuffered)
        {
            ExecuteWaterJump();
        }
    }
    
    public override void FixedUpdate()
    {
        // Apply swimming movement and buoyancy
        swimming.FixedUpdateSwim();
        
        // Apply horizontal swimming movement
        if (!swimming.IsSwimDashing)
        {
            swimming.ApplySwimMovement(inputHandler.MoveInput.x);
        }
    }
    
    public override void HandleInput()
    {
        // Handle swim dash
        if (inputHandler.DashPressed)
        {
            if (abilitySystem != null && abilitySystem.CanDash()) // CHECK ABILITY
            {
                swimming.StartSwimDash();
            }
        }
        
        // Handle jump out of water - buffer the input
        if (inputHandler.JumpPressed)
        {
            jumpBuffered = true;
            jumpBufferTimer = jumpBufferTime;
        }
    }
    
    private void UpdateAnimation()
    {
        if (swimming.IsSwimDashing)
        {
            controller.AnimationController.PlayAnimation("Kalb_dash");
        }
        else if (Mathf.Abs(inputHandler.MoveInput.x) > 0.1f)
        {
            if (inputHandler.DashHeld && !swimming.IsSwimDashing)
            {
                if (abilitySystem != null && abilitySystem.CanRun()) // CHECK ABILITY
                {
                    controller.AnimationController.PlayAnimation("Kalb_swim_fast");
                }
                else
                {
                    controller.AnimationController.PlayAnimation("Kalb_swim");
                }
            }
            else
            {
                controller.AnimationController.PlayAnimation("Kalb_swim");
            }
        }
        else
        {
            controller.AnimationController.PlayAnimation("Kalb_swim_idle");
        }
    }
    
    private void HandleFlip()
    {
        if (Mathf.Abs(inputHandler.MoveInput.x) < 0.1f) return;
        
        // Get the movement component and flip if needed
        if (movement != null)
        {
            bool shouldFaceRight = inputHandler.MoveInput.x > 0;
            
            if (shouldFaceRight != movement.FacingRight)
            {
                movement.ForceFlip(shouldFaceRight);
            }
        }
    }
    
    private void ExitToAppropriateState()
    {
        // Check if we exited water via jump (positive velocity)
        if (rb.linearVelocity.y > 0 || swimming.IsJumpingFromWater)
        {
            stateMachine.ChangeState(controller.AirState);
        }
        // Check if we're grounded
        else if (controller.CollisionDetector.IsGrounded)
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
        // Otherwise, we're falling
        else
        {
            stateMachine.ChangeState(controller.AirState);
        }
    }
    
    private void ExecuteWaterJump()
    {
        // Perform the swim jump
        swimming.SwimJump();
        
        // CRITICAL: Immediately change to air state
        stateMachine.ChangeState(controller.AirState);
        
        // Reset jump buffer
        jumpBuffered = false;
        jumpBufferTimer = 0f;
        
        // Reset jump input so we don't double-jump
        inputHandler.ResetJumpInput();
        
        Debug.Log("Water jump executed from state");
    }
}