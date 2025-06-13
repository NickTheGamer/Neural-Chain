using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class Agent : MonoBehaviour
{
    public int maxHealth;
    public int currentHealth;
    public LayerMask visionMask;
    public Transform visionOrigin;

    public GameObject bulletPrefab;
    public Transform firePoint;
    public int maxAmmo = 12;
    public int currentAmmo;
    public float burstInterval = 0.1f;
    public float burstCooldown = 1f;
    public float fireAngleThreshold = 2f;

    public NavMeshAgent navAgent;

    public GameObject currentTarget;
    private bool isShooting = false;

    public float viewDistance = 20f;
    public float fieldOfView = 90f;
    public int rayCount = 15;
    public int sideRayCount = 4;
    public float sideRayArc = 60f;
    public float sideViewDistance = 5f;
    public int rearRayCount = 5;
    public float rearViewDistance = 3f;
    private bool showRay = true;

    public TextMeshProUGUI healthAmmoText;
    private Quaternion UIRotation;

    private AudioSource audioPlayer;
    private AudioClip gunshot;

    public static event Action<Vector3> OnBulletBurstFired;

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        currentAmmo = maxAmmo;

        gunshot = Resources.Load<AudioClip>("Sounds/gunshot3");
        GameObject globalAudioObj = GameObject.FindWithTag("Audio");
        if (globalAudioObj != null)
        {
            audioPlayer = globalAudioObj.GetComponent<AudioSource>();
        }

        if (healthAmmoText != null) UIRotation = healthAmmoText.transform.rotation;
    }

    protected virtual void Update()
    {
        if (healthAmmoText != null)
        {
            healthAmmoText.text = $"Health: {currentHealth}\nAmmo: {currentAmmo}";
            healthAmmoText.transform.rotation = UIRotation;
        }

        ScanForTargets();

        if (currentTarget != null)
        {
            RotateTowardTarget();

            if (CanFireAtTarget())
            {
                StartCoroutine(FireBurst());
            }
        }
        else {
            navAgent.updateRotation = true;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HealthPack"))
        {
            currentHealth = maxHealth;
            other.gameObject.SetActive(false);
        }

        else if (other.CompareTag("AmmoBox"))
        {
            currentAmmo = maxAmmo;
            other.gameObject.SetActive(false);
        }
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }

    private void ScanForTargets()
    {
        currentTarget = null;
        float closestDist = float.MaxValue;

        Vector3 forward = transform.forward;

        // === Front FOV rays ===
        float halfFOV = fieldOfView / 2f;
        for (int i = 0; i < rayCount; i++)
        {
            float angle = Mathf.Lerp(-halfFOV, halfFOV, i / (float)(rayCount - 1));
            Vector3 dir = Quaternion.Euler(0, angle, 0) * forward;

            if (CastVisionRay(dir, viewDistance, ref closestDist)) continue;

            if (showRay)
                Debug.DrawRay(visionOrigin.position, dir * viewDistance, Color.green);
        }

        // === Side rays (left and right arcs) ===
        float sideArcStart = fieldOfView / 2f;
        float sideArcEnd = sideArcStart + sideRayArc;

        // Right side
        for (int i = 0; i < sideRayCount; i++)
        {
            float angle = Mathf.Lerp(sideArcStart, sideArcEnd, i / (float)(sideRayCount - 1));
            Vector3 dir = Quaternion.Euler(0, angle, 0) * forward;

            if (CastVisionRay(dir, sideViewDistance, ref closestDist)) continue;

            if (showRay)
                Debug.DrawRay(visionOrigin.position, dir * sideViewDistance, Color.magenta);
        }

        // Left side (mirrored angles)
        for (int i = 0; i < sideRayCount; i++)
        {
            float angle = Mathf.Lerp(-sideArcEnd, -sideArcStart, i / (float)(sideRayCount - 1));
            Vector3 dir = Quaternion.Euler(0, angle, 0) * forward;

            if (CastVisionRay(dir, sideViewDistance, ref closestDist)) continue;

            if (showRay)
                Debug.DrawRay(visionOrigin.position, dir * sideViewDistance, Color.magenta);
        }

        // === Rear rays ===
        // Total arc already used = front FOV + left side arc + right side arc
        float usedArc = fieldOfView + (2 * sideRayArc);
        float rearArc = 360f - usedArc;

        // Center the rear arc directly behind the agent
        float rearStart = 180f - (rearArc / 2f);
        float rearEnd = 180f + (rearArc / 2f);

        for (int i = 0; i < rearRayCount; i++)
        {
            float angle = Mathf.Lerp(rearStart, rearEnd, i / (float)(rearRayCount - 1));
            Vector3 dir = Quaternion.Euler(0, angle, 0) * forward;

            if (CastVisionRay(dir, rearViewDistance, ref closestDist)) continue;

            if (showRay)
                Debug.DrawRay(visionOrigin.position, dir * rearViewDistance, Color.yellow);
        }
    }

    private bool CastVisionRay(Vector3 direction, float distance, ref float closestDist)
    {
        if (Physics.Raycast(visionOrigin.position, direction, out RaycastHit hit, distance, visionMask))
        {
            GameObject hitObj = hit.collider.gameObject;

            if (IsEnemy(hitObj))
            {
                float dist = Vector3.Distance(transform.position, hit.point);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    currentTarget = hitObj;
                }
            }

            if (showRay) Debug.DrawRay(visionOrigin.position, direction * hit.distance, Color.red);

            return true;
        }

        return false;
    }

    private void RotateTowardTarget()
    {
        if (currentTarget == null) return;

        navAgent.updateRotation = false;

        Vector3 dirToTarget = (currentTarget.transform.position - transform.position).normalized;
        dirToTarget.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(dirToTarget);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, navAgent.angularSpeed * 2 * Time.deltaTime);
    }

    private bool CanFireAtTarget()
    {
        if (isShooting || currentAmmo < 3 || currentTarget == null) return false;

        Vector3 dirToTarget = (currentTarget.transform.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToTarget);

        return angle < fireAngleThreshold;
    }

    protected IEnumerator FireBurst()
    {
        isShooting = true;
        currentAmmo -= 3;

        // Notify enemies
        if (gameObject.tag == "PlayerAgent") OnBulletBurstFired?.Invoke(transform.position);
        else if (gameObject.tag == "EnemyAgent") OnBulletBurstFired?.Invoke(currentTarget.transform.position);

        Quaternion bulletRotation = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(-90, 0, 0);

        for (int i = 0; i < 3; i++)
        {
            audioPlayer.PlayOneShot(gunshot);
            Instantiate(bulletPrefab, firePoint.position, bulletRotation);
            yield return new WaitForSeconds(burstInterval);
        }

        yield return new WaitForSeconds(burstCooldown);
        isShooting = false;
    }

    protected virtual bool IsEnemy(GameObject obj)
    {
        if (gameObject.CompareTag("PlayerAgent"))
        {
            return obj.CompareTag("EnemyAgent");
        }
        else if (gameObject.CompareTag("EnemyAgent"))
        {
            return obj.CompareTag("PlayerAgent");
        }

        return false;
    }
}