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
    
    // NEW: Track if we're in full control mode during bounce
    private bool fullControlDuringBounce = true;
    
    // Constants
    private const float POGO_ATTACK_DURATION = 0.15f;
    private const float POGO_COOLDOWN = 0.2f;
    private const float POGO_BOUNCE_BUFFER = 0.05f;
    private const float PEAK_DETECTION_DELAY = 0.05f;
    private const float BOUNCE_ANIMATION_MIN_DURATION = 0.2f;
    
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
        Debug.Log($"[KalbPogoAttackState] ENTER - CanPogo: {CanPogo}, CurrentChain: {currentPogoChain}");
        
        if (!CanPogo)
        {
            Debug.Log("[KalbPogoAttackState] Cannot pogo, exiting to AirState");
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
        Debug.Log($"[KalbPogoAttackState] EXIT - Chain: {currentPogoChain}, ChainTimer: {pogoChainTimer}");
        
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
                Debug.Log("[Pogo] Cooldown finished, can pogo again");
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
                Debug.Log($"[Pogo] Chain timer expired, resetting chain from {currentPogoChain} to 0");
                currentPogoChain = 0;
            }
        }
        
        // Update bounce animation timer
        if (bounceAnimationTimer > 0)
        {
            bounceAnimationTimer -= Time.deltaTime;
        }
        
        // Check if we're in bounce phase and should transition to falling
        if (isInBounce && !hasReachedBouncePeak)
        {
            // Detect if we've started falling
            if (rb.linearVelocity.y < -0.5f) // Moving down significantly
            {
                Debug.Log($"[Pogo] Detected falling after bounce! Velocity Y: {rb.linearVelocity.y:F2}");
                hasReachedBouncePeak = true;
                
                // Small delay before transitioning to ensure smooth animation
                bouncePeakTimer = PEAK_DETECTION_DELAY;
            }
            // Also detect if we've reached the peak (velocity near zero and then negative)
            else if (rb.linearVelocity.y <= 0.1f && bounceStartY > 0f)
            {
                // Check if we're below the bounce start position (falling)
                if (controller.transform.position.y < bounceStartY - 0.1f)
                {
                    Debug.Log($"[Pogo] Detected falling by position! Current Y: {controller.transform.position.y:F2}, Bounce Start: {bounceStartY:F2}");
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
                    Debug.Log("[Pogo] Transitioning to AirState (falling)");
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
        
        // Check if attack animation finished
        if (isPogoAttacking && pogoAttackTimer <= 0 && !isInBounce)
        {
            Debug.Log("[Pogo] Attack finished, waiting for bounce");
            isPogoAttacking = false;
        }
        
        // If not in bounce and not attacking, check if we should transition
        if (!isInBounce && !isPogoAttacking && !hasReachedBouncePeak)
        {
            // No hit registered, just fall
            Debug.Log("[Pogo] No hit, transitioning to AirState");
            stateMachine.ChangeState(controller.AirState);
            return;
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
        if (isPogoAttacking)
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
                movement.ApplyAirControl(moveInput);
            }
        }
        else if (isInBounce && !hasReachedBouncePeak)
        {
            // MODIFIED: FULL AIR CONTROL DURING BOUNCE PHASE
            // Use the same air control as in AirState/JumpState
            
            // Get current max air speed (run/walk based on dash held)
            float maxAirSpeed = movement.GetCurrentMaxAirSpeed();
            
            // Apply full air control with standard acceleration
            float moveInput = inputHandler.MoveInput.x;
            float currentXVelocity = rb.linearVelocity.x;
            float targetXVelocity = moveInput * maxAirSpeed;
            
            // Use the same air acceleration as in AirState
            float acceleration = settings.airAcceleration;
            
            // Smoothly move toward target velocity
            float newXVelocity = Mathf.MoveTowards(currentXVelocity, targetXVelocity, 
                acceleration * Time.fixedDeltaTime * 50f); // Scale for FixedUpdate
            
            rb.linearVelocity = new Vector2(newXVelocity, rb.linearVelocity.y);
            
            // Flip sprite based on input if needed
            if (Mathf.Abs(moveInput) > 0.1f)
            {
                bool shouldFaceRight = moveInput > 0;
                if (shouldFaceRight != movement.FacingRight)
                {
                    movement.ForceFlip(shouldFaceRight);
                }
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
            Debug.Log($"[Pogo] Chain input detected! Current chain: {currentPogoChain}, Timer: {pogoChainTimer}");
            
            // Check if we can pogo again
            if (CanPogo)
            {
                Debug.Log($"[Pogo] EXECUTING CHAIN POGO #{currentPogoChain + 1}");
                
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
                Debug.Log($"[Pogo] Cannot chain - CanPogo: {CanPogo}, Cooldown: {pogoCooldownTimer}");
            }
        }
        
        // Allow jump during pogo (with full control)
        if (inputHandler.JumpPressed)
        {
            physics.SetJumpBuffer();
            
            if (controller.InputBuffer?.ConsumeBufferedInput("Jump") == true)
            {
                Debug.Log("[Pogo] Jump pressed, transitioning to JumpState");
                stateMachine.ChangeState(controller.JumpState);
                inputHandler.ResetJumpInput();
            }
        }
        
        // MODIFIED: Allow dash during bounce phase
        if (inputHandler.DashPressed && isInBounce && !hasReachedBouncePeak)
        {
            if (controller.AbilitySystem.CanDash() && controller.CanDashFromCurrentState() && controller.DashCooldownTimer <= 0)
            {
                controller.InputBuffer.BufferDash();
                
                if (controller.InputBuffer.ConsumeBufferedInput("Dash"))
                {
                    Debug.Log("[Pogo] Dash pressed during bounce, transitioning to DashState");
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
            Debug.Log($"[Pogo] HIT! Targets: {hitTargets.Length}");
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
        Debug.Log($"[Pogo] Chain increased to {currentPogoChain}, chain timer set to {pogoChainTimer}");
        
        // Set cooldown
        canPogo = false;
        pogoCooldownTimer = settings.pogoAttackCooldown;
        Debug.Log($"[Pogo] Cooldown set to {pogoCooldownTimer}");
        
        // Reset air abilities
        ResetAirAbilities();
        
        // Spawn hit effect
        SpawnHitEffect();
        
        // Play bounce animation
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
        
        // Calculate bounce force based on fall speed
        float fallSpeed = Mathf.Abs(Mathf.Min(prePogoVelocity.y, 0));
        float normalizedFallSpeed = Mathf.Clamp01(fallSpeed / Mathf.Abs(settings.maxFallSpeed));
        float bounceForce = Mathf.Lerp(settings.pogoMinBounceForce, settings.pogoMaxBounceForce, normalizedFallSpeed);
        
        Debug.Log($"[Pogo] Bounce - FallSpeed: {fallSpeed:F2}, Normalized: {normalizedFallSpeed:F2}, Force: {bounceForce:F2}");
        
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
            Debug.Log("[Pogo] Playing bounce animation");
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
            Debug.Log("[Pogo] Reset double jump");
        }
        
        // Reset air dash
        if (settings.pogoResetsAirDash && dashState != null)
        {
            dashState.ResetAirDash();
            Debug.Log("[Pogo] Reset air dash");
        }


        if (controller.FloatFallState != null)
        {
            controller.FloatFallState.ResetFloat();
            Debug.Log("[Pogo] Reset float ability");
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
    
    private void EndPogoAttack()
    {
        Debug.Log($"[Pogo] EndPogoAttack - Chain: {currentPogoChain}, ChainTimer: {pogoChainTimer}");
        
        isPogoAttacking = false;
        
        // If we can still chain, stay in this state but ready for next input
        if (pogoChainTimer > 0 && currentPogoChain < settings.maxPogoChains)
        {
            Debug.Log($"[Pogo] Waiting for chain input... {pogoChainTimer:F2}s remaining");
            // Stay in state, waiting for next attack input
            return;
        }
        
        // If we're in bounce phase, let the falling detection handle transition
        if (isInBounce)
        {
            Debug.Log("[Pogo] In bounce phase, will transition when falling");
            return;
        }
        
        // Exit to appropriate state
        if (controller.IsEffectivelyGrounded())
        {
            Debug.Log("[Pogo] Grounded, exiting to ground state");
            if (Mathf.Abs(inputHandler.MoveInput.x) > 0.1f)
            {
                stateMachine.ChangeState(controller.WalkState);
            }
            else
            {
                stateMachine.ChangeState(controller.IdleState);
            }
        }
        else
        {
            Debug.Log("[Pogo] In air, exiting to AirState");
            stateMachine.ChangeState(controller.AirState);
        }
    }
    
    public void ForceReset()
    {
        Debug.Log("[Pogo] Force reset");
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