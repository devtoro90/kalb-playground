// BaseEnemy.cs
using UnityEngine;
using System;

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

    protected virtual void Awake()
    {
        InitializeComponents();
    }

    protected virtual void InitializeComponents()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (enemyCollider == null) enemyCollider = GetComponent<Collider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();

        // Add health if not present
        if (health == null)
        {
            health = gameObject.AddComponent<EnemyHealth>();
            health.Initialize(settings);
        }

        // Subscribe to health events
        health.OnHealthChanged += HandleHealthChanged;
        health.OnDeath += HandleDeath;
    }

    protected virtual void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        OnDamaged?.Invoke(gameObject);
    }

    protected virtual void HandleDeath()
    {
        OnDeath?.Invoke();
        // Death logic will be implemented later
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
        if (!IsAlive) return;

        health.TakeDamage(damage);
        ApplyKnockback(hitSource, knockbackForce, knockbackDirection);
    }

    protected virtual void ApplyKnockback(Vector2 hitSource, float force, Vector2 direction)
    {
        // To be overridden by derived classes
        OnKnockbackReceived?.Invoke(direction * force);
    }

    protected virtual void OnDestroy()
    {
        if (health != null)
        {
            health.OnHealthChanged -= HandleHealthChanged;
            health.OnDeath -= HandleDeath;
        }
    }
}