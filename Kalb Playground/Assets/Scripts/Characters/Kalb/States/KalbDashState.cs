using UnityEngine;

public class KalbDashState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbCollisionDetector collisionDetector;
    private KalbMovement movement;
    private KalbSwimming swimming;
    private KalbAbilitySystem abilitySystem;
    private KalbSettings settings;
    
    // Dash state
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector2 dashDirection = Vector2.right;
    private int airDashCount = 0;
    private float preDashGravityScale;
    
    public bool IsDashing => isDashing;
    public float DashTimer => dashTimer;
    public float DashCooldownTimer => dashCooldownTimer;
    public Vector2 DashDirection => dashDirection;
    public int AirDashCount => airDashCount;
    
    public KalbDashState(KalbController controller, KalbStateMachine stateMachine) 
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
        Debug.Log("=== ENTERING DASH STATE ===");
        
        if (!CanDash())
        {
            Debug.LogWarning("Cannot dash from Enter()");
            ExitToAppropriateState();
            return;
        }
        
        StartDash();
    }
    
    public override void Exit()
    {
        Debug.Log("Exiting Dash State");
        
        if (isDashing)
        {
            EndDash();
        }
    }
    
    public override void Update()
    {
        // Check if dash should end
        if (isDashing && dashTimer <= 0)
        {
            Debug.Log("Dash timer expired, exiting state");
            ExitToAppropriateState();
            return;
        }
        
        // Update timers
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
        }
        
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
        
        // Check for swimming (cancel dash)
        if (swimming != null && swimming.IsInWater && isDashing)
        {
            Debug.Log("Entering water, canceling dash");
            CancelDash();
            stateMachine.ChangeState(controller.SwimState);
        }

        Debug.Log($"Dash Update - IsDashing: {isDashing}, DashTimer: {dashTimer:F2}, Cooldown: {dashCooldownTimer:F2}");
    }
    
    public override void FixedUpdate()
    {
        if (!isDashing) return;
        
        ApplyDashMovement();
    }
    
    public override void HandleInput()
    {
        // Dash doesn't process input during dash
    }
    
    private bool CanDash()
    {
        // Check ability
        if (abilitySystem == null || !abilitySystem.CanDash())
        {
            Debug.Log("Dash failed: Ability not unlocked");
            return false;
        }
        
        // Check if dashing
        if (isDashing)
        {
            Debug.Log("Dash failed: Already dashing");
            return false;
        }
        
        // Check cooldown
        if (dashCooldownTimer > 0)
        {
            Debug.Log($"Dash failed: On cooldown ({dashCooldownTimer:F2}s)");
            return false;
        }
        
        // Check swimming
        if (swimming != null && swimming.IsSwimming)
        {
            Debug.Log("Dash failed: Swimming");
            return false;
        }
        
        // Ground dash - always available
        if (collisionDetector.IsGrounded)
        {
            Debug.Log("Dash available: Ground dash");
            return true;
        }
        
        // Air dash - check limits
        if (!settings.canAirDash)
        {
            Debug.Log("Dash failed: Air dash disabled");
            return false;
        }
        
        if (airDashCount >= settings.maxAirDashes)
        {
            Debug.Log($"Dash failed: Max air dashes ({airDashCount}/{settings.maxAirDashes})");
            return false;
        }
        
        if (collisionDetector.IsWallSliding)
        {
            Debug.Log("Dash failed: Wall sliding");
            return false;
        }
        
        Debug.Log("Dash available: Air dash");
        return true;
    }
    
    private void StartDash()
    {
        isDashing = true;
        dashTimer = settings.dashDuration;
        dashCooldownTimer = settings.dashCooldown;
        
        // Save gravity
        preDashGravityScale = controller.Rb.gravityScale;
        
        // Determine direction
        DetermineDashDirection();
        
        // Track air dash
        if (!collisionDetector.IsGrounded)
        {
            airDashCount++;
            Debug.Log($"Air dash #{airDashCount} (Max: {settings.maxAirDashes})");
        }
        else
        {
            Debug.Log("Ground dash");
        }
        
        // Cancel combo
        controller.ComboSystem?.CancelCombo();
        
        // Stop movement
        movement.StopHorizontalMovement();
        movement.ResetSmoothing();
        
        // Play animation
        controller.AnimationController.PlayAnimation("Kalb_dash");
        
        Debug.Log($"Dash started! Direction: {dashDirection}");
    }
    
    private void DetermineDashDirection()
    {
        // Default to facing
        dashDirection = movement.FacingRight ? Vector2.right : Vector2.left;
        
        // Use input
        if (Mathf.Abs(inputHandler.MoveInput.x) > 0.1f)
        {
            dashDirection = new Vector2(Mathf.Sign(inputHandler.MoveInput.x), 0);
            
            if (settings.canDashDiagonal && Mathf.Abs(inputHandler.MoveInput.y) > 0.1f)
            {
                dashDirection = new Vector2(
                    Mathf.Sign(inputHandler.MoveInput.x),
                    Mathf.Sign(inputHandler.MoveInput.y)
                ).normalized * settings.diagonalDashMultiplier;
            }
        }
        else if (settings.canDashDiagonal && Mathf.Abs(inputHandler.MoveInput.y) > 0.1f)
        {
            dashDirection = new Vector2(0, Mathf.Sign(inputHandler.MoveInput.y));
        }
        
        dashDirection = dashDirection.normalized;
    }
    
    private void ApplyDashMovement()
    {
        if (!isDashing || controller.Rb == null) return;
        
        controller.Rb.linearVelocity = dashDirection * settings.dashSpeed;
        controller.Rb.gravityScale = 0f;
    }
    
    private void EndDash()
    {
        if (!isDashing) return;
        
        isDashing = false;
        
        // Restore gravity
        controller.Rb.gravityScale = preDashGravityScale;
        
        // Slow down
        controller.Rb.linearVelocity = new Vector2(
            controller.Rb.linearVelocity.x * settings.dashEndSlowdown,
            controller.Rb.linearVelocity.y * settings.dashEndSlowdown
        );
        
        dashCooldownTimer = 0f;
        Debug.Log("Dash ended (velocity slowed)");
    }
    
    private void CancelDash()
    {
        if (isDashing)
        {
            EndDash();
        }
    }
    
    private void ExitToAppropriateState()
    {
        Debug.Log("Exiting to appropriate state...");
        
        // End dash first
        if (isDashing)
        {
            EndDash();
        }
        
        // Check swimming
        if (swimming != null && swimming.IsInWater)
        {
            Debug.Log("-> Swim State");
            stateMachine.ChangeState(controller.SwimState);
            return;
        }
        
        // Check grounded
        if (!collisionDetector.IsGrounded)
        {
            Debug.Log("-> Air State");
            stateMachine.ChangeState(controller.AirState);
            return;
        }
        
        // Check movement input
        if (Mathf.Abs(inputHandler.MoveInput.x) > 0.1f)
        {
            // Check if should run
            if (inputHandler.DashHeld && abilitySystem.CanRun())
            {
                Debug.Log("-> Run State");
                stateMachine.ChangeState(controller.RunState);
            }
            else
            {
                Debug.Log("-> Walk State");
                stateMachine.ChangeState(controller.WalkState);
            }
        }
        else
        {
            Debug.Log("-> Idle State");
            stateMachine.ChangeState(controller.IdleState);
        }
    }
    
    // Public methods for external access
    public void ResetAirDash()
    {
        if (collisionDetector.IsGrounded && settings.resetAirDashOnGround)
        {
            airDashCount = 0;
            Debug.Log("Air dash count reset (grounded)");
        }
    }
    
    public void ForceResetDash()
    {
        isDashing = false;
        dashTimer = 0f;
        dashCooldownTimer = 0f;
        airDashCount = 0;
        
        if (controller.Rb != null)
            controller.Rb.gravityScale = settings.normalGravityScale;
            
        Debug.Log("Dash forcefully reset");
    }
    
    public void DebugDashState()
    {
        Debug.Log("=== DASH STATE DEBUG ===");
        Debug.Log($"IsDashing: {isDashing}");
        Debug.Log($"Dash Timer: {dashTimer:F2}");
        Debug.Log($"Cooldown: {dashCooldownTimer:F2}");
        Debug.Log($"Grounded: {collisionDetector.IsGrounded}");
        Debug.Log($"Air Dash Count: {airDashCount}/{settings.maxAirDashes}");
        Debug.Log($"Can Dash Now: {CanDash()}");
        Debug.Log("========================");
    }
}