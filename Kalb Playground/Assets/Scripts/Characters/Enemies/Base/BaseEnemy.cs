// BaseEnemy.cs
using UnityEngine;
using System;
using System.Collections;

public class BaseEnemy : MonoBehaviour
{
    [Header("Core Components")]
    [SerializeField] protected Rigidbody2D rb;
    [SerializeField] protected Collider2D enemyCollider;
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected Animator animator;

    [Header("Configuration")]
    [SerializeField] protected EnemySettings settings;

    [Header("State")]
    [SerializeField] protected EnemyHealth health;
    [SerializeField] protected bool isFacingRight = true;

    // Events for communication
    public event Action<GameObject> OnDamaged;
    public event Action OnDeath;
    public event Action<Vector2> OnKnockbackReceived;

    // Properties
    public Rigidbody2D Rb => rb;
    public EnemySettings Settings => settings;
    public bool IsFacingRight => isFacingRight;
    public bool IsAlive => health != null && health.CurrentHealth > 0;

    // Coroutine tracking
    private Coroutine hitFlashCoroutine;
    private Color originalColor;
    private Material originalMaterial;
    private bool isFlashing = false;

    protected virtual void Awake()
    {
        InitializeComponents();
    }

    protected virtual void Start()
    {
        // Ensure health is initialized
        if (health != null && settings != null)
        {
            health.Initialize(settings);
        }

        // Store original color
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            originalMaterial = spriteRenderer.material;
        }
    }

    protected virtual void InitializeComponents()
    {
        // Get components if not assigned
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (enemyCollider == null) enemyCollider = GetComponent<Collider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();

        // Add health if not present
        if (health == null)
        {
            health = GetComponent<EnemyHealth>();
            if (health == null)
            {
                health = gameObject.AddComponent<EnemyHealth>();
            }
        }

        // Subscribe to health events
        if (health != null)
        {
            health.OnDamaged += HandleDamaged;
            health.OnHealthChanged += HandleHealthChanged;
            health.OnDeath += HandleDeath;
        }
    }

    protected virtual void HandleDamaged()
    {
        Debug.Log($"{gameObject.name} was damaged!");
        OnDamaged?.Invoke(gameObject);
        PlayHitFlash();
    }

    protected virtual void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        // This is called when health changes
    }

    protected virtual void HandleDeath()
    {
        Debug.Log($"{gameObject.name} died!");
        // Stop any ongoing hit flash
        if (hitFlashCoroutine != null)
            StopCoroutine(hitFlashCoroutine);

        // Reset color on death
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        OnDeath?.Invoke();
    }

    public virtual void FlipSprite()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    public virtual void TakeDamage(int damage, Vector2 hitSource, float knockbackForce, Vector2 knockbackDirection)
    {
        if (!IsAlive || health == null) return;

        Debug.Log($"{gameObject.name} taking {damage} damage");
        health.TakeDamage(damage);
        ApplyKnockback(hitSource, knockbackForce, knockbackDirection);
    }

    protected virtual void ApplyKnockback(Vector2 hitSource, float force, Vector2 direction)
    {
        // To be overridden by derived classes
        OnKnockbackReceived?.Invoke(direction * force);
    }

    #region Hit Effects
    public virtual void PlayHitFlash()
    {
        if (spriteRenderer == null || settings == null)
        {
            Debug.LogWarning("Cannot play hit flash: missing spriteRenderer or settings");
            return;
        }

        // Don't stop existing flash - let it complete
        // Instead, track that we've been hit again
        if (hitFlashCoroutine != null)
        {
            // Option 1: Just return if already flashing
            // return;

            // Option 2: Track multiple hits and extend the flash
            // (implemented below)
            flashHitCount++;
            return;
        }

        flashHitCount = 1;
        hitFlashCoroutine = StartCoroutine(HitFlashRoutine());
    }

    // Add this field to BaseEnemy class
    private int flashHitCount = 0;

    protected virtual IEnumerator HitFlashRoutine()
    {
        isFlashing = true;
        Color flashColor = settings.hitFlashColor;
        float flashDuration = settings.hitFlashDuration;
        int baseFlashCount = Mathf.Max(1, settings.hitFlashCount);

        // Calculate total flashes based on how many hits occurred during the flash
        int totalFlashes = baseFlashCount * flashHitCount;
        flashHitCount = 0; // Reset for next time

        Debug.Log($"Starting flash with color: {flashColor} for {totalFlashes} flashes");

        for (int i = 0; i < totalFlashes; i++)
        {
            // Flash to hit color
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration / 2f);

            // Return to original
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration / 2f);
        }

        spriteRenderer.color = originalColor;
        isFlashing = false;
        hitFlashCoroutine = null;
    }
    #endregion

    protected virtual void OnDestroy()
    {
        if (health != null)
        {
            health.OnDamaged -= HandleDamaged;
            health.OnHealthChanged -= HandleHealthChanged;
            health.OnDeath -= HandleDeath;
        }
    }
}