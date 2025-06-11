using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Agent : MonoBehaviour
{
    public int maxHealth;
    private int currentHealth;

    public float viewDistance = 20f;
    public float fieldOfView = 90f;
    public int rayCount = 15;
    public LayerMask visionMask;
    public Transform visionOrigin;

    public GameObject bulletPrefab;
    public Transform firePoint;
    public int maxAmmo = 12;
    private int currentAmmo;
    public float burstInterval = 0.1f;
    public float burstCooldown = 1f;
    public float fireAngleThreshold = 2f;

    public NavMeshAgent navAgent;

    private GameObject currentTarget;
    private bool isShooting = false;

    private bool showRay = true;

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        currentAmmo = maxAmmo;
    }

    protected virtual void Update()
    {
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

    protected virtual void Die()
    {
        Destroy(gameObject);
    }

    private void ScanForTargets()
    {
        currentTarget = null;
        float closestDist = float.MaxValue;

        float halfFOV = fieldOfView / 2f;
        Vector3 forward = transform.forward;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = Mathf.Lerp(-halfFOV, halfFOV, i / (float)(rayCount - 1));
            Vector3 dir = Quaternion.Euler(0, angle, 0) * forward;

            if (Physics.Raycast(visionOrigin.position, dir, out RaycastHit hit, viewDistance, visionMask))
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

                if (showRay) Debug.DrawRay(visionOrigin.position, dir * hit.distance, Color.red);
            }
            else
            {
                if (showRay) Debug.DrawRay(visionOrigin.position, dir * viewDistance, Color.green);
            }
        }
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

    private IEnumerator FireBurst()
    {
        isShooting = true;
        currentAmmo -= 3;

        Quaternion bulletRotation = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(-90, 0, 0);

        for (int i = 0; i < 3; i++)
        {
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