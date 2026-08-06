using UnityEngine;
using UnityEngine.AI; // NavMesh 기능을 사용하기 위해 반드시 필요합니다.
using System.Collections.Generic;

public class MobSpawn : MonoBehaviour
{
    [SerializeField] private GameObject[] monsters;
    [SerializeField] private PoolId[] monsterPoolIds;
    [SerializeField, Min(0)] private int initialPoolSize = 10;
    [SerializeField, Min(1)] private int maxPoolSize = 64;

    private readonly List<PoolId> spawnPoolIds = new();
    GameObject Player;
    float timer = 0f;

    void Start() 
    {
        Player = GameObject.FindWithTag("Player");

        if (monsters == null)
        {
            return;
        }

        for (int i = 0; i < monsters.Length; i++)
        {
            GameObject monster = monsters[i];
            Zombie zombiePrefab = monster != null
                ? monster.GetComponent<Zombie>()
                : null;

            if (zombiePrefab == null)
            {
                Debug.LogError("Monster prefab requires a Zombie component.", this);
                continue;
            }

            PoolId poolId = ResolvePoolId(i, zombiePrefab);

            if (PoolManager.Instance.Register(
                    poolId,
                    zombiePrefab,
                    initialPoolSize,
                    maxPoolSize))
            {
                spawnPoolIds.Add(poolId);
            }
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer > 3f)
        {
            if (Player == null || spawnPoolIds.Count == 0)
            {
                return;
            }

            Vector3 finalSpawnPos = GetRandomNavMeshPosition();
            Vector3 toPlayer = Player.transform.position - finalSpawnPos; 
            toPlayer.y = 0f;
            Quaternion look = Quaternion.LookRotation(toPlayer);

            PoolId poolId = spawnPoolIds[Random.Range(0, spawnPoolIds.Count)];
            PoolManager.Instance.Rent<Zombie>(poolId, finalSpawnPos, look);

            timer = 0f;
        }
    }

    // AI 길 위에서만 좌표를 찾아주는 함수
    //Vector3 GetRandomNavMeshPosition()
    //{
    //    NavMeshHit hit;
    //    Vector3 randomDir = Player.transform.position + Random.insideUnitSphere * spawnRadius;

    //    if (NavMesh.SamplePosition(randomDir, out hit, spawnRadius, NavMesh.AllAreas))
    //    {
    //        return hit.position;
    //    }

    //    // 운이 없다면
    //    return Player.transform.position;
    //}
    Vector3 GetRandomNavMeshPosition()
    {
        if (Player == null) { return Vector3.zero; }

        NavMeshHit hit;

        Vector3 randomDirection = Random.onUnitSphere;
        randomDirection.y = 0f;
        randomDirection.Normalize(); 

        // 거리 랜덤
        float randomDistance = Random.Range(3f, 10f);


        Vector3 targetPos = Player.transform.position + (randomDirection * randomDistance);

        // 무작위 거리 좌표 근처에서 가장 가까운 AI 길(NavMesh)을 찾습니다.
        if (NavMesh.SamplePosition(targetPos, out hit, 3.0f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return Vector3.zero;
    }

    private PoolId ResolvePoolId(int index, Zombie zombiePrefab)
    {
        if (monsterPoolIds != null &&
            index < monsterPoolIds.Length &&
            monsterPoolIds[index] != PoolId.None)
        {
            return monsterPoolIds[index];
        }

        return zombiePrefab is BossZombie
            ? PoolId.BossZombie
            : PoolId.NormalZombie;
    }
}




