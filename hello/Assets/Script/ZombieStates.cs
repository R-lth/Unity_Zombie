using UnityEngine;
using ZombieGame.AI;

public class ZombieStates : MonoBehaviour
{
    public IState Chase { get; private set; }
    public IState Attack { get; private set; }
    public IState Death { get; private set; }

    public void Initialize(Zombie zombie)
    {
        Chase = new ChaseState(zombie);
        Attack = new AttackState(zombie);
        Death = new DeathState(zombie);
    }

    private abstract class ZombieState : State
    {
        protected readonly Zombie Zombie;

        protected ZombieState(Zombie zombie)
        {
            Zombie = zombie;
        }
    }

    private sealed class ChaseState : ZombieState
    {
        public ChaseState(Zombie zombie) : base(zombie)
        {
        }

        public override void Enter()
        {
            Zombie.StartChasing();
        }

        public override void Tick()
        {
            if (!Zombie.HasTarget)
            {
                return;
            }

            if (Zombie.IsPlayerInAttackRange)
            {
                Zombie.ChangeToAttackState();
                return;
            }

            Zombie.ChasePlayer();
        }

        public override void Exit()
        {
            Zombie.StopMoving();
        }
    }

    private sealed class AttackState : ZombieState
    {
        public AttackState(Zombie zombie) : base(zombie)
        {
        }

        public override void Enter()
        {
            Zombie.StartAttacking();
        }

        public override void Tick()
        {
            if (!Zombie.IsPlayerInAttackRange)
            {
                Zombie.ChangeToChaseState();
            }
        }
    }

    // 사망 상태의 동작은 추후 구현합니다.
    private sealed class DeathState : ZombieState
    {
        public DeathState(Zombie zombie) : base(zombie)
        {
        }
    }
}
