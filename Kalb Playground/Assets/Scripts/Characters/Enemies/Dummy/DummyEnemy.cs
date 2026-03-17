// DummyEnemy.cs
using UnityEngine;
using System.Collections;

public class DummyEnemy : BaseEnemy
{
    [Header("Dummy Specific")]
    [SerializeField] private float knockbackMultiplier = 1.5f;
    [SerializeField] private float recoverTime = 1.5f;
    [SerializeField] private Transform resetPosition;

    [Header("Hit Shake")]
    [SerializeField] private bool enableHitShake = true;
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeMagnitude = 0.1f;
    [SerializeField] private int shakeVibrato = 10;
    [SerializeField] private bool shakeEvenIfStuck = true;

    private Vector2 startPosition;
    private bool isRecovering = false;
    private Coroutine knockbackRoutine;
    private Coroutine shakeRoutine;

    protected override void Awake()
    {
        base.Awake();
        startPosition = transform.position;

        // Make the dummy immortal
        if (health != null)
        {
            health.SetImmortal(true);
        }

        if (resetPosition == null)
        {
            GameObject resetObj = new GameObject("ResetPosition");
            resetObj.transform.parent = transform.parent;
            resetObj.transform.position = startPosition;
            resetPosition = resetObj.transform;
        }
    }

    // Override to add shake effect
    protected override void HandleDamaged()
    {
        Debug.Log("Dummy took damage - triggering hit shake and knockback");
        base.HandleDamaged(); // This triggers the flash and event

        // Add hit shake
        if (enableHitShake)
        {
            Debug.Log("Playing hit shake effect");
            PlayHitShake();
        }
    }

    public override void TakeDamage(int damage, Vector2 hitSource, float knockbackForce, Vector2 knockbackDirection)
    {
        if (!IsAlive || isRecovering) return;

        base.TakeDamage(damage, hitSource, knockbackForce, knockbackDirection);
    }

    protected override void ApplyKnockback(Vector2 hitSource, float force, Vector2 direction)
    {
        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        knockbackRoutine = StartCoroutine(KnockbackRoutine(direction * force * knockbackMultiplier));
    }

    // Override death handling to do nothing (immortal)
    protected override void HandleDeath()
    {
        // Dummy doesn't die - override to do nothing
        Debug.Log("Dummy is immortal - ignoring death");
    }

    #region Hit Shake
    private int shakeHitCount = 0;

    public void PlayHitShake()
    {
        Debug.Log("Attempting to play hit shake");

        // Don't stop existing shake - queue it instead
        if (shakeRoutine != null)
        {
            shakeHitCount++;
            Debug.Log($"Hit during shake - queueing. Count: {shakeHitCount}");
            return;
        }

        shakeHitCount = 1;
        shakeRoutine = StartCoroutine(HitShakeRoutine());
    }

    private IEnumerator HitShakeRoutine()
    {
        Debug.Log("Starting hit shake");

        int totalShakes = shakeHitCount;
        shakeHitCount = 0; // Reset

        for (int s = 0; s < totalShakes; s++)
        {
            Vector3 originalPos = transform.localPosition;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                float xOffset = Random.Range(-1f, 1f) * shakeMagnitude;
                float yOffset = Random.Range(-1f, 1f) * shakeMagnitude;

                transform.localPosition = originalPos + new Vector3(xOffset, yOffset, 0);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Return to original position between shakes
            transform.localPosition = originalPos;

            // Small pause between shakes if multiple hits
            if (s < totalShakes - 1)
            {
                yield return new WaitForSeconds(0.05f);
            }
        }

        shakeRoutine = null;
    }
    #endregion

    private IEnumerator KnockbackRoutine(Vector2 knockbackVelocity)
    {
        isRecovering = true;

        // Apply knockback
        if (rb != null)
        {
            rb.linearVelocity = knockbackVelocity;
        }

        // Let physics handle the knockback arc
        float elapsed = 0f;
        float maxTime = 2f;

        while (elapsed < maxTime && rb != null)
        {
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
        if (rb != null)
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
        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            KalbController player = collision.gameObject.GetComponent<KalbController>();
            if (player != null && settings != null && settings.contactDamage > 0)
            {
                Vector2 hitDirection = (collision.transform.position - transform.position).normalized;
                player.TakeDamage(settings.contactDamage, hitDirection);
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