using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSkillSystem : MonoBehaviour
{
    public enum SkillId
    {
        Attack,
        Horn,
        Flash
    }

    [Header("Common")]
    [SerializeField, Min(0f)] private float globalInputLockDuration = 0.05f;

    [Header("Attack")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject sphereBulletPrefab;
    [SerializeField, Min(1)] private int bulletCount = 5;
    [SerializeField, Range(0f, 90f)] private float attackHalfAngle = 30f;
    [SerializeField, Min(0.1f)] private float attackRange = 20f;
    [SerializeField, Min(0f)] private float bulletSpeed = 25f;
    [SerializeField, Min(0f)] private float attackDamage = 100f;
    [SerializeField, Min(0f)] private float attackCooldown = 1f;
    [SerializeField, Min(0.05f)] private float fallbackBulletScale = 0.3f;

    [Header("Horn")]
    [SerializeField, Min(0.1f)] private float hornRange = 10f;
    [SerializeField, Range(0f, 180f)] private float hornHalfAngle = 45f;
    [SerializeField, Min(0f)] private float hornKnockbackForce = 8f;
    [SerializeField, Min(0.01f)] private float hornKnockbackDuration = 3f;
    [SerializeField, Range(0f, 1f)] private float hornSideSpread = 0.2f;
    [SerializeField, Min(0f)] private float hornCooldown = 5f;

    [Header("Flash")]
    [SerializeField, Min(0.1f)] private float flashRange = 15f;
    [SerializeField, Range(0f, 180f)] private float flashHalfAngle = 60f;
    [SerializeField, Min(0f)] private float flashStunDuration = 5f;
    [SerializeField, Min(0f)] private float flashCooldown = 10f;

    private readonly List<Zombie> affectedZombies = new();
    private float attackReadyTime;
    private float hornReadyTime;
    private float flashReadyTime;
    private float nextSkillInputTime;
    private int lastSkillUseFrame = -1;

    public event Action<SkillId, float, float> CooldownChanged;

    private void Update()
    {
        NotifyCooldowns();
    }

    public bool TryUseSkill(SkillId skill)
    {
        if (lastSkillUseFrame == Time.frameCount || Time.time < nextSkillInputTime || !IsCooldownReady(skill))
        {
            return false;
        }

        lastSkillUseFrame = Time.frameCount;
        nextSkillInputTime = Time.time + Mathf.Max(0f, globalInputLockDuration);

        ExecuteSkill(skill);
        StartCooldown(skill);
        NotifyCooldown(skill);
        return true;
    }

    private void ExecuteSkill(SkillId skill)
    {
        switch (skill)
        {
            case SkillId.Attack:
                ExecuteAttack();
                break;

            case SkillId.Horn:
                ExecuteHorn();
                break;

            case SkillId.Flash:
                ExecuteFlash();
                break;
        }
    }

    private void ExecuteAttack()
    {
        IReadOnlyCollection<Zombie> zombies = FindZombiesInSector(attackRange, attackHalfAngle);

        foreach (Zombie zombie in zombies)
        {
            zombie.HealthComponent.TakeDamage(attackDamage);
        }

        int count = Mathf.Max(1, bulletCount);

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float angle = Mathf.Lerp(-attackHalfAngle, attackHalfAngle, t);
            float radians = angle * Mathf.Deg2Rad;

            Vector3 direction = transform.forward * Mathf.Cos(radians) + transform.right * Mathf.Sin(radians);

            SpawnSphereBullet(direction.normalized);
        }
    }

    private void ExecuteHorn()
    {
        IReadOnlyCollection<Zombie> zombies = FindZombiesInSector(hornRange, hornHalfAngle);

        float angleLimit = Mathf.Cos(hornHalfAngle * Mathf.Deg2Rad);

        foreach (Zombie zombie in zombies)
        {
            Vector3 offset = zombie.transform.position - transform.position;
            offset.y = 0f;

            float distance = offset.magnitude;

            if (distance <= 0.001f)
            {
                offset = transform.forward;
                distance = 0f;
            }

            Vector3 direction = offset.normalized;
            float dot = Vector3.Dot(transform.forward, direction);
            float crossY = Vector3.Cross(transform.forward, direction).y;
            float sideSign = Mathf.Approximately(crossY, 0f) ? 0f : Mathf.Sign(crossY);
            float distanceFactor = 1f - Mathf.Clamp01(distance / hornRange);
            float angleFactor = Mathf.InverseLerp(angleLimit, 1f, dot);
            float falloff = distanceFactor * angleFactor;
            float strength = Mathf.Lerp(0.35f, 1f, falloff);
            float force = hornKnockbackForce * strength;

            Vector3 knockbackDirection = (direction + transform.right * sideSign * hornSideSpread).normalized;

            zombie.ApplyKnockback(knockbackDirection, force, hornKnockbackDuration);
        }
    }

    private void ExecuteFlash()
    {
        IReadOnlyCollection<Zombie> zombies = FindZombiesInSector(flashRange, flashHalfAngle);

        foreach (Zombie zombie in zombies)
        {
            zombie.ApplyStun(flashStunDuration);
        }
    }

    private IReadOnlyCollection<Zombie> FindZombiesInSector(float range, float halfAngle)
    {
        affectedZombies.Clear();
        Zombie[] candidates = FindObjectsByType<Zombie>(FindObjectsSortMode.None);

        foreach (Zombie zombie in candidates)
        {
            if (zombie == null || zombie.IsDead || !IsInsideSector(zombie.transform.position, range, halfAngle))
            {
                continue;
            }

            affectedZombies.Add(zombie);
        }

        return affectedZombies;
    }

    private bool IsInsideSector(Vector3 targetPosition, float range, float halfAngle)
    {
        Vector3 offset = targetPosition - transform.position;
        offset.y = 0f;

        float distance = offset.magnitude;

        if (distance > range)
        {
            return false;
        }

        if (distance <= 0.001f)
        {
            return true;
        }

        Vector3 direction = offset / distance;
        float dot = Vector3.Dot(transform.forward, direction);
        float angleLimit = Mathf.Cos(halfAngle * Mathf.Deg2Rad);
        return dot >= angleLimit;
    }

    private void SpawnSphereBullet(Vector3 direction)
    {
        Vector3 spawnPosition = GetFirePosition();
        GameObject bulletObject;

        if (sphereBulletPrefab != null)
        {
            bulletObject = Instantiate(
                sphereBulletPrefab,
                spawnPosition,
                Quaternion.LookRotation(direction));
        }
        else
        {
            bulletObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulletObject.name = "Sphere Bullet";
            bulletObject.transform.SetPositionAndRotation(
                spawnPosition,
                Quaternion.LookRotation(direction));
            bulletObject.transform.localScale =
                Vector3.one * fallbackBulletScale;
        }

        SphereBullet bullet = bulletObject.GetComponent<SphereBullet>();

        if (bullet == null)
        {
            bullet = bulletObject.AddComponent<SphereBullet>();
        }

        bullet.Initialize(
            gameObject,
            direction,
            bulletSpeed,
            attackRange);
    }

    private Vector3 GetFirePosition()
    {
        return firePoint != null
            ? firePoint.position
            : transform.position + transform.forward * 2.5f + Vector3.up * 0.5f;
    }

    private bool IsCooldownReady(SkillId skill)
    {
        return skill switch
        {
            SkillId.Attack => Time.time >= attackReadyTime,
            SkillId.Horn => Time.time >= hornReadyTime,
            SkillId.Flash => Time.time >= flashReadyTime,
            _ => false
        };
    }

    private void StartCooldown(SkillId skill)
    {
        switch (skill)
        {
            case SkillId.Attack:
                attackReadyTime = Time.time + attackCooldown;
                break;

            case SkillId.Horn:
                hornReadyTime = Time.time + hornCooldown;
                break;

            case SkillId.Flash:
                flashReadyTime = Time.time + flashCooldown;
                break;
        }
    }

    private void NotifyCooldowns()
    {
        NotifyCooldown(SkillId.Attack);
        NotifyCooldown(SkillId.Horn);
        NotifyCooldown(SkillId.Flash);
    }

    private void NotifyCooldown(SkillId skill)
    {
        float readyTime = GetReadyTime(skill);
        float duration = GetCooldownDuration(skill);
        float remaining = Mathf.Max(0f, readyTime - Time.time);
        float normalizedReady = duration <= 0f? 1f : 1f - Mathf.Clamp01(remaining / duration);

        CooldownChanged?.Invoke(skill, normalizedReady, remaining);
    }

    private float GetReadyTime(SkillId skill)
    {
        return skill switch
        {
            SkillId.Attack => attackReadyTime,
            SkillId.Horn => hornReadyTime,
            SkillId.Flash => flashReadyTime,
            _ => 0f
        };
    }

    private float GetCooldownDuration(SkillId skill)
    {
        return skill switch
        {
            SkillId.Attack => attackCooldown,
            SkillId.Horn => hornCooldown,
            SkillId.Flash => flashCooldown,
            _ => 0f
        };
    }

    private void OnDrawGizmosSelected()
    {
        DrawSectorBoundary(attackRange, attackHalfAngle, Color.red);
        DrawSectorBoundary(hornRange, hornHalfAngle, Color.yellow);
        DrawSectorBoundary(flashRange, flashHalfAngle, Color.cyan);
    }

    private void DrawSectorBoundary(float range, float halfAngle, Color color)
    {
        Gizmos.color = color;
        Vector3 left =
            Quaternion.AngleAxis(-halfAngle, Vector3.up) * transform.forward;
        Vector3 right =
            Quaternion.AngleAxis(halfAngle, Vector3.up) * transform.forward;
        Gizmos.DrawRay(transform.position, left * range);
        Gizmos.DrawRay(transform.position, right * range);
    }
}
