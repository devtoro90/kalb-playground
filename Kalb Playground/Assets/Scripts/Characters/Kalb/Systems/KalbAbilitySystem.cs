using UnityEngine;

public class KalbAbilitySystem : MonoBehaviour
{
    [SerializeField] private KalbSettings settings;
    
    // Events for ability unlocks (optional, for future use)
    public System.Action<bool> OnRunUnlocked;
    public System.Action<bool> OnDashUnlocked;
    public System.Action<bool> OnDoubleJumpUnlocked;
    
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
    
    public void UnlockAllAbilities()
    {
        UnlockRun();
        UnlockDash();
        UnlockDoubleJump();
    }

    public void ResetAbilities()
    {
        settings.runUnlocked = false;
        settings.dashUnlocked = false;
        settings.doubleJumpUnlocked = false;
    }
    
    // Helper methods to check abilities
    public bool CanRun() => settings.runUnlocked;
    public bool CanDash() => settings.dashUnlocked;
    public bool CanDoubleJump() => settings.doubleJumpUnlocked;
}