using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class SphereBullet : MonoBehaviour, IPoolable
{
    private Vector3 direction;
    private Vector3 startPosition;
    private float speed;
    private float maxDistance;
    private bool initialized;
    private Rigidbody body;
    private Transform ownerTransform;
    private float castRadius;
    private SphereCollider bulletCollider;
    private Collider[] ignoredOwnerColliders;

    public void Initialize(
        GameObject owner,
        Vector3 moveDirection,
        float moveSpeed,
        float attackRange)
    {
        direction = moveDirection.normalized;
        ownerTransform = owner.transform;
        speed = Mathf.Max(0f, moveSpeed);
        maxDistance = Mathf.Max(0.1f, attackRange);
        startPosition = transform.position;
        initialized = true;

        RestoreOwnerCollisions();
        bulletCollider = GetComponent<SphereCollider>();
        bulletCollider.isTrigger = true;
        castRadius = Mathf.Max(0.01f, bulletCollider.bounds.extents.x);

        body = GetComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        ignoredOwnerColliders = owner.GetComponentsInChildren<Collider>();

        foreach (Collider ownerCollider in ignoredOwnerColliders)
        {
            Physics.IgnoreCollision(bulletCollider, ownerCollider);
        }
    }

    private void FixedUpdate()
    {
        if (!initialized)
        {
            return;
        }

        float moveDistance = speed * Time.fixedDeltaTime;
        RaycastHit[] hits = Physics.SphereCastAll(
            body.position,
            castRadius,
            direction,
            moveDistance,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide);

        RaycastHit? nearestHit = null;

        foreach (RaycastHit hit in hits)
        {
            Transform hitTransform = hit.collider.transform;

            if (IsOwnerTransform(hitTransform))
            {
                continue;
            }

            if (!nearestHit.HasValue ||
                hit.distance < nearestHit.Value.distance)
            {
                nearestHit = hit;
            }
        }

        if (nearestHit.HasValue)
        {
            if (HandleHit(nearestHit.Value.collider))
            {
                return;
            }
        }

        body.MovePosition(body.position + direction * moveDistance);
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        if ((transform.position - startPosition).sqrMagnitude >=
            maxDistance * maxDistance)
        {
            initialized = false;
            PoolManager.Instance.Return(this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!initialized || IsOwnerTransform(other.transform))
        {
            return;
        }

        HandleHit(other);
    }

    private bool IsOwnerTransform(Transform target)
    {
        return target == ownerTransform || target.IsChildOf(ownerTransform);
    }

    private bool HandleHit(Collider other)
    {
        Zombie zombie = other.GetComponentInParent<Zombie>();

        if (zombie == null && other.isTrigger)
        {
            return false;
        }

        initialized = false;
        PoolManager.Instance.Return(this);
        return true;
    }

    public void OnPoolSpawned()
    {
        initialized = false;
    }

    public void OnPoolDespawned()
    {
        initialized = false;
        ownerTransform = null;
        RestoreOwnerCollisions();

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    private void RestoreOwnerCollisions()
    {
        if (bulletCollider == null || ignoredOwnerColliders == null)
        {
            return;
        }

        foreach (Collider ownerCollider in ignoredOwnerColliders)
        {
            if (ownerCollider != null)
            {
                Physics.IgnoreCollision(
                    bulletCollider,
                    ownerCollider,
                    false);
            }
        }

        ignoredOwnerColliders = null;
    }
}
