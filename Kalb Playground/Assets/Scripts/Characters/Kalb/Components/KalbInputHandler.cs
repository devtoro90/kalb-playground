using UnityEngine;
using UnityEngine.InputSystem;

public class KalbInputHandler : MonoBehaviour
{
    // Input Actions
    private InputAction moveAction;
    private InputAction jumpAction;
    
    // Input Values
    private Vector2 moveInput;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool jumpReleased;
    
    public Vector2 MoveInput => moveInput;
    public bool JumpPressed => jumpPressed;
    public bool JumpHeld => jumpHeld;
    public bool JumpReleased => jumpReleased;
    
    private void Awake()
    {
        // Get input actions from PlayerInput component
        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            moveAction = playerInput.actions["Move"];
            jumpAction = playerInput.actions["Jump"];
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
    }
    
    public void ResetJumpInput()
    {
        jumpPressed = false;
        jumpReleased = false;
    }
}