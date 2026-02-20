using UnityEngine;

public class KalbHardLandState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbMovement movement;
    private KalbPhysics physics;
    private KalbAnimationController animController;
    private Rigidbody2D rb;
    private KalbSettings settings;
    private MetroidvaniaCamera gameCamera;
    
    // Hard landing state
    private float recoveryTimer = 0f;
    private float fallDistance = 0f;
    private bool isRecovering = true;
    private bool hasExited = false;
    
    public bool IsRecovering => isRecovering && recoveryTimer > 0;
    public float RecoveryRemaining => recoveryTimer;
    
    public KalbHardLandState(KalbController controller, KalbStateMachine stateMachine) 
        : base(controller, stateMachine)
    {
        inputHandler = controller.InputHandler;
        movement = controller.Movement;
        physics = controller.Physics;
        animController = controller.AnimationController;
        rb = controller.Rb;
        settings = controller.Settings;
    }
    
    public void SetFallDistance(float distance)
    {
        fallDistance = distance;
        Debug.Log($"[HardLand] Setting fall distance: {distance}");
    }
    
    public override void Enter()
    {
        Debug.Log($"[HardLand] ENTER - Fall distance: {fallDistance}, Threshold: {settings.hardLandingFallThreshold}");
        
        if (!settings.enableHardLanding || fallDistance < settings.hardLandingFallThreshold)
        {
            Debug.Log("[HardLand] Conditions not met, exiting immediately");
            ExitToAppropriateState();
            return;
        }
        
        isRecovering = true;
        recoveryTimer = settings.hardLandingRecoveryTime;
        hasExited = false;
        
        // Stop all movement immediately
        rb.linearVelocity = Vector2.zero;
        movement.StopHorizontalMovement();
        
        // Play hard landing animation
        if (animController != null && !string.IsNullOrEmpty(settings.hardLandingAnimation))
        {
            Debug.Log($"[HardLand] Playing animation: {settings.hardLandingAnimation}");
            animController.PlayAnimation(settings.hardLandingAnimation);
        }
        
        // Find camera if not already found
        if (gameCamera == null)
        {
            gameCamera = Object.FindFirstObjectByType<MetroidvaniaCamera>();
        }
        
        // Trigger camera shake based on fall distance
        TriggerCameraShake();
        
        // Clear input buffers
        controller.InputBuffer?.ClearAllBuffersOnStateChange();
        
        // Reset jump state
        physics.ResetJumpState();
        
        // Reset air abilities
        if (controller.AbilitySystem.CanDoubleJump())
        {
            physics.ResetDoubleJump();
            physics.SetCanDoubleJump(true);
        }
        
        if (controller.DashState != null)
        {
            controller.DashState.ResetAirDash();
        }
    }
    
    public override void Exit()
    {
        if (hasExited) return;
        
        Debug.Log("[HardLand] EXIT");
        isRecovering = false;
        hasExited = true;
    }
    
    public override void Update()
    {
        if (!isRecovering || hasExited) return;
        
        // Update recovery timer
        if (recoveryTimer > 0)
        {
            recoveryTimer -= Time.deltaTime;
            Debug.Log($"[HardLand] Recovery timer: {recoveryTimer}");
            
            if (recoveryTimer <= 0)
            {
                Debug.Log("[HardLand] Recovery complete, transitioning");
                ExitToAppropriateState();
            }
        }
    }
    
    public override void FixedUpdate()
    {
        if (!isRecovering || hasExited) return;
        
        // During recovery, keep player locked in place
        rb.linearVelocity = Vector2.zero;
    }
    
    public override void HandleInput()
    {
        // No input during recovery
        // Intentionally empty - player is locked
    }
    
    private void TriggerCameraShake()
    {
        if (gameCamera == null) 
        {
            Debug.LogWarning("[HardLand] Camera not found for shake");
            return;
        }
        
        // Calculate shake intensity based on fall distance
        float intensity = settings.hardLandingShakeIntensity.Evaluate(fallDistance);
        intensity = Mathf.Clamp(intensity, 0.1f, 0.5f);
        float duration = settings.hardLandingRecoveryTime * 0.7f;
        
        // Direction: mostly vertical with slight random horizontal
        Vector3 shakeDirection = new Vector3(Random.Range(-0.3f, 0.3f), 1f, 0f);
        
        Debug.Log($"[HardLand] Triggering camera shake - Intensity: {intensity}, Duration: {duration}");
        gameCamera.TriggerScreenShake(intensity, duration, shakeDirection, true);
    }
    
    private void ExitToAppropriateState()
    {
        if (hasExited) return;
        
        Debug.Log("[HardLand] Exiting to appropriate state");
        
        // Double-check we're still grounded
        if (!controller.IsEffectivelyGrounded())
        {
            Debug.Log("[HardLand] Not grounded, going to AirState");
            stateMachine.ChangeState(controller.AirState);
            return;
        }
        
        // Transition based on input
        if (Mathf.Abs(inputHandler.MoveInput.x) > 0.1f)
        {
            // Check for run
            if (inputHandler.DashHeld && controller.AbilitySystem.CanRun())
            {
                Debug.Log("[HardLand] Moving with run input, going to RunState");
                stateMachine.ChangeState(controller.RunState);
            }
            else
            {
                Debug.Log("[HardLand] Moving, going to WalkState");
                stateMachine.ChangeState(controller.WalkState);
            }
        }
        else
        {
            Debug.Log("[HardLand] No movement, going to IdleState");
            stateMachine.ChangeState(controller.IdleState);
        }
    }
}