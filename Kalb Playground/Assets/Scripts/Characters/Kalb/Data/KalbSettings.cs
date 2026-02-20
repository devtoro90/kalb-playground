using UnityEngine;

[CreateAssetMenu(fileName = "KalbSettings", menuName = "KalbCharacter/Settings")]
public class KalbSettings : ScriptableObject
{
    [Header("Basic Movement")]
    public float moveSpeed = 7f;
    public float jumpForce = 15f;
    [Range(0, 0.3f)] public float movementSmoothing = 0.05f;
    
    [Header("Jump & Air")]
    public float jumpCutMultiplier = 0.5f;
    public float jumpHorizontalPreservation = 0.8f; // % of run speed preserved on jump
    public float runningJumpBoost = 2.5f;           // Extra forward force on running jumps
    public float coyoteTime = 0.12f;               // Ground forgiveness time
    public float jumpBufferTime = 0.1f;   

    [Header("Air Control")]
    public float airAcceleration = 10f;          // Normal air acceleration
    public float airDeceleration = 8f;          // Slowing down in air (no input)
    public float airTurnAcceleration = 10f;      // Quick turn acceleration
    public float airFriction = 4f;  
    
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

    [Header("Upward Attack Settings")]
    public bool enableUpwardAttack = true;
    public float upwardAttackDamage = 25f;
    public float upwardAttackKnockback = 8f;
    public float upwardAttackRange = 0.7f;
    public float upwardAttackDuration = 0.2f;
    public float upwardAttackCooldown = 0.3f;
    public Vector2 upwardAttackPointOffset = new Vector2(0f, 0.8f); // Above player
    public Vector2 upwardAttackKnockbackDirection = new Vector2(0f, 1f); // Straight up
    public string upwardAttackAnimation = "Kalb_attack_up";
    public float upwardAttackForwardForce = 0f; // No forward movement
    public float upwardAttackUpwardForce = 0f; // Slight upward boost
    public bool enableUpwardAirAttack = true;
    public float upwardAirAttackDamage = 20f;
    public float upwardAirAttackKnockback = 6f;
    public string upwardAirAttackAnimation = "Kalb_attack_up";
    
    [Header("Wall Attack Settings")]
    public bool enableWallAttack = true;
    public float wallAttackDamage = 30f;
    public float wallAttackKnockback = 10f;
    public float wallAttackRange = 0.8f;
    public float wallAttackDuration = 0.25f;
    public float wallAttackCooldown = 0.3f;
    public Vector2 wallAttackPointOffset = new Vector2(0.8f, 0f); // Horizontal offset from player center
    public Vector2 wallAttackKnockbackDirection = new Vector2(2f, 1f); // Away from wall and up
    public string wallAttackAnimation = "Kalb_attack_wall";
    
    [Header("Wall Attack Exit Options")]
    public bool stayInWallLockAfterAttack = true; // Stay in wall lock after attack
    public bool allowWallJumpDuringAttack = true; // Can jump to cancel attack
    public float wallAttackExitDelay = 0.1f; // Delay before exiting wall lock

    [Header("Pogo Attack Settings")]
    public bool enablePogoAttack = true;
    public float pogoAttackDamage = 15f;
    public float pogoAttackKnockback = 5f;
    public float pogoAttackRange = 0.6f;
    public float pogoAttackDuration = 0.15f;  // Short attack window
    public float pogoAttackCooldown = 0.2f;    // Brief cooldown
    public float pogoBounceForce = 15f;
    public float pogoMinBounceForce = 15f;
    public float pogoMaxBounceForce = 15f;
    public float pogoMomentumPreservation = 0.7f;
    public float pogoInputControlTime = 0.1f;
    public float pogoChainWindow = 0.25f;      // Time to chain next pogo
    public bool pogoResetsDoubleJump = true;
    public bool pogoResetsAirDash = true;
    public int maxPogoChains = 5;               // Max consecutive pogos
    public Vector2 pogoAttackPointOffset = new Vector2(0f, -0.8f);
    public Vector2 pogoAttackKnockbackDirection = new Vector2(0f, 1f);
    public string pogoAttackAnimation = "Kalb_pogo_attack";
    public string pogoBounceAnimation = "Kalb_pogo_bounce"; 
    public LayerMask pogoTargetLayers;  

    [Header("Hard Landing Settings")]
    public bool enableHardLanding = true;
    public float hardLandingFallThreshold = 10f;           // Minimum fall distance to trigger hard landing
    public float hardLandingRecoveryTime = 1f;          // How long player is locked in recovery
    public float hardLandingMinVelocityThreshold = 8f;    // Minimum fall speed to trigger (optional fallback)
    public AnimationCurve hardLandingShakeIntensity = AnimationCurve.Linear(0, 0.1f, 10, 0.4f); // Maps fall distance to shake intensity
    public string hardLandingAnimation = "Kalb_hard_land";

    [Header("Ability Unlocks")]
    public bool ledgeGrabUnlocked = true;
    public bool runUnlocked = false;
    public bool dashUnlocked = false;
    public bool doubleJumpUnlocked = false;
    public bool wallJumpUnlocked = false;
    public bool wallLockUnlocked = false;
    public bool floatingFallUnlocked = false;

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

    [Header("Ledge Climb Validation")]
    public float maxClimbDistance = 2f; // Maximum allowed climb distance
    public float climbSurfaceCheckDistance = 1.5f; // How far to check for platform surface
    public float climbHorizontalBuffer = 0.3f; // Buffer from platform edge
    
    [Header("Run Settings")]
    public float runSpeed = 10f;
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
    public float wallSlideSpeed = -2f;// Time player sticks to wall before sliding
    public LayerMask wallLayer;
    public bool requireInputForWallSlide = true; 
    public float awayInputGracePeriod = 0.25f; // Time before slide disengages when pressing away
    public float awayInputDisengageDistance = 0.3f;
    
    [Header("Wall Jump Force")]
    public float wallJumpForce = 18f;
    public Vector2 wallJumpAngle = new Vector2(1f, 1.5f); // X,Y ratiopublic 
    public float wallJumpHorizontalLockDuration = 0.2f; // Reduced for more control
    public float wallJumpControlReduction = 0.1f; 

    [Header("Wall Lock Settings")]
    public float wallLockEnterSpeed = 0.2f; // Time to transition into lock
    public float wallLockExitSpeed = 0.15f; // Time to transition out of lock
    public float wallLockInputThreshold = 0.3f; // Minimum input to trigger lock

    [Header("Wall Sliding Physics")]
    [Tooltip("Initial slide speed when first grabbing wall")]
    public float wallSlideMinSpeed = -2f;        // Initial downward speed
    [Tooltip("Maximum slide speed after acceleration")]
    public float wallSlideMaxSpeed = -8f;        // Max downward speed
    [Tooltip("How quickly slide speed increases (units per second squared)")]
    public float wallSlideAcceleration = 15f;    // Acceleration rate
    [Tooltip("How quickly slide speed decreases when releasing")]
    public float wallSlideDeceleration = 20f;    // Deceleration rate
    [Tooltip("How long player sticks to wall before sliding")]
    public float wallStickDuration = 0.15f;      // Initial stickiness
    [Tooltip("Force pushing player toward wall")]
    public float wallStickForce = 10f;
    [Tooltip("Distance tolerance for wall sticking")]
    public float wallStickTolerance = 0.1f;
    [Tooltip("Multiplier for slide speed when not holding toward wall")]
    public float neutralSlideMultiplier = 0.5f;  // Slower slide when not pressing toward wall
    [Tooltip("How much slide momentum is preserved in wall jump")]
    [Range(0f, 1f)]
    public float wallJumpMomentumRetention = 0.4f; // Percentage of slide speed kept in jump
    [Tooltip("Bonus speed when tapping toward wall repeatedly")]
    public float wallTapBoostAmount = 1.5f;      // Speed boost per tap
    [Tooltip("Time window for tap boost detection")]
    public float wallTapBoostWindow = 0.3f;      // Time to register consecutive taps
    [Tooltip("Maximum number of tap boosts")]
    public int maxWallTapBoosts = 3;              // Max speed boosts from tapping
    [Header("Floating Fall Settings")]
    public bool enableFloatingFall = true;
    public float floatFallSpeed = -3f; // Slowed descent speed
    public float floatFallGravityMultiplier = 0.3f; // Reduced gravity while floating
    public float floatFallAcceleration = 5f; // How quickly float activates
    public float floatFallCooldown = 0.5f; // Cooldown after floating
    public float floatFallMinHeight = 1f; // Minimum height above ground
    public float floatFallMaxDuration = 2f; // Maximum float time
    public float floatFallHorizontalControl = 0.8f; // Horizontal control while floating (0-1)
    public bool floatFallResetsOnGround = true;
    public bool floatFallResetsOnWall = true;
    public bool floatFallResetsOnPogo = true;
    public bool floatFallResetsOnDash = true;
    public string floatFallAnimation = "Kalb_float_fall";
    public LayerMask groundCheckLayers; // For height check
}