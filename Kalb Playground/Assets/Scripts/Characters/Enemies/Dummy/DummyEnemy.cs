// DummyEnemy.cs
using UnityEngine;
using System.Collections;

public class DummyEnemy : BaseEnemy
{
    [Header("Dummy Specific")]
    [SerializeField] private float knockbackMultiplier = 1.5f; // Dummy flies farther
    [SerializeField] private float recoverTime = 1.5f; // Time to reset position
    [SerializeField] private Transform resetPosition;

    private Vector2 startPosition;
    private bool isRecovering = false;
    private Coroutine knockbackRoutine;

    protected override void Awake()
    {
        base.Awake();
        startPosition = transform.position;

        if (resetPosition == null)
        {
            GameObject resetObj = new GameObject("ResetPosition");
            resetObj.transform.parent = transform.parent;
            resetObj.transform.position = startPosition;
            resetPosition = resetObj.transform;
        }
    }

    public override void TakeDamage(int damage, Vector2 hitSource, float knockbackForce, Vector2 knockbackDirection)
    {
        if (!IsAlive || isRecovering) return;

        base.TakeDamage(damage, hitSource, knockbackForce, knockbackDirection);

        // Play hit effect
        StartCoroutine(HitFlashRoutine());
    }

    protected override void ApplyKnockback(Vector2 hitSource, float force, Vector2 direction)
    {
        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        knockbackRoutine = StartCoroutine(KnockbackRoutine(direction * force * knockbackMultiplier));
    }

    private IEnumerator KnockbackRoutine(Vector2 knockbackVelocity)
    {
        isRecovering = true;

        // Apply knockback
        rb.linearVelocity = knockbackVelocity;

        // Let physics handle the knockback arc
        float elapsed = 0f;
        float maxTime = 2f; // Max time before forced reset

        while (elapsed < maxTime)
        {
            // Check if we've landed/stabilized
            if (Mathf.Abs(rb.linearVelocity.y) < 0.1f &&
                Mathf.Abs(rb.linearVelocity.x) < 0.1f &&
                elapsed > 0.5f)
            {
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Recover to start position
        yield return StartCoroutine(RecoverToStart());

        isRecovering = false;
        knockbackRoutine = null;
    }

    private IEnumerator RecoverToStart()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector2 startPos = transform.position;

        // Disable physics during recovery
        rb.simulated = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.position = Vector2.Lerp(startPos, resetPosition.position, t);
            yield return null;
        }

        transform.position = resetPosition.position;

        // Re-enable physics
        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;
    }

    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer == null) yield break;

        Color originalColor = spriteRenderer.color;
        float flashDuration = settings.hitFlashDuration;
        float halfFlash = flashDuration * 0.5f;

        // Flash to hit color
        float elapsed = 0f;
        while (elapsed < halfFlash)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfFlash;
            spriteRenderer.color = Color.Lerp(originalColor, settings.hitFlashColor, t);
            yield return null;
        }

        // Flash back
        elapsed = 0f;
        while (elapsed < halfFlash)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfFlash;
            spriteRenderer.color = Color.Lerp(settings.hitFlashColor, originalColor, t);
            yield return null;
        }

        spriteRenderer.color = originalColor;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if colliding with player
        if (collision.gameObject.CompareTag("Player"))
        {
            // Get player controller and deal damage
            KalbController player = collision.gameObject.GetComponent<KalbController>();
            if (player != null && settings.contactDamage > 0)
            {
                Vector2 hitDirection = (collision.transform.position - transform.position).normalized;
                player.TakeDamage(settings.contactDamage, transform.position);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (resetPosition != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(resetPosition.position, 0.3f);
            Gizmos.DrawLine(transform.position, resetPosition.position);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}