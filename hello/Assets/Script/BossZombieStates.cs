using ZombieGame.AI;

public class BossZombieStates : ZombieStates
{
    protected override IState CreateAdvanceAttackState(Zombie zombie)
    {
        return new BossAdvanceAttackState((BossZombie)zombie);
    }

    protected override IState CreateStationaryAttackState(Zombie zombie)
    {
        return new BossStationaryAttackState((BossZombie)zombie);
    }

    private sealed class BossAdvanceAttackState : AdvanceAttackState
    {
        private readonly BossZombie boss;

        public BossAdvanceAttackState(BossZombie boss) : base(boss)
        {
            this.boss = boss;
        }

        protected override void OnAfterTick()
        {
            boss.TryAttack();
        }
    }

    private sealed class BossStationaryAttackState : StationaryAttackState
    {
        private readonly BossZombie boss;

        public BossStationaryAttackState(BossZombie boss) : base(boss)
        {
            this.boss = boss;
        }

        protected override void OnAfterTick()
        {
            boss.TryAttack();
        }
    }
}
