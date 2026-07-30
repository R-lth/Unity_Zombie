namespace ZombieGame.AI
{
    public interface IState
    {
        void Enter();
        void Tick();
        void Exit();
    }

    public abstract class State : IState
    {
        public virtual void Enter()
        {
        }

        public virtual void Tick()
        {
        }

        public virtual void Exit()
        {
        }
    }

    public class FSM
    {
        public IState CurrentState { get; private set; }

        public void ChangeState(IState nextState)
        {
            if (nextState == null || ReferenceEquals(CurrentState, nextState))
            {
                return;
            }

            CurrentState?.Exit();
            CurrentState = nextState;
            CurrentState.Enter();
        }

        public void Tick()
        {
            CurrentState?.Tick();
        }
    }
}
