using UnityEngine;

public enum CharacterRole
{
    Player,
    Enemy,
    Boss
}

// Entity의 역할과 데이터 Component 참조만 보관합니다.
[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public sealed class CharacterEntity : MonoBehaviour
{
    [SerializeField] private CharacterRole role = CharacterRole.Enemy;
    [SerializeField] private Health health;

    public CharacterRole Role => role;
    public Health Health => health;
    public bool IsAlive => health != null && !health.IsDead;

    private void Awake()
    {
        if (health == null)
        {
            health = GetComponent<Health>();
        }
    }

    private void OnEnable()
    {
        CharacterManager.Instance?.Register(this);
    }

    private void OnDisable()
    {
        CharacterManager.Instance?.Unregister(this);
    }

    public void Configure(CharacterRole newRole)
    {
        bool roleChanged = role != newRole;
        role = newRole;

        if (!roleChanged || !isActiveAndEnabled || CharacterManager.Instance == null)
        {
            return;
        }

        CharacterManager.Instance.Unregister(this);
        CharacterManager.Instance.Register(this);
    }

    public static CharacterEntity Ensure(GameObject target, CharacterRole role)
    {
        CharacterEntity entity = target.GetComponent<CharacterEntity>();

        if (entity == null)
        {
            entity = target.AddComponent<CharacterEntity>();
        }

        entity.Configure(role);
        return entity;
    }
}
