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
    
    // State Properties
    public KalbIdleState IdleState => idleState;
    public KalbWalkState WalkState => walkState;
    public KalbJumpState JumpState => jumpState;
    public KalbAirState AirState => airState;
    public KalbSwimState SwimState => swimState;
    public KalbCombatState CombatState => combatState; 
    public KalbRunState RunState => runState;    
    public KalbDashState DashState => dashState; 
    
    public bool FacingRight => movement != null ? movement.FacingRight : true;
    
    private void Awake()
    {
        InitializeComponents();
        InitializeStateMachine();
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
        
        // Start with idle state
        stateMachine.Initialize(idleState);
    }
    
    private void Update()
    {
        if (health.IsDead) return;
        
        // Check for swimming state transition
        if (swimming.IsInWater && !swimming.IsJumpingFromWater && !(stateMachine.CurrentState is KalbSwimState))
        {
            stateMachine.ChangeState(swimState);
            comboSystem.CancelCombo();
        }
        
        // Update coyote time and jump buffer based on ground state
        if (collisionDetector.IsGrounded)
        {
            physics.SetCoyoteTime();
            
            // Reset air dash when grounded
            if (dashState != null)
            {
                dashState.ResetAirDash();
            }
        }
        
        // Check for jump input
        if (inputHandler.JumpPressed)
        {
            physics.SetJumpBuffer();
        }
        
        // Process jump
        if (!swimming.IsSwimming && !swimming.IsJumpingFromWater && 
            physics.JumpBufferCounter > 0 && physics.CoyoteTimeCounter > 0)
        {
            stateMachine.ChangeState(jumpState);
            inputHandler.ResetJumpInput();
        }

        // DASH INPUT - CRITICAL FIX
        if (inputHandler.DashPressed && abilitySystem.CanDash())
        {
            // Check if we're NOT already in dash state
            // This prevents re-entering dash state while dashing
            if (!(stateMachine.CurrentState is KalbDashState))
            {
                Debug.Log($"Dash pressed, current state: {stateMachine.CurrentState.GetType().Name}");
                
                // Check if dash is available from current state
                if (CanDashFromCurrentState())
                {
                    Debug.Log("Changing to dash state");
                    stateMachine.ChangeState(dashState);
                    inputHandler.ResetDashInput();
                }
                else
                {
                    Debug.Log($"Cannot dash from {stateMachine.CurrentState.GetType().Name}");
                }
            }
            else
            {
                Debug.Log("Already in dash state, ignoring dash input");
            }
        }

        // Check for run state
        if (ShouldEnterRunState() && !(stateMachine.CurrentState is KalbRunState))
        {
            stateMachine.ChangeState(runState);
        }
        else if (stateMachine.CurrentState is KalbRunState && !ShouldContinueRunState())
        {
            ExitToAppropriateState();
        }

        // Check for attack
        if (inputHandler.AttackPressed && comboSystem.CanAttack)
        {
            if (CanAttackFromCurrentState())
            {
                stateMachine.ChangeState(combatState);
                inputHandler.ResetAttackInput();
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
    
    public void TakeDamage(int damage, Vector3 damageSource)
    {
        health.TakeDamage(damage);
        
        // Cancel combo when taking damage
        comboSystem.CancelCombo();

        // Cancel dash when taking damage (NEW)
        if (stateMachine.CurrentState is KalbDashState)
        {
            dashState.ForceResetDash();
        }
        
        // Force exit combat state if taking damage
        if (stateMachine.CurrentState is KalbCombatState)
        {
            stateMachine.ChangeState(airState);
        }
        
        if (health.IsDead)
        {
            // Handle death
            rb.linearVelocity = Vector2.zero;
            animationController.PlayAnimation("Kalb_death");
        }
    }
    
    // Public methods for ability integration
    public bool CanJump()
    {
        return physics.CoyoteTimeCounter > 0 || physics.JumpBufferCounter > 0;
    }

    private bool CanAttackFromCurrentState()
    {
        // Don't allow attacking while swimming
        if (swimming.IsSwimming)
            return false;

        // Don't allow attacking while dashing (NEW)
        if (stateMachine.CurrentState is KalbDashState)
            return false;
        
        // Allow attacking in these states:
        if (stateMachine.CurrentState is KalbIdleState || 
            stateMachine.CurrentState is KalbWalkState ||
            stateMachine.CurrentState is KalbAirState ||
            stateMachine.CurrentState is KalbJumpState ||
            stateMachine.CurrentState is KalbRunState)
            return true;
        
        // Don't allow attacking while jumping from water (special case)
        // Actually, we should allow it! The issue is that IsJumpingFromWater might be true
        // Let's check if we're actually ascending after water jump
        if (swimming.IsJumpingFromWater && rb.linearVelocity.y > 0)
        {
            // Allow attacks while ascending from water jump
            return true;
        }
        
        // Check if we're dashing (you'll need to create KalbDashState if not exists)
        // if (stateMachine.CurrentState is KalbDashState)
        //     return false;
        
        return false;
    }

    private bool CanDashFromCurrentState()
    {
        // Don't allow dashing from these states:
        if (stateMachine.CurrentState is KalbSwimState)
            return false;
        
        if (stateMachine.CurrentState is KalbCombatState)
            return false;
        
        // Allow dashing from these states:
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

    
    
    // NEW: Check if should enter run state
    private bool ShouldEnterRunState()
    {
        // Must have run ability unlocked
        if (!abilitySystem.CanRun())
            return false;
        
        // Must be grounded
        if (!collisionDetector.IsGrounded)
            return false;
        
        // Must be holding dash button
        if (!inputHandler.DashHeld)
            return false;
        
        // Must have horizontal input
        if (Mathf.Abs(inputHandler.MoveInput.x) < 0.1f)
            return false;
        
        // Can't enter run from these states
        if (stateMachine.CurrentState is KalbDashState ||
            stateMachine.CurrentState is KalbCombatState ||
            stateMachine.CurrentState is KalbSwimState)
            return false;
        
        return true;
    }
    
    // NEW: Check if should continue run state
    private bool ShouldContinueRunState()
    {
        // Must still meet run conditions
        return ShouldEnterRunState();
    }
    
    // NEW: Exit to appropriate state
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
    
    public void ForceStateChange(KalbState newState)
    {
        stateMachine.ChangeState(newState);
    }
}