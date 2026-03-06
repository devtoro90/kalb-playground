using UnityEngine;

public class KalbPogoAttackState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbMovement movement;
    private KalbPhysics physics;
    private KalbSwimming swimming;
    private KalbDashState dashState;
    private Rigidbody2D rb;
    private KalbSettings settings;
    private KalbAnimationController animController;
    
    // Pogo state
    private bool isPogoAttacking = false;
    private float pogoAttackTimer = 0f;
    private float pogoCooldownTimer = 0f;
    private float inputLockTimer = 0f;
    private float pogoChainTimer = 0f;
    private int currentPogoChain = 0;
    private bool canPogo = true;
    private bool pogoHitRegistered = false;
    private bool isInBounce = false;
    private bool hasReachedBouncePeak = false;
    private bool bounceAnimationPlaying = false;
    private Vector2 prePogoVelocity;
    private Transform pogoAttackPoint;
    private float bounceStartY = 0f;
    private float bouncePeakTimer = 0f;
    private float bounceAnimationTimer = 0f;
    private float bounceStartTime = 0f;
    
    // Debug
    private float lastDebugTime = 0f;
    private const float DEBUG_INTERVAL = 0.1f;
    
    // Constants
    private const float PEAK_DETECTION_DELAY = 0.05f;
    private const float BOUNCE_ANIMATION_MIN_DURATION = 0.2f;
    private const float MIN_BOUNCE_CONTROL_TIME = 0.15f;
    
    // Properties
    public bool IsPogoAttacking => isPogoAttacking;
    public bool CanPogo => canPogo && pogoCooldownTimer <= 0 && !swimming.IsSwimming && !controller.IsEffectivelyGrounded();
    
    public KalbPogoAttackState(KalbController controller, KalbStateMachine stateMachine) 
        : base(controller, stateMachine)
    {
        inputHandler = controller.InputHandler;
        movement = controller.Movement;
        physics = controller.Physics;
        swimming = controller.Swimming;
        dashState = controller.DashState;
        rb = controller.Rb;
        settings = controller.Settings;
        animController = controller.AnimationController;
        
        // Create attack point
        CreatePogoAttackPoint();
    }
    
    private void CreatePogoAttackPoint()
    {
        GameObject obj = new GameObject("PogoAttackPoint");
        obj.transform.parent = controller.transform;
        pogoAttackPoint = obj.transform;
        UpdateAttackPointPosition();
    }
    
    private void UpdateAttackPointPosition()
    {
        if (pogoAttackPoint != null)
        {
            pogoAttackPoint.localPosition = new Vector3(
                settings.pogoAttackPointOffset.x,
                settings.pogoAttackPointOffset.y,
                0
            );
        }
    }
    
    public override void Enter()
    {
        
        
        if (!CanPogo)
        {
            
            stateMachine.ChangeState(controller.AirState);
            return;
        }
        
        // Reset state for new pogo
        isPogoAttacking = true;
        isInBounce = false;
        hasReachedBouncePeak = false;
        bounceAnimationPlaying = false;
        pogoAttackTimer = settings.pogoAttackDuration;
        pogoHitRegistered = false;
        bouncePeakTimer = 0f;
        bounceAnimationTimer = 0f;
        bounceStartTime = 0f;
        
        // Store pre-pogo velocity for momentum preservation
        prePogoVelocity = rb.linearVelocity;
        
        // Brief input lock at start of pogo
        inputLockTimer = settings.pogoInputControlTime;
        
        // Cancel any upward momentum for faster downward thrust
        if (rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }
        
        // Cancel any ongoing combo
        controller.ComboSystem?.CancelCombo();
        
        // Play pogo attack animation
        if (animController != null && !string.IsNullOrEmpty(settings.pogoAttackAnimation))
        {
            animController.PlayAnimation(settings.pogoAttackAnimation);
        }
        
        // Update attack point position
        UpdateAttackPointPosition();
        
        controller.InputBuffer?.ClearBufferedInput("Jump");
        controller.InputBuffer?.ClearBufferedInput("Dash");
        controller.InputBuffer?.ClearBufferedInput("Attack");
    }
    
    public override void Exit()
    {
        
        
        isPogoAttacking = false;
        pogoHitRegistered = false;
        isInBounce = false;
        hasReachedBouncePeak = false;
        bounceAnimationPlaying = false;
        
        // Restore normal gravity
        controller.GravityManager?.SetNormalGravity();
    }
    
    public override void Update()
    {
        // Update timers
        if (pogoAttackTimer > 0)
        {
            pogoAttackTimer -= Time.deltaTime;
        }
        
        if (pogoCooldownTimer > 0)
        {
            pogoCooldownTimer -= Time.deltaTime;
            if (pogoCooldownTimer <= 0)
            {
                canPogo = true;
                
            }
        }
        
        if (inputLockTimer > 0)
        {
            inputLockTimer -= Time.deltaTime;
        }
        
        // Update chain timer
        if (pogoChainTimer > 0)
        {
            pogoChainTimer -= Time.deltaTime;
            if (pogoChainTimer <= 0)
            {
                
                currentPogoChain = 0;
            }
        }
        
        // Update bounce animation timer
        if (bounceAnimationTimer > 0)
        {
            bounceAnimationTimer -= Time.deltaTime;
        }
        
        // When attack timer expires, enter bounce phase even without hit
        if (isPogoAttacking && pogoAttackTimer <= 0 && !isInBounce)
        {
            
            EnterBouncePhase();
        }
        
        // Check if we're in bounce phase and should transition to falling
        if (isInBounce && !hasReachedBouncePeak)
        {
            // Check if we've been in bounce long enough
            bool hasHadControlTime = (Time.time - bounceStartTime) > MIN_BOUNCE_CONTROL_TIME;
            
            // Detect if we've started falling AND we've had minimum control time
            if (rb.linearVelocity.y < -0.5f && hasHadControlTime)
            {
                
                hasReachedBouncePeak = true;
                
                // Small delay before transitioning to ensure smooth animation
                bouncePeakTimer = PEAK_DETECTION_DELAY;
            }
            // Also detect if we've reached the peak (velocity near zero) AND had control time
            else if (rb.linearVelocity.y <= 0.1f && bounceStartY > 0f && hasHadControlTime)
            {
                // Check if we're below the bounce start position (falling)
                if (controller.transform.position.y < bounceStartY - 0.1f)
                {
                    
                    hasReachedBouncePeak = true;
                    bouncePeakTimer = PEAK_DETECTION_DELAY;
                }
            }
        }
        
        // Countdown after detecting peak before transition
        if (bouncePeakTimer > 0)
        {
            bouncePeakTimer -= Time.deltaTime;
            if (bouncePeakTimer <= 0)
            {
                // Only transition if bounce animation has played for minimum duration
                if (!bounceAnimationPlaying || bounceAnimationTimer <= 0)
                {
                    
                    stateMachine.ChangeState(controller.AirState);
                    return;
                }
            }
        }
        
        // Check for pogo hit during attack window
        if (isPogoAttacking && !pogoHitRegistered && pogoAttackTimer > 0)
        {
            CheckPogoHit();
        }
        
        // Check for swimming
        if (swimming != null && swimming.IsInWater)
        {
            stateMachine.ChangeState(controller.SwimState);
            return;
        }
    }
    
    public override void FixedUpdate()
    {
        // CRITICAL: Log every FixedUpdate to confirm we're here
        
        
        // BOUNCE PHASE HAS HIGHEST PRIORITY - Check this FIRST
        if (isInBounce && !hasReachedBouncePeak)
        {
            // FORCE HORIZONTAL CONTROL DURING BOUNCE
            
            float moveInput = inputHandler.MoveInput.x;
           
            
            // Get current velocity before change
            float beforeVelocityX = rb.linearVelocity.x;
            
            // Determine max speed based on run input
            float maxSpeed = settings.moveSpeed;
            if (inputHandler.DashHeld && controller.AbilitySystem.CanRun())
            {
                maxSpeed = settings.runSpeed;
                
            }
            
            // Calculate target velocity
            float targetXVelocity = moveInput * maxSpeed;
            
            // FORCE DIRECT VELOCITY SET
            if (Mathf.Abs(moveInput) > 0.1f)
            {
                // Set velocity directly
                rb.linearVelocity = new Vector2(targetXVelocity, rb.linearVelocity.y);
                
                // Log the change
                
            }
            else
            {
                // No input - slow down with friction
                float newXVelocity = Mathf.MoveTowards(rb.linearVelocity.x, 0, 
                    settings.airFriction * Time.fixedDeltaTime * 20f);
                rb.linearVelocity = new Vector2(newXVelocity, rb.linearVelocity.y);
                
            }
            
            // Flip sprite based on input if needed
            if (Mathf.Abs(moveInput) > 0.1f)
            {
                bool shouldFaceRight = moveInput > 0;
                if (shouldFaceRight != movement.FacingRight)
                {
                    movement.ForceFlip(shouldFaceRight);
                    
                }
            }
            
            // Log final velocity periodically
            if (Time.time - lastDebugTime > DEBUG_INTERVAL)
            {
                
                lastDebugTime = Time.time;
            }
        }
        else if (isPogoAttacking)
        {
            // During attack phase, limited control
            if (inputLockTimer > 0)
            {
                // During initial input lock, preserve some horizontal momentum
                float preservedSpeed = prePogoVelocity.x * settings.pogoMomentumPreservation;
                rb.linearVelocity = new Vector2(preservedSpeed, rb.linearVelocity.y);
                
            }
            else
            {
                // After initial lock, allow limited air control during attack
                float moveInput = inputHandler.MoveInput.x * 0.3f;
                
                // DIRECT VELOCITY CONTROL
                float targetSpeed = moveInput * settings.moveSpeed;
                float currentSpeed = rb.linearVelocity.x;
                float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, 
                    settings.airAcceleration * Time.fixedDeltaTime * 20f);
                rb.linearVelocity = new Vector2(newSpeed, rb.linearVelocity.y);
                
            }
        }
        
        // Ensure normal gravity
        controller.GravityManager?.SetNormalGravity();
    }
    
    public override void HandleInput()
    {
        // IMPORTANT: This is for CHAINING pogos (hitting attack again during bounce)
        if (inputHandler.AttackPressed && pogoChainTimer > 0 && currentPogoChain < settings.maxPogoChains)
        {
            
            
            // Check if we can pogo again
            if (CanPogo)
            {
                
                
                // Reset attack state but keep chain info
                isPogoAttacking = true;
                isInBounce = false;
                hasReachedBouncePeak = false;
                bounceAnimationPlaying = false;
                pogoAttackTimer = settings.pogoAttackDuration;
                pogoHitRegistered = false;
                bouncePeakTimer = 0f;
                bounceAnimationTimer = 0f;
                
                // Store current velocity for momentum
                prePogoVelocity = rb.linearVelocity;
                
                // Brief input lock
                inputLockTimer = settings.pogoInputControlTime;
                
                // Cancel upward momentum
                if (rb.linearVelocity.y > 0)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                }
                
                // Play attack animation
                if (animController != null && !string.IsNullOrEmpty(settings.pogoAttackAnimation))
                {
                    animController.PlayAnimation(settings.pogoAttackAnimation);
                }
                
                inputHandler.ResetAttackInput();
                return;
            }
            else
            {
                
            }
        }
        
        // Allow jump during pogo (with full control)
        if (inputHandler.JumpPressed)
        {
            physics.SetJumpBuffer();
            
            if (controller.InputBuffer?.ConsumeBufferedInput("Jump") == true)
            {
                
                stateMachine.ChangeState(controller.JumpState);
                inputHandler.ResetJumpInput();
            }
        }
        
        // Allow dash during bounce phase
        if (inputHandler.DashPressed && isInBounce && !hasReachedBouncePeak)
        {
            if (controller.AbilitySystem.CanDash() && controller.CanDashFromCurrentState() && controller.DashCooldownTimer <= 0)
            {
                controller.InputBuffer.BufferDash();
                
                if (controller.InputBuffer.ConsumeBufferedInput("Dash"))
                {
                    
                    stateMachine.ChangeState(controller.DashState);
                    inputHandler.ResetDashInput();
                }
            }
        }
    }
    
    private void CheckPogoHit()
    {
        if (pogoAttackPoint == null) return;
        
        // Check for hits below player
        Collider2D[] hitTargets = Physics2D.OverlapCircleAll(
            pogoAttackPoint.position, 
            settings.pogoAttackRange, 
            settings.pogoTargetLayers
        );
        
        if (hitTargets.Length > 0)
        {
            
            RegisterPogoHit(hitTargets);
            pogoHitRegistered = true;
        }
    }
    
    private void RegisterPogoHit(Collider2D[] targets)
    {
        // Apply effects to targets
        foreach (Collider2D target in targets)
        {
            ApplyPogoEffects(target);
        }
        
        // Apply bounce to player
        ApplyPogoBounce();
        
        // Increment chain
        currentPogoChain++;
        pogoChainTimer = settings.pogoChainWindow;
        
        
        // Set cooldown
        canPogo = false;
        pogoCooldownTimer = settings.pogoAttackCooldown;
        
        
        // Reset air abilities
        ResetAirAbilities();
        
        // Spawn hit effect
        SpawnHitEffect();
        
        // Play bounce animation
        PlayBounceAnimation();
    }
    
    private void EnterBouncePhase()
    {
        isPogoAttacking = false;
        isInBounce = true;
        hasReachedBouncePeak = false;
        bounceStartY = controller.transform.position.y;
        bounceStartTime = Time.time;
        
        
        
        // If no hit was registered, still apply a small bounce
        if (!pogoHitRegistered)
        {
            
            
            // Apply a small bounce even without hit
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x * settings.pogoMomentumPreservation,
                5f // Small bounce force
            );
        }
        
        // Set cooldown
        canPogo = false;
        pogoCooldownTimer = settings.pogoAttackCooldown;
        
        // Increment chain even without hit to prevent infinite pogos
        currentPogoChain++;
        pogoChainTimer = settings.pogoChainWindow;
        
        // Reset air abilities even without hit
        ResetAirAbilities();
        
        // Play bounce animation if available
        PlayBounceAnimation();
    }
    
    private void ApplyPogoEffects(Collider2D target)
    {
        // Apply damage - adapt to your enemy system
        /*
        var health = target.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.TakeDamage((int)settings.pogoAttackDamage);
        }
        */
        
        // Apply knockback
        var targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb != null)
        {
            Vector2 knockbackDirection = settings.pogoAttackKnockbackDirection.normalized;
            targetRb.AddForce(knockbackDirection * settings.pogoAttackKnockback, ForceMode2D.Impulse);
        }
    }
    
    private void ApplyPogoBounce()
    {
        isInBounce = true;
        hasReachedBouncePeak = false;
        bounceStartY = controller.transform.position.y;
        bounceStartTime = Time.time;
        
        // Calculate bounce force based on fall speed
        float fallSpeed = Mathf.Abs(Mathf.Min(prePogoVelocity.y, 0));
        float normalizedFallSpeed = Mathf.Clamp01(fallSpeed / Mathf.Abs(settings.maxFallSpeed));
        float bounceForce = Mathf.Lerp(settings.pogoMinBounceForce, settings.pogoMaxBounceForce, normalizedFallSpeed);
        
        
        
        // Apply bounce with some horizontal preservation
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x * settings.pogoMomentumPreservation,
            bounceForce
        );
        
        // Reduce input lock time on bounce
        inputLockTimer = settings.pogoInputControlTime * 0.5f;
    }
    
    private void PlayBounceAnimation()
    {
        if (animController != null && !string.IsNullOrEmpty(settings.pogoBounceAnimation))
        {
            
            animController.PlayAnimation(settings.pogoBounceAnimation);
            bounceAnimationPlaying = true;
            bounceAnimationTimer = BOUNCE_ANIMATION_MIN_DURATION;
        }
    }
    
    private void ResetAirAbilities()
    {
        // Reset double jump
        if (settings.pogoResetsDoubleJump && physics != null)
        {
            physics.ResetDoubleJump();
            physics.SetCanDoubleJump(true);
            
        }
        
        // Reset air dash
        if (settings.pogoResetsAirDash && dashState != null)
        {
            dashState.ResetAirDash();
            
        }

        if (controller.FloatFallState != null)
        {
            controller.FloatFallState.ResetFloat();
            
        }
    }
    
    private void SpawnHitEffect()
    {
        if (settings.hitEffectPrefab != null && pogoAttackPoint != null)
        {
            GameObject effect = Object.Instantiate(settings.hitEffectPrefab, pogoAttackPoint.position, Quaternion.identity);
            Object.Destroy(effect, settings.hitEffectDuration);
        }
    }
    
    public void ForceReset()
    {
        
        
        isPogoAttacking = false;
        pogoHitRegistered = false;
        isInBounce = false;
        hasReachedBouncePeak = false;
        bounceAnimationPlaying = false;
        currentPogoChain = 0;
        pogoChainTimer = 0;
        pogoCooldownTimer = 0;
        canPogo = true;
        bouncePeakTimer = 0f;
        bounceAnimationTimer = 0f;
    }
    
    // Debug visualization
    public void OnDrawGizmos()
    {
        if (pogoAttackPoint != null && settings != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(pogoAttackPoint.position, settings.pogoAttackRange);
        }
    }
}