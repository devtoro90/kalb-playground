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
    
    // Dash cooldown tracking - MOVED HERE from KalbDashState
    private float dashCooldownTimer = 0f;
    
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
    
    public bool FacingRight => movement != null ? movement.FacingRight : true;
    
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
        
        // Update dash cooldown timer
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        // Check for ledge grab
        if (settings.ledgeGrabUnlocked && !IsInLedgeState() && ledgeDetector != null && 
            rb.linearVelocity.y < 0 && !collisionDetector.IsGrounded)
        {
            // Skip if on cooldown
            if (!ledgeDetector.IsOnCooldown)
            {
                bool ledgeFound = ledgeDetector.CheckForLedge(this);
                
                if (ledgeFound && !collisionDetector.IsGrounded && 
                    !swimming.IsSwimming && !dashState.IsDashing && 
                    !comboSystem.IsAttacking)
                {
                    // Check if player is at appropriate height to grab
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
        if (collisionDetector.IsGrounded && !collisionDetector.IsTouchingCeiling)
        {
            physics.SetCoyoteTime();
            physics.ResetDoubleJump();
            physics.SetCanDoubleJump(true);
            
            // Reset air dash when grounded
            if (dashState != null)
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
        if (inputHandler.AttackPressed && comboSystem.CanAttack)
        {
            if (CanAttackFromCurrentState())
            {
                inputBuffer.BufferAttack();

                if(inputBuffer.ConsumeBufferedInput("Attack"))
                {
                    stateMachine.ChangeState(combatState);
                    inputHandler.ResetAttackInput();
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
            // Apply a strong forward impulse for running jumps
            Vector2 forwardDirection = movement.FacingRight ? Vector2.right : Vector2.left;
            
            // Calculate forward force - stronger for faster runs
            float runSpeedRatio = Mathf.Clamp01(Mathf.Abs(currentXVelocity) / settings.runSpeed);
            float forwardForce = settings.runJumpForwardForce * runSpeedRatio;
            
            // Apply the forward impulse
            rb.AddForce(forwardDirection * forwardForce, ForceMode2D.Impulse);
            
            // Start jump momentum preservation
            movement.StartJumpMomentum(0.4f); // 0.4 seconds of momentum preservation
            
            
        }
        else if (Mathf.Abs(currentXVelocity) > 0.1f)
        {
            // Walking jump - preserve momentum with shorter duration
            movement.StartJumpMomentum(0.2f); // 0.2 seconds for walking jumps
            
        }
        else
        {
            // Stationary jump - no momentum
            
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

    private bool CanAttackFromCurrentState()
    {
        if (swimming.IsSwimming)
            return false;

        if (stateMachine.CurrentState is KalbDashState)
            return false;
        
        if (stateMachine.CurrentState is KalbIdleState || 
            stateMachine.CurrentState is KalbWalkState ||
            stateMachine.CurrentState is KalbAirState ||
            stateMachine.CurrentState is KalbJumpState ||
            stateMachine.CurrentState is KalbRunState)
            return true;
        
        if (swimming.IsJumpingFromWater && rb.linearVelocity.y > 0)
        {
            return true;
        }
        
        return false;
    }

    private bool CanDashFromCurrentState()
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
        
        if (stateMachine.CurrentState is KalbAirState)
            return true;
        
        if (stateMachine.CurrentState is KalbJumpState)
            return true;
        
        return false;
    }
    
    private bool ShouldEnterRunState()
    {
        if (!abilitySystem.CanRun())
            return false;
        
        if (!collisionDetector.IsGrounded)
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
    
    private bool ShouldContinueRunState()
    {
        return ShouldEnterRunState();
    }

    private bool IsInLedgeState()
    {
        return stateMachine.CurrentState is KalbLedgeState || 
            stateMachine.CurrentState is KalbLedgeClimbState;
    }
    
    private void ExitToAppropriateState()
    {
        if (swimming.IsInWater)
        {
            stateMachine.ChangeState(swimState);
        }
        else if (!collisionDetector.IsGrounded)
        {
            stateMachine.ChangeState(airState);
        }
        else if (Mathf.Abs(inputHandler.MoveInput.x) > 0.1f)
        {
            if (inputHandler.DashHeld && abilitySystem.CanRun())
            {
                stateMachine.ChangeState(runState);
            }
            else
            {
                stateMachine.ChangeState(walkState);
            }
        }
        else
        {
            stateMachine.ChangeState(idleState);
        }
    }
    
    public void ResetDashCooldown()
    {
        dashCooldownTimer = 0f;
    }
    
    public void ForceStateChange(KalbState newState)
    {
        stateMachine.ChangeState(newState);
    }

    private void EnsureProperGravity()
    {
        // Skip if swimming (swimming handles its own gravity)
        if (swimming.IsSwimming || swimming.IsJumpingFromWater)
            return;
        
        // Skip if in states that intentionally modify gravity
        if (stateMachine.CurrentState is KalbDashState && dashState.IsDashing)
            return;
        
        if (stateMachine.CurrentState is KalbLedgeState || 
            stateMachine.CurrentState is KalbLedgeClimbState)
            return;
        
        // Skip if in swim dash
        if (swimming.IsSwimDashing)
            return;
        
        // Reset to normal gravity scale for all other cases
        if (rb.gravityScale != settings.normalGravityScale)
        {
            rb.gravityScale = settings.normalGravityScale;
        }
    }
}