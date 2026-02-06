using UnityEngine;

public class KalbWallLockState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbMovement movement;
    private KalbWallJump wallJump;
    private Rigidbody2D rb;
    private KalbPhysics physics;
    private KalbAbilitySystem abilitySystem;
    
    // Wall Lock state
    private bool isWallLocked = false;
    private int lockWallSide = 0;
    private Vector2 lockPosition;
    private bool isTransitioningIn = false;
    private bool isTransitioningOut = false;
    private bool wantsToRelease = false;
    
    // Properties
    public bool IsWallLocked => isWallLocked;
    public bool IsTransitioning => isTransitioningIn || isTransitioningOut;
    
    public KalbWallLockState(KalbController controller, KalbStateMachine stateMachine) 
        : base(controller, stateMachine)
    {
        inputHandler = controller.InputHandler;
        movement = controller.Movement;
        wallJump = controller.WallJump;
        rb = controller.Rb;
        physics = controller.Physics;
        abilitySystem = controller.AbilitySystem;
    }
    
    public override void Enter()
    {
        if (!CanEnterWallLock())
        {
            ExitToWallSlide();
            return;
        }
        
        lockWallSide = wallJump.WallSide;
        lockPosition = rb.position;
        
        isWallLocked = false;
        isTransitioningIn = true;
        isTransitioningOut = false;
        wantsToRelease = false;
        
        // Start transition
        StartLockTransition();
        
        // Play lock animation
        controller.AnimationController.PlayAnimation("Kalb_walllock");
        
        controller.InputBuffer?.ClearAllBuffersOnStateChange();
    }
    
    public override void Exit()
    {
        isWallLocked = false;
        isTransitioningIn = false;
        isTransitioningOut = false;
        wantsToRelease = false;
        
        // Restore gravity
        controller.GravityManager.ClearOverride("WallLock");
    }
    
    public override void Update()
    {
        // Don't check conditions while transitioning
        if (isTransitioningIn || isTransitioningOut)
            return;
        
        if (isWallLocked)
        {
            // Check for jump input (highest priority)
            if (inputHandler.JumpPressed)
            {
                ExecuteWallJumpFromLock();
                return;
            }
            
            // Check for dash input
            if (inputHandler.DashPressed && controller.AbilitySystem.CanDash())
            {
                if (controller.CanDashFromCurrentState() && controller.DashCooldownTimer <= 0)
                {
                    ReleaseForDash();
                    return;
                }
            }
            
            // Check if still holding toward wall
            if (!IsPushingTowardWall())
            {
                StartReleaseTransition();
            }
        }
    }
    
    public override void FixedUpdate()
    {
        if (isTransitioningIn)
        {
            // Smoothly transition to lock position
            float transitionTime = controller.Settings.wallLockEnterSpeed;
            float elapsed = transitionTime - controller.Settings.wallLockEnterSpeed * Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionTime);
            
            Vector2 targetPos = lockPosition;
            
            // Add slight inward movement during transition
            float inwardOffset = Mathf.Lerp(0.05f, 0f, t);
            targetPos.x += lockWallSide * inwardOffset;
            
            rb.MovePosition(Vector2.Lerp(rb.position, targetPos, t));
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, t);
            
            // Check if transition complete
            if (t >= 0.95f)
            {
                CompleteLockTransition();
            }
        }
        else if (isWallLocked)
        {
            // Maintain lock position
            rb.linearVelocity = Vector2.zero;
            rb.MovePosition(lockPosition);
        }
        else if (isTransitioningOut)
        {
            // Smoothly transition back to wall slide
            float transitionTime = controller.Settings.wallLockExitSpeed;
            float elapsed = transitionTime - controller.Settings.wallLockExitSpeed * Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionTime);
            
            // Gradually restore downward velocity
            Vector2 targetVelocity = new Vector2(0, controller.Settings.wallSlideSpeed);
            rb.linearVelocity = Vector2.Lerp(Vector2.zero, targetVelocity, t);
            
            // Check if transition complete
            if (t >= 0.95f)
            {
                CompleteReleaseTransition();
            }
        }
    }
    
    public override void HandleInput()
    {
        // Input is handled in Update for priority
    }
    
    private bool CanEnterWallLock()
    {
        // Check ability
        if (abilitySystem == null || !abilitySystem.CanWallLock())
            return false;
        
        // Must be wall sliding
        if (!wallJump.IsWallSliding)
            return false;
        
        // Must be pushing toward wall
        if (!IsPushingTowardWall())
            return false;
        
        return true;
    }
    
    private bool IsPushingTowardWall()
    {
        if (!wallJump.IsTouchingWall)
            return false;
        
        float inputDirection = Mathf.Sign(inputHandler.MoveInput.x);
        float wallSide = wallJump.WallSide;
        
        // Check if input is toward wall (with threshold)
        return Mathf.Abs(inputHandler.MoveInput.x) > controller.Settings.wallLockInputThreshold && 
               Mathf.Approximately(inputDirection, wallSide);
    }
    
    private void StartLockTransition()
    {
        // Set zero gravity during lock
        controller.GravityManager.SetZeroGravity("WallLock");
    }
    
    private void CompleteLockTransition()
    {
        isTransitioningIn = false;
        isWallLocked = true;
        
        // Ensure perfect position lock
        rb.position = lockPosition;
        rb.linearVelocity = Vector2.zero;
    }
    
    private void StartReleaseTransition()
    {
        isWallLocked = false;
        isTransitioningOut = true;
        
        // Start restoring gravity
        controller.GravityManager.SetNormalGravity();
    }
    
    private void CompleteReleaseTransition()
    {
        isTransitioningOut = false;
        
        // Return to wall slide state
        ExitToWallSlide();
    }
    
    private void ExecuteWallJumpFromLock()
    {
        // Execute normal wall jump
        wallJump.ExecuteWallJump();
        
        // Play jump animation
        controller.AnimationController.PlayAnimation("Kalb_jump");
        
        // Reset jump buffer
        physics.SetJumpBuffer();
        inputHandler.ResetJumpInput();
        
        // Transition to air state
        stateMachine.ChangeState(controller.AirState);
    }
    
    private void ReleaseForDash()
    {
        controller.InputBuffer.BufferDash();
        
        if (controller.InputBuffer.ConsumeBufferedInput("Dash"))
        {
            // Quick release before dashing
            controller.GravityManager.SetNormalGravity();
            stateMachine.ChangeState(controller.DashState);
            inputHandler.ResetDashInput();
        }
    }
    
    private void ExitToWallSlide()
    {
        stateMachine.ChangeState(controller.WallSlideState);
    }
}