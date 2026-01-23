using UnityEngine;

public class KalbAbilitySystem : MonoBehaviour
{
    [SerializeField] private KalbSettings settings;
    
    // Events for ability unlocks (optional, for future use)
    public System.Action<bool> OnRunUnlocked;
    public System.Action<bool> OnDashUnlocked;
    public System.Action<bool> OnWallJumpUnlocked;
    public System.Action<bool> OnDoubleJumpUnlocked;
    public System.Action<bool> OnWallLockUnlocked;
    public System.Action<bool> OnPogoUnlocked;
    
    // Public methods to unlock abilities
    public void UnlockRun()
    {
        settings.runUnlocked = true;
        OnRunUnlocked?.Invoke(true);
        Debug.Log("Run ability unlocked!");
    }
    
    public void UnlockDash()
    {
        settings.dashUnlocked = true;
        OnDashUnlocked?.Invoke(true);
        Debug.Log("Dash ability unlocked!");
    }
    
    public void UnlockWallJump()
    {
        settings.wallJumpUnlocked = true;
        OnWallJumpUnlocked?.Invoke(true);
        Debug.Log("Wall jump ability unlocked!");
    }
    
    public void UnlockDoubleJump()
    {
        settings.doubleJumpUnlocked = true;
        OnDoubleJumpUnlocked?.Invoke(true);
        Debug.Log("Double jump ability unlocked!");
    }
    
    public void UnlockWallLock()
    {
        settings.wallLockUnlocked = true;
        OnWallLockUnlocked?.Invoke(true);
        Debug.Log("Wall lock ability unlocked!");
    }
    
    public void UnlockPogo()
    {
        settings.pogoUnlocked = true;
        OnPogoUnlocked?.Invoke(true);
        Debug.Log("Pogo ability unlocked!");
    }
    
    public void UnlockAllAbilities()
    {
        UnlockRun();
        UnlockDash();
        UnlockWallJump();
        UnlockDoubleJump();
        UnlockWallLock();
        UnlockPogo();
    }
    
    public void ResetAbilities()
    {
        settings.runUnlocked = false;
        settings.dashUnlocked = false;
        settings.wallJumpUnlocked = false;
        settings.doubleJumpUnlocked = false;
        settings.wallLockUnlocked = false;
        settings.pogoUnlocked = false;
        
        Debug.Log("All abilities reset!");
    }
    
    // Helper methods to check abilities
    public bool CanRun() => settings.runUnlocked;
    public bool CanDash() => settings.dashUnlocked;
    public bool CanWallJump() => settings.wallJumpUnlocked;
    public bool CanDoubleJump() => settings.doubleJumpUnlocked;
    public bool CanWallLock() => settings.wallLockUnlocked;
    public bool CanPogo() => settings.pogoUnlocked;
}