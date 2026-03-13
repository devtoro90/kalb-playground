// KalbHurtState.cs
using UnityEngine;

public class KalbHurtState : KalbState
{
    // Component references
    private KalbInputHandler inputHandler;
    private KalbMovement movement;
    private KalbPhysics physics;
    private Rigidbody2D rb;
    private KalbHitReaction hitReaction;
    private KalbAnimationController animController;
    private KalbSettings settings;
    private KalbSwimming swimming;

    // Hurt state variables
    private float hurtStateTimer = 0f;
    private float minimumHurtDuration;
    private bool hasKnockbackApplied = false;
    private bool isGroundedHurt = true;
    private Vector2 knockbackVelocity;
    private Vector2 hitSource;
    private int damageTaken;

    // Knockback maintenance
    private float knockbackMaintainTimer = 0f;
    private const float KNOCKBACK_MAINTAIN_DURATION = 0.15f;

    // Events for external systems
    public System.Action OnHurtStarted;
    public System.Action OnHurtEnded;

    // Properties
    public bool IsInHurtState => hurtStateTimer > 0;
    public float TimeRemaining => hurtStateTimer;
    public bool IsGroundedHurt => isGroundedHurt;
    public Vector2 HitSource => hitSource;

    public KalbHurtState(KalbController controller, KalbStateMachine stateMachine)
        : base(controller, stateMachine)
    {
        // Get component references
        inputHandler = controller.InputHandler;
        movement = controller.Movement;
        physics = controller.Physics;
        rb = controller.Rb;
        hitReaction = controller.HitReaction;
        animController = controller.AnimationController;
        settings = controller.Settings;
        swimming = controller.Swimming;
    }

    public void SetHitData(int damage, Vector2 source)
    {
        damageTaken = damage;
        hitSource = source;
    }

    float entryTime;
    public override void Enter()
    {
        Debug.Log($"[HurtState] ENTER - Damage: {damageTaken}, Source: {hitSource}, Grounded: {controller.IsEffectivelyGrounded()}");
        Debug.Log($"hurtStateMinDuration: {settings.hurtStateMinDuration}, hitStunDuration: {settings.hitStunDuration}");
        entryTime = Time.time;
        hurtStateTimer = Mathf.Max(settings.hitStunDuration, settings.hurtStateMinDuration);
        Debug.Log($"[HurtState] Timer set to {hurtStateTimer:F2}s based on settings");

        // Don't enter hurt state if already in a hurt state
        if (controller.StateMachine.CurrentState is KalbHurtState) return;

        // DEBUG: Check if settings exists
        if (settings == null)
        {
            Debug.LogError("[HurtState] CRITICAL: settings is NULL!");
            return;
        }

        // DEBUG: Log all relevant settings values
        Debug.Log($"[HurtState] Settings values - hitStunDuration: {settings.hitStunDuration}, hurtStateMinDuration: {settings.hurtStateMinDuration}");
        Debug.Log($"[HurtState] Settings - hitKnockbackForce: {settings.hitKnockbackForce}, hitUpwardForce: {settings.hitUpwardForce}");
        Debug.Log($"[HurtState] Settings - hurtAnimationName: '{settings.hurtAnimationName}'");

        // Store initial state
        isGroundedHurt = controller.IsEffectivelyGrounded();

        // CRITICAL: Check if these values are actually set
        if (settings.hitStunDuration <= 0)
        {
            Debug.LogWarning("[HurtState] hitStunDuration is <= 0, using default 0.3f");
            settings.hitStunDuration = 0.3f;
        }

        if (settings.hurtStateMinDuration <= 0)
        {
            Debug.LogWarning("[HurtState] hurtStateMinDuration is <= 0, using default 0.2f");
            settings.hurtStateMinDuration = 0.2f;
        }

        // DEBUG: Show what we set
        Debug.Log($"[HurtState] Timer set to {hurtStateTimer:F2}s (HitStun: {settings.hitStunDuration}, MinDuration: {minimumHurtDuration})");

        // If timer is still 0, force a default value
        if (hurtStateTimer <= 0)
        {
            Debug.LogError("[HurtState] Timer is still 0! Forcing to 0.3f");
            hurtStateTimer = 0.3f;
        }

        hasKnockbackApplied = false;
        knockbackMaintainTimer = KNOCKBACK_MAINTAIN_DURATION;

        // CRITICAL: Cancel any ongoing actions
        CancelCurrentActions();

        // Calculate and apply knockback
        CalculateKnockback();

        // Play appropriate hurt animation
        PlayHurtAnimation();

        // Clear all input buffers
        controller.InputBuffer?.ClearAllBuffersOnStateChange();

        // Reset jump state
        physics.ResetJumpState();

        // Trigger event
        OnHurtStarted?.Invoke();
    }

