using UnityEngine;

public class KalbController : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private KalbSettings settings;
    
    // Core Components
    private KalbInputHandler inputHandler;
    private KalbCollisionDetector collisionDetector;
    private KalbMovement movement;
    private KalbPhysics physics;
    private KalbAnimationController animationController;
    private KalbHealth health;
    private KalbSwimming swimming;
    private KalbAbilitySystem abilitySystem;
    private KalbComboSystem comboSystem;
    private KalbLedgeDetector ledgeDetector;
    private KalbGravityManager gravityManager;
    private KalbInputBuffer inputBuffer;
    private KalbWallJump wallJump;
    
    // State Machine
    private KalbStateMachine stateMachine;
    
    // States
    private KalbIdleState idleState;
    private KalbWalkState walkState;
    private KalbJumpState jumpState;
    private KalbAirState airState;
    private KalbSwimState swimState;
    private KalbCombatState combatState;
    private KalbRunState runState;    
    private KalbDashState dashState;  
    private KalbLedgeState ledgeState;
    private KalbLedgeClimbState ledgeClimbState;
    private KalbWallSlideState wallSlideState;
    private KalbWallLockState wallLockState;
    private KalbPogoAttackState pogoAttackState;
    private KalbFloatFallState floatFallState;
    
    // Dash cooldown tracking - MOVED HERE from KalbDashState
    private float dashCooldownTimer = 0f;
    
    // Ground check tolerance to prevent flickering
    private bool wasGroundedLastFrame = true;
    private float groundStickTimer = 0f;
    private const float GROUND_STICK_THRESHOLD = 0.15f; // How long to stay "grounded" after leaving ground

    private float wallLockCooldownTimer = 0f;
    
    // Properties for component access
    public KalbInputHandler InputHandler => inputHandler;
    public KalbCollisionDetector CollisionDetector => collisionDetector;
    public KalbMovement Movement => movement;
    public KalbPhysics Physics => physics;
    public KalbAnimationController AnimationController => animationController;
    public KalbHealth Health => health;
    public KalbSwimming Swimming => swimming;
    public KalbAbilitySystem AbilitySystem => abilitySystem;
    public KalbSettings Settings => settings;
    public Rigidbody2D Rb => rb;
    public KalbComboSystem ComboSystem => comboSystem;
    public KalbLedgeDetector LedgeDetector => ledgeDetector;
    public KalbGravityManager GravityManager => gravityManager;
    public KalbInputBuffer InputBuffer => inputBuffer;
    public KalbWallJump WallJump => wallJump;
    public float WallLockCooldownTimer
    {
        get => wallLockCooldownTimer;
        set => wallLockCooldownTimer = value;
    }
    
    // Dash cooldown property - NEW
    public float DashCooldownTimer
    {
        get => dashCooldownTimer;
        set => dashCooldownTimer = value;
    }
    
    // State Properties
    public KalbIdleState IdleState => idleState;
    public KalbWalkState WalkState => walkState;
    public KalbJumpState JumpState => jumpState;
    public KalbAirState AirState => airState;
    public KalbSwimState SwimState => swimState;
    public KalbCombatState CombatState => combatState; 
    public KalbRunState RunState => runState;    
    public KalbDashState DashState => dashState; 
    public KalbLedgeState LedgeState => ledgeState;
    public KalbLedgeClimbState LedgeClimbState => ledgeClimbState;
    public KalbWallSlideState WallSlideState => wallSlideState;
    public KalbWallLockState WallLockState => wallLockState;
    public KalbPogoAttackState PogoAttackState => pogoAttackState;
    public KalbFloatFallState FloatFallState => floatFallState;
    
    public bool FacingRight => movement != null ? movement.FacingRight : true;
    public bool IsLookingUp => animationController != null ? animationController.IsLookingUp : false;

    public KalbStateMachine StateMachine => stateMachine;

    public System.Action OnStateChanged;
    public System.Action OnLanded;
    public System.Action OnWallSlideStarted;
    
    private void Awake()
    {
        InitializeComponents();
        InitializeStateMachine();
        SetupPhysicsMaterial();
    }
    
    private void InitializeComponents()
    {
        // Get or add required components
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        
        inputHandler = GetComponent<KalbInputHandler>();
        if (inputHandler == null) inputHandler = gameObject.AddComponent<KalbInputHandler>();
        
        collisionDetector = GetComponent<KalbCollisionDetector>();
        if (collisionDetector == null) collisionDetector = gameObject.AddComponent<KalbCollisionDetector>();
        
        movement = GetComponent<KalbMovement>();
        if (movement == null) movement = gameObject.AddComponent<KalbMovement>();
        
        physics = GetComponent<KalbPhysics>();
        if (physics == null) physics = gameObject.AddComponent<KalbPhysics>();
        
        animationController = GetComponent<KalbAnimationController>();
        if (animationController == null) animationController = gameObject.AddComponent<KalbAnimationController>();
        
        health = GetComponent<KalbHealth>();
        if (health == null) health = gameObject.AddComponent<KalbHealth>();
        
        swimming = GetComponent<KalbSwimming>();
        if (swimming == null) swimming = gameObject.AddComponent<KalbSwimming>();

        abilitySystem = GetComponent<KalbAbilitySystem>();
        if (abilitySystem == null) abilitySystem = gameObject.AddComponent<KalbAbilitySystem>();

        comboSystem = GetComponent<KalbComboSystem>(); 
        if (comboSystem == null) comboSystem = gameObject.AddComponent<KalbComboSystem>();

        ledgeDetector = GetComponent<KalbLedgeDetector>();
        if (ledgeDetector == null) ledgeDetector = gameObject.AddComponent<KalbLedgeDetector>();

        gravityManager = GetComponent<KalbGravityManager>();
        if (gravityManager == null) gravityManager = gameObject.AddComponent<KalbGravityManager>();

        inputBuffer = GetComponent<KalbInputBuffer>();
        if (inputBuffer == null) inputBuffer = gameObject.AddComponent<KalbInputBuffer>();

        wallJump = GetComponent<KalbWallJump>();
        if (wallJump == null) wallJump = gameObject.AddComponent<KalbWallJump>();
        
        // Create default settings if none provided
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<KalbSettings>();
        }

    }
    
    private void InitializeStateMachine()
    {
        stateMachine = new KalbStateMachine();
        
        // Create states
        idleState = new KalbIdleState(this, stateMachine);
        walkState = new KalbWalkState(this, stateMachine);
        jumpState = new KalbJumpState(this, stateMachine);
        airState = new KalbAirState(this, stateMachine);
        swimState = new KalbSwimState(this, stateMachine);
        combatState = new KalbCombatState(this, stateMachine);
        runState = new KalbRunState(this, stateMachine);    
        dashState = new KalbDashState(this, stateMachine);  
        ledgeState = new KalbLedgeState(this, stateMachine);        
        ledgeClimbState = new KalbLedgeClimbState(this, stateMachine); 
        wallSlideState = new KalbWallSlideState(this, stateMachine);    
        wallLockState = new KalbWallLockState(this, stateMachine);
        pogoAttackState = new KalbPogoAttackState(this, stateMachine);
        floatFallState = new KalbFloatFallState(this,stateMachine);
        
        // Start with idle state
        stateMachine.Initialize(idleState);
    }

    private void SetupPhysicsMaterial()
    {
        // Get or create collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            // Load or create frictionless material
            PhysicsMaterial2D frictionlessMaterial = Resources.Load<PhysicsMaterial2D>("Frictionless");
            if (frictionlessMaterial == null)
            {
                // Create it programmatically if not found
                frictionlessMaterial = new PhysicsMaterial2D();
                frictionlessMaterial.name = "Frictionless";
                frictionlessMaterial.friction = 0f;
                frictionlessMaterial.bounciness = 0f;
            }
            
            collider.sharedMaterial = frictionlessMaterial;
        }
    }
    
    private void Update()
    {
        if (health.IsDead) return;
        
        // Update timers
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (wallLockCooldownTimer > 0)
        {
            wallLockCooldownTimer -= Time.deltaTime;
        }
        
        UpdateGroundStickTimer();
        
        // FIXED: Skip certain state checks if in water exit grace period
        bool inWaterGracePeriod = swimming != null && swimming.IsInWaterExitGracePeriod;

        // CRITICAL: Check for wall lock first (highest priority when pushing toward wall)
        if (!inWaterGracePeriod && ShouldEnterWallLockState())
        {
            stateMachine.ChangeState(wallLockState);
            return;
        }
        
        // Then check for wall slide
        if (!inWaterGracePeriod && ShouldEnterWallSlideState() && !(stateMachine.CurrentState is KalbWallSlideState))
        {
            stateMachine.ChangeState(wallSlideState);
            return;
        }

        // Check for ledge grab
        if (!inWaterGracePeriod && abilitySystem.CanLedgeGrab() && !IsInLedgeState() && ledgeDetector != null && 
            rb.linearVelocity.y < 0 && !IsEffectivelyGrounded())
        {
            if (!ledgeDetector.IsOnCooldown)
            {
                bool ledgeFound = ledgeDetector.CheckForLedge(this);
                
                if (ledgeFound && !IsEffectivelyGrounded() && 
                    !swimming.IsSwimming && !dashState.IsDashing && 
                    !comboSystem.IsAttacking)
                {
                    Collider2D playerCollider = GetComponent<Collider2D>();
                    if (playerCollider != null)
                    {
                        float playerBottom = playerCollider.bounds.min.y;
                        float ledgeTop = ledgeDetector.LedgePosition.y;
                        
                        float grabRange = 1.0f;
                        if (playerBottom < ledgeTop && playerBottom > ledgeTop - grabRange)
                        {
                            if (rb.linearVelocity.y < -0.1f)
                            {
                                stateMachine.ChangeState(ledgeState);
                                return;
                            }
                        }
                    }
                }
            }
        }
        
        // Check for swimming state transition
        if (swimming.IsInWater && !swimming.IsJumpingFromWater && !(stateMachine.CurrentState is KalbSwimState))
        {
            stateMachine.ChangeState(swimState);
            comboSystem.CancelCombo();

            // Reset air dash when entering swim state
            if (dashState != null)
            {   
                
                dashState.ResetAirDash();
            }

            physics.ResetDoubleJump();
            physics.SetCanDoubleJump(true);
        }
        
        // Update coyote time and jump buffer
        if (IsEffectivelyGrounded() && !collisionDetector.IsTouchingCeiling)
        {
            physics.SetCoyoteTime();
            physics.ResetDoubleJump();
            physics.SetCanDoubleJump(true);
            
            // Reset air dash when grounded
            if (dashState != null && stateMachine.CurrentState is not KalbDashState)
            {
                
                dashState.ResetAirDash();
            }
        }
        else if (collisionDetector.IsTouchingCeiling)
        {
            if (rb.linearVelocity.y > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            }
        }

        // DASH INPUT
        if (inputHandler.DashPressed && abilitySystem.CanDash())
        {
            if (!(stateMachine.CurrentState is KalbDashState))
            {
                if (CanDashFromCurrentState() && dashCooldownTimer <= 0)
                {
                    inputBuffer.BufferDash();

                    if(inputBuffer.ConsumeBufferedInput("Dash"))
                    {
                        
                        stateMachine.ChangeState(dashState);
                        inputHandler.ResetDashInput();
                    }
                }
            }
        }
        
        // Check for run state transitions
        if (ShouldEnterRunState() && !(stateMachine.CurrentState is KalbRunState))
        {
            // Don't transition to run if we're in incompatible states
            if (CanTransitionToRunState())
            {
                stateMachine.ChangeState(runState);
                return; // Skip further input processing this frame
            }
        }
        
        // Check for jump input
        if (inputHandler.JumpPressed)
        {
            physics.SetJumpBuffer();
            inputBuffer.BufferJump();
        }
        
        // Process jump with momentum boost
        if (!swimming.IsSwimming && !swimming.IsJumpingFromWater && 
            physics.JumpBufferCounter > 0 && physics.CoyoteTimeCounter > 0 &&
            stateMachine.CurrentState is not KalbAirState)
        {
            if(inputBuffer.ConsumeBufferedInput("Jump"))
            {
                // Apply strong forward force for running jumps
                ApplyRunningJumpForce();
                stateMachine.ChangeState(jumpState);
                inputHandler.ResetJumpInput();
            }
        }

        // Check for attack
        if (inputHandler.AttackPressed)
        {
            // LOGIC ORDER (Priority):
            // 1. Pogo Attack (air + down held)
            // 2. Wall Attack (in wall lock)
            // 3. Upward Attack (up held)
            // 4. Normal Attack
            
            bool attackHandled = false;
            
            // PRIORITY 1: Pogo Attack - when in air and holding down
            if (!IsEffectivelyGrounded() && inputHandler.IsDownHeld && 
                settings.enablePogoAttack && pogoAttackState != null)
            {
                if (pogoAttackState.CanPogo)
                {
                    inputBuffer.BufferAttack();
                    
                    if (inputBuffer.ConsumeBufferedInput("Attack"))
                    {
                        
                        stateMachine.ChangeState(pogoAttackState);
                        inputHandler.ResetAttackInput();
                        attackHandled = true;
                    }
                }
            }
            
            // PRIORITY 2: Wall Attack
            if (!attackHandled && stateMachine.CurrentState is KalbWallLockState)
            {
                if (CanPerformWallAttack())
                {
                    comboSystem.StartWallAttack();
                    inputHandler.ResetAttackInput();
                    attackHandled = true;
                }
            }
            
            // PRIORITY 3: Upward Attack
            if (!attackHandled && inputHandler.IsUpHeld && settings.enableUpwardAttack)
            {
                // Let combo system handle upward attack
                if (comboSystem.CanPerformUpwardAttack)
                {
                    stateMachine.ChangeState(combatState);
                    inputHandler.ResetAttackInput();
                    attackHandled = true;
                }
            }
            
            // PRIORITY 4: Normal Attack
            if (!attackHandled && CanAttackFromCurrentState())
            {
                inputBuffer.BufferAttack();
                
                if (inputBuffer.ConsumeBufferedInput("Attack"))
                {
                    stateMachine.ChangeState(combatState);
                    inputHandler.ResetAttackInput();
                    attackHandled = true;
                }
            }
        }
        
        // Handle state updates
        stateMachine.HandleInput();
        stateMachine.Update();
    }
    
    private void FixedUpdate()
    {
        if (health.IsDead) return;
        
        stateMachine.FixedUpdate();
    }
    
    // NEW: Update ground stick timer to prevent flickering
    private void UpdateGroundStickTimer()
    {
        bool currentlyGrounded = collisionDetector.IsGrounded;
        
        if (wasGroundedLastFrame && !currentlyGrounded)
        {
            groundStickTimer = GROUND_STICK_THRESHOLD;
        }
        else if (currentlyGrounded)
        {
            groundStickTimer = 0f;
            
            // NEW: Force state transition when grounded
            // This helps catch any states that might be stuck
            if (stateMachine.CurrentState is KalbFloatFallState)
            {
                Debug.Log("[Controller] Grounded while in FloatState - forcing transition");
                stateMachine.ChangeState(idleState);
            }
        }
        else if (groundStickTimer > 0)
        {
            groundStickTimer -= Time.deltaTime;
        }
        
        wasGroundedLastFrame = currentlyGrounded;
    }
    
    public bool IsEffectivelyGrounded()
    {
        bool wasGrounded = collisionDetector.IsGrounded || groundStickTimer > 0;
        bool result = collisionDetector.IsGrounded || groundStickTimer > 0;
        
        if (result && !wasGroundedLastFrame)
        {
            OnLanded?.Invoke();
        }
        
        wasGroundedLastFrame = result;
        return result;
    }
    
    // Apply strong forward force for running jumps
    private void ApplyRunningJumpForce()
    {
        float currentXVelocity = rb.linearVelocity.x;
        
        // Determine if this is a running jump
        bool isRunningJump = false;
        float runSpeedThreshold = settings.runSpeed * 0.7f;
        
        if (stateMachine.CurrentState is KalbRunState)
        {
            isRunningJump = true;
        }
        else if (inputHandler.DashHeld && abilitySystem.CanRun() && Mathf.Abs(currentXVelocity) > runSpeedThreshold)
        {
            isRunningJump = true;
        }
        
        if (isRunningJump)
        {
            // Hollow Knight-style: Strong forward preservation with boost
            float runSpeedRatio = Mathf.Clamp01(Mathf.Abs(currentXVelocity) / settings.runSpeed);
            
            // Preserve 80-100% of running speed
            float preservedSpeed = Mathf.Lerp(
                settings.moveSpeed * settings.jumpHorizontalPreservation,
                settings.runSpeed * settings.jumpHorizontalPreservation,
                runSpeedRatio
            );
            
            // Apply forward boost for running jumps
            Vector2 forwardDirection = movement.FacingRight ? Vector2.right : Vector2.left;
            float forwardForce = settings.runningJumpBoost * runSpeedRatio;
            
            // Set velocity directly for instant response
            rb.linearVelocity = new Vector2(
                forwardDirection.x * preservedSpeed + (forwardDirection.x * forwardForce),
                rb.linearVelocity.y
            );
            
            // Start jump momentum preservation
            movement.StartJumpMomentum(0.3f); // Shorter for more control
        }
        else if (Mathf.Abs(currentXVelocity) > 0.1f)
        {
            // Walking jump - preserve momentum
            float preservedSpeed = currentXVelocity * settings.jumpHorizontalPreservation;
            rb.linearVelocity = new Vector2(preservedSpeed, rb.linearVelocity.y);
            movement.StartJumpMomentum(0.15f);
        }
    }
    
    public void TakeDamage(int damage, Vector3 damageSource)
    {
        health.TakeDamage(damage);
        
        // Cancel combo when taking damage
        comboSystem.CancelCombo();

        // Cancel dash when taking damage
        if (stateMachine.CurrentState is KalbDashState)
        {
            dashState.ForceResetDash();
            dashCooldownTimer = 0f;
        }
        
        // Force exit combat state if taking damage
        if (stateMachine.CurrentState is KalbCombatState)
        {
            stateMachine.ChangeState(airState);
        }
        
        if (health.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            animationController.PlayAnimation("Kalb_death");
        }
    }
    
    public bool CanJump()
    {
        return physics.CoyoteTimeCounter > 0 || physics.JumpBufferCounter > 0;
    }

    public bool CanAttackFromCurrentState()
    {
        if (swimming.IsSwimming)
            return false;

        if (stateMachine.CurrentState is KalbDashState)
            return false;
        
        if (stateMachine.CurrentState is KalbWallLockState)
            return true;
        
        if (stateMachine.CurrentState is KalbIdleState || 
            stateMachine.CurrentState is KalbWalkState ||
            stateMachine.CurrentState is KalbAirState ||
            stateMachine.CurrentState is KalbJumpState ||
            stateMachine.CurrentState is KalbRunState)
            return true;
        
        if (stateMachine.CurrentState is KalbCombatState && 
            (comboSystem.IsUpwardAttacking || comboSystem.IsWallAttacking))
            return true;
        
        if (swimming.IsJumpingFromWater && rb.linearVelocity.y > 0)
        {
            return true;
        }
        
        return false;
    }

    public bool CanDashFromCurrentState()
    {
        if (stateMachine.CurrentState is KalbSwimState)
            return false;
        
        if (stateMachine.CurrentState is KalbCombatState)
            return false;
        
        if (stateMachine.CurrentState is KalbIdleState)
            return true;
        
        if (stateMachine.CurrentState is KalbWalkState)
            return true;
        
        if (stateMachine.CurrentState is KalbRunState)
            return true;
        
        if (stateMachine.CurrentState is KalbAirState && dashState.AirDashCount < settings.maxAirDashes)
            return true;
        
        if (stateMachine.CurrentState is KalbJumpState)
            return true;
        
        return false;
    }
    
    private bool ShouldEnterRunState()
    {
        if (!abilitySystem.CanRun())
            return false;
        
        if (!IsEffectivelyGrounded()) // Use effective grounded check
            return false;
        
        if (!inputHandler.DashHeld)
            return false;
        
        if (Mathf.Abs(inputHandler.MoveInput.x) < 0.1f)
            return false;
        
        if (stateMachine.CurrentState is KalbDashState ||
            stateMachine.CurrentState is KalbCombatState ||
            stateMachine.CurrentState is KalbSwimState)
            return false;
        
        return true;
    }

    private bool CanTransitionToRunState()
    {
        // Can't transition to run from these states
        if (stateMachine.CurrentState is KalbDashState ||
            stateMachine.CurrentState is KalbCombatState ||
            stateMachine.CurrentState is KalbSwimState ||
            stateMachine.CurrentState is KalbLedgeState ||
            stateMachine.CurrentState is KalbLedgeClimbState)
        {
            return false;
        }
        
        return true;
    }

    private bool ShouldEnterWallSlideState()
    {
        // Check if wall jump/slide ability is unlocked
        if (abilitySystem != null && !abilitySystem.CanWallJump())
        {
            return false;
        }

        if (wallJump == null) return false;
        
        // Don't enter if we're on wall slide cooldown (from ledge release)
        if (wallJump.CooldownRemaining > 0)
        {
            return false;
        }
        
        // Must be wall sliding (active state)
        if (!wallJump.IsWallSliding) return false;
        
        // Don't enter from incompatible states
        if (stateMachine.CurrentState is KalbDashState ||
            stateMachine.CurrentState is KalbCombatState ||
            stateMachine.CurrentState is KalbSwimState ||
            stateMachine.CurrentState is KalbLedgeState ||
            stateMachine.CurrentState is KalbLedgeClimbState ||
            stateMachine.CurrentState is KalbWallLockState)
        {
            return false;
        }
        
        return true;
    }

    private bool ShouldCheckForWallSlide()
    {
        // Check for wall slide transitions from these states:
        return stateMachine.CurrentState is KalbAirState ||
            stateMachine.CurrentState is KalbJumpState ||
            stateMachine.CurrentState is KalbDashState; // Optional: after dash ends
    }

    private bool ShouldEnterWallLockState()
    {
        if (!abilitySystem.CanWallLock())
            return false;
        
        // Don't check if we're already in wall lock or transitioning to it
        if (stateMachine.CurrentState is KalbWallLockState)
            return false;
        
        if (!wallJump.IsWallSliding)
            return false;
        
        // Check if pushing toward wall
        float inputDirection = Mathf.Sign(inputHandler.MoveInput.x);
        float wallSide = wallJump.WallSide;
        
        bool pushingTowardWall = Mathf.Abs(inputHandler.MoveInput.x) > settings.wallLockInputThreshold && 
                                Mathf.Approximately(inputDirection, wallSide);
        
        if (!pushingTowardWall)
            return false;
        
        // Don't enter from incompatible states
        if (stateMachine.CurrentState is KalbDashState ||
            stateMachine.CurrentState is KalbCombatState ||
            stateMachine.CurrentState is KalbSwimState ||
            stateMachine.CurrentState is KalbLedgeState ||
            stateMachine.CurrentState is KalbLedgeClimbState)
        {
            return false;
        }
        
        return true;
    }

    public bool IsInLedgeState()
    {
        return stateMachine.CurrentState is KalbLedgeState || 
            stateMachine.CurrentState is KalbLedgeClimbState;
    }

    public void ResetDashCooldown()
    {
        dashCooldownTimer = 0f;
    }
    
    public void ForceStateChange(KalbState newState)
    {
        stateMachine.ChangeState(newState);
        OnStateChanged?.Invoke();
    }

    public bool CanLookUp()
    {
        // Can look up when grounded, not moving significantly, and not in action states
        bool isGrounded = IsEffectivelyGrounded();
        bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        bool isInActionState = dashState.IsDashing || 
                            comboSystem.IsAttacking || 
                            swimming.IsSwimming ||
                            (wallJump != null && wallJump.IsWallSliding) ||
                            IsInLedgeState();
        
        return isGrounded && !isMoving && !isInActionState;
    }

    private bool CanPerformWallAttack()
    {
        if (!(stateMachine.CurrentState is KalbWallLockState))
            return false;
        
        if (comboSystem == null)
            return false;
        
        if (!settings.enableWallAttack)
            return false;
        
        if (comboSystem.IsAnyAttackActive)
            return false;
        
        if (wallJump == null || !wallJump.IsTouchingWall)
            return false;
        
        return comboSystem.CanPerformWallAttack;
    }

    

}