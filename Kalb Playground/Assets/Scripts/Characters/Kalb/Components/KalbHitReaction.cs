// KalbHitReaction.cs - Full version with debug
using UnityEngine;
using System.Collections;

public class KalbHitReaction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KalbController controller;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private KalbSettings settings;
    [SerializeField] private KalbHealth health;

    [Header("State")]
    [SerializeField] private bool isInvulnerable = false;
    [SerializeField] private float invulnerabilityTimer = 0f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private Color gizmoColor = Color.yellow;
    [SerializeField] private float gizmoSize = 0.5f;

    public System.Action<int, Vector2> OnHitTriggered;
    public System.Action OnInvulnerabilityStarted;
    public System.Action OnInvulnerabilityEnded;

    public bool IsInvulnerable => isInvulnerable;
    public float InvulnerabilityRemaining => invulnerabilityTimer;

    private Coroutine flashRoutine;
    private Color originalColor;
    private Material originalMaterial;

    private void Awake()
    {


        if (controller == null) controller = GetComponent<KalbController>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (health == null) health = GetComponent<KalbHealth>();
        if (settings == null && controller != null) settings = controller.Settings;

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            originalMaterial = spriteRenderer.material;

        }

        if (health != null)
        {
            health.OnDamaged += HandleDamageTaken;

        }


    }

    private void Update()
    {
        if (invulnerabilityTimer > 0)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (enableDebugLogs && invulnerabilityTimer % 0.5f < Time.deltaTime)


                if (invulnerabilityTimer <= 0)
                {
                    EndInvulnerability();
                }
        }
    }

    private void HandleDamageTaken(int damage, Vector2 hitSource)
    {


        if (isInvulnerable)
        {

            return;
        }

        OnHitTriggered?.Invoke(damage, hitSource);


        StartInvulnerability();
    }

    public void TriggerHit(Vector2 hitSource, int damage)
    {


        if (isInvulnerable)
        {

            return;
        }

        if (health == null)
        {
            Debug.LogError("[HitReaction] Health component is null!");
            return;
        }

        if (health.IsDead)
        {

            return;
        }


        health.TakeDamage(damage, hitSource);
    }

    private void StartInvulnerability()
    {


        if (isInvulnerable) return;

        isInvulnerable = true;
        invulnerabilityTimer = settings != null ? settings.invulnerabilityDuration : 1.5f;



        if (health != null)
        {
            health.SetInvulnerable(true);
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }
        flashRoutine = StartCoroutine(InvulnerabilityFlashRoutine());

        OnInvulnerabilityStarted?.Invoke();


    }

    private void EndInvulnerability()
    {


        if (!isInvulnerable) return;

        isInvulnerable = false;
        invulnerabilityTimer = 0f;

        if (health != null)
        {
            health.SetInvulnerable(false);
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
            if (originalMaterial != null)
            {
                spriteRenderer.material = originalMaterial;
            }
        }

        OnInvulnerabilityEnded?.Invoke();


    }

    private IEnumerator InvulnerabilityFlashRoutine()
    {
        if (spriteRenderer == null) yield break;



        float flashDuration = settings != null ? settings.invulnerabilityDuration : 1.5f;
        float flashInterval = settings != null ? settings.invulnerabilityFlashInterval : 0.1f;

        bool isVisible = true;
        float flashTimer = 0f;
        int flashCount = 0;

        while (isInvulnerable)
        {
            flashTimer += Time.deltaTime;

            if (flashTimer >= flashInterval)
            {
                flashTimer = 0f;
                isVisible = !isVisible;
                flashCount++;

                Color newColor = originalColor;
                newColor.a = isVisible ? 1f : 0.3f;
                spriteRenderer.color = newColor;


            }

            yield return null;
        }


        spriteRenderer.color = originalColor;
        flashRoutine = null;
    }

    public bool CanTakeDamage()
    {
        bool canTake = !isInvulnerable && health != null && !health.IsDead;
        return canTake;
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDamaged -= HandleDamageTaken;
        }
    }

    private void OnDrawGizmos()
    {
        if (!enableDebugLogs) return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position + Vector3.up, gizmoSize);

        if (isInvulnerable)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, gizmoSize * 0.5f);
        }
    }
}