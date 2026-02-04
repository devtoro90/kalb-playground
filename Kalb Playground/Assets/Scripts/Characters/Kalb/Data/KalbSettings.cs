using UnityEngine;

[CreateAssetMenu(fileName = "KalbSettings", menuName = "KalbCharacter/Settings")]
public class KalbSettings : ScriptableObject
{
    [Header("Basic Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 15f;
    [Range(0, 0.3f)] public float movementSmoothing = 0.05f;
    
    [Header("Jump & Air")]
    public float jumpCutMultiplier = 0.5f;
    public float jumpHorizontalPreservation = 0.8f; // % of run speed preserved on jump
    public float runningJumpBoost = 2.5f;           // Extra forward force on running jumps
    public float coyoteTime = 0.12f;               // Ground forgiveness time
    public float jumpBufferTime = 0.1f;   

    [Header("Air Control")]
    public float airAcceleration = 20f;          // Normal air acceleration
    public float airDeceleration = 15f;          // Slowing down in air (no input)
    public float airTurnAcceleration = 30f;      // Quick turn acceleration
    public float maxAirSpeed = 8f;              // Maximum horizontal air speed
    public float airFriction = 2f;  
    
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

[Header("Ability Unlocks")]
    public bool runUnlocked = false;
    public bool dashUnlocked = false;
    public bool doubleJumpUnlocked = false;
    public bool wallJumpUnlocked = false;
    
    [Header("Run Settings")]
    public float runSpeed = 8f;
    public float runAcceleration = 20f;
    public float runDeceleration = 25f;
    public float runTurnaroundMultiplier = 0.7f;
    public float runJumpForwardForce = 2.5f; // NEW: Strong forward force for running jumps
    
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

    [Header("Double Jump Settings")]
    public float doubleJumpForce = 10f;
    public bool doubleJumpMaintainsMomentum = true;
    public float doubleJumpHorizontalBoost = 1.5f;

    [Header("Wall Jump Settings")]
    public float wallCheckDistance = 0.45f;
    public float wallSlideSpeed = -2.5f;// Time player sticks to wall before sliding
    public LayerMask wallLayer;
    public float wallStickForce = 5f;
    public float wallStickTolerance = 0.1f;
    
    [Header("Wall Jump Force")]
    public float wallJumpForce = 13f;
    public Vector2 wallJumpAngle = new Vector2(1f, 1.5f); // X,Y ratiopublic 
    public float wallJumpHorizontalLockDuration = 0.3f; // Reduced for more control
    public float wallJumpControlReduction = 0.3f; 
}