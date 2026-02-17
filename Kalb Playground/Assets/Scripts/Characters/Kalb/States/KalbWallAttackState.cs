using UnityEngine;

public class KalbWallAttackState : KalbState
{
    private KalbInputHandler inputHandler;
    private KalbWallJump wallJump;
    private KalbComboSystem comboSystem;
    private Rigidbody2D rb;
    private KalbPhysics physics;
    private KalbGravityManager gravityManager;
    private KalbMovement movement;
    
    // Wall attack state
    private bool isWallAttacking = false;
    private float attackTimer = 0f;
    private int attackWallSide = 0;
    private bool canExitEarly = true;
    
    public bool IsWallAttacking => isWallAttacking;
    
    public KalbWallAttackState(KalbController controller, KalbStateMachine stateMachine) 
        : base(controller, stateMachine)
    {
        inputHandler = controller.InputHandler;
        wallJump = controller.WallJump;
        comboSystem = controller.ComboSystem;
        rb = controller.Rb;
        physics = controller.Physics;
        gravityManager = controller.GravityManager;
        movement = controller.Movement;
    }
    
    public override void Enter()
    {
        // Start wall attack in combo system
        comboSystem.StartAttack();
        
        // Store wall side from wall jump
        attackWallSide = wallJump.WallSide;
        
        // Set wall attacking flag
        isWallAttacking = true;
        attackTimer = controller.Settings.wallAttackDuration;
        
        // FREEZE POSITION - don't move during attack
        rb.linearVelocity = Vector2.zero;
        gravityManager.SetZeroGravity("WallAttack");
        
        // Ensure we're pushed against wall
        if (wallJump.IsTouchingWall)
        {
            Vector2 wallPush = new Vector2(attackWallSide * 5f, 0);
            rb.AddForce(wallPush, ForceMode2D.Impulse);
        }
        
        // Clear input buffers
        controller.InputBuffer?.ClearBufferedInput("Jump");
        controller.InputBuffer?.ClearBufferedInput("Dash");
        controller.InputBuffer?.ClearBufferedInput("Attack");
        
        // Log for debugging
        
        canExitEarly = true;
    }
    
    public override void Exit()
    {
        isWallAttacking = false;
        
        // Restore gravity
        gravityManager.ClearOverride("WallAttack");
        
        // Set small cooldown for wall attacks
        if (comboSystem != null)
        {
            // Cooldown is handled in combo system
        }
    }
    
    public override void Update()
    {
        // Update attack timer
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
            
            // Check if attack animation should end
            if (attackTimer <= 0)
            {
                EndWallAttack();
                return;
            }
        }
        
        // Check if we should exit early (jump or drop)
        if (canExitEarly)
        {
            // Jump to cancel into wall jump
            if (inputHandler.JumpPressed && controller.Settings.allowWallJumpDuringAttack)
            {
                ExecuteWallJumpFromAttack();
                return;
            }
            
            // Pressing away from wall to drop
            if (IsPressingAwayFromWall() && attackTimer < controller.Settings.wallAttackDuration * 0.7f)
            {
                ReleaseFromWall();
                return;
            }
        }
        
        // If we lose wall contact, exit
        if (!wallJump.IsTouchingWall)
        {
            ExitToFall();
            return;
        }
    }
    
    public override void FixedUpdate()
    {
        if (!isWallAttacking) return;
        
        // MAINTAIN POSITION - stick to wall during attack
        rb.linearVelocity = Vector2.zero;
        
        // Apply force toward wall to maintain contact
        if (wallJump.IsTouchingWall)
        {
            Vector2 wallStickForce = new Vector2(attackWallSide * 10f, 0);
            rb.AddForce(wallStickForce);
        }
        
        // Clamp position to wall
        rb.MovePosition(new Vector2(
            rb.position.x,
            rb.position.y
        ));
    }
    
    public override void HandleInput()
    {
        // Most input is handled in Update
        // Attack input during wall attack is ignored (cooldown)
        
        // We could allow dash input to cancel into dash
        if (inputHandler.DashPressed && controller.AbilitySystem.CanDash())
        {
            if (controller.CanDashFromCurrentState() && controller.DashCooldownTimer <= 0)
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
    
    private bool IsPressingAwayFromWall()
    {
        if (inputHandler == null) return false;
        
        float inputDirection = Mathf.Sign(inputHandler.MoveInput.x);
        return Mathf.Abs(inputHandler.MoveInput.x) > 0.1f && 
               Mathf.Approximately(inputDirection, -attackWallSide);
    }
    
    private void EndWallAttack()
    {
        isWallAttacking = false;
        
        // Decide next state based on settings and input
        if (controller.Settings.stayInWallLockAfterAttack)
        {
            // Try to return to wall lock if still touching wall
            if (wallJump.IsTouchingWall && IsPushingTowardWall())
            {
                stateMachine.ChangeState(controller.WallLockState);
            }
            // Otherwise return to wall slide
            else if (wallJump.IsTouchingWall)
            {
                stateMachine.ChangeState(controller.WallSlideState);
            }
            else
            {
                ExitToFall();
            }
        }
        else
        {
            // Exit to wall slide regardless
            if (wallJump.IsTouchingWall)
            {
                stateMachine.ChangeState(controller.WallSlideState);
            }
            else
            {
                ExitToFall();
            }
        }
    }
    
    private bool IsPushingTowardWall()
    {
        if (inputHandler == null) return false;
        
        float inputDirection = Mathf.Sign(inputHandler.MoveInput.x);
        return Mathf.Abs(inputHandler.MoveInput.x) > controller.Settings.wallLockInputThreshold && 
               Mathf.Approximately(inputDirection, attackWallSide);
    }
    
    private void ExecuteWallJumpFromAttack()
    {
        if (wallJump == null) return;
        
        // Execute wall jump
        wallJump.ExecuteWallJump();
        
        // Play jump animation
        controller.AnimationController.PlayAnimation("Kalb_jump");
        
        // Set jump buffer
        physics.SetJumpBuffer();
        inputHandler.ResetJumpInput();
        
        // Transition to air state
        stateMachine.ChangeState(controller.AirState);
    }
    
    private void ReleaseFromWall()
    {
        // Small release force away from wall
        float releaseHorizontal = -attackWallSide * controller.Settings.ledgeReleaseForce * 0.5f;
        float releaseVertical = 0f;
        
        rb.linearVelocity = new Vector2(releaseHorizontal, releaseVertical);
        
        // Exit to fall
        ExitToFall();
    }
    
    private void ExitToFall()
    {
        // Set small cooldown on wall jump to prevent immediate re-grab
        if (wallJump != null)
        {
            wallJump.SetWallSlideCooldown();
        }
        
        stateMachine.ChangeState(controller.AirState);
    }
}