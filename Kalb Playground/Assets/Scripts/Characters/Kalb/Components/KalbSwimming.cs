using UnityEngine;

public class KalbSwimming : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private KalbSettings settings;
    [SerializeField] private Transform waterCheckPoint;
    [SerializeField] private Collider2D playerCollider;
    [SerializeField] private KalbPhysics physics;
    [SerializeField] private KalbMovement movement;
    [SerializeField] private KalbAbilitySystem abilitySystem; 
    
    // State
    private bool isSwimming = false;
    private bool isSwimDashing = false;
    private bool isInWater = false;
    private float swimDashTimer = 0f;
    private float swimDashCooldownTimer = 0f;
    private float swimDashDuration = 0.15f;
    private float swimDashCooldown = 0.3f;
    private Vector2 swimDashDirection = Vector2.right;
    private float waterSurfaceY = 0f;
    private Collider2D currentWaterCollider = null;
    private float preDashGravityScale;
    
    // Floating Effect
    private float floatTimer = 0f;
    private float currentFloatOffset = 0f;
    private float targetFloatOffset = 0f;
    private Vector3 originalPosition;
    
    // Input tracking
    private KalbInputHandler inputHandler;
    
    // Water jump cooldown
    private float waterJumpCooldown = 0f;
    private bool isJumpingFromWater = false;
    
    public bool IsSwimming => isSwimming;
    public bool IsSwimDashing => isSwimDashing;
    public bool IsInWater => isInWater;
    public bool IsJumpingFromWater => isJumpingFromWater;
    
    private void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (playerCollider == null) playerCollider = GetComponent<Collider2D>();
        if (physics == null) physics = GetComponent<KalbPhysics>();
        if (movement == null) movement = GetComponent<KalbMovement>();
        if (abilitySystem == null) abilitySystem = GetComponent<KalbAbilitySystem>();

        // Get input handler
        inputHandler = GetComponent<KalbInputHandler>();
        
        if (waterCheckPoint == null)
        {
            GameObject obj = new GameObject("WaterCheck");
            obj.transform.parent = transform;
            obj.transform.localPosition = new Vector3(0, 0.2f, 0);
            waterCheckPoint = obj.transform;
        }
    }
    
    private void Update()
    {
        CheckWater();
        UpdateSwimTimers();
        
        // Update water jump cooldown
        if (waterJumpCooldown > 0)
        {
            waterJumpCooldown -= Time.deltaTime;
        }
        
        // Reset jump flag after cooldown
        if (isJumpingFromWater && waterJumpCooldown <= 0)
        {
            isJumpingFromWater = false;
        }
    }
    
    private void CheckWater()
    {
        // Skip water check if we just jumped from water
        if (isJumpingFromWater && waterJumpCooldown > 0)
        {
            // Force not in water during jump cooldown
            if (isInWater)
            {
                OnExitWater();
            }
            return;
        }
        
        bool wasInWater = isInWater;
        
        // Check for water overlap using circle cast
        Collider2D waterCollider = Physics2D.OverlapCircle(
            waterCheckPoint.position, 
            settings.waterCheckRadius, 
            settings.waterLayer
        );
        
        // Update water state
        isInWater = waterCollider != null;
        currentWaterCollider = waterCollider;
        
        // Store water surface position if in water
        if (isInWater && waterCollider != null)
        {
            waterSurfaceY = waterCollider.bounds.max.y;
        }
        
        // Handle state transitions
        if (isInWater && !wasInWater)
        {
            OnEnterWater();
        }
        else if (!isInWater && wasInWater)
        {
            OnExitWater();
        }
    }
    
    private void OnEnterWater()
    {
        // Don't enter water if we're jumping from it
        if (isJumpingFromWater && waterJumpCooldown > 0) return;
        
        isSwimming = true;
        
        // Initialize floating effect with random phase
        floatTimer = Random.Range(0f, Mathf.PI * 2f);
        originalPosition = transform.position;
        currentFloatOffset = 0f;
        targetFloatOffset = 0f;
        
        // Adjust velocity for water entry
        if (rb.linearVelocity.y < 0)
        {
            // Slow down downward momentum when entering water
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * settings.waterEntrySpeedReduction, -3f);
        }
        else
        {
            // Reduce upward momentum when entering water
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * settings.waterEntrySpeedReduction, 
                Mathf.Min(rb.linearVelocity.y, 2f) * settings.waterEntrySpeedReduction);
        }
        
        // Set water physics (reduced gravity)
        if (rb != null)
        {
            rb.gravityScale = settings.waterEntryGravity;
        }
    }
    
    private void OnExitWater()
    {
        isSwimming = false;
        isSwimDashing = false;
        isInWater = false;
        
        // Restore normal gravity
        if (rb != null)
        {
            rb.gravityScale = settings.normalGravityScale;
        }
        swimDashCooldownTimer = 0f;
    }
    
    private void UpdateSwimTimers()
    {
        if (swimDashCooldownTimer > 0) swimDashCooldownTimer -= Time.deltaTime;
        if (isSwimDashing)
        {
            swimDashTimer -= Time.deltaTime;
            if (swimDashTimer <= 0) EndSwimDash();
        }
    }
    
    public void EnterSwim()
    {
        // Don't enter swim if we're jumping from water
        if (isJumpingFromWater && waterJumpCooldown > 0) return;
        
        // Ensure swimming state is properly set
        isSwimming = true;
        if (rb != null)
        {
            rb.gravityScale = settings.waterEntryGravity;
        }
    }
    
    public void ExitSwim()
    {
        if (isSwimDashing)
        {
            EndSwimDash();
        }
    }
    
    public void UpdateSwim()
    {
        // Update floating effect when not dashing
        if (!isSwimDashing && settings.enableFloating)
        {
            UpdateFloatingEffect();
        }
    }
    
    public void ApplySwimMovement(float moveInput)
    {
        if (!isSwimming || rb == null || isSwimDashing) return;
        
        // Calculate current swim speed
        float currentSwimSpeed = settings.swimSpeed;
        
        // Check for fast swimming (when holding dash button)
        if (inputHandler != null && inputHandler.DashHeld && !isSwimDashing)
        {
            if (abilitySystem != null && abilitySystem.CanRun()) // CHECK ABILITY
            {
                currentSwimSpeed = settings.swimFastSpeed;
            }
        }
        
        // Apply horizontal movement with velocity control
        float targetXVelocity = moveInput * currentSwimSpeed;
        float currentXVelocity = rb.linearVelocity.x;
        
        // Smooth horizontal movement but prioritize buoyancy
        float horizontalAcceleration = 25f; // Faster acceleration in water
        float newXVelocity = Mathf.MoveTowards(currentXVelocity, targetXVelocity, 
                                            Time.fixedDeltaTime * horizontalAcceleration);
        
        // Apply the horizontal velocity (buoyancy handles vertical)
        rb.linearVelocity = new Vector2(newXVelocity, rb.linearVelocity.y);
        
        // Clamp horizontal speed
        float maxHorizontalSpeed = currentSwimSpeed * 1.2f;
        if (Mathf.Abs(rb.linearVelocity.x) > maxHorizontalSpeed)
        {
            rb.linearVelocity = new Vector2(
                Mathf.Sign(rb.linearVelocity.x) * maxHorizontalSpeed,
                rb.linearVelocity.y
            );
        }
    }
    
    public void FixedUpdateSwim()
    {
        if (!isSwimming || rb == null) return;
        
        // Handle swim dash first (highest priority)
        if (isSwimDashing)
        {
            rb.linearVelocity = swimDashDirection * settings.swimDashSpeed;
            rb.gravityScale = 0f; // No gravity during dash
            return;
        }
        
        // Apply buoyancy FIRST (most important) - always active
        ApplyBuoyancy();
        
        // Apply floating effect when not actively moving horizontally
        if (settings.enableFloating)
        {
            ApplyFloatingPosition();
        }
        
        // Force buoyancy correction if player is too high above target
        ApplyBuoyancyCorrection();
    }
    
    private void ApplyBuoyancy()
    {
        if (!isSwimming || currentWaterCollider == null || rb == null) return;
        
        // Calculate target position (face above water)
        float playerHeight = playerCollider.bounds.extents.y * 2f;
        float targetY = waterSurfaceY + settings.waterSurfaceOffset - (playerHeight * 0.8f);
        
        // Adjust for floating effect
        if (settings.enableFloating && !isSwimDashing)
        {
            targetY += currentFloatOffset;
        }
        
        // Current position and depth difference
        float currentY = transform.position.y;
        float depthDifference = targetY - currentY;
        
        // Calculate buoyancy force with damping (spring physics)
        float buoyancyForce = depthDifference * settings.buoyancyStrength;
        float dampingForce = -rb.linearVelocity.y * settings.buoyancyDamping;
        float totalForce = Mathf.Clamp(buoyancyForce + dampingForce, -settings.maxBuoyancyForce, settings.maxBuoyancyForce);
        
        // Apply force
        rb.AddForce(new Vector2(0, totalForce));
    }
    
    private void ApplyBuoyancyCorrection()
    {
        if (isSwimming && currentWaterCollider != null && rb != null)
        {
            float playerHeight = playerCollider.bounds.extents.y * 2f;
            float targetY = waterSurfaceY + settings.waterSurfaceOffset - (playerHeight * 0.8f);
            float currentY = transform.position.y;
            float yDifference = targetY - currentY;
            
            // If player is significantly above target position (more buoyant than expected)
            if (yDifference > 0.5f) // Increased threshold for more aggressive correction
            {
                // Apply additional downward force
                float correctionForce = -Mathf.Min(yDifference * 5f, 10f);
                rb.AddForce(new Vector2(0, correctionForce));
            }
        }
    }
    
    private void UpdateFloatingEffect()
    {
        if (!isSwimming || isSwimDashing || !settings.enableFloating) return;
        
        // Update timer for sine wave
        floatTimer += Time.deltaTime * settings.floatFrequency;
        
        // Calculate sine wave for bobbing
        float sineWave = Mathf.Sin(floatTimer * Mathf.PI * 2f);
        
        // Target float offset
        targetFloatOffset = sineWave * settings.floatAmplitude;
        
        // Smooth interpolation between current and target offset
        currentFloatOffset = Mathf.Lerp(currentFloatOffset, targetFloatOffset, Time.deltaTime * settings.floatSmoothness);
    }
    
    private void ApplyFloatingPosition()
    {
        if (!isSwimming || isSwimDashing || !settings.enableFloating) return;
        
        // Update original position reference for floating
        if (Mathf.Abs(currentFloatOffset) > 0.01f)
        {
            originalPosition = new Vector3(originalPosition.x, 
                                        transform.position.y - currentFloatOffset, 
                                        originalPosition.z);
        }
        
        // Apply the floating offset to position
        Vector3 currentPos = transform.position;
        transform.position = new Vector3(currentPos.x, 
                                    originalPosition.y + currentFloatOffset, 
                                    currentPos.z);
    }
    
    public void StartSwimDash()
    {
        // Check if dash ability is unlocked
        if (abilitySystem != null && !abilitySystem.CanDash())
        {
           
            return;
        }

        if (isSwimDashing || swimDashCooldownTimer > 0 || rb == null) return;
        
        isSwimDashing = true;
        swimDashTimer = swimDashDuration;
        swimDashCooldownTimer = swimDashCooldown;
        
        // Save and disable gravity during swim dash
        preDashGravityScale = rb.gravityScale;
        rb.gravityScale = 0f;
        
        // Determine dash direction based on input or facing direction
        KalbController controller = GetComponent<KalbController>();
        if (controller != null)
        {
            KalbInputHandler inputHandler = controller.InputHandler;
            if (inputHandler != null && Mathf.Abs(inputHandler.MoveInput.x) > 0.1f)
            {
                swimDashDirection = new Vector2(Mathf.Sign(inputHandler.MoveInput.x), 0);
            }
            else
            {
                swimDashDirection = controller.FacingRight ? Vector2.right : Vector2.left;
            }
        }
        else
        {
            swimDashDirection = Vector2.right; // Fallback
        }
    }
    
    private void EndSwimDash()
    {
        isSwimDashing = false;
        if (rb != null)
        {
            rb.gravityScale = preDashGravityScale;
            
            // Slow down gradually after dash
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, rb.linearVelocity.y * 0.5f);
        }
    }
    
    public void SwimJump()
    {
        if (!isSwimming || isSwimDashing || rb == null || physics == null) return;
        
       
        
        // Set jumping flag to prevent immediate re-entry
        isJumpingFromWater = true;
        waterJumpCooldown = 0.5f; // Half second cooldown
        
        // 1. Immediately exit swimming state
        isSwimming = false;
        
        // 2. Force exit water state
        OnExitWater();
        
        // 3. Apply strong upward jump force
        // Use impulse for immediate effect, and ensure we clear water
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); // Reset vertical velocity
        rb.AddForce(Vector2.up * settings.swimJumpForce * 1.5f, ForceMode2D.Impulse); // 50% extra force
        
        // 4. Apply additional upward velocity to ensure clearing water
        float additionalLift = Mathf.Max(0, (waterSurfaceY + 0.5f) - transform.position.y);
        if (additionalLift > 0)
        {
            rb.AddForce(Vector2.up * additionalLift * 5f, ForceMode2D.Impulse);
        }
        
        // 5. Temporarily increase jump force in physics for better clearance
        float originalJumpForce = settings.jumpForce;
        settings.jumpForce = settings.swimJumpForce * 1.3f; // 30% stronger than normal
        
        // 6. Use physics system for jump consistency
        physics.Jump(settings.jumpForce);
        physics.SetJumpButtonState(true);
        
        // 7. Restore original jump force after a delay
        StartCoroutine(RestoreJumpForce(originalJumpForce, 0.1f));
        
        
    }
    
    private System.Collections.IEnumerator RestoreJumpForce(float originalForce, float delay)
    {
        yield return new WaitForSeconds(delay);
        settings.jumpForce = originalForce;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (waterCheckPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(waterCheckPoint.position, settings.waterCheckRadius);
            
            // Draw jump clearance zone
            if (Application.isPlaying && currentWaterCollider != null)
            {
                Gizmos.color = Color.yellow;
                float clearanceY = waterSurfaceY + 0.5f;
                Gizmos.DrawLine(
                    new Vector3(transform.position.x - 1f, clearanceY, 0),
                    new Vector3(transform.position.x + 1f, clearanceY, 0)
                );
            }
        }
        
        if (isSwimming && currentWaterCollider != null && Application.isPlaying)
        {
            // Target surface position
            float playerHeight = playerCollider != null ? playerCollider.bounds.extents.y * 2f : 1f;
            float targetY = waterSurfaceY + settings.waterSurfaceOffset - (playerHeight * 0.8f);
            
            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                new Vector3(transform.position.x - 0.5f, targetY, 0),
                new Vector3(transform.position.x + 0.5f, targetY, 0)
            );
            
            // Current position
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                new Vector3(transform.position.x - 0.5f, transform.position.y, 0),
                new Vector3(transform.position.x + 0.5f, transform.position.y, 0)
            );
            
            // Water surface
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(
                new Vector3(transform.position.x - 0.5f, waterSurfaceY, 0),
                new Vector3(transform.position.x + 0.5f, waterSurfaceY, 0)
            );
        }
    }
}