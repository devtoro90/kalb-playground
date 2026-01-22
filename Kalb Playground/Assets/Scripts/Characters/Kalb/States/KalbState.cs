using UnityEngine;

public abstract class KalbState : IKalbState
{
    protected KalbController controller;
    protected KalbStateMachine stateMachine;

    
    protected KalbState(KalbController controller, KalbStateMachine stateMachine)
    {
        this.controller = controller;
        this.stateMachine = stateMachine;
    }
    
    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
    public virtual void HandleInput() { }
}