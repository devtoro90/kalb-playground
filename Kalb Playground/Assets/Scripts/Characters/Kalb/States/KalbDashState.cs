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
    private Vector2 dashDirection = Vector2.right;
    private int airDashCount = 0;
    private float preDashGravityScale;
    
    public bool IsDashing => isDashing;
    public float DashTimer => dashTimer;
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
           
            ExitToAppropriateState();
            return;
        }
        
        // Update timers
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
        }
        
        // Check for swimming (cancel dash)
        if (swimming != null && swimming.IsInWater && isDashing)
        {
           
            CancelDash();
            stateMachine.ChangeState(controller.SwimState);
        }

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
           
            return false;
        }
        
        // Check if dashing
        if (isDashing)
        {
           
            return false;
        }
        
        // Check cooldown
        if (controller.DashCooldownTimer > 0)
        {
            
            return false;
        }
        
        // Check swimming
        if (swimming != null && swimming.IsSwimming)
        {
           
            return false;
        }
        
        // Ground dash - always available
        if (collisionDetector.IsGrounded)
        {
           
            return true;
        }
        
        // Air dash - check limits
        if (!settings.canAirDash)
        {
           
            return false;
        }
        
        if (airDashCount >= settings.maxAirDashes)
        {
            
            return false;
        }
        
        if (collisionDetector.IsWallSliding)
        {
           
            return false;
        }
        
       
        return true;
    }
    
    private void StartDash()
    {
        isDashing = true;
        dashTimer = settings.dashDuration;
        
        // Save gravity
        preDashGravityScale = controller.Rb.gravityScale;
        
        // Determine direction
        DetermineDashDirection();
        
        // Track air dash
        if (!collisionDetector.IsGrounded)
        {
            airDashCount++;
            
        }
        else
        {
           
        }
        
        // Cancel combo
        controller.ComboSystem?.CancelCombo();
        
        // Stop movement
        movement.StopHorizontalMovement();
        movement.ResetSmoothing();
        
        // Play animation
        controller.AnimationController.PlayAnimation("Kalb_dash");
        
        
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

        controller.DashCooldownTimer = settings.dashCooldown;
        
       
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
       
        
        // End dash first
        if (isDashing)
        {
            EndDash();
        }
        
        // Check swimming
        if (swimming != null && swimming.IsInWater)
        {
           
            stateMachine.ChangeState(controller.SwimState);
            return;
        }
        
        // Check grounded
        if (!collisionDetector.IsGrounded)
        {
           
            stateMachine.ChangeState(controller.AirState);
            return;
        }
        
        // Check movement input
        if (Mathf.Abs(inputHandler.MoveInput.x) > 0.1f)
        {
            // Check if should run
            if (inputHandler.DashHeld && abilitySystem.CanRun())
            {
               
                stateMachine.ChangeState(controller.RunState);
            }
            else
            {
               
                stateMachine.ChangeState(controller.WalkState);
            }
        }
        else
        {
           
            stateMachine.ChangeState(controller.IdleState);
        }
    }
    
    // Public methods for external access
    public void ResetAirDash(string source = "")
    {
        if (airDashCount != 0 && 
            ((collisionDetector.IsGrounded && settings.resetAirDashOnGround &&  source == "Grounded" )|| 
            (swimming != null && swimming.IsInWater && source == "Swimmning")))
        {
            airDashCount = 0;
            
        }
    }
    
    public void ForceResetDash()
    {
        isDashing = false;
        dashTimer = 0f;
        airDashCount = 0;
        
        if (controller.Rb != null)
            controller.Rb.gravityScale = settings.normalGravityScale;
            
       
    }
    
    public void DebugDashState()
    {
       
        
        
        
        
        
        
       
    }
}