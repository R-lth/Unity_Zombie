using System;
using System.Collections.Generic;
using UnityEngine;

// CharacterEntity + Health 데이터를 가진 Entity의 생명주기를 처리하는 System입니다.
public class CharacterManager : MonoBehaviour
{
    private readonly Dictionary<CharacterEntity, Action> deathHandlers = new();

    public static CharacterManager Instance { get; private set; }

    public int PlayerCount => Count(CharacterRole.Player);
    public int EnemyCount => Count(CharacterRole.Enemy);
    public int BossCount => Count(CharacterRole.Boss);

    public event Action<CharacterEntity> CharacterDied;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("중복된 CharacterManager가 발견되어 파괴되었습니다.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        RegisterSceneEntities();
        GameManager.Instance.AttachCharacterManager(this);
    }

    private void OnDestroy()
    {
        Clear();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool Register(CharacterEntity entity)
    {
        if (entity == null || entity.Health == null || deathHandlers.ContainsKey(entity))
        {
            return false;
        }

        Action deathHandler = () => HandleDeath(entity);
        deathHandlers.Add(entity, deathHandler);
        entity.Health.Died += deathHandler;
        return true;
    }

    public bool Unregister(CharacterEntity entity)
    {
        if (entity == null || !deathHandlers.Remove(entity, out Action deathHandler))
        {
            return false;
        }

        if (entity.Health != null)
        {
            entity.Health.Died -= deathHandler;
        }

        return true;
    }

    public bool TryGetRole(Health health, out CharacterRole role)
    {
        foreach (CharacterEntity entity in deathHandlers.Keys)
        {
            if (entity != null && entity.Health == health)
            {
                role = entity.Role;
                return true;
            }
        }

        role = default;
        return false;
    }

    private void RegisterSceneEntities()
    {
        CharacterEntity[] entities = FindObjectsByType<CharacterEntity>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (CharacterEntity entity in entities)
        {
            Register(entity);
        }
    }

    private void HandleDeath(CharacterEntity entity)
    {
        CharacterDied?.Invoke(entity);
    }

    private int Count(CharacterRole role)
    {
        int count = 0;

        foreach (CharacterEntity entity in deathHandlers.Keys)
        {
            if (entity != null && entity.Role == role)
            {
                count++;
            }
        }

        return count;
    }

    private void Clear()
    {
        foreach (KeyValuePair<CharacterEntity, Action> pair in deathHandlers)
        {
            if (pair.Key != null && pair.Key.Health != null)
            {
                pair.Key.Health.Died -= pair.Value;
            }
        }

        deathHandlers.Clear();
    }
}
