using UnityEngine;

public class KalbWallLockState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbMovement movement;
    private KalbWallJump wallJump;
    private Rigidbody2D rb;
    private KalbPhysics physics;
    private KalbAbilitySystem abilitySystem;
    private KalbComboSystem comboSystem; // NEW: Add reference

    // Wall Lock state
    private bool isWallLocked = false;
    private int lockWallSide = 0;
    private Vector2 lockPosition;
    private bool isTransitioningIn = false;
    private bool isTransitioningOut = false;

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
        comboSystem = controller.ComboSystem; // NEW
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

        StartLockTransition();

        controller.AnimationController.PlayAnimation("Kalb_walllock");

        controller.InputBuffer?.ClearAllBuffersOnStateChange();
    }

    public override void Exit()
    {


        isWallLocked = false;
        isTransitioningIn = false;
        isTransitioningOut = false;

        controller.GravityManager.ClearOverride("WallLock");

        // Ensure we're not stuck
        if (controller.WallJump != null)
        {
            // Small cooldown to prevent immediate re-lock
            controller.WallLockCooldownTimer = 0.2f;
        }
    }

    public override void Update()
    {
        if (isTransitioningIn || isTransitioningOut)
            return;

        if (isWallLocked)
        {
            // NEW: Check for wall attack input (highest priority)
            /*if (inputHandler.AttackPressed && comboSystem != null && 
                controller.Settings.enableWallAttack && comboSystem.ShouldPerformWallAttack())
            {
                controller.InputBuffer.BufferAttack();
                
                if (controller.InputBuffer.ConsumeBufferedInput("Attack"))
                {
                    stateMachine.ChangeState(controller.WallAttackState);
                    inputHandler.ResetAttackInput();
                    return;
                }
            }*/

            if (inputHandler.JumpPressed)
            {
                ExecuteWallJumpFromLock();
                return;
            }

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
            float transitionTime = controller.Settings.wallLockEnterSpeed;
            float elapsed = transitionTime - controller.Settings.wallLockEnterSpeed * Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionTime);

            Vector2 targetPos = lockPosition;
            float inwardOffset = Mathf.Lerp(0.05f, 0f, t);
            targetPos.x += lockWallSide * inwardOffset;

            rb.MovePosition(Vector2.Lerp(rb.position, targetPos, t));
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, t);

            if (t >= 0.95f)
            {
                CompleteLockTransition();
            }
        }
        else if (isWallLocked)
        {
            rb.linearVelocity = Vector2.zero;
            rb.MovePosition(lockPosition);
        }
        else if (isTransitioningOut)
        {
            float transitionTime = controller.Settings.wallLockExitSpeed;
            float elapsed = transitionTime - controller.Settings.wallLockExitSpeed * Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionTime);

            Vector2 targetVelocity = new Vector2(0, controller.Settings.wallSlideSpeed);
            rb.linearVelocity = Vector2.Lerp(Vector2.zero, targetVelocity, t);

            if (t >= 0.95f)
            {
                CompleteReleaseTransition();
            }
        }
    }

    public override void HandleInput()
    {
        // Input handled in Update
    }

    private bool CanEnterWallLock()
    {
        if (abilitySystem == null || !abilitySystem.CanWallLock())
            return false;

        if (!wallJump.IsWallSliding)
            return false;

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

        return Mathf.Abs(inputHandler.MoveInput.x) > controller.Settings.wallLockInputThreshold &&
               Mathf.Approximately(inputDirection, wallSide);
    }

    private void StartLockTransition()
    {
        controller.GravityManager.SetZeroGravity("WallLock");
    }

    private void CompleteLockTransition()
    {
        isTransitioningIn = false;
        isWallLocked = true;

        rb.position = lockPosition;
        rb.linearVelocity = Vector2.zero;
    }

    private void StartReleaseTransition()
    {
        isWallLocked = false;
        isTransitioningOut = true;

        controller.GravityManager.SetNormalGravity();

        if (controller.WallJump != null)
        {
            controller.WallJump.ResetSlideSpeed();
        }
    }

    private void CompleteReleaseTransition()
    {
        isTransitioningOut = false;
        ExitToWallSlide();
    }

    private void ExecuteWallJumpFromLock()
    {
        wallJump.ExecuteWallJump();
        controller.AnimationController.PlayAnimation("Kalb_jump");
        if (controller.ParticleController != null)
        {
            controller.ParticleController.PlayJumpDust();
        }
        physics.SetJumpBuffer();
        inputHandler.ResetJumpInput();
        stateMachine.ChangeState(controller.AirState);
    }

    private void ReleaseForDash()
    {
        controller.InputBuffer.BufferDash();

        if (controller.InputBuffer.ConsumeBufferedInput("Dash"))
        {
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