using UnityEngine;

public class KalbComboSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KalbSettings settings;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private KalbAnimationController animationController;
    [SerializeField] private KalbMovement movement;
    
    [Header("Attack Point")]
    [SerializeField] private Transform attackPoint;
    
    [Header("Combo State")]
    private int currentCombo = 0;
    private bool isAttacking = false;
    private bool isComboFinishing = false;
    private bool attackQueued = false;
    
    private float comboWindowTimer = 0f;
    private float comboResetTimer = 0f;
    private float attackCooldownTimer = 0f;
    private float attackTimer = 0f;
    
    // Properties
    public int CurrentCombo => currentCombo;
    public bool IsAttacking => isAttacking;
    public bool IsComboFinishing => isComboFinishing;
    public bool CanAttack => !isAttacking && attackCooldownTimer <= 0 && currentCombo < settings.maxComboHits;
    public float ComboWindowTimer => comboWindowTimer;
    public bool IsInComboWindow => comboWindowTimer > 0;
    
    private void Start()
    {
        if (settings == null)
        {
            Debug.LogWarning("KalbComboSystem: No settings assigned!");
            enabled = false;
            return;
        }
        
        // Create attack point if not assigned
        if (attackPoint == null)
        {
            CreateAttackPoint();
        }
        
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animationController == null) animationController = GetComponent<KalbAnimationController>();
        if (movement == null) movement = GetComponent<KalbMovement>();
    }
    
    private void CreateAttackPoint()
    {
        GameObject obj = new GameObject("AttackPoint");
        obj.transform.parent = transform;
        attackPoint = obj.transform;
        UpdateAttackPointPosition();
    }
    
    private void UpdateAttackPointPosition()
    {
        if (attackPoint != null && movement != null)
        {
            attackPoint.localPosition = new Vector3(
                settings.attackPointOffset.x * (movement.FacingRight ? 1 : -1),
                settings.attackPointOffset.y,
                0
            );
        }
    }
    
    private void Update()
    {
        UpdateTimers();
    }
    
    private void UpdateTimers()
    {
        // Combo window timer
        if (comboWindowTimer > 0)
        {
            comboWindowTimer -= Time.deltaTime;
            if (comboWindowTimer <= 0)
            {
                comboWindowTimer = 0;
                
                // If not attacking and window closed, start reset timer
                if (!isAttacking && currentCombo > 0)
                {
                    comboResetTimer = settings.comboResetTime;
                }
            }
        }
        
        // Combo reset timer
        if (comboResetTimer > 0)
        {
            comboResetTimer -= Time.deltaTime;
            if (comboResetTimer <= 0)
            {
                ResetCombo();
            }
        }
        
        // Attack cooldown timer
        if (attackCooldownTimer > 0)
        {
            attackCooldownTimer -= Time.deltaTime;
        }
        
        // Attack duration timer
        if (isAttacking && attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0)
            {
                EndAttack();
            }
        }
    }
    
    public void StartAttack()
    {
        if (!CanAttack) return;
        
        // If currently attacking, queue next attack if within combo window
        if (isAttacking)
        {
            if (comboWindowTimer > 0 && currentCombo > 0 && currentCombo < settings.maxComboHits)
            {
                attackQueued = true;
                comboWindowTimer = settings.comboWindow; // Extend window for queued attack
            }
            return;
        }
        
        // Determine combo index (0-based)
        int comboIndex = Mathf.Clamp(currentCombo, 0, settings.maxComboHits - 1);
        
        // Start attack state
        isAttacking = true;
        attackTimer = settings.comboAttackDurations[comboIndex];
        attackCooldownTimer = settings.comboCooldowns[comboIndex];
        
        // Update combo state
        currentCombo++;
        comboWindowTimer = settings.comboWindow;  // Open window for next attack
        comboResetTimer = settings.comboResetTime; // Reset overall combo timer
        
        // Check if this is the final hit
        isComboFinishing = (currentCombo >= settings.maxComboHits);
        
        // Execute attack logic
        ExecuteAttack(comboIndex);
        
        // Apply movement effects
        ApplyAttackMovement(comboIndex);
        
        // Update animation
        UpdateComboAnimation();
    }
    
    private void ExecuteAttack(int comboIndex)
    {
        // Ensure arrays have data
        if (settings.comboRange.Length <= comboIndex || 
            settings.comboDamage.Length <= comboIndex || 
            settings.comboKnockback.Length <= comboIndex)
        {
            Debug.LogWarning($"Combo data missing for index {comboIndex}");
            return;
        }
        
        // Check for enemies in attack range
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            attackPoint.position, 
            settings.comboRange[comboIndex], 
            settings.enemyLayers
        );
        
        // Spawn hit effect if available
        if (settings.hitEffectPrefab != null && hitEnemies.Length > 0)
        {
            GameObject effect = Instantiate(settings.hitEffectPrefab, attackPoint.position, Quaternion.identity);
            Destroy(effect, settings.hitEffectDuration);
        }
    }
    
    private void ApplyAttackMovement(int comboIndex)
    {
        // Stop movement during attack
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        
        // Apply forward force for first two hits
        if (comboIndex < 2 && settings.comboForwardForce.Length > comboIndex)
        {
            float forwardForce = settings.comboForwardForce[comboIndex];
            if (forwardForce > 0 && rb != null && movement != null)
            {
                Vector2 forceDirection = movement.FacingRight ? Vector2.right : Vector2.left;
                rb.AddForce(forceDirection * forwardForce, ForceMode2D.Impulse);
            }
        }
        
        // Apply upward force for final hit if grounded
        if (comboIndex == 2 && settings.comboUpwardForce.Length > comboIndex)
        {
            float upwardForce = settings.comboUpwardForce[comboIndex];
            if (upwardForce > 0 && rb != null)
            {
                rb.AddForce(Vector2.up * upwardForce, ForceMode2D.Impulse);
            }
        }
    }
    
    private void UpdateComboAnimation()
    {
        if (animationController == null) return;
        
        // Determine which animation to play
        string animationName;
        if (isComboFinishing)
        {
            animationName = settings.comboAnimations[settings.maxComboHits - 1];
        }
        else
        {
            int comboIndex = Mathf.Clamp(currentCombo - 1, 0, settings.maxComboHits - 1);
            animationName = settings.comboAnimations[comboIndex];
        }
        
        // Play the animation
        animationController.PlayAnimation(animationName);
    }
    
    private void EndAttack()
    {
        isAttacking = false;
        
        // Check if we have a queued attack to execute
        if (attackQueued && comboWindowTimer > 0 && currentCombo < settings.maxComboHits)
        {
            attackQueued = false; // Clear the queue
            StartAttack();   // Execute the queued attack
            return;
        }

        // NEW: If we just finished the full combo, reset immediately
        if (currentCombo >= settings.maxComboHits)
        {
            ResetCombo(); // Reset immediately instead of waiting for comboResetTimer
            return;
        }
        
        // No queued attack, check if combo window is closed
        if (comboWindowTimer <= 0 && currentCombo > 0)
        {
            // Start reset timer
            comboResetTimer = settings.comboResetTime;
            
            // Play combo reset animation if available
            if (animationController != null && !string.IsNullOrEmpty(settings.comboResetAnimation))
            {
                animationController.PlayAnimation(settings.comboResetAnimation);
            }
        }
        else if (currentCombo == 0)
        {
            // No combo active, reset immediately
            ResetCombo();
        }
    }
    
    public void ResetCombo()
    {
        currentCombo = 0;
        comboWindowTimer = 0f;
        comboResetTimer = 0f;
        isComboFinishing = false;
        attackQueued = false;
        isAttacking = false;
        attackCooldownTimer = 0f;
    }
    
    public void CancelCombo()
    {
        ResetCombo();
    }
    
    public void UpdateAttackPointWithFacing(bool facingRight)
    {
        if (attackPoint != null)
        {
            attackPoint.localPosition = new Vector3(
                settings.attackPointOffset.x * (facingRight ? 1 : -1),
                attackPoint.localPosition.y,
                attackPoint.localPosition.z
            );
        }
    }
    
    public int GetCurrentCombo() => currentCombo;
    public int GetMaxCombo() => settings.maxComboHits;
    
    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null && settings != null && currentCombo > 0)
        {
            int comboIndex = Mathf.Clamp(currentCombo - 1, 0, settings.maxComboHits - 1);
            
            // Ensure combo range array has data
            if (settings.comboRange.Length > comboIndex)
            {
                float range = settings.comboRange[comboIndex];
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(attackPoint.position, range);
            }
        }
    }
}