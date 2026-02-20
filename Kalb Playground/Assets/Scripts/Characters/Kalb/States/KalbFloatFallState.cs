using UnityEngine;

public class KalbFloatFallState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbMovement movement;
    private KalbPhysics physics;
    private KalbSwimming swimming;
    private KalbDashState dashState;
    private KalbPogoAttackState pogoState;
    private Rigidbody2D rb;
    private KalbSettings settings;
    private KalbAnimationController animController;
    private KalbGravityManager gravityManager;
    private KalbCollisionDetector collisionDetector;
    
    // Float state
    private bool isFloating = false;
    private float floatTimer = 0f;
    private bool canFloat = true;
    private float heightCheckTimer = 0f;
    private const float HEIGHT_CHECK_INTERVAL = 0.1f;
    
    // Properties
    public bool IsFloating => isFloating;
    
    // MODIFIED: CanFloat now just checks basic requirements - no usage limits
    public bool CanFloat 
    { 
        get 
        {
            if (!settings.enableFloatingFall) return false;
            if (!controller.AbilitySystem.CanFloatingFall()) return false;
            if (swimming.IsSwimming) return false;
            if (controller.IsEffectivelyGrounded()) return false;
            if (!canFloat) return false;
            
            return true;
        }
    }
    
    public float FloatTimer => floatTimer;
    public float FloatPercentage => Mathf.Clamp01(floatTimer / settings.floatFallMaxDuration);
    
    public KalbFloatFallState(KalbController controller, KalbStateMachine stateMachine) 
        : base(controller, stateMachine)
    {
        inputHandler = controller.InputHandler;
        movement = controller.Movement;
        physics = controller.Physics;
        swimming = controller.Swimming;
        dashState = controller.DashState;
        pogoState = controller.PogoAttackState;
        rb = controller.Rb;
        settings = controller.Settings;
        animController = controller.AnimationController;
        gravityManager = controller.GravityManager;
        collisionDetector = controller.CollisionDetector;
    }
    
    public override void Enter()
    {
        Debug.Log("[KalbFloatFallState] ENTER");
        
        if (!CanFloat || !ShouldFloat())
        {
            Debug.Log("[KalbFloatFallState] Cannot float, exiting to AirState");
            stateMachine.ChangeState(controller.AirState);
            return;
        }
        
        StartFloating();
        
        controller.InputBuffer?.ClearBufferedInput("Jump");
        controller.InputBuffer?.ClearBufferedInput("Dash");
        controller.InputBuffer?.ClearBufferedInput("Attack");
    }
    
    public override void Exit()
    {
        Debug.Log("[KalbFloatFallState] EXIT");
        
        if (isFloating)
        {
            EndFloating();
        }
        
        isFloating = false;
    }
    
    public override void Update()
    {
        UpdateTimers();
        
        // Periodic height check
        heightCheckTimer -= Time.deltaTime;
        if (heightCheckTimer <= 0)
        {
            heightCheckTimer = HEIGHT_CHECK_INTERVAL;
            CheckHeightAboveGround();
        }
        
        // MODIFIED: Dynamic float toggling based on jump button
        if (isFloating)
        {
            // Check if we should stop floating (jump released or other conditions)
            bool shouldStopFloating = false;
            string stopReason = "";
            
            // Release jump button - exit float but stay in air state
            if (!inputHandler.JumpHeld)
            {
                shouldStopFloating = true;
                stopReason = "Jump released";
            }
            // Hit ground
            else if (settings.floatFallResetsOnGround && controller.IsEffectivelyGrounded())
            {
                shouldStopFloating = true;
                stopReason = "Grounded";
            }
            // Hit wall
            else if (settings.floatFallResetsOnWall && controller.WallJump != null && controller.WallJump.IsTouchingWall)
            {
                shouldStopFloating = true;
                stopReason = "Touching wall";
            }
            // Pogo attack
            else if (settings.floatFallResetsOnPogo && pogoState != null && pogoState.IsPogoAttacking)
            {
                shouldStopFloating = true;
                stopReason = "Pogo attack";
            }
            // Max duration reached
            else if (floatTimer >= settings.floatFallMaxDuration)
            {
                shouldStopFloating = true;
                stopReason = "Max duration";
            }
            // Swimming
            else if (swimming.IsSwimming)
            {
                shouldStopFloating = true;
                stopReason = "Swimming";
            }
            
            if (shouldStopFloating)
            {
                Debug.Log($"[Float] Stopping float: {stopReason}");
                StopFloating();
                return;
            }
            
            // Update float timer
            floatTimer += Time.deltaTime;
        }
        else
        {
            // MODIFIED: If not floating but jump is held and we're falling, try to enter float
            if (inputHandler.JumpHeld && rb.linearVelocity.y < -1f && CanFloat && ShouldFloat())
            {
                Debug.Log("[Float] Jump held while falling, entering float");
                StartFloating();
            }
        }
        
        // Check for transitions out of float state
        if (!isFloating)
        {
            // Go back to air state if we're in air
            if (!controller.IsEffectivelyGrounded())
            {
                stateMachine.ChangeState(controller.AirState);
                return;
            }
        }
    }
    
    public override void FixedUpdate()
    {
        if (!isFloating) return;
        
        // Apply floating physics
        ApplyFloatPhysics();
        
        // Apply horizontal control during float (reduced control)
        ApplyFloatMovement();
    }
    
    public override void HandleInput()
    {
        if (!isFloating) return;
        
        // Allow dash to cancel float
        if (inputHandler.DashPressed && settings.floatFallResetsOnDash)
        {
            if (controller.AbilitySystem.CanDash() && controller.CanDashFromCurrentState() && controller.DashCooldownTimer <= 0)
            {
                Debug.Log("[Float] Dash pressed, transitioning to DashState");
                stateMachine.ChangeState(controller.DashState);
                inputHandler.ResetDashInput();
                return;
            }
        }
        
        // Allow attack to cancel float
        if (inputHandler.AttackPressed)
        {
            // Check for pogo (down + attack)
            if (inputHandler.IsDownHeld && pogoState != null && pogoState.CanPogo)
            {
                Debug.Log("[Float] Pogo input, transitioning to PogoAttackState");
                stateMachine.ChangeState(controller.PogoAttackState);
                inputHandler.ResetAttackInput();
                return;
            }
            // Regular attack
            else if (controller.CanAttackFromCurrentState())
            {
                Debug.Log("[Float] Attack pressed, transitioning to CombatState");
                stateMachine.ChangeState(controller.CombatState);
                inputHandler.ResetAttackInput();
                return;
            }
        }
        
        // MODIFIED: Jump press during float will exit float and jump
        if (inputHandler.JumpPressed)
        {
            Debug.Log("[Float] Jump pressed, transitioning to JumpState");
            
            // First stop floating
            StopFloating();
            
            // Then set jump buffer
            physics.SetJumpBuffer();
            
            // Change to jump state
            stateMachine.ChangeState(controller.JumpState);
            inputHandler.ResetJumpInput();
        }
    }
    
    private bool ShouldFloat()
    {
        // Must be falling (negative vertical velocity)
        if (rb.linearVelocity.y >= 0) return false;
        
        // Jump button must be held
        if (!inputHandler.JumpHeld) return false;
        
        // Check height above ground
        if (!IsHighEnoughToFloat()) return false;
        
        // Don't float if we're in certain states
        if (swimming.IsSwimming) return false;
        if (dashState.IsDashing) return false;
        if (pogoState != null && pogoState.IsPogoAttacking) return false;
        
        return true;
    }
    
    private bool IsHighEnoughToFloat()
    {
        // Raycast down to check distance to ground
        RaycastHit2D hit = Physics2D.Raycast(
            controller.transform.position,
            Vector2.down,
            settings.floatFallMinHeight + 1f,
            settings.groundCheckLayers != 0 ? settings.groundCheckLayers : settings.environmentLayer
        );
        
        if (hit.collider != null)
        {
            float distanceToGround = hit.distance;
            return distanceToGround > settings.floatFallMinHeight;
        }
        
        // If no ground detected, assume we're high enough
        return true;
    }
    
    private void CheckHeightAboveGround()
    {
        if (!isFloating) return;
        
        // If we're too low, stop floating
        if (!IsHighEnoughToFloat())
        {
            Debug.Log("[Float] Too close to ground, stopping float");
            StopFloating();
        }
    }
    
    private void StartFloating()
    {
        isFloating = true;
        floatTimer = 0f;
        
        // Apply float gravity override through gravity manager
        float floatGravity = settings.normalGravityScale * settings.floatFallGravityMultiplier;
        gravityManager.SetGravityOverride(floatGravity, "FloatingFall");
        
        // Clamp downward velocity to float speed
        if (rb.linearVelocity.y < settings.floatFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, settings.floatFallSpeed);
        }
        
        // Play float animation
        if (animController != null && !string.IsNullOrEmpty(settings.floatFallAnimation))
        {
            animController.PlayAnimation(settings.floatFallAnimation);
        }
        
        Debug.Log($"[Float] Started - Gravity: {floatGravity}, Speed: {settings.floatFallSpeed}");
    }
    
    private void ApplyFloatPhysics()
    {
        // Ensure we maintain float speed
        if (rb.linearVelocity.y < settings.floatFallSpeed)
        {
            // Gradually slow descent to float speed
            float newYVelocity = Mathf.MoveTowards(
                rb.linearVelocity.y, 
                settings.floatFallSpeed, 
                settings.floatFallAcceleration * Time.fixedDeltaTime
            );
            
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, newYVelocity);
        }
        else if (rb.linearVelocity.y > 0)
        {
            // If moving up (bounce from pogo), don't interfere
            // Let the upward motion happen naturally
        }
    }
    
    private void ApplyFloatMovement()
    {
        // Apply horizontal movement with reduced control
        float moveInput = inputHandler.MoveInput.x;
        
        if (Mathf.Abs(moveInput) > 0.1f)
        {
            // Calculate target speed (can use run speed if ability unlocked)
            float targetSpeed = moveInput * settings.moveSpeed * settings.floatFallHorizontalControl;
            
            // If run is held and unlocked, use run speed
            if (inputHandler.DashHeld && controller.AbilitySystem.CanRun())
            {
                targetSpeed = moveInput * settings.runSpeed * settings.floatFallHorizontalControl;
            }
            
            // Smoothly move toward target speed
            float currentSpeed = rb.linearVelocity.x;
            float newSpeed = Mathf.MoveTowards(
                currentSpeed, 
                targetSpeed, 
                settings.airAcceleration * Time.fixedDeltaTime * 20f
            );
            
            rb.linearVelocity = new Vector2(newSpeed, rb.linearVelocity.y);
            
            // Flip sprite based on movement
            bool shouldFaceRight = moveInput > 0;
            if (shouldFaceRight != movement.FacingRight)
            {
                movement.ForceFlip(shouldFaceRight);
            }
        }
        else
        {
            // No input - apply air friction
            if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            {
                float frictionForce = -Mathf.Sign(rb.linearVelocity.x) * settings.airFriction * 0.5f;
                rb.AddForce(new Vector2(frictionForce, 0));
            }
        }
    }
    
    private void StopFloating()
    {
        if (!isFloating) return;
        
        isFloating = false;
        
        // Clear gravity override
        gravityManager.ClearOverride("FloatingFall");
        
        // Don't set cooldown - allow immediate re-float if conditions met
    }
    
    private void EndFloating()
    {
        // Clear gravity override
        gravityManager.ClearOverride("FloatingFall");
    }
    
    private void UpdateTimers()
    {
        // No cooldown timer needed for unlimited toggling
        // Keep canFloat always true
        canFloat = true;
    }
    
    public void ResetFloat()
    {
        // Nothing to reset for unlimited toggling
        canFloat = true;
    }
}