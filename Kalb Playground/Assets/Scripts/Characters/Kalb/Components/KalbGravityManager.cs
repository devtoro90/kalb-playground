using UnityEngine;

public class KalbGravityManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private KalbSettings settings;
    [SerializeField] private KalbSwimming swimming;
    
    [Header("Debug")]
    [SerializeField] private bool showGravityDebug = false;
    
    // Gravity state
    private float baseGravityScale = 2f;
    private float currentOverride = -1f;
    private string overrideSource = "None";
    private float overrideTimer = 0f;
    
    // Properties
    public float CurrentGravityScale => currentOverride >= 0 ? currentOverride : baseGravityScale;
    public string OverrideSource => overrideSource;
    
    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (settings == null)
        {
            // Try to get from controller
            KalbController controller = GetComponent<KalbController>();
            if (controller != null) settings = controller.Settings;
        }
        if (swimming == null) swimming = GetComponent<KalbSwimming>();
        
        if (settings != null)
        {
            baseGravityScale = settings.normalGravityScale;
        }
    }
    
    private void Update()
    {
        // Update override timer
        if (overrideTimer > 0)
        {
            overrideTimer -= Time.deltaTime;
            if (overrideTimer <= 0)
            {
                ClearOverride();
            }
        }
    }
    
    private void FixedUpdate()
    {
        ApplyGravity();
    }
    
    public void SetGravityOverride(float gravityScale, string source = "Unknown", float duration = 0f)
    {
        currentOverride = gravityScale;
        overrideSource = source;
        
        if (duration > 0)
        {
            overrideTimer = duration;
        }
        
        if (showGravityDebug)
        {
            Debug.Log($"[GravityManager] Override set to {gravityScale} by {source}");
        }
    }
    
    public void ClearOverride(string source = null)
    {
        // Only clear if source matches or no source specified
        if (source == null || overrideSource == source)
        {
            currentOverride = -1f;
            overrideSource = "None";
            overrideTimer = 0f;
            
            if (showGravityDebug)
            {
                
            }
        }
    }
    
    public void ResetToBaseGravity()
    {
        ClearOverride();
        ApplyGravity();
    }
    
    private void ApplyGravity()
    {
        if (rb == null) return;
        
        // Skip if swimming - swimming system handles buoyancy
        if (swimming != null && swimming.IsSwimming)
        {
            // Swimming handles its own gravity/buoyancy
            return;
        }
        
        float targetGravity = currentOverride >= 0 ? currentOverride : baseGravityScale;
        
        // Only update if changed
        if (Mathf.Abs(rb.gravityScale - targetGravity) > 0.01f)
        {
            rb.gravityScale = targetGravity;
            
            if (showGravityDebug)
            {
                
            }
        }
    }
    
    public void SetBaseGravity(float gravity)
    {
        baseGravityScale = gravity;
        if (currentOverride < 0) // If no override active
        {
            ApplyGravity();
        }
    }
    
    // Helper methods for common states
    public void SetNormalGravity()
    {
        if (settings != null)
        {
            SetGravityOverride(settings.normalGravityScale, "Normal");
        }
    }
    
    public void SetFallingGravity()
    {
        if (settings != null)
        {
            SetGravityOverride(settings.fallingGravityScale, "Falling");
        }
    }
    
    public void SetQuickFallGravity()
    {
        if (settings != null)
        {
            SetGravityOverride(settings.fallingGravityScale * settings.quickFallGravityMultiplier, "QuickFall");
        }
    }
    
    public void SetZeroGravity(string source = "ZeroGravity")
    {
        SetGravityOverride(0f, source);
    }
}