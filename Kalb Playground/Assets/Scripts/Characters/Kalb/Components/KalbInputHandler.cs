using UnityEngine;
using UnityEngine.InputSystem;

public class KalbInputHandler : MonoBehaviour
{
    // Input Actions
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private InputAction attackAction;
    
    // Input Values
    private Vector2 moveInput;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool jumpReleased;
    private bool dashPressed;
    private bool dashHeld;
    private bool dashReleased;
    private bool attackPressed;
    
    public Vector2 MoveInput => moveInput;
    public bool JumpPressed => jumpPressed;
    public bool JumpHeld => jumpHeld;
    public bool JumpReleased => jumpReleased;
    public bool DashPressed => dashPressed;
    public bool DashHeld => dashHeld;
    public bool DashReleased => dashReleased;
    public bool AttackPressed => attackPressed;
    
    private void Awake()
    {
        // Get input actions from PlayerInput component
        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            moveAction = playerInput.actions["Move"];
            jumpAction = playerInput.actions["Jump"];
            dashAction = playerInput.actions["Dash/Run"];
            attackAction = playerInput.actions["Attack"];
        }
    }
    
    private void Update()
    {
        ReadInputs();
    }
    
    private void ReadInputs()
    {
        // Read movement input
        moveInput = moveAction.ReadValue<Vector2>();
        
        // Read jump input
        jumpPressed = jumpAction.WasPressedThisFrame();
        jumpHeld = jumpAction.IsPressed();
        jumpReleased = jumpAction.WasReleasedThisFrame();
        
        // Read dash input
        dashPressed = dashAction.WasPressedThisFrame();
        dashHeld = dashAction.IsPressed();
        dashReleased = dashAction.WasReleasedThisFrame();

        //Read attack input
        attackPressed = attackAction.WasPressedThisFrame();
    }
    
    public void ResetJumpInput()
    {
        jumpPressed = false;
        jumpReleased = false;
    }
    
    public void ResetDashInput()
    {
        dashPressed = false;
        dashReleased = false;
    }

    public void ResetAttackInput()
    {
        attackPressed = false;
    }
}