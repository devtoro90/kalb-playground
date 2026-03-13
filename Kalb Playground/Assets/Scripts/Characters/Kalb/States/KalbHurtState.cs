// KalbHurtState.cs - COMPLETE REWRITE with proper air knockback
using UnityEngine;

public class KalbHurtState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbMovement movement;
    private KalbPhysics physics;
    private Rigidbody2D rb;
    private KalbHitReaction hitReaction;
    private KalbAnimationController animController;
    private KalbSettings settings;
    private KalbSwimming swimming;
    private MetroidvaniaCamera gameCamera;

    private float hurtStateTimer = 0f;
    private Vector2 knockbackVelocity;
    private Vector2 hitSource;
    private int damageTaken;

    // Track if we've applied the initial knockback
    private bool initialKnockbackApplied = false;

    // Store the target horizontal velocity to maintain during air hits
    private float targetHorizontalVelocity = 0f;

    // How long to maintain full knockback in air
    private float airKnockbackMaintainTimer = 0f;
    private const float AIR_KNOCKBACK_MAINTAIN_DURATION = 0.15f;

    public KalbHurtState(KalbController controller, KalbStateMachine stateMachine)
        : base(controller, stateMachine)
    {
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

    public override void Enter()
    {


        // Set timer
        hurtStateTimer = settings.hitStunDuration;

        // Reset tracking variables
        initialKnockbackApplied = false;
        airKnockbackMaintainTimer = 0f;

        // Cancel all current actions
        CancelCurrentActions();

        // Calculate knockback (this will set knockbackVelocity)
        CalculateKnockback();

        // Play hurt animation
        PlayHurtAnimation();

        // Clear input buffers
        controller.InputBuffer?.ClearAllBuffersOnStateChange();

        // Reset jump state
        physics.ResetJumpState();

        // Find camera for screen shake
        if (gameCamera == null)
        {
            gameCamera = Object.FindFirstObjectByType<MetroidvaniaCamera>();
        }

        // Trigger camera shake
        if (gameCamera != null)
        {
            gameCamera.TriggerScreenShake(0.15f, 0.2f, Vector3.one, true);
        }


    }

    public override void Exit()
    {

        hurtStateTimer = 0f;
        initialKnockbackApplied = false;
    }

    public override void Update()
    {
        // Update timers
        if (hurtStateTimer > 0)
        {
            hurtStateTimer -= Time.deltaTime;

            if (hurtStateTimer <= 0)
            {
                ExitToAppropriateState();
                return;
            }
        }

        // Update air knockback maintain timer
        if (airKnockbackMaintainTimer > 0)
        {
            airKnockbackMaintainTimer -= Time.deltaTime;
        }

        // Emergency exit if we fall in water
        if (swimming != null && swimming.IsInWater)
        {
            stateMachine.ChangeState(controller.SwimState);
        }
    }

    public override void FixedUpdate()
    {
        // Apply knockback physics
        ApplyKnockback();
    }

    public override void HandleInput()
    {
        // No input during hurt state
    }

    private void CancelCurrentActions()
    {
        // Stop movement
        if (movement != null)
        {
            movement.StopHorizontalMovement();
            movement.ResetSmoothing();
        }

        // Cancel combo
        if (controller.ComboSystem != null)
        {
            controller.ComboSystem.CancelCombo();
        }

        // Reset dash if dashing
        if (controller.DashState != null && controller.DashState.IsDashing)
        {
            controller.DashState.ForceResetDash();
        }
    }

    private void CalculateKnockback()
    {
        if (rb == null) return;

        bool isGrounded = controller.IsEffectivelyGrounded();

        // Determine knockback direction
        Vector2 knockbackDirection;

        // If hit source is very close, use opposite of facing direction
        if (Vector2.Distance(controller.transform.position, hitSource) < 0.3f)
        {
            float facingMultiplier = controller.FacingRight ? -1f : 1f;
            knockbackDirection = new Vector2(facingMultiplier, 0.7f).normalized;
        }
        else
        {
            // Normal direction away from hit source
            knockbackDirection = (controller.transform.position - (Vector3)hitSource).normalized;
            // Ensure there's always some upward component
            knockbackDirection.y = Mathf.Max(0.5f, Mathf.Abs(knockbackDirection.y));
            knockbackDirection.Normalize();
        }

        // FORCE HORIZONTAL MAGNITUDE to exactly hitKnockbackForce
        // This ensures the horizontal force is always the same regardless of direction
        float forcedHorizontalMagnitude = settings.hitKnockbackForce;

        // Calculate the horizontal component based on direction
        float horizontalSign = Mathf.Sign(knockbackDirection.x);
        float horizontalForce = horizontalSign * forcedHorizontalMagnitude;

        // Calculate vertical force (separate from horizontal)
        // Use hitUpwardForce for the vertical component
        float verticalForce = settings.hitUpwardForce;

        // Add a small amount of the horizontal force to vertical for diagonal feel
        // but keep it mostly separate
        verticalForce += Mathf.Abs(knockbackDirection.y * forcedHorizontalMagnitude * 0.3f);

        // FINAL KNOCKBACK VELOCITY
        // Horizontal is EXACTLY hitKnockbackForce (positive or negative)
        // Vertical is hitUpwardForce + a small diagonal contribution
        knockbackVelocity = new Vector2(
            horizontalForce,
            verticalForce
        );

        // Store target horizontal velocity for air maintenance
        targetHorizontalVelocity = horizontalForce;

        // Set maintain timer based on state
        if (!isGrounded)
        {
            // In air, maintain full knockback longer
            airKnockbackMaintainTimer = 0.25f; // 0.25 seconds of full force in air

        }
        else
        {
            // On ground, brief maintain
            airKnockbackMaintainTimer = 0.1f;

        }

        // IMMEDIATELY apply knockback
        rb.linearVelocity = knockbackVelocity;
        initialKnockbackApplied = true;


    }

    private void ApplyKnockback()
    {
        if (!initialKnockbackApplied || rb == null) return;

        bool isGrounded = controller.IsEffectivelyGrounded();

        if (!isGrounded)
        {
            // AIR KNOCKBACK - Force maintain horizontal velocity
            if (airKnockbackMaintainTimer > 0)
            {
                // During maintain window, FORCE the exact horizontal velocity
                // This ensures the knockback doesn't weaken in air
                rb.linearVelocity = new Vector2(
                    targetHorizontalVelocity,  // Force exact horizontal force
                    rb.linearVelocity.y
                );

                airKnockbackMaintainTimer -= Time.fixedDeltaTime;

            }
            else
            {
                // After maintain window, let it slow down naturally but keep some momentum
                float currentXVelocity = rb.linearVelocity.x;

                // Only apply very light resistance
                if (Mathf.Abs(currentXVelocity) > 0.1f)
                {
                    // Much lighter resistance in air
                    float resistance = 1f * Time.fixedDeltaTime;
                    float newXVelocity = Mathf.MoveTowards(currentXVelocity, 0, resistance);
                    rb.linearVelocity = new Vector2(newXVelocity, rb.linearVelocity.y);
                }
            }
        }
        else
        {
            // GROUND KNOCKBACK - Brief maintain then let friction handle it
            if (airKnockbackMaintainTimer > 0)
            {
                rb.linearVelocity = new Vector2(
                    targetHorizontalVelocity,
                    rb.linearVelocity.y
                );
                airKnockbackMaintainTimer -= Time.fixedDeltaTime;
            }
        }

        // Always ensure gravity is normal
        rb.gravityScale = settings.normalGravityScale;
    }

    private void PlayHurtAnimation()
    {
        if (animController == null)
        {
            Debug.LogError("[HurtState] animController is NULL!");
            return;
        }

        string animationName = settings.hurtAnimationName;

        if (!string.IsNullOrEmpty(animationName))
        {

            animController.PlayAnimation(animationName);
        }
        else
        {
            Debug.LogError("[HurtState] Animation name is empty!");
        }
    }

    private void ExitToAppropriateState()
    {


        if (controller.Health != null && controller.Health.IsDead)
        {
            return;
        }

        if (swimming != null && swimming.IsInWater)
        {
            stateMachine.ChangeState(controller.SwimState);
            return;
        }

        if (controller.IsEffectivelyGrounded())
        {
            if (Mathf.Abs(inputHandler.MoveInput.x) > 0.1f)
            {
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
            stateMachine.ChangeState(controller.AirState);
        }
    }
}