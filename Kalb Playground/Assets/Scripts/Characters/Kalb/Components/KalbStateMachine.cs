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
        if (currentState != null)
        {

            currentState.Exit();
        }

        currentState = newState;

        if (currentState != null)
        {
            currentState.Enter();
        }
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

    public void ForceChangeState(KalbState newState)
    {
        if (currentState != null)
        {

            currentState.Exit();
        }

        currentState = newState;

        if (currentState != null)
        {

            currentState.Enter();
        }
    }
}