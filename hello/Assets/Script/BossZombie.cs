using UnityEngine;
using UnityEngine.AI;
using ZombieGame.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(BossZombieStates))]
public class BossZombie : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform playerTransform;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackCooldown = 1.5f;

    private NavMeshAgent agent;
    private Animator animator;
    private BossZombieStates states;
    private FSM fsm;
    private float attackTimer;

    private static readonly int SpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    public bool IsDead => fsm.CurrentState == states.Death;

    internal bool HasTarget => playerTransform != null;
    internal bool IsPlayerInAttackRange =>
        HasTarget && GetDistanceToPlayer() <= attackRange;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        states = GetComponent<BossZombieStates>();

        if (states == null)
        {
            states = gameObject.AddComponent<BossZombieStates>();
        }

        fsm = new FSM();
        states.Initialize(this);
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");

            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        agent.speed = moveSpeed;
        ChangeToChaseState();
    }

    private void Update()
    {
        attackTimer += Time.deltaTime;
        fsm.Tick();
        UpdateAnimation();
    }

    public void Die()
    {
        fsm.ChangeState(states.Death);
    }

    internal void ChangeToChaseState()
    {
        fsm.ChangeState(states.Chase);
    }

    internal void ChangeToAttackState()
    {
        fsm.ChangeState(states.Attack);
    }

    internal void StartChasing()
    {
        agent.isStopped = false;
        agent.speed = moveSpeed;
    }

    internal void ChasePlayer()
    {
        if (playerTransform != null)
        {
            agent.SetDestination(playerTransform.position);
        }
    }

    internal void StopMoving()
    {
        agent.ResetPath();
    }

    internal void StartAttacking()
    {
        agent.isStopped = true;
    }

    internal void UpdateAttack()
    {
        FacePlayer();
        TryAttack();
    }

    private float GetDistanceToPlayer()
    {
        if (playerTransform == null)
        {
            return float.PositiveInfinity;
        }

        return Vector3.Distance(transform.position, playerTransform.position);
    }

    private void FacePlayer()
    {
        if (playerTransform == null)
        {
            return;
        }

        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                10f * Time.deltaTime);
        }
    }

    private void TryAttack()
    {
        if (attackTimer >= attackCooldown)
        {
            attackTimer = 0f;
            animator.SetTrigger(AttackHash);
        }
    }

    private void UpdateAnimation()
    {
        float speed = 0f;

        if (!agent.isStopped && agent.speed > 0f)
        {
            speed = agent.velocity.magnitude / agent.speed;
        }

        speed = Mathf.Clamp01(speed);
        animator.SetFloat(SpeedHash, speed, 0.1f, Time.deltaTime);
    }
}
