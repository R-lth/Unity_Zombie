using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using ZombieGame.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(ZombieStates))]
[RequireComponent(typeof(Health))]
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
    private float stunEndTime;
    private Vector3 knockbackDirection;
    private float knockbackStartSpeed;
    private float knockbackDuration;
    private float knockbackElapsed;

    private static readonly int IsRunHash = Animator.StringToHash("IsRun");
    private static readonly int IsStunnedHash =
        Animator.StringToHash("IsStunned");
    private static readonly int IdleStateHash =
        Animator.StringToHash("Base Layer.Idle");

    protected NavMeshAgent Agent { get; private set; }
    protected Animator AnimatorComponent { get; private set; }
    public Health HealthComponent { get; private set; }

    public bool IsDead =>fsm != null && ReferenceEquals(fsm.CurrentState, states.Death);
    public bool IsStunned => Time.time < stunEndTime;
    protected bool IsKnockedBack =>
        fsm != null && ReferenceEquals(fsm.CurrentState, states.Knockback);
    protected bool IsCrowdControlled => IsStunned || IsKnockedBack;

    protected virtual void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        AnimatorComponent = GetComponent<Animator>();
        states = GetComponent<ZombieStates>();
        HealthComponent = GetComponent<Health>();

        if (HealthComponent == null)
        {
            HealthComponent = gameObject.AddComponent<Health>();
        }

        HealthComponent.Died += Die;

        Agent.updateRotation = false;

        fsm = new FSM();
        states.Initialize(this);
    }

    protected virtual void OnDestroy()
    {
        if (HealthComponent != null)
        {
            HealthComponent.Died -= Die;
        }
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
        if (IsDead)
        {
            return;
        }

        fsm.ChangeState(states.Death);
    }

    public void ApplyStun(float duration)
    {
        if (IsDead || duration <= 0f)
        {
            return;
        }

        stunEndTime = Mathf.Max(stunEndTime, Time.time + duration);

        if (!ReferenceEquals(fsm.CurrentState, states.Knockback))
        {
            fsm.ChangeState(states.Stunned);
        }
    }

    public void ApplyKnockback(
        Vector3 direction,
        float startSpeed,
        float duration)
    {
        if (IsDead ||
            direction.sqrMagnitude <= 0.001f ||
            startSpeed <= 0f ||
            duration <= 0f)
        {
            return;
        }

        direction.y = 0f;
        knockbackDirection = direction.normalized;
        knockbackStartSpeed = startSpeed;
        knockbackDuration = duration;
        knockbackElapsed = 0f;
        fsm.ChangeState(states.Knockback);
    }

    internal void EnterPursuitMode(ZombiePursuitMode mode)
    {
        SetStunnedAnimation(false);
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

    internal void EnterStunned()
    {
        StopAndClearPath();
        SetMovingAnimation(false);
        SetStunnedAnimation(true);
    }

    internal void TickStunned()
    {
        if (IsStunned)
        {
            SetMovingAnimation(false);
            SetStunnedAnimation(true);
            return;
        }

        fsm.ChangeState(states.Chase);
    }

    internal void EnterKnockback()
    {
        StopAndClearPath();
        SetMovingAnimation(false);
        SetStunnedAnimation(true);
    }

    internal void TickKnockback()
    {
        SetMovingAnimation(false);
        SetStunnedAnimation(true);

        knockbackElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(knockbackElapsed / knockbackDuration);
        float speed = knockbackStartSpeed * (1f - progress);

        Vector3 displacement =
            knockbackDirection * speed * Time.deltaTime;

        if (Agent.isOnNavMesh)
        {
            Vector3 desiredPosition = transform.position + displacement;

            if (NavMesh.SamplePosition(
                    desiredPosition,
                    out NavMeshHit hit,
                    Mathf.Max(0.5f, Agent.radius * 2f),
                    Agent.areaMask))
            {
                Agent.Warp(hit.position);
            }
        }
        else
        {
            transform.position += displacement;
        }

        if (progress < 1f)
        {
            return;
        }

        fsm.ChangeState(IsStunned ? states.Stunned : states.Chase);
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

    protected virtual void SetStunnedAnimation(bool isStunned)
    {
        AnimatorComponent.SetBool(IsStunnedHash, isStunned);

        if (isStunned)
        {
            PlayControlledIdle(IdleStateHash, "Base Layer.Idle");
        }
    }

    protected void PlayControlledIdle(int stateHash, string statePath)
    {
        if (!AnimatorComponent.HasState(0, stateHash))
        {
            Debug.LogWarning(
                $"{name}: Animator state '{statePath}' was not found.",
                this);
            return;
        }

        AnimatorStateInfo currentState =
            AnimatorComponent.GetCurrentAnimatorStateInfo(0);

        if (currentState.fullPathHash == stateHash)
        {
            return;
        }

        AnimatorComponent.speed = 1f;
        AnimatorComponent.Play(stateHash, 0, 0f);
        AnimatorComponent.Update(0f);

        currentState = AnimatorComponent.GetCurrentAnimatorStateInfo(0);

        Debug.Log(
            $"{name}: crowd-control animation -> {statePath}, " +
            $"entered={currentState.fullPathHash == stateHash}",
            this);
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
