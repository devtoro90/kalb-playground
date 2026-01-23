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

    private void Start()
    {
        if (abilitySystem == null) abilitySystem = GetComponent<KalbAbilitySystem>();
    }
    
    private void Update()
    {
        UpdateAnimations();
    }
    
    private void UpdateAnimations()
    {
        if (animator == null) return;
        
        // Check if swimming
        if (swimming != null && swimming.IsSwimming)
        {
            UpdateSwimmingAnimations();
            return;
        }
        
        // Set movement speed parameter
        float speed = Mathf.Abs(rb.linearVelocity.x);
        animator.SetFloat("Speed", speed);
        
        // Set grounded parameter
        animator.SetBool("IsGrounded", collisionDetector != null && collisionDetector.IsGrounded);
        
        // Set vertical velocity parameter
        animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);
        
        // Set facing direction
        if (movement != null)
        {
            animator.SetBool("FacingRight", movement.FacingRight);
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