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
    private bool wasGroundedLastFrame = false; // NEW: Track ground state
    
    // Properties
    public bool IsFloating => isFloating;
    
    public bool CanFloat 
    { 
        get 
        {
            if (!settings.enableFloatingFall) return false;
            if (!controller.AbilitySystem.CanFloatingFall()) return false;
            if (swimming.IsSwimming) return false;
            if (controller.IsEffectivelyGrounded()) return false; // CRITICAL: Can't float if grounded
            if (!canFloat) return false;
            
            return true;
        }
    }
    
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
        
        
        if (!CanFloat || !ShouldFloat())
        {
            
            ExitToAppropriateState();
            return;
        }
        
        StartFloating();
        wasGroundedLastFrame = false;
        
        controller.InputBuffer?.ClearBufferedInput("Jump");
        controller.InputBuffer?.ClearBufferedInput("Dash");
        controller.InputBuffer?.ClearBufferedInput("Attack");
    }
    
    public override void Exit()
    {
        
        
        if (isFloating)
        {
            EndFloating();
        }
        
        isFloating = false;
    }
    
    public override void Update()
    {
        // CRITICAL: Ground check FIRST - highest priority
        if (controller.IsEffectivelyGrounded())
        {
            // Only log once when we first become grounded
            if (!wasGroundedLastFrame)
            {
                
                wasGroundedLastFrame = true;
                
                // Immediately exit to appropriate grounded state
                StopFloating();
                ExitToGroundedState();
                return;
            }
        }
        else
        {
            wasGroundedLastFrame = false;
        }
        
        // If we're still floating, continue normal update
        if (isFloating)
        {
            UpdateTimers();
            
            // Periodic height check
            heightCheckTimer -= Time.deltaTime;
            if (heightCheckTimer <= 0)
            {
                heightCheckTimer = HEIGHT_CHECK_INTERVAL;
                CheckHeightAboveGround();
            }
            
            // Check if we should stop floating (jump released or other conditions)
            bool shouldStopFloating = false;
            
            // Release jump button - exit float but stay in air state
            if (!inputHandler.JumpHeld)
            {
                shouldStopFloating = true;
            }
            // Hit wall
            else if (settings.floatFallResetsOnWall && controller.WallJump != null && controller.WallJump.IsTouchingWall)
            {
                shouldStopFloating = true;
            }
            // Pogo attack
            else if (settings.floatFallResetsOnPogo && pogoState != null && pogoState.IsPogoAttacking)
            {
                shouldStopFloating = true;
            }
            // Max duration reached
            else if (floatTimer >= settings.floatFallMaxDuration)
            {
                shouldStopFloating = true;
            }
            // Swimming
            else if (swimming.IsSwimming)
            {
                shouldStopFloating = true;
            }
            
            if (shouldStopFloating)
            {
                
                StopFloating();
                
                // Exit to appropriate state after stopping
                ExitToAppropriateState();
                return;
            }
            
            // Update float timer
            floatTimer += Time.deltaTime;
        }
        else
        {
            // If not floating but jump is held and we're falling, try to enter float
            if (inputHandler.JumpHeld && rb.linearVelocity.y < -1f && CanFloat && ShouldFloat())
            {
                
                StartFloating();
            }
            else if (!controller.IsEffectivelyGrounded())
            {
                // If we're not floating and not grounded, make sure we're in air state
                stateMachine.ChangeState(controller.AirState);
                return;
            }
        }
    }
    
    public override void FixedUpdate()
    {
        if (!isFloating) return;
        
        // Double-check ground in FixedUpdate too (for safety)
        if (controller.IsEffectivelyGrounded())
        {
            if (!wasGroundedLastFrame)
            {
                
                wasGroundedLastFrame = true;
                StopFloating();
                ExitToGroundedState();
                return;
            }
        }
        
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
                
                StopFloating();
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
                
                StopFloating();
                stateMachine.ChangeState(controller.PogoAttackState);
                inputHandler.ResetAttackInput();
                return;
            }
            // Regular attack
            else if (controller.CanAttackFromCurrentState())
            {
                
                StopFloating();
                stateMachine.ChangeState(controller.CombatState);
                inputHandler.ResetAttackInput();
                return;
            }
        }
        
        // Jump press during float will exit float and jump
        if (inputHandler.JumpPressed)
        {
            
            
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
        // CRITICAL: Can't float if grounded
        if (controller.IsEffectivelyGrounded()) return false;
        
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
        
        
    }
    
    private void ApplyFloatPhysics()
    {
        // Ensure we maintain float speed
        if (rb.linearVelocity.y < settings.floatFallSpeed)
        {
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
        }
    }
    
    private void ApplyFloatMovement()
    {
        float moveInput = inputHandler.MoveInput.x;
        
        if (Mathf.Abs(moveInput) > 0.1f)
        {
            float targetSpeed = moveInput * settings.moveSpeed * settings.floatFallHorizontalControl;
            
            if (inputHandler.DashHeld && controller.AbilitySystem.CanRun())
            {
                targetSpeed = moveInput * settings.runSpeed * settings.floatFallHorizontalControl;
            }
            
            float currentSpeed = rb.linearVelocity.x;
            float newSpeed = Mathf.MoveTowards(
                currentSpeed, 
                targetSpeed, 
                settings.airAcceleration * Time.fixedDeltaTime * 20f
            );
            
            rb.linearVelocity = new Vector2(newSpeed, rb.linearVelocity.y);
            
            bool shouldFaceRight = moveInput > 0;
            if (shouldFaceRight != movement.FacingRight)
            {
                movement.ForceFlip(shouldFaceRight);
            }
        }
        else
        {
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
    }
    
    private void EndFloating()
    {
        gravityManager.ClearOverride("FloatingFall");
    }
    
    private void UpdateTimers()
    {
        canFloat = true; // Always can float when conditions met
    }
    
    // NEW: Helper method to exit to grounded state
    private void ExitToGroundedState()
    {
        
        
        // Clear any remaining float effects
        if (isFloating)
        {
            StopFloating();
        }
        
        // Check movement input to decide between idle and walk
        if (Mathf.Abs(inputHandler.MoveInput.x) > 0.1f)
        {
            // Check if should run
            if (inputHandler.DashHeld && controller.AbilitySystem.CanRun())
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
    
    // NEW: Helper method to exit to appropriate state based on current conditions
    private void ExitToAppropriateState()
    {
        // First check ground
        if (controller.IsEffectivelyGrounded())
        {
            ExitToGroundedState();
            return;
        }
        
        // Then check other states
        if (swimming != null && swimming.IsInWater)
        {
            stateMachine.ChangeState(controller.SwimState);
            return;
        }
        
        // Default to air state
        stateMachine.ChangeState(controller.AirState);
    }
    
    public void ResetFloat()
    {
        canFloat = true;
    }
}