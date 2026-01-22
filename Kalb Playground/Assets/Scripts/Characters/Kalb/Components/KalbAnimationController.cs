using UnityEngine;

public class KalbAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private KalbMovement movement;
    [SerializeField] private KalbCollisionDetector collisionDetector;
    [SerializeField] private Rigidbody2D rb;
    
    private void Update()
    {
        UpdateAnimations();
    }
    
    private void UpdateAnimations()
    {
        if (animator == null) return;
        
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
    
    public void PlayAnimation(string animationName)
    {
        if (animator != null)
        {
            animator.Play(animationName);
        }
    }
}