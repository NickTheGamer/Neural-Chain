using UnityEngine;
using UnityEngine.AI;

public class EnemyAgent : Agent
{
    public float hearingRadius = 50f;
    private NavMeshAgent agent;

    protected override void Start()
    {
        base.Start();
        OnBulletBurstFired += OnBulletHeard;
        agent = gameObject.GetComponent<NavMeshAgent>();
    }

    private void OnDestroy()
    {
        OnBulletBurstFired -= OnBulletHeard;
    }

    private void OnBulletHeard(Vector3 shotOrigin)
    {
        if (Vector3.Distance(transform.position, shotOrigin) <= hearingRadius)
        {
            agent.SetDestination(shotOrigin);
        }
    }
}