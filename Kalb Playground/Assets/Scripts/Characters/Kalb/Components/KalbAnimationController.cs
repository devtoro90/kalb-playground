using UnityEngine;

public class KalbAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private KalbMovement movement;
    [SerializeField] private KalbCollisionDetector collisionDetector;
    [SerializeField] private KalbSwimming swimming;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private KalbAbilitySystem abilitySystem;
    [SerializeField] private KalbComboSystem comboSystem;
    [SerializeField] private KalbController controller; 
    
    private void Start()
    {
        if (abilitySystem == null) abilitySystem = GetComponent<KalbAbilitySystem>();
        if (comboSystem == null) comboSystem = GetComponent<KalbComboSystem>();
        if (controller == null) controller = GetComponent<KalbController>();
    }
    
    private void Update()
    {
        UpdateAnimations();
    }
    
    private void UpdateAnimations()
    {
        if (animator == null) return;
        
        // Check if wall sliding (add this near the top with other priority checks)
        if (controller.WallJump != null && controller.WallJump.IsWallSliding)
        {
            PlayAnimation("Kalb_wallslide");
            return;
        }

        // Check if ledge climbing (highest priority)
        if (controller != null && controller.LedgeClimbState != null && controller.LedgeClimbState.IsLedgeClimbing)
        {
            PlayAnimation("Kalb_ledge_climb");
            return;
        }
        
        // Check if ledge grabbing
        if (controller != null && controller.LedgeState != null && controller.LedgeState.IsLedgeGrabbing)
        {
            PlayAnimation("Kalb_ledge_grab");
            return;
        }

        // Check if dashing (high priority)
        if (controller != null && controller.DashState != null && controller.DashState.IsDashing)
        {
            UpdateDashAnimation();
            return;
        }

        // Check if attacking 
        if (comboSystem != null && comboSystem.IsAttacking)
        {
            UpdateComboAnimations();
            return;
        }
        
        // Check if swimming
        if (swimming != null && swimming.IsSwimming)
        {
            UpdateSwimmingAnimations();
            return;
        }

        // Check if running (NEW)
        if (controller != null && controller.RunState != null && controller.RunState.IsRunning)
        {
            PlayAnimation("Kalb_run");
            return;
        }

// Set movement speed parameter
        float speed = Mathf.Abs(rb.linearVelocity.x);
        animator.SetFloat("Speed", speed);
        
        // Set grounded parameter
        animator.SetBool("IsGrounded", collisionDetector != null && controller.IsEffectivelyGrounded());
        
        // Set vertical velocity parameter
        animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);
        
        // Set facing direction
        if (movement != null)
        {
            animator.SetBool("FacingRight", movement.FacingRight);
        }
    }
    
    private void UpdateDashAnimation()
    {
        if (controller.DashState == null) return;
        
        // Get the current dash direction type
        var dashState = controller.DashState;
        
        // Use appropriate animation based on dash direction
        switch (dashState.CurrentDashDirectionType)
        {
            case KalbDashState.DashDirectionType.Forward:
                PlayAnimation("Kalb_dash");
                break;
            case KalbDashState.DashDirectionType.Up:
                PlayAnimation("Kalb_dash_up");
                break;
            case KalbDashState.DashDirectionType.Down:
                PlayAnimation("Kalb_dash_down");
                break;
            case KalbDashState.DashDirectionType.UpDiagonal:
                PlayAnimation("Kalb_dash_up_diagonal");
                break;
            case KalbDashState.DashDirectionType.DownDiagonal:
                PlayAnimation("Kalb_dash_down_diagonal");
                break;
            default:
                PlayAnimation("Kalb_dash"); // Fallback
                break;
        }
    }
    
    private void UpdateComboAnimations() // NEW
    {
        if (comboSystem.IsComboFinishing)
        {
            PlayAnimation("Kalb_attack3");
        }
        else
        {
            switch (comboSystem.CurrentCombo)
            {
                case 1:
                    PlayAnimation("Kalb_attack1");
                    break;
                case 2:
                    PlayAnimation("Kalb_attack2");
                    break;
                case 3:
                    PlayAnimation("Kalb_attack3");
                    break;
            }
        }
    }
    
    private void UpdateSwimmingAnimations()
    {
        if (swimming.IsSwimDashing)
        {
            PlayAnimation("Kalb_dash");
        }
        else
        {
            KalbController controller = GetComponent<KalbController>();
            KalbInputHandler inputHandler = controller?.InputHandler;
            
            if (inputHandler != null && Mathf.Abs(inputHandler.MoveInput.x) > 0.1f)
            {
                if (inputHandler.DashHeld && abilitySystem != null && abilitySystem.CanRun())
                {
                    PlayAnimation("Kalb_swim_fast");
                }
                else
                {
                    PlayAnimation("Kalb_swim");
                }
            }
            else
            {
                PlayAnimation("Kalb_swim_idle");
            }
        }
    }
    
    public void PlayAnimation(string animationName)
    {
        if (animator != null)
        {
            animator.Play(animationName);
        }
    }
}