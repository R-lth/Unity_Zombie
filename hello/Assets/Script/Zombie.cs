using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using ZombieGame.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(ZombieStates))]
public class Zombie : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] protected Transform playerTransform;

    [Header("Movement")]
    [FormerlySerializedAs("moveSpeed")]
    [SerializeField] protected float chaseSpeed = 10f;
    [SerializeField] protected float attackMoveSpeed = 1f;
    [SerializeField] protected float turnSpeed = 540f;

    [Header("Pursuit Ranges")]
    [FormerlySerializedAs("attackRange")]
    [SerializeField] protected float stationaryAttackRange = 3f;
    [SerializeField] protected float advanceAttackRange = 6f;
    [SerializeField] protected float rangeHysteresis = 0.35f;

    [Header("Path Refresh")]
    [SerializeField] protected float repathInterval = 0.15f;
    [SerializeField] protected float repathDistance = 0.25f;

    private ZombieStates states;
    private FSM fsm;
    private Vector3 lastDestination;
    private float nextRepathTime;
    private bool hasDestination;
    private Collider[] targetColliders;

    private static readonly int IsRunHash = Animator.StringToHash("IsRun");

    protected NavMeshAgent Agent { get; private set; }
    protected Animator AnimatorComponent { get; private set; }

    public bool IsDead => fsm.CurrentState == states.Death;

    protected virtual void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        AnimatorComponent = GetComponent<Animator>();
        states = GetComponent<ZombieStates>();

        Agent.updateRotation = false;

        fsm = new FSM();
        states.Initialize(this);
    }

    protected virtual void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");

            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        CacheTargetColliders();
        fsm.ChangeState(states.Chase);
    }

    protected virtual void Update()
    {
        OnBeforeStateTick();
        fsm.Tick();
        UpdateAnimation();
    }

    public void Die()
    {
        fsm.ChangeState(states.Death);
    }

    internal void EnterPursuitMode(ZombiePursuitMode mode)
    {
        UpdatePursuit(mode);
    }

    internal bool TickPursuitMode(ZombiePursuitMode mode)
    {
        if (playerTransform == null)
        {
            return false;
        }

        ZombiePursuitMode desiredMode = EvaluatePursuitMode(mode);

        if (desiredMode != mode)
        {
            fsm.ChangeState(GetState(desiredMode));
            return false;
        }

        UpdatePursuit(mode);
        return true;
    }

    internal void EnterDeath()
    {
        StopAndClearPath();
        SetMovingAnimation(false);
        OnDeath();
    }

    protected virtual void OnBeforeStateTick()
    {
    }

    protected virtual void UpdateChase()
    {
        SetMovingAnimation(true);
        MoveToward(chaseSpeed);
    }

    protected virtual void UpdateAdvanceAttack()
    {
        SetMovingAnimation(false);
        MoveToward(attackMoveSpeed);
    }

    protected virtual void UpdateStationaryAttack()
    {
        SetMovingAnimation(false);
        StopAndFacePlayer();
    }

    protected virtual void SetMovingAnimation(bool isMoving)
    {
        AnimatorComponent.SetBool(IsRunHash, isMoving);
    }

    protected virtual void UpdateAnimation()
    {
    }

    protected virtual void OnDeath()
    {
    }

    private void UpdatePursuit(ZombiePursuitMode mode)
    {
        if (playerTransform == null)
        {
            return;
        }

        switch (mode)
        {
            case ZombiePursuitMode.Chase:
                UpdateChase();
                break;

            case ZombiePursuitMode.AdvanceAttack:
                UpdateAdvanceAttack();
                break;

            case ZombiePursuitMode.StationaryAttack:
                UpdateStationaryAttack();
                break;
        }
    }

    private ZombiePursuitMode EvaluatePursuitMode(ZombiePursuitMode currentMode)
    {
        RefreshPath();

        float surfaceDistance = GetDistanceToTargetSurface();
        float stationaryExitRange = stationaryAttackRange + rangeHysteresis;
        float advanceRange = Mathf.Max(stationaryAttackRange, advanceAttackRange);
        float advanceExitRange = advanceRange + rangeHysteresis;

        if (currentMode == ZombiePursuitMode.StationaryAttack &&
            surfaceDistance <= stationaryExitRange)
        {
            return ZombiePursuitMode.StationaryAttack;
        }

        if (surfaceDistance <= stationaryAttackRange)
        {
            return ZombiePursuitMode.StationaryAttack;
        }

        float pathDistance = GetTravelDistance();

        if (currentMode == ZombiePursuitMode.AdvanceAttack &&
            pathDistance <= advanceExitRange)
        {
            return ZombiePursuitMode.AdvanceAttack;
        }

        return pathDistance <= advanceRange
            ? ZombiePursuitMode.AdvanceAttack
            : ZombiePursuitMode.Chase;
    }

    private IState GetState(ZombiePursuitMode mode)
    {
        switch (mode)
        {
            case ZombiePursuitMode.AdvanceAttack:
                return states.AdvanceAttack;
            case ZombiePursuitMode.StationaryAttack:
                return states.StationaryAttack;
            default:
                return states.Chase;
        }
    }

    private void MoveToward(float speed)
    {
        RefreshPath();

        if (Agent.isOnNavMesh)
        {
            Agent.isStopped = false;
            Agent.speed = speed;
        }

        RotateTowards(GetDirectionToPlayer());
    }

    private void StopAndFacePlayer()
    {
        // 정지 중에도 remainingDistance를 갱신할 수 있도록 경로는 유지합니다.
        RefreshPath();

        if (Agent.isOnNavMesh)
        {
            Agent.isStopped = true;
        }

        RotateTowards(GetDirectionToPlayer());
    }

    private void StopAndClearPath()
    {
        if (!Agent.isOnNavMesh)
        {
            return;
        }

        Agent.isStopped = true;
        Agent.ResetPath();
        hasDestination = false;
    }

    private void RefreshPath()
    {
        if (playerTransform == null ||
            !Agent.isOnNavMesh ||
            Time.time < nextRepathTime)
        {
            return;
        }

        Vector3 targetPosition = playerTransform.position;
        float repathDistanceSqr =
            Mathf.Max(0.01f, repathDistance * repathDistance);
        bool targetMoved = !hasDestination ||
                           (targetPosition - lastDestination).sqrMagnitude >=
                           repathDistanceSqr;
        bool needsPath = !Agent.hasPath && !Agent.pathPending;

        nextRepathTime = Time.time + Mathf.Max(0.05f, repathInterval);

        if (!targetMoved && !needsPath)
        {
            return;
        }

        if (Agent.SetDestination(targetPosition))
        {
            lastDestination = targetPosition;
            hasDestination = true;
        }
    }

    private float GetTravelDistance()
    {
        if (Agent.isOnNavMesh &&
            !Agent.pathPending &&
            Agent.hasPath &&
            !float.IsInfinity(Agent.remainingDistance))
        {
            return Agent.remainingDistance;
        }

        return GetDirectionToPlayer().magnitude;
    }

    private void CacheTargetColliders()
    {
        targetColliders = playerTransform != null
            ? playerTransform.GetComponentsInChildren<Collider>()
            : null;
    }

    private float GetDistanceToTargetSurface()
    {
        if (targetColliders == null || targetColliders.Length == 0)
        {
            return GetDirectionToPlayer().magnitude;
        }

        Vector3 zombiePosition = transform.position;
        float nearestDistanceSqr = float.PositiveInfinity;

        foreach (Collider targetCollider in targetColliders)
        {
            if (targetCollider == null ||
                !targetCollider.enabled ||
                targetCollider.isTrigger)
            {
                continue;
            }

            Vector3 closestPoint = targetCollider.ClosestPoint(zombiePosition);
            closestPoint.y = zombiePosition.y;

            float distanceSqr =
                (closestPoint - zombiePosition).sqrMagnitude;
            nearestDistanceSqr = Mathf.Min(nearestDistanceSqr, distanceSqr);
        }

        return float.IsPositiveInfinity(nearestDistanceSqr)
            ? GetDirectionToPlayer().magnitude
            : Mathf.Sqrt(nearestDistanceSqr);
    }

    private Vector3 GetDirectionToPlayer()
    {
        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0f;
        return direction;
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            Mathf.Max(0f, turnSpeed) * Time.deltaTime);
    }
}
