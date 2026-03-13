using UnityEngine;

public class KalbComboSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KalbSettings settings;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private KalbAnimationController animationController;
    [SerializeField] private KalbMovement movement;
    [SerializeField] private KalbInputHandler inputHandler;
    [SerializeField] private KalbWallJump wallJump;

    [Header("Attack Point")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Transform upwardAttackPoint;
    [SerializeField] private Transform wallAttackPoint;

    [Header("Combo State")]
    private int currentCombo = 0;
    private bool isAttacking = false;
    private bool isComboFinishing = false;
    private bool attackQueued = false;

    // Separate attack states
    private bool isUpwardAttacking = false;
    private bool isWallAttacking = false;

    // Cooldowns
    private bool canUpwardAttack = true;
    private float upwardAttackCooldownTimer = 0f;
    private bool canWallAttack = true;
    private float wallAttackCooldownTimer = 0f;
    private int wallAttackSide = 0;

    private float comboWindowTimer = 0f;
    private float comboResetTimer = 0f;
    private float attackCooldownTimer = 0f;
    private float attackTimer = 0f;

    // Properties
    public int CurrentCombo => currentCombo;
    public bool IsAttacking => isAttacking;
    public bool IsUpwardAttacking => isUpwardAttacking;
    public bool IsWallAttacking => isWallAttacking;
    public int WallAttackSide => wallAttackSide;
    public bool IsComboFinishing => isComboFinishing;
    public bool CanAttack => !IsAnyAttackActive && attackCooldownTimer <= 0 && currentCombo < settings.maxComboHits;
    public float ComboWindowTimer => comboWindowTimer;
    public bool IsInComboWindow => comboWindowTimer > 0;
    public bool IsAnyAttackActive => isAttacking || isUpwardAttacking || isWallAttacking;

    // Separate ability checks
    public bool CanPerformUpwardAttack => settings.enableUpwardAttack && canUpwardAttack && upwardAttackCooldownTimer <= 0;
    public bool CanPerformWallAttack => settings.enableWallAttack && canWallAttack && wallAttackCooldownTimer <= 0;

    private void Start()
    {
        if (settings == null)
        {
            Debug.LogWarning("KalbComboSystem: No settings assigned!");
            enabled = false;
            return;
        }

        // Get references if not assigned
        if (inputHandler == null)
            inputHandler = GetComponent<KalbInputHandler>();

        if (wallJump == null)
            wallJump = GetComponent<KalbWallJump>();

        // Create attack points if not assigned
        if (attackPoint == null)
            CreateAttackPoint();

        if (upwardAttackPoint == null)
            CreateUpwardAttackPoint();

        // NEW: Create wall attack point
        if (wallAttackPoint == null)
            CreateWallAttackPoint();

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

    // NEW: Create separate upward attack point
    private void CreateUpwardAttackPoint()
    {
        GameObject obj = new GameObject("UpwardAttackPoint");
        obj.transform.parent = transform;
        upwardAttackPoint = obj.transform;
        UpdateUpwardAttackPointPosition();
    }

    private void CreateWallAttackPoint()
    {
        GameObject obj = new GameObject("WallAttackPoint");
        obj.transform.parent = transform;
        wallAttackPoint = obj.transform;
        UpdateWallAttackPointPosition(1); // Default to right side
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

    // NEW: Update upward attack point position
    private void UpdateUpwardAttackPointPosition()
    {
        if (upwardAttackPoint != null)
        {
            // Upward attack point is centered above player, doesn't flip with facing
            upwardAttackPoint.localPosition = new Vector3(
                0, // Centered horizontally
                settings.upwardAttackPointOffset.y,
                0
            );
        }
    }
    private void UpdateWallAttackPointPosition(int wallSide)
    {
        if (wallAttackPoint == null) return;

        // Wall side: -1 = wall on left, 1 = wall on right
        // Attack point goes to the OPPOSITE side of the wall
        float attackSide = -wallSide; // Opposite direction

        wallAttackPoint.localPosition = new Vector3(
            settings.wallAttackPointOffset.x * attackSide,
            settings.wallAttackPointOffset.y,
            0
        );
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
                if (!isAttacking && !isUpwardAttacking && currentCombo > 0)
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

        // NEW: Upward attack cooldown timer
        if (upwardAttackCooldownTimer > 0)
        {
            upwardAttackCooldownTimer -= Time.deltaTime;
            if (upwardAttackCooldownTimer <= 0)
            {
                canUpwardAttack = true;
            }
        }

        if (wallAttackCooldownTimer > 0)
        {
            wallAttackCooldownTimer -= Time.deltaTime;
            if (wallAttackCooldownTimer <= 0)
            {
                canWallAttack = true;
            }
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

        // NEW: Upward attack duration timer
        if (isUpwardAttacking && attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0)
            {
                EndUpwardAttack();
            }
        }

        // NEW: Wall attack duration timer
        if (isWallAttacking && attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0)
            {
                EndWallAttack();
            }
        }
    }

    // NEW: Check if upward attack should be performed
    public bool ShouldPerformUpwardAttack()
    {
        if (!settings.enableUpwardAttack) return false;
        if (!canUpwardAttack) return false;
        if (inputHandler == null) return false;

        // Check if up is held
        bool upHeld = inputHandler.IsUpHeld;

        // For air attacks, we might want a separate setting
        bool isGrounded = movement != null &&
                          GetComponent<KalbController>() != null &&
                          GetComponent<KalbController>().IsEffectivelyGrounded();

        if (!isGrounded && !settings.enableUpwardAirAttack)
            return false;

        return upHeld;
    }
    public bool ShouldPerformWallAttack()
    {

        if (!settings.enableWallAttack) return false;
        if (!canWallAttack) return false;

        // Check if we're in wall slide or wall lock state
        KalbController controller = GetComponent<KalbController>();
        if (controller == null) return false;

        bool isInWallState = controller.StateMachine.CurrentState is KalbWallLockState;




        if (!isInWallState) return false;

        // Check if we're actually touching a wall
        if (wallJump == null || !wallJump.IsTouchingWall) return false;
        return true;
    }

    public void StartAttack()
    {
        // Check for wall attack first (highest priority when on wall)
        if (ShouldPerformWallAttack())
        {
            StartWallAttack();
            return;
        }

        // Check for upward attack
        if (ShouldPerformUpwardAttack())
        {
            StartUpwardAttack();
            return;
        }

        if (inputHandler.IsUpHeld) return;

        if (!CanAttack) return;

        // If currently attacking, queue next attack if within combo window
        if (isAttacking)
        {
            if (comboWindowTimer > 0 && currentCombo > 0 && currentCombo < settings.maxComboHits)
            {
                attackQueued = true;
                comboWindowTimer = settings.comboWindow;
            }
            return;
        }

        // Don't start new ground combo if wall attacking
        if (isWallAttacking) return;

        // Determine combo index (0-based)
        int comboIndex = Mathf.Clamp(currentCombo, 0, settings.maxComboHits - 1);

        // Start attack state
        isAttacking = true;
        attackTimer = settings.comboAttackDurations[comboIndex];
        attackCooldownTimer = settings.comboCooldowns[comboIndex];

        // Update combo state
        currentCombo++;
        comboWindowTimer = settings.comboWindow;
        comboResetTimer = settings.comboResetTime;

        // Check if this is the final hit
        isComboFinishing = (currentCombo >= settings.maxComboHits);

        // Execute attack logic
        ExecuteAttack(comboIndex);

        // Apply movement effects
        ApplyAttackMovement(comboIndex);

        // Update animation
        UpdateComboAnimation();
    }

    // NEW: Start upward attack
    private void StartUpwardAttack()
    {
        if (!canUpwardAttack) return;

        // Set upward attack state
        isUpwardAttacking = true;
        attackTimer = settings.upwardAttackDuration;
        upwardAttackCooldownTimer = settings.upwardAttackCooldown;
        canUpwardAttack = false;

        // Reset ground combo (as requested)
        ResetCombo();

        // Execute upward attack
        ExecuteUpwardAttack();

        // Apply upward movement effect
        ApplyUpwardAttackMovement();

        // Play upward animation
        PlayUpwardAttackAnimation();
    }

    // NEW: Execute upward attack hit detection
    private void ExecuteUpwardAttack()
    {
        if (upwardAttackPoint == null) return;

        // Check for enemies in upward attack range
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            upwardAttackPoint.position,
            settings.upwardAttackRange,
            settings.enemyLayers
        );

        foreach (Collider2D enemy in hitEnemies)
        {
            BaseEnemy baseEnemy = enemy.GetComponent<BaseEnemy>();
            if (baseEnemy != null)
            {
                Vector2 hitDirection = settings.upwardAttackKnockbackDirection.normalized;
                baseEnemy.TakeDamage(
                    (int)settings.upwardAttackDamage,
                    transform.position,
                    settings.upwardAttackKnockback,
                    hitDirection
                );
            }
            else
            {
                // Fallback knockback for non-enemy objects
                var rb = enemy.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 knockback = settings.upwardAttackKnockbackDirection.normalized *
                                        settings.upwardAttackKnockback;
                    rb.AddForce(knockback, ForceMode2D.Impulse);
                }
            }
        }

        // Spawn hit effect if available
        if (settings.hitEffectPrefab != null && hitEnemies.Length > 0)
        {
            GameObject effect = Instantiate(settings.hitEffectPrefab, upwardAttackPoint.position, Quaternion.identity);
            Destroy(effect, settings.hitEffectDuration);
        }
    }

    // NEW: Apply upward attack movement (slight upward boost)
    private void ApplyUpwardAttackMovement()
    {
        if (rb == null) return;

        // Stop horizontal movement
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        // Apply upward boost
        if (settings.upwardAttackUpwardForce > 0)
        {
            rb.AddForce(Vector2.up * settings.upwardAttackUpwardForce, ForceMode2D.Impulse);
        }
    }

    // NEW: Play upward attack animation
    private void PlayUpwardAttackAnimation()
    {
        if (animationController == null) return;

        // Choose appropriate animation based on grounded/air
        bool isGrounded = movement != null &&
                          GetComponent<KalbController>() != null &&
                          GetComponent<KalbController>().IsEffectivelyGrounded();

        string animName = isGrounded ? settings.upwardAttackAnimation : settings.upwardAirAttackAnimation;

        if (!string.IsNullOrEmpty(animName))
        {
            animationController.PlayAnimation(animName);
        }
    }

    // NEW: End upward attack
    private void EndUpwardAttack()
    {
        isUpwardAttacking = false;
        CancelCombo();

        // Transition to appropriate state - will be handled by state machine
    }

    public void StartWallAttack()
    {
        if (!CanPerformWallAttack || isWallAttacking || isUpwardAttacking)
        {

            return;
        }

        // CRITICAL: Don't reset or change state here - we're already in wall lock
        wallAttackSide = wallJump.WallSide;
        UpdateWallAttackPointPosition(wallAttackSide);

        isWallAttacking = true;
        attackTimer = settings.wallAttackDuration;
        wallAttackCooldownTimer = settings.wallAttackCooldown;
        canWallAttack = false;



        ExecuteWallAttack();
        PlayWallAttackAnimation();
    }

    private void ExecuteWallAttack()
    {
        if (wallAttackPoint == null) return;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            wallAttackPoint.position,
            settings.wallAttackRange,
            settings.enemyLayers
        );

        foreach (Collider2D enemy in hitEnemies)
        {
            BaseEnemy baseEnemy = enemy.GetComponent<BaseEnemy>();
            if (baseEnemy != null)
            {
                Vector2 hitDirection = settings.upwardAttackKnockbackDirection.normalized;
                baseEnemy.TakeDamage(
                    (int)settings.upwardAttackDamage,
                    transform.position,
                    settings.upwardAttackKnockback,
                    hitDirection
                );
            }
            else
            {
                // Fallback knockback for non-enemy objects
                var rb = enemy.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 knockback = settings.upwardAttackKnockbackDirection.normalized *
                                        settings.upwardAttackKnockback;
                    rb.AddForce(knockback, ForceMode2D.Impulse);
                }
            }
        }

        if (settings.hitEffectPrefab != null && hitEnemies.Length > 0)
        {
            GameObject effect = Instantiate(settings.hitEffectPrefab, wallAttackPoint.position, Quaternion.identity);
            Destroy(effect, settings.hitEffectDuration);
        }
    }

    private void PlayWallAttackAnimation()
    {
        if (animationController == null) return;

        if (!string.IsNullOrEmpty(settings.wallAttackAnimation))
        {
            animationController.PlayAnimation(settings.wallAttackAnimation);
        }
    }

    private void EndWallAttack()
    {
        isWallAttacking = false;
        wallAttackCooldownTimer = settings.wallAttackCooldown * 0.5f;
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

        // Apply damage and knockback to enemies
        foreach (Collider2D enemy in hitEnemies)
        {
            BaseEnemy baseEnemy = enemy.GetComponent<BaseEnemy>();
            if (baseEnemy != null)
            {
                // Calculate hit direction and knockback
                Vector2 hitDirection = (enemy.transform.position - transform.position).normalized;
                float knockbackForce = settings.comboKnockback[comboIndex];

                // Apply damage and knockback through enemy's TakeDamage method
                baseEnemy.TakeDamage(
                    (int)settings.comboDamage[comboIndex],
                    transform.position,
                    knockbackForce,
                    hitDirection
                );
            }

            // Apply knockback
            var rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 knockbackDirection = (enemy.transform.position - transform.position).normalized;
                knockbackDirection.y = 0.5f; // Add some upward component
                rb.AddForce(knockbackDirection * settings.comboKnockback[comboIndex], ForceMode2D.Impulse);
            }
        }

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

        // If we just finished the full combo, reset immediately
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
        // NEW: Don't reset upward attack state here
    }

    public void CancelCombo()
    {
        ResetCombo();
        isUpwardAttacking = false;
        isWallAttacking = false;
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

        // NEW: Upward attack point doesn't need to flip
        UpdateUpwardAttackPointPosition();
    }

    public int GetCurrentCombo() => currentCombo;
    public int GetMaxCombo() => settings.maxComboHits;

    private void OnDrawGizmosSelected()
    {
        // Draw normal attack range
        if (attackPoint != null && settings != null && currentCombo > 0)
        {
            int comboIndex = Mathf.Clamp(currentCombo - 1, 0, settings.maxComboHits - 1);

            if (settings.comboRange.Length > comboIndex)
            {
                float range = settings.comboRange[comboIndex];
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(attackPoint.position, range);
            }
        }

        // NEW: Draw upward attack range
        if (upwardAttackPoint != null && settings != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(upwardAttackPoint.position, settings.upwardAttackRange);
        }

        if (wallAttackPoint != null && settings != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(wallAttackPoint.position, settings.wallAttackRange);
        }
    }
}