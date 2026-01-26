using UnityEngine;

public class KalbStateMachine
{
    private KalbState currentState;
    
    public KalbState CurrentState => currentState;
    
    public void Initialize(KalbState startingState)
    {
        currentState = startingState;
        currentState.Enter();
    }
    
    public void ChangeState(KalbState newState)
    {
        currentState.Exit();
        currentState = newState;
        currentState.Enter();
    }
    
    public void Update()
    {
        currentState.Update();
        Debug.Log($"[StateMachine] Current State: {currentState.GetType().Name}");
    }
    
    public void FixedUpdate()
    {
        currentState.FixedUpdate();
    }
    
    public void HandleInput()
    {
        currentState.HandleInput();
    }
}