    public override void Exit()
    {
        Debug.Log($"[HurtState] EXIT - Had been in state for {Time.time - entryTime:F2}s, Moving to next state");
        // Ensure we're no longer in hurt state
        hurtStateTimer = 0f;

        // Trigger event
        OnHurtEnded?.Invoke();

        Debug.Log("[HurtState] Exited");
    }

    public override void Update()
    {
        // Force knockback during the maintain window
        if (knockbackMaintainTimer > 0)
        {
            ForceKnockback();
        }

        // DEBUG: Log timer value each frame
        Debug.Log($"[HurtState] Update - Timer before: {hurtStateTimer:F2}");

        // Update timer
        if (hurtStateTimer > 0)
        {
            hurtStateTimer -= Time.deltaTime;
            Debug.Log($"[HurtState] Timer after decrement: {hurtStateTimer:F2}");
        }

        // Check if we should exit hurt state - ONLY when timer reaches zero
        if (hurtStateTimer <= 0)
        {
            Debug.Log("[HurtState] Timer expired, exiting to appropriate state");
            ExitToAppropriateState();
            return;
        }

        // Check for swimming (emergency exit if we fall in water)
        if (swimming != null && swimming.IsInWater)
        {
            Debug.Log("[HurtState] Entered water, switching to SwimState");
            stateMachine.ChangeState(controller.SwimState);
            return;
        }

        // Update animation if needed (for looping hurt animations)
        UpdateAnimation();
    }

    public override void FixedUpdate()
    {
        // Force knockback during the maintain window
        if (knockbackMaintainTimer > 0)
        {
            ForceKnockback();
            knockbackMaintainTimer -= Time.fixedDeltaTime;
        }

        // Apply knockback physics
        ApplyKnockbackPhysics();

        // Apply friction/air resistance during hurt
        ApplyMovementResistance();
    }

    public override void HandleInput()
    {
        // NO INPUT DURING HURT STATE - Player cannot act
        // This is intentional - all input is ignored while hurt
    }

    private void CancelCurrentActions()
    {
        // Cancel combo if enabled
        if (settings.cancelComboOnHit && controller.ComboSystem != null)
        {
            controller.ComboSystem.CancelCombo();
        }

        // Cancel dash if enabled
        if (settings.cancelDashOnHit && controller.DashState != null)
        {
            if (controller.DashState.IsDashing)
            {
                controller.DashState.ForceResetDash();
            }
        }

        // Cancel pogo attack
        if (controller.PogoAttackState != null && controller.PogoAttackState.IsPogoAttacking)
        {
            controller.PogoAttackState.ForceReset();
        }

        // Stop any ongoing movement
        movement.StopHorizontalMovement();
        movement.ResetSmoothing();
    }

    private void CalculateKnockback()
    {
        if (rb == null) return;

        Debug.Log($"[HurtState] CalculateKnockback - Current velocity before: {rb.linearVelocity}");

        // Calculate knockback direction based on hit source
        Vector2 knockbackDirection;

        // If the hit source is almost at the same position as player (happens when walking into enemy)
        if (Vector2.Distance(controller.transform.position, hitSource) < 0.1f)
        {
            // Use the player's movement direction to determine knockback
            // If player is moving right, knock them left, and vice versa
            float movementDirection = Mathf.Sign(rb.linearVelocity.x);
            if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            {
                // Knock back opposite to movement direction
                knockbackDirection = new Vector2(-movementDirection, 0.5f);
                Debug.Log($"[HurtState] Using movement-based direction: {knockbackDirection} (movement: {movementDirection})");
            }
            else
            {
                // If not moving, use facing direction
                float facingDirection = controller.FacingRight ? 1f : -1f;
                knockbackDirection = new Vector2(-facingDirection, 0.5f);
                Debug.Log($"[HurtState] Using facing-based direction: {knockbackDirection} (facing: {facingDirection})");
            }
        }
        else
        {
            // Normal calculation when hit source is away from player
            knockbackDirection = (controller.transform.position - (Vector3)hitSource).normalized;
            Debug.Log($"[HurtState] Using hit source direction: {knockbackDirection}");
        }

        // FORCE a significant upward component (minimum 0.5f)
        knockbackDirection.y = Mathf.Max(0.7f, Mathf.Abs(knockbackDirection.y) + settings.hitUpwardForce / settings.hitKnockbackForce);
        knockbackDirection.Normalize();

        // Use strong force
        float force = settings.hitKnockbackForce * 2f; // Double force for testing

        // Minimum force
        if (force < 20f) force = 30f;

        // Calculate knockback velocity
        knockbackVelocity = knockbackDirection * force;

        // Add extra upward boost
        knockbackVelocity.y += 10f;

        Debug.Log($"[HurtState] FINAL Knockback: {knockbackVelocity}");

        // IMMEDIATELY apply knockback
        rb.linearVelocity = knockbackVelocity;
        hasKnockbackApplied = true;

        Debug.Log($"[HurtState] Knockback applied - New velocity: {rb.linearVelocity}");
    }

    private void ForceKnockback()
    {
        if (rb == null || knockbackVelocity == Vector2.zero) return;

        // Force the knockback velocity, preserving the Y component
        rb.linearVelocity = new Vector2(knockbackVelocity.x, rb.linearVelocity.y);

        // Ensure gravity is normal during knockback
        rb.gravityScale = settings.normalGravityScale;
    }

    private void ApplyKnockbackPhysics()
    {
        if (rb == null) return;

        // Apply knockback only once at the beginning
        if (!hasKnockbackApplied)
        {
            rb.linearVelocity = knockbackVelocity;
            hasKnockbackApplied = true;
            Debug.Log($"[HurtState] Knockback applied: {rb.linearVelocity}");
        }
    }

    private void ApplyMovementResistance()
    {
        if (rb == null) return;

        // Apply air resistance during hurt to make knockback feel natural
        if (!controller.IsEffectivelyGrounded())
        {
            // Slow down horizontal movement gradually
            float currentXVelocity = rb.linearVelocity.x;
            if (Mathf.Abs(currentXVelocity) > 0.1f)
            {
                float resistance = 2f * Time.fixedDeltaTime; // Light air resistance
                float newXVelocity = Mathf.MoveTowards(currentXVelocity, 0, resistance);
                rb.linearVelocity = new Vector2(newXVelocity, rb.linearVelocity.y);
            }
        }
    }

    private void PlayHurtAnimation()
    {
        if (animController == null)
        {
            Debug.LogError("[HurtState] animController is NULL!");
            return;
        }

        // Choose between ground and air hurt animations
        string animationToPlay = isGroundedHurt ? settings.hurtAnimationName : settings.hurtAirAnimationName;

        // Fallback to ground hurt if air animation not specified
        if (string.IsNullOrEmpty(animationToPlay))
        {
            animationToPlay = settings.hurtAnimationName;
        }

        Debug.Log($"[HurtState] Attempting to play animation: {animationToPlay}");

        if (!string.IsNullOrEmpty(animationToPlay))
        {
            // Force the animation to play
            animController.PlayAnimation(animationToPlay);
            Debug.Log($"[HurtState] PlayAnimation called for: {animationToPlay}");
        }
        else
        {
            Debug.LogError("[HurtState] Animation name is empty! Check settings.hurtAnimationName");
        }
    }

    private void UpdateAnimation()
    {
        animController.PlayAnimation(settings.hurtAnimationName);
    }

    private bool ShouldExitHurtState()
    {
        // Stay in hurt state until timer expires AND knockback has settled
        if (hurtStateTimer > 0) return false;

        // Additional safety: wait until we're not moving too much
        if (rb != null)
        {
            float velocityMagnitude = rb.linearVelocity.magnitude;
            if (velocityMagnitude > 1f)
            {
                // Still moving a lot, wait a bit longer
                return false;
            }
        }

        return true;
    }

    private void ExitToAppropriateState()
    {
        // Check if we're dead
        if (controller.Health != null && controller.Health.IsDead)
        {
            // Death state would be handled separately
            return;
        }

        // Check for swimming
        if (swimming != null && swimming.IsInWater)
        {
            stateMachine.ChangeState(controller.SwimState);
            return;
        }

        // Check grounded state first
        if (controller.IsEffectivelyGrounded())
        {
            // Check movement input
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
        else
        {
            // In air - go to air state
            stateMachine.ChangeState(controller.AirState);
        }
    }
}