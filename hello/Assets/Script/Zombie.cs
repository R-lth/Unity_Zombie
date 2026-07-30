using UnityEngine;
using UnityEngine.AI;
using ZombieGame.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(ZombieStates))]
public class Zombie : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float attackMoveSpeed = 1f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 3f;

    private Transform playerTransform;
    private NavMeshAgent agent;
    private Animator animator;
    private ZombieStates states;
    private FSM fsm;

    private static readonly int IsRunHash = Animator.StringToHash("IsRun");

    public bool IsDead => fsm.CurrentState == states.Death;

    internal bool HasTarget => playerTransform != null;
    internal bool IsPlayerInAttackRange =>
        HasTarget && GetDistanceToPlayer() <= attackRange;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        states = GetComponent<ZombieStates>();

        if (states == null)
        {
            states = gameObject.AddComponent<ZombieStates>();
        }

        fsm = new FSM();
        states.Initialize(this);
    }

    private void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            playerTransform = player.transform;
        }

        ChangeToChaseState();
    }

    private void Update()
    {
        fsm.Tick();
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
        agent.speed = chaseSpeed;
        animator.SetBool(IsRunHash, true);
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
        agent.speed = attackMoveSpeed;
        animator.SetBool(IsRunHash, false);
    }

    private float GetDistanceToPlayer()
    {
        if (playerTransform == null)
        {
            return float.PositiveInfinity;
        }

        return Vector3.Distance(transform.position, playerTransform.position);
    }
}
