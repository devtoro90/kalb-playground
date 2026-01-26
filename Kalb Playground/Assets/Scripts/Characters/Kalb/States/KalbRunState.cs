using UnityEngine;

public class KalbRunState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbCollisionDetector collisionDetector;
    private KalbMovement movement;
    private KalbSwimming swimming;
    private KalbAbilitySystem abilitySystem;
    private KalbSettings settings;
    
    // Run state
    private bool isRunning = false;
    private float currentRunSpeed = 0f;
    private float runTransitionTimer = 0f;
    private const float RUN_TRANSITION_TIME = 0.1f;
    private Vector3 runVelocity = Vector3.zero;
    
    public bool IsRunning => isRunning;
    public float CurrentRunSpeed => currentRunSpeed;
    
    public KalbRunState(KalbController controller, KalbStateMachine stateMachine) 
        : base(controller, stateMachine)
    {
        inputHandler = controller.InputHandler;
        collisionDetector = controller.CollisionDetector;
        movement = controller.Movement;
        swimming = controller.Swimming;
        abilitySystem = controller.AbilitySystem;
        settings = controller.Settings;
    }
    
    public override void Enter()
    {
        if (!CanRun())
        {
            // Can't run, go to appropriate state
            ExitToAppropriateState();
            return;
        }
        
        isRunning = true;
        runTransitionTimer = 0f;
        currentRunSpeed = Mathf.Max(movement.FacingRight ? controller.Rb.linearVelocity.x : -controller.Rb.linearVelocity.x, settings.moveSpeed);
        
       
        
        // Update animation
        UpdateAnimation();
    }
    
    public override void Exit()
    {
        isRunning = false;
        currentRunSpeed = 0f;
        
        // Reset movement smoothing
        movement.ResetSmoothing();
        
       
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

        // Check if we should exit run state
        if (!CanRun() || !ShouldContinueRunning())
        {
            ExitToAppropriateState();
            return;
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
        
        // Update run speed with acceleration
        UpdateRunSpeed();
        
        // Update animation
        UpdateAnimation();
    }
    
    public override void FixedUpdate()
    {
        if (!isRunning) return;
        
        // Apply run movement
        ApplyRunMovement();
    }
    
    public override void HandleInput()
    {
        // Check for jump input (run jump)
        if (inputHandler.JumpPressed)
        {
            controller.Physics.SetJumpBuffer();
            
            // Apply run jump boost if jumping
            if (controller.Physics.JumpBufferCounter > 0 && controller.Physics.CoyoteTimeCounter > 0)
            {
                controller.ForceStateChange(controller.JumpState);
                ApplyRunJumpBoost();
                return;
            }
        }
        
        if (inputHandler.JumpReleased)
        {
            controller.Physics.ApplyJumpCut();
        }
        
        // Check for dash input
        if (inputHandler.DashPressed && abilitySystem.CanDash())
        {
            // Transition to dash state from run
            controller.ForceStateChange(controller.DashState);
        }
    }
    
    private bool CanRun()
    {
        // Check if run ability is unlocked
        if (abilitySystem != null && !abilitySystem.CanRun())
            return false;
        
        // Must be grounded to run
        if (!collisionDetector.IsGrounded)
            return false;
        
        // Must be holding dash button
        if (!inputHandler.DashHeld)
            return false;
        
        // Must have horizontal input
        if (Mathf.Abs(inputHandler.MoveInput.x) < 0.1f)
            return false;
        
        return true;
    }
    
    private bool ShouldContinueRunning()
    {
        // Stop running if we lose any condition
        return CanRun();
    }
    
    private void UpdateRunSpeed()
    {
        float targetSpeed = settings.runSpeed;
        
        // Check for turnaround (changing direction)
        float currentDirection = movement.FacingRight ? 1f : -1f;
        float inputDirection = Mathf.Sign(inputHandler.MoveInput.x);
        
        if (Mathf.Abs(inputHandler.MoveInput.x) > 0.1f && Mathf.Sign(inputDirection) != Mathf.Sign(currentDirection))
        {
            // Turnaround - slower acceleration
            targetSpeed *= settings.runTurnaroundMultiplier;
        }
        
        // Accelerate or decelerate to target speed
        float acceleration = (currentRunSpeed < targetSpeed) ? settings.runAcceleration : settings.runDeceleration;
        currentRunSpeed = Mathf.MoveTowards(currentRunSpeed, targetSpeed, acceleration * Time.deltaTime);
        
        // Update run transition
        if (runTransitionTimer < RUN_TRANSITION_TIME)
        {
            runTransitionTimer += Time.deltaTime;
        }
    }
    
    private void ApplyRunMovement()
    {
        if (!isRunning || controller.Rb == null) return;
        
        float moveInput = inputHandler.MoveInput.x;
        
        // Calculate target velocity
        float targetSpeed = moveInput * currentRunSpeed;
        
        // Smooth movement
        Vector2 targetVelocity = new Vector2(targetSpeed, controller.Rb.linearVelocity.y);
        Vector2 currentVelocity = controller.Rb.linearVelocity;
        
        // Faster smoothing for run state
        float runSmoothing = settings.movementSmoothing * 0.7f;
        controller.Rb.linearVelocity = Vector3.SmoothDamp(currentVelocity, targetVelocity, ref runVelocity, runSmoothing);

        movement.Velocity = runVelocity;
        
        // Flip if needed
        if (moveInput != 0)
        {
            bool shouldFaceRight = moveInput > 0;
            if (shouldFaceRight != movement.FacingRight)
            {
                movement.ForceFlip(shouldFaceRight);
            }
        }
    }
    
    private void ApplyRunJumpBoost()
    {
        // Apply additional horizontal velocity when jumping from run
        float runJumpBoost = Mathf.Clamp(currentRunSpeed / settings.runSpeed, 1f, settings.runJumpBoost);
        
        Vector2 currentVelocity = controller.Rb.linearVelocity;
        controller.Rb.linearVelocity = new Vector2(
            currentVelocity.x * runJumpBoost,
            currentVelocity.y
        );
    }
    
    private void UpdateAnimation()
    {
        if (isRunning)
        {
            controller.AnimationController.PlayAnimation("Kalb_run");
        }
        else
        {
            controller.AnimationController.PlayAnimation("Kalb_walk");
        }
    }
    
    private void ExitToAppropriateState()
    {
        if (swimming != null && swimming.IsInWater)
        {
            stateMachine.ChangeState(controller.SwimState);
        }
        else if (!collisionDetector.IsGrounded)
        {
            stateMachine.ChangeState(controller.AirState);
        }
        else if (Mathf.Abs(inputHandler.MoveInput.x) > 0.1f)
        {
            stateMachine.ChangeState(controller.WalkState);
        }
        else
        {
            stateMachine.ChangeState(controller.IdleState);
        }
    }
    
    // Public methods for external access
    public float GetRunSpeedRatio()
    {
        return Mathf.Clamp01(currentRunSpeed / settings.runSpeed);
    }
    
    public bool IsAtFullSpeed()
    {
        return currentRunSpeed >= settings.runSpeed * 0.95f;
    }
}