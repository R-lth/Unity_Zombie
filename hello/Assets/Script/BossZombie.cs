using UnityEngine;

public class BossZombie : Zombie
{
    [Header("Boss Attack")]
    [SerializeField] private float attackCooldown = 1.5f;

    private float attackTimer;

    private static readonly int SpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    protected override void OnBeforeStateTick()
    {
        attackTimer += Time.deltaTime;
    }

    protected override void SetMovingAnimation(bool isMoving)
    {
        // 보스 Animator는 IsRun 대신 MoveSpeed 파라미터를 사용합니다.
    }

    protected override void UpdateAnimation()
    {
        float normalizedSpeed = 0f;

        if (Agent.isOnNavMesh && Agent.speed > 0f)
        {
            normalizedSpeed = Agent.velocity.magnitude / Agent.speed;
        }

        AnimatorComponent.SetFloat(
            SpeedHash,
            Mathf.Clamp01(normalizedSpeed),
            0.1f,
            Time.deltaTime);
    }

    internal void TryAttack()
    {
        if (attackTimer < attackCooldown)
        {
            return;
        }

        attackTimer = 0f;
        AnimatorComponent.SetTrigger(AttackHash);
    }
}
