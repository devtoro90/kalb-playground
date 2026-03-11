using UnityEngine;

public class KalbDashState : KalbState
{
    [Header("Dash Line Trail")]
    private KalbDashLineTrail dashLineTrail;
    private bool useLineTrailEffect = true;
    private KalbInputHandler inputHandler;
    private KalbCollisionDetector collisionDetector;
    private KalbMovement movement;
    private KalbSwimming swimming;
    private KalbAbilitySystem abilitySystem;
    private KalbSettings settings;
    private KalbDashTrail dashTrail;

    // Dash state
    private bool isDashing = false;
    private float dashTimer = 0f;
    private Vector2 dashDirection = Vector2.right;
    private int airDashCount;
    private float preDashGravityScale;

    // Dash direction type
    public enum DashDirectionType
    {
        Forward,
        Up,
        Down,
        UpDiagonal,
        DownDiagonal
    }
    private DashDirectionType currentDashDirectionType = DashDirectionType.Forward;

    public bool IsDashing => isDashing;
    public float DashTimer => dashTimer;
    public Vector2 DashDirection => dashDirection;
    public int AirDashCount => airDashCount;
    public DashDirectionType CurrentDashDirectionType => currentDashDirectionType;

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
            ExitToAppropriateState();
            return;
        }

        StartDash();

        controller.GravityManager?.SetZeroGravity("Dash");

        controller.InputBuffer?.ClearBufferedInput("Jump");
        controller.InputBuffer?.ClearBufferedInput("Dash");
        controller.InputBuffer?.ClearBufferedInput("Attack");

        if (dashTrail == null)
        {
            dashTrail = controller.GetComponent<KalbDashTrail>();
        }

        // Get line trail reference
        if (dashLineTrail == null)
        {
            dashLineTrail = controller.GetComponent<KalbDashLineTrail>();
            if (dashLineTrail == null && useLineTrailEffect)
            {
                dashLineTrail = controller.gameObject.AddComponent<KalbDashLineTrail>();
            }
        }


        // Start dash effects
        StartDashEffects();


    }

    public override void Exit()
    {
        if (isDashing)
        {
            EndDash();
        }

        // Stop dash effects
        StopDashEffects();
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
        if (controller.IsEffectivelyGrounded())
        {
            return true;
        }

        // Air dash - check limits
        if (!settings.canAirDash)
        {
            return false;
        }

        // Check air dash count - ALL air dashes count toward the limit

        if (airDashCount >= settings.maxAirDashes)
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

        // Determine dash direction type for animation
        DetermineDashDirectionType();

        // Track air dash - ALL air dashes count toward the limit
        if (!controller.IsEffectivelyGrounded() || currentDashDirectionType != DashDirectionType.Forward)
        {
            airDashCount++;

            // Debug log to track air dashes
        }

        // Cancel combo
        controller.ComboSystem?.CancelCombo();

        // Stop movement
        movement.StopHorizontalMovement();
        movement.ResetSmoothing();

        // Play appropriate animation
        PlayDashAnimation();
    }

    private void DetermineDashDirection()
    {
        // Default to facing
        dashDirection = movement.FacingRight ? Vector2.right : Vector2.left;

        // Use input
        if (Mathf.Abs(inputHandler.MoveInput.x) > 0.1f || Mathf.Abs(inputHandler.MoveInput.y) > 0.1f)
        {
            Vector2 rawInput = new Vector2(inputHandler.MoveInput.x, inputHandler.MoveInput.y);

            // FIX 1: If grounded, ignore downward input and treat as forward/horizontal
            if (controller.IsEffectivelyGrounded() && rawInput.y < -0.1f)
            {
                // Grounded + down pressed = dash forward only
                dashDirection = new Vector2(rawInput.x, 0).normalized;

                // If no horizontal input, default to facing direction
                if (Mathf.Abs(dashDirection.x) < 0.1f)
                {
                    dashDirection = movement.FacingRight ? Vector2.right : Vector2.left;
                }
            }
            else
            {
                // Normal dash direction for all other cases
                dashDirection = rawInput.normalized;
            }

            // Apply diagonal multiplier if not pure horizontal/vertical
            if (settings.canDashDiagonal &&
                Mathf.Abs(inputHandler.MoveInput.x) > 0.1f &&
                Mathf.Abs(inputHandler.MoveInput.y) > 0.1f)
            {
                dashDirection *= settings.diagonalDashMultiplier;
            }
        }
    }

    private void DetermineDashDirectionType()
    {
        float horizontalInput = Mathf.Abs(inputHandler.MoveInput.x);
        float verticalInput = Mathf.Abs(inputHandler.MoveInput.y);
        float inputThreshold = 0.1f;

        // FIX 1: If grounded and trying to dash down, force forward dash
        if (controller.IsEffectivelyGrounded() && inputHandler.MoveInput.y < -0.1f)
        {
            currentDashDirectionType = DashDirectionType.Forward;
            return;
        }

        // If no directional input, use forward dash
        if (horizontalInput < inputThreshold && verticalInput < inputThreshold)
        {
            currentDashDirectionType = DashDirectionType.Forward;
            return;
        }

        // Pure vertical dashes
        if (horizontalInput < inputThreshold && verticalInput > inputThreshold)
        {
            if (inputHandler.MoveInput.y > 0)
                currentDashDirectionType = DashDirectionType.Up;
            else
                currentDashDirectionType = DashDirectionType.Down;
            return;
        }

        // Pure horizontal dashes
        if (horizontalInput > inputThreshold && verticalInput < inputThreshold)
        {
            currentDashDirectionType = DashDirectionType.Forward;
            return;
        }

        // Diagonal dashes
        if (horizontalInput > inputThreshold && verticalInput > inputThreshold)
        {
            if (inputHandler.MoveInput.y > 0)
                currentDashDirectionType = DashDirectionType.UpDiagonal;
            else
                currentDashDirectionType = DashDirectionType.DownDiagonal;
        }
    }

    private void PlayDashAnimation()
    {
        switch (currentDashDirectionType)
        {
            case DashDirectionType.Forward:
                controller.AnimationController.PlayAnimation("Kalb_dash");
                break;
            case DashDirectionType.Up:
                controller.AnimationController.PlayAnimation("Kalb_dash_up");
                break;
            case DashDirectionType.Down:
                controller.AnimationController.PlayAnimation("Kalb_dash_down");
                break;
            case DashDirectionType.UpDiagonal:
                controller.AnimationController.PlayAnimation("Kalb_dash_up_diagonal");
                break;
            case DashDirectionType.DownDiagonal:
                controller.AnimationController.PlayAnimation("Kalb_dash_down_diagonal");
                break;
        }
    }

    private void ApplyDashMovement()
    {
        if (!isDashing || controller.Rb == null) return;

        // Apply dash movement based on direction
        controller.Rb.linearVelocity = dashDirection * settings.dashSpeed;
        controller.Rb.gravityScale = 0f;
    }

    private void EndDash()
    {
        if (!isDashing) return;

        isDashing = false;

        controller.GravityManager.ClearOverride("Dash");

        // FIX 2: Handle upward dash momentum differently
        bool wasUpwardDash = currentDashDirectionType == DashDirectionType.Up ||
                            currentDashDirectionType == DashDirectionType.UpDiagonal;

        // Store current velocity before restoring gravity
        Vector2 endVelocity = controller.Rb.linearVelocity;

        // Restore gravity
        controller.Rb.gravityScale = preDashGravityScale;

        if (wasUpwardDash)
        {
            // FIX 2: For upward dashes, kill upward momentum and let gravity take over
            // This prevents the floaty jump after upward dash
            controller.Rb.linearVelocity = new Vector2(
                endVelocity.x * settings.dashEndSlowdown,
                Mathf.Min(endVelocity.y * 0.2f, 2f) // Reduce upward momentum significantly
            );
        }
        else
        {
            // Normal dash slowdown for other directions
            controller.Rb.linearVelocity = new Vector2(
                endVelocity.x * settings.dashEndSlowdown,
                endVelocity.y * settings.dashEndSlowdown
            );
        }

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

        if (controller.FloatFallState != null)
        {
            controller.FloatFallState.ResetFloat();
        }

        // Check for wall slide immediately after dash
        if (controller.WallJump != null && controller.WallJump.IsWallSliding &&
            controller.AbilitySystem != null && controller.AbilitySystem.CanWallJump())
        {
            stateMachine.ChangeState(controller.WallSlideState);
            return;
        }

        // Check swimming
        if (swimming != null && swimming.IsInWater)
        {
            stateMachine.ChangeState(controller.SwimState);
            return;
        }

        // FIX 2: For upward dashes, ALWAYS go to falling state
        bool wasUpwardDash = currentDashDirectionType == DashDirectionType.Up ||
                            currentDashDirectionType == DashDirectionType.UpDiagonal;

        // Check grounded
        if (!controller.IsEffectivelyGrounded())
        {
            // For upward dashes, force air state (which will show falling animation)
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
    public void ResetAirDash()
    {

        airDashCount = 0;
    }

    public void ForceResetDash()
    {

        isDashing = false;
        dashTimer = 0f;
        airDashCount = 0;

        if (controller.Rb != null)
            controller.Rb.gravityScale = settings.normalGravityScale;
    }

    // NEW: Helper method to check if a specific direction dash is available
    public bool CanDashInDirection(DashDirectionType direction)
    {
        if (!CanDash()) return false;

        // If we're grounded, all directions are available
        if (controller.IsEffectivelyGrounded()) return true;

        // If we're in air, check air dash count

        if (airDashCount >= settings.maxAirDashes) return false;

        return true;
    }

    private void StartDashEffects()
    {


        // Get references if needed
        if (dashTrail == null)
        {
            dashTrail = controller.GetComponent<KalbDashTrail>();

        }

        // Start ghost trail
        if (dashTrail != null)
        {

            dashTrail.StartDashTrail();
        }
        else
        {
            Debug.LogError("[DashState] dashTrail is NULL even after GetComponent!");
        }

        // Start line trail effect
        if (dashLineTrail != null && useLineTrailEffect)
        {
            dashLineTrail.StartDashLines(dashDirection, settings.dashDuration);
        }

        // Start flash coroutine
        controller.StartCoroutine(DashFlashEffect());
    }

    private void StopDashEffects()
    {
        if (dashTrail != null)
        {
            dashTrail.StopDashTrail();
            dashTrail.ForceCleanupTrails();
        }

        if (dashLineTrail != null && useLineTrailEffect)
        {
            dashLineTrail.StopDashLines();
        }

        if (controller.ParticleController != null)
        {
            controller.ParticleController.StopDashTrailParticles();
        }
    }

    private System.Collections.IEnumerator DashFlashEffect()
    {
        SpriteRenderer sprite = controller.GetComponent<SpriteRenderer>();
        if (sprite == null) yield break;

        Color originalColor = sprite.color;
        float flashDuration = settings.dashDuration * 0.5f;
        float halfFlash = flashDuration * 0.5f;

        // Flash to white
        float elapsed = 0f;
        while (elapsed < halfFlash)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfFlash;
            sprite.color = Color.Lerp(originalColor, Color.white, t);
            yield return null;
        }

        // Flash back
        elapsed = 0f;
        while (elapsed < halfFlash)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfFlash;
            sprite.color = Color.Lerp(Color.white, originalColor, t);
            yield return null;
        }

        sprite.color = originalColor;
    }
}