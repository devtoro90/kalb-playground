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
            ExitToAppropriateState();
            return;
        }
        
        isRunning = true;
        runTransitionTimer = 0f;
        currentRunSpeed = Mathf.Max(movement.FacingRight ? controller.Rb.linearVelocity.x : -controller.Rb.linearVelocity.x, settings.moveSpeed);
        
        UpdateAnimation();
    }
    
    public override void Exit()
    {
        isRunning = false;
        currentRunSpeed = 0f;
        movement.ResetSmoothing();
    }
    
    public override void Update()
    {
        // Check for ledge state
        if (controller.LedgeDetector.LedgeDetected && !collisionDetector.IsGrounded && 
            controller.Rb.linearVelocity.y < 0 && controller.Settings.ledgeGrabUnlocked)
        {
            float playerBottom = controller.GetComponent<Collider2D>().bounds.min.y;
            float ledgeTop = controller.LedgeDetector.LedgePosition.y;
            
            if (playerBottom < ledgeTop && playerBottom > ledgeTop - 1.0f)
            {
                stateMachine.ChangeState(controller.LedgeState);
                return;
            }
        }

        if (!CanRun() || !ShouldContinueRunning())
        {
            ExitToAppropriateState();
            return;
        }
        
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
        
        UpdateRunSpeed();
        UpdateAnimation();
    }
    
    public override void FixedUpdate()
    {
        if (!isRunning) return;
        ApplyRunMovement();
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
        
        if (inputHandler.DashPressed && abilitySystem.CanDash())
        {
            controller.ForceStateChange(controller.DashState);
        }
    }
    
    private bool CanRun()
    {
        if (abilitySystem != null && !abilitySystem.CanRun())
            return false;
        
        if (!collisionDetector.IsGrounded)
            return false;
        
        if (!inputHandler.DashHeld)
            return false;
        
        if (Mathf.Abs(inputHandler.MoveInput.x) < 0.1f)
            return false;
        
        return true;
    }
    
    private bool ShouldContinueRunning()
    {
        return CanRun();
    }
    
    private void UpdateRunSpeed()
    {
        float targetSpeed = settings.runSpeed;
        
        float currentDirection = movement.FacingRight ? 1f : -1f;
        float inputDirection = Mathf.Sign(inputHandler.MoveInput.x);
        
        if (Mathf.Abs(inputHandler.MoveInput.x) > 0.1f && Mathf.Sign(inputDirection) != Mathf.Sign(currentDirection))
        {
            targetSpeed *= settings.runTurnaroundMultiplier;
        }
        
        float acceleration = (currentRunSpeed < targetSpeed) ? settings.runAcceleration : settings.runDeceleration;
        currentRunSpeed = Mathf.MoveTowards(currentRunSpeed, targetSpeed, acceleration * Time.deltaTime);
        
        if (runTransitionTimer < RUN_TRANSITION_TIME)
        {
            runTransitionTimer += Time.deltaTime;
        }
    }
    
    private void ApplyRunMovement()
    {
        if (!isRunning || controller.Rb == null) return;
        
        float moveInput = inputHandler.MoveInput.x;
        float targetSpeed = moveInput * currentRunSpeed;
        
        Vector2 targetVelocity = new Vector2(targetSpeed, controller.Rb.linearVelocity.y);
        Vector2 currentVelocity = controller.Rb.linearVelocity;
        
        float runSmoothing = settings.movementSmoothing * 0.7f;
        controller.Rb.linearVelocity = Vector3.SmoothDamp(currentVelocity, targetVelocity, ref runVelocity, runSmoothing);

        movement.Velocity = runVelocity;
        
        if (moveInput != 0)
        {
            bool shouldFaceRight = moveInput > 0;
            if (shouldFaceRight != movement.FacingRight)
            {
                movement.ForceFlip(shouldFaceRight);
            }
        }
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
    
    public float GetRunSpeedRatio()
    {
        return Mathf.Clamp01(currentRunSpeed / settings.runSpeed);
    }
    
    public bool IsAtFullSpeed()
    {
        return currentRunSpeed >= settings.runSpeed * 0.95f;
    }
}