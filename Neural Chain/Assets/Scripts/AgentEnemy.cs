using UnityEngine;
using UnityEngine.AI;

public class EnemyAgent : Agent
{
    public float hearingRadius = 50f;
    private NavMeshAgent agent;
    private bool chasing = false;
    public bool allowedToChase = true;
    private Vector3 chasingLocation;

    protected override void Start()
    {
        base.Start();
        OnBulletBurstFired += OnBulletHeard;
        agent = gameObject.GetComponent<NavMeshAgent>();
    }

    protected override void Update()
    {
        base.Update();
        if (chasing && Vector3.Distance(transform.position, chasingLocation) <= 5f)
        {
            chasing = false;
            agent.SetDestination(transform.position);
        }
    }

    private void OnDestroy()
    {
        OnBulletBurstFired -= OnBulletHeard;
    }

    private void OnBulletHeard(Vector3 shotOrigin)
    {
        if (Vector3.Distance(transform.position, shotOrigin) <= hearingRadius && allowedToChase)
        {
            chasingLocation = shotOrigin;
            agent.SetDestination(shotOrigin);
            chasing = true;
        }
    }
}