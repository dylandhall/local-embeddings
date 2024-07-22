namespace LocalEmbeddings.Managers;

public abstract class BaseState(CurrentState currentState) : IProgramStateManager
{
    protected CurrentState CurrentState { get; set; } = currentState;

    protected IProgramStateManager ToState(CurrentState state)
    {
        CurrentState = state;
        return this;
    }

    public abstract Task<IProgramStateManager> UpdateAndProcess();
    public bool IsFinished => CurrentState == CurrentState.Finished;
}