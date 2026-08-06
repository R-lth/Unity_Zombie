using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class EscapeZone : MonoBehaviour
{
    [SerializeField] private GameObject availableVisual;
    [SerializeField, Min(0.5f)] private float defaultRadius = 3f;

    public bool IsAvailable { get; private set; }

    private void Awake()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;

        if (availableVisual == null)
        {
            availableVisual = CreateDefaultMarker();
        }
    }

    private void OnEnable()
    {
        GameManager.Instance.StateChanged += HandleGameStateChanged;
        SetAvailable(GameManager.Instance.State == GameState.EscapeReady);
    }

    private void OnDisable()
    {
        if (GameManager.Current != null)
        {
            GameManager.Current.StateChanged -= HandleGameStateChanged;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsAvailable)
        {
            return;
        }

        CharacterEntity entity = other.GetComponentInParent<CharacterEntity>();

        if (entity != null && entity.Role == CharacterRole.Player)
        {
            GameManager.Instance.RequestEscape(entity);
        }
    }

    public void SetAvailable(bool available)
    {
        IsAvailable = available;

        if (availableVisual != null)
        {
            availableVisual.SetActive(available);
        }
    }

    public static EscapeZone EnsureForScene()
    {
        EscapeZone[] zones = FindObjectsByType<EscapeZone>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (zones.Length > 0)
        {
            return zones[0];
        }

        GameObject player = GameObject.FindWithTag("Player");
        Vector3 origin = player != null ? player.transform.position : Vector3.zero;
        Vector3 desiredPosition = origin + Vector3.forward * 25f;
        Vector3 zonePosition = desiredPosition;

        if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, 15f, NavMesh.AllAreas))
        {
            zonePosition = hit.position;
        }

        GameObject zoneObject = new GameObject("EscapeZone");
        zoneObject.transform.position = zonePosition;

        SphereCollider trigger = zoneObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 3f;
        trigger.center = Vector3.up;

        return zoneObject.AddComponent<EscapeZone>();
    }

    private void HandleGameStateChanged(GameState state)
    {
        SetAvailable(state == GameState.EscapeReady);
    }

    private GameObject CreateDefaultMarker()
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "Escape Marker";
        marker.transform.SetParent(transform, false);
        marker.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        marker.transform.localScale = new Vector3(defaultRadius, 0.05f, defaultRadius);

        Collider markerCollider = marker.GetComponent<Collider>();

        if (markerCollider != null)
        {
            Destroy(markerCollider);
        }

        Renderer markerRenderer = marker.GetComponent<Renderer>();

        if (markerRenderer != null)
        {
            markerRenderer.material.color = new Color(0.1f, 1f, 0.35f, 0.85f);
        }

        return marker;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsAvailable ? Color.green : Color.gray;
        Gizmos.DrawWireSphere(transform.position + Vector3.up, defaultRadius);
    }
}
