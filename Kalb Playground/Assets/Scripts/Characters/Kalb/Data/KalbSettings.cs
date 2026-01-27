using UnityEngine;

[CreateAssetMenu(fileName = "KalbSettings", menuName = "KalbCharacter/Settings")]
public class KalbSettings : ScriptableObject
{
    [Header("Basic Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    [Range(0, 0.3f)] public float movementSmoothing = 0.05f;
    
    [Header("Jump & Air")]
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.1f;
    public float jumpCutMultiplier = 0.5f;
    public float airControlMultiplier = 0.5f;
    public float maxAirSpeed = 10f;
    public float airAcceleration = 15f;
    
    [Header("Physics")]
    public float fallingGravityScale = 2.5f;
    public float normalGravityScale = 2f;
    public float quickFallGravityMultiplier = 1.2f;
    public float maxFallSpeed = -20f;
    
    [Header("Environment Detection")]
    public float groundCheckRadius = 0.2f;
    public LayerMask environmentLayer;
    
    [Header("Swimming Settings")]
    public float swimSpeed = 3f;
    public float swimFastSpeed = 5f;
    public float swimDashSpeed = 10f;
    public float swimJumpForce = 8f;
    public float waterSurfaceOffset = 1.20f;
    public float waterEntrySpeedReduction = 0.5f;
    public LayerMask waterLayer;
    public float waterCheckRadius = 0.5f;
    public float waterEntryGravity = 0.5f;
    public float buoyancyStrength = 50f;
    public float buoyancyDamping = 10f;
    public float maxBuoyancyForce = 20f;
    public float floatAmplitude = 0.05f;
    public float floatFrequency = 1f;
    public float floatSmoothness = 5f;
    public bool enableFloating = true;
    public float swimDashDuration = 0.15f;
    public float swimDashCooldown = 0.3f;

    [Header("Combo System Settings")]
    public int maxComboHits = 3;
    public float comboWindow = 0.2f;
    public float comboResetTime = 0.6f;
    public bool enableAirCombo = true;
    
    [Header("Combo Attack Data")]
    public float[] comboDamage = new float[] { 20f, 25f, 35f };
    public float[] comboKnockback = new float[] { 5f, 7f, 12f };
    public float[] comboRange = new float[] { 0.5f, 0.5f, 0.6f };
    public float[] comboAttackDurations = new float[] { 0.2f, 0.2f, 0.2f };
    public float[] comboCooldowns = new float[] { 0.2f, 0.1f, 0.3f };
    public float[] comboForwardForce = new float[] { 3f, 4f, 0f };
    public float[] comboUpwardForce = new float[] { 0f, 0f, 3f };
    
    [Header("Combo Animation Names")]
    public string[] comboAnimations = new string[] { "Kalb_attack1", "Kalb_attack2", "Kalb_attack3" };
    public string comboResetAnimation = "Kalb_attack_reset";
    
    [Header("Combo Attack Point")]
    public Vector2 attackPointOffset = new Vector2(0.5f, 0f);
    public LayerMask enemyLayers;
    
    [Header("Combo Visual Effects")]
    public GameObject hitEffectPrefab;
    public float hitEffectDuration = 0.3f;
    public Color comboFlashColor = Color.white;
    public float comboFlashDuration = 0.1f;

    [Header("Ability Unlocks")]
    public bool runUnlocked = false;
    public bool dashUnlocked = false;
    
    [Header("Run Settings")]
    public float runSpeed = 8f;
    public float runAcceleration = 20f;
    public float runDeceleration = 25f;
    public float runTurnaroundMultiplier = 0.7f;
    public float runJumpBoost = 1.2f;
    
    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.5f;
    public bool canAirDash = true;
    public bool resetAirDashOnGround = true;
    public int maxAirDashes = 1;
    public float dashEndSlowdown = 0.5f;
    public float dashTurnaroundMultiplier = 0.5f;
    public float dashJumpBoost = 1.5f;
    public bool canDashDiagonal = true;
    public float diagonalDashMultiplier = 0.707f; // 1/√2 for 45-degree dashes

    [Header("Ledge Settings")]
    public float ledgeDetectionDistance = 0.5f;
    public float ledgeGrabOffsetY = 0.15f;
    public float ledgeGrabOffsetX = 0.55f;
    public float ledgeClimbTime = 0.2f;
    public float ledgeJumpForce = 12f;
    public Vector2 ledgeJumpAngle = new Vector2(1, 2);
    public float ledgeClimbCheckRadius = 0.2f;
    public float minLedgeHoldTime = 0.3f;
    public float ledgeReleaseForce = 5f;
    public float ledgeReleaseCooldown = 0.2f;
    public bool ledgeGrabUnlocked = true;

    [Header("Ledge Climb Validation")]
    public float maxClimbDistance = 2f; // Maximum allowed climb distance
    public float climbSurfaceCheckDistance = 1.5f; // How far to check for platform surface
    public float climbHorizontalBuffer = 0.3f; // Buffer from platform edge
}