using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class SphereBullet : MonoBehaviour
{
    private Vector3 direction;
    private Vector3 startPosition;
    private float speed;
    private float maxDistance;
    private bool initialized;
    private Rigidbody body;
    private Transform ownerTransform;
    private float castRadius;

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

        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        castRadius = Mathf.Max(0.01f, sphereCollider.bounds.extents.x);

        body = GetComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();

        foreach (Collider ownerCollider in ownerColliders)
        {
            Physics.IgnoreCollision(sphereCollider, ownerCollider);
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
            Destroy(gameObject);
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
        Destroy(gameObject);
        return true;
    }
}
