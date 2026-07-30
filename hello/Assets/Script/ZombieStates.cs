using UnityEngine;
using ZombieGame.AI;

public enum ZombiePursuitMode
{
    Chase,
    AdvanceAttack,
    StationaryAttack
}

public class ZombieStates : MonoBehaviour
{
    public IState Chase { get; protected set; }
    public IState AdvanceAttack { get; protected set; }
    public IState StationaryAttack { get; protected set; }
    public IState Death { get; protected set; }

    public virtual void Initialize(Zombie zombie)
    {
        Chase = CreateChaseState(zombie);
        AdvanceAttack = CreateAdvanceAttackState(zombie);
        StationaryAttack = CreateStationaryAttackState(zombie);
        Death = CreateDeathState(zombie);
    }

    protected virtual IState CreateChaseState(Zombie zombie)
    {
        return new ChaseState(zombie);
    }

    protected virtual IState CreateAdvanceAttackState(Zombie zombie)
    {
        return new AdvanceAttackState(zombie);
    }

    protected virtual IState CreateStationaryAttackState(Zombie zombie)
    {
        return new StationaryAttackState(zombie);
    }

    protected virtual IState CreateDeathState(Zombie zombie)
    {
        return new DeathState(zombie);
    }

    protected abstract class ZombieState : State
    {
        protected readonly Zombie Zombie;
        protected abstract ZombiePursuitMode Mode { get; }

        protected ZombieState(Zombie zombie)
        {
            Zombie = zombie;
        }

        public override void Enter()
        {
            Zombie.EnterPursuitMode(Mode);
        }

        public override void Tick()
        {
            if (Zombie.TickPursuitMode(Mode))
            {
                OnAfterTick();
            }
        }

        protected virtual void OnAfterTick()
        {
        }
    }

    protected class ChaseState : ZombieState
    {
        protected override ZombiePursuitMode Mode => ZombiePursuitMode.Chase;

        public ChaseState(Zombie zombie) : base(zombie)
        {
        }
    }

    protected class AdvanceAttackState : ZombieState
    {
        protected override ZombiePursuitMode Mode => ZombiePursuitMode.AdvanceAttack;

        public AdvanceAttackState(Zombie zombie) : base(zombie)
        {
        }
    }

    protected class StationaryAttackState : ZombieState
    {
        protected override ZombiePursuitMode Mode =>
            ZombiePursuitMode.StationaryAttack;

        public StationaryAttackState(Zombie zombie) : base(zombie)
        {
        }
    }

    protected class DeathState : State
    {
        private readonly Zombie zombie;

        public DeathState(Zombie zombie)
        {
            this.zombie = zombie;
        }

        public override void Enter()
        {
            zombie.EnterDeath();
        }
    }
}
