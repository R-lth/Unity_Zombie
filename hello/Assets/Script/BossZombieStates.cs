using UnityEngine;
using ZombieGame.AI;

public class BossZombieStates : MonoBehaviour
{
    public IState Chase { get; private set; }
    public IState Attack { get; private set; }
    public IState Death { get; private set; }

    public void Initialize(BossZombie bossZombie)
    {
        Chase = new ChaseState(bossZombie);
        Attack = new AttackState(bossZombie);
        Death = new DeathState(bossZombie);
    }

    private abstract class BossState : State
    {
        protected readonly BossZombie Boss;

        protected BossState(BossZombie bossZombie)
        {
            Boss = bossZombie;
        }
    }

    private sealed class ChaseState : BossState
    {
        public ChaseState(BossZombie bossZombie) : base(bossZombie)
        {
        }

        public override void Enter()
        {
            Boss.StartChasing();
        }

        public override void Tick()
        {
            if (!Boss.HasTarget)
            {
                return;
            }

            if (Boss.IsPlayerInAttackRange)
            {
                Boss.ChangeToAttackState();
                return;
            }

            Boss.ChasePlayer();
        }

        public override void Exit()
        {
            Boss.StopMoving();
        }
    }

    private sealed class AttackState : BossState
    {
        public AttackState(BossZombie bossZombie) : base(bossZombie)
        {
        }

        public override void Enter()
        {
            Boss.StartAttacking();
        }

        public override void Tick()
        {
            if (!Boss.IsPlayerInAttackRange)
            {
                Boss.ChangeToChaseState();
                return;
            }

            Boss.UpdateAttack();
        }
    }

    // 사망 상태의 동작은 추후 구현합니다.
    private sealed class DeathState : BossState
    {
        public DeathState(BossZombie bossZombie) : base(bossZombie)
        {
        }
    }
}
