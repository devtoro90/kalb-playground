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
        Debug.Log($"KalbStateMachine: Changing state from {currentState.GetType().Name} to {newState.GetType().Name}");
        currentState.Exit();
        currentState = newState;
        currentState.Enter();
    }
    
    public void Update()
    {
        currentState.Update();
        
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