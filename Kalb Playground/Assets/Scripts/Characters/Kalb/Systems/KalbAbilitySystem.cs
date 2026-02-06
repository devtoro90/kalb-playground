using UnityEngine;

public class KalbAbilitySystem : MonoBehaviour
{
    [SerializeField] private KalbSettings settings;
    
    // Events for ability unlocks (optional, for future use)
    public System.Action<bool> OnRunUnlocked;
    public System.Action<bool> OnDashUnlocked;
    public System.Action<bool> OnDoubleJumpUnlocked;
    public System.Action<bool> OnWallJumpUnlocked;
    public System.Action<bool> OnWallLockUnlocked;
    
    // Public methods to unlock abilities
    public void UnlockRun()
    {
        settings.runUnlocked = true;
        OnRunUnlocked?.Invoke(true);
    }
    
    public void UnlockDash()
    {
        settings.dashUnlocked = true;
        OnDashUnlocked?.Invoke(true);
    }

    public void UnlockDoubleJump()
    {
        settings.doubleJumpUnlocked = true;
        OnDoubleJumpUnlocked?.Invoke(true);
    }

    public void UnlockWallJump()
    {
        settings.wallJumpUnlocked = true;
        OnWallJumpUnlocked?.Invoke(true);
    }
    public void UnlockWallLock()
    {
        settings.wallLockUnlocked = true;
        OnWallLockUnlocked?.Invoke(true);
    }
    
    public void UnlockAllAbilities()
    {
        UnlockRun();
        UnlockDash();
        UnlockDoubleJump();
        UnlockWallJump();
        UnlockWallLock();
    }

    public void ResetAbilities()
    {
        settings.runUnlocked = false;
        settings.dashUnlocked = false;
        settings.doubleJumpUnlocked = false;
        settings.wallJumpUnlocked = false;
        settings.wallLockUnlocked = false;
    }
    
    // Helper methods to check abilities
    public bool CanRun() => settings.runUnlocked;
    public bool CanDash() => settings.dashUnlocked;
    public bool CanDoubleJump() => settings.doubleJumpUnlocked;
    public bool CanWallJump() => settings.wallJumpUnlocked;
    public bool CanWallLock() => settings.wallLockUnlocked;
}