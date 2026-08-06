using UnityEngine;

public class BossZombie : Zombie
{
    [Header("Boss Attack")]
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackDamageRange = 6f;

    private float attackTimer;
    private Health playerHealth;
    private Collider[] playerColliders;

    private static readonly int SpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int IdleStateHash = Animator.StringToHash("Base.Walk");

    public override void OnPoolSpawned()
    {
        attackTimer = 0f;
        base.OnPoolSpawned();
    }

    protected override void Start()
    {
        base.Start();

        if (playerTransform != null)
        {
            playerHealth = playerTransform.GetComponent<Health>();
            playerColliders =
                playerTransform.GetComponentsInChildren<Collider>();
        }
    }

    protected override void OnBeforeStateTick()
    {
        attackTimer += Time.deltaTime;
    }

    protected override void SetMovingAnimation(bool isMoving)
    {
        if (!isMoving)
        {
            AnimatorComponent.SetFloat(SpeedHash, 0f);
        }
    }

    protected override void SetStunnedAnimation(bool isStunned)
    {
        if (!isStunned)
        {
            return;
        }

        AnimatorComponent.ResetTrigger(AttackHash);
        AnimatorComponent.SetFloat(SpeedHash, 0f);
        PlayControlledIdle(IdleStateHash, "Base.Walk");
    }

    protected override void UpdateAnimation()
    {
        float normalizedSpeed = 0f;

        if (!IsCrowdControlled &&
            Agent.isOnNavMesh &&
            Agent.speed > 0f)
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
        ApplyAttackDamage();
    }

    public void ApplyAttackDamage()
    {
        if (playerHealth == null || playerHealth.IsDead || !IsPlayerInsideDamageRange())
        {
            return;
        }

        playerHealth.TakeDamage(attackDamage);
    }

    private bool IsPlayerInsideDamageRange()
    {
        float nearestDistanceSqr = float.PositiveInfinity;

        if (playerColliders != null)
        {
            foreach (Collider playerCollider in playerColliders)
            {
                if (playerCollider == null || !playerCollider.enabled || playerCollider.isTrigger)
                {
                    continue;
                }

                Vector3 closestPoint = playerCollider.ClosestPoint(transform.position);
                closestPoint.y = transform.position.y;

                nearestDistanceSqr = Mathf.Min(nearestDistanceSqr, (closestPoint - transform.position).sqrMagnitude);
            }
        }

        if (float.IsPositiveInfinity(nearestDistanceSqr))
        {
            Vector3 offset = playerHealth.transform.position - transform.position;
            offset.y = 0f;
            nearestDistanceSqr = offset.sqrMagnitude;
        }

        return nearestDistanceSqr <= attackDamageRange * attackDamageRange;
    }
}
