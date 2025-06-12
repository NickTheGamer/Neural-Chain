using FSM;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAgent : Agent
{
    public float hearingRadius = 50f;
    private NavMeshAgent agent;
    private bool heardShots = false;
    private Vector3 chasingLocation;
    private StateMachine enemyAI;

    // These flags control state transitions and are toggled by GameManager.
    public bool canChase;
    public bool shouldGetItem;
    public bool shouldDefend;
    public bool isAtTarget;
    public Vector3 itemTarget;
    private Vector3 startPos;
    public Vector3 defensivePosition;


    protected override void Start()
    {
        CreateFSM();
        base.Start();
        startPos = transform.position;
        OnBulletBurstFired += OnBulletHeard;
        agent = gameObject.GetComponent<NavMeshAgent>();
    }

    protected override void Update()
    {
        base.Update();
        /*if (chasing && Vector3.Distance(transform.position, chasingLocation) <= 5f)
        {
            chasing = false;
            agent.SetDestination(transform.position);
        }*/
    }

    private void CreateFSM()
    {
        enemyAI = new StateMachine(this, needsExitTime: false);

        enemyAI.AddState("Idle", new State(
            onEnter: (state) => Idle()
        ));

        enemyAI.AddState("Chasing", new State(
            onEnter: (state) => Chasing()
        ));

        enemyAI.AddState("GettingItem", new State(
            onEnter: (state) => GettingItem()
        ));

        enemyAI.AddState("DefensivePosition", new State(
            onEnter: (state) => Defensive()
        ));

        enemyAI.SetStartState("Idle");

        // === Idle Transitions ===
        enemyAI.AddTransition(new Transition("Idle", "Chasing", (transition) => canChase && heardShots));
        enemyAI.AddTransition(new Transition("Idle", "GettingItem", (transition) => shouldGetItem));
        enemyAI.AddTransition(new Transition("Idle", "TakingDefensivePosition", (transition) => shouldDefend));

        // === Chasing Transitions ===
        enemyAI.AddTransition(new Transition("Chasing", "Idle", (transition) => !canChase && isAtTarget));
        enemyAI.AddTransition(new Transition("Chasing", "GettingItem", (transition) => shouldGetItem));
        enemyAI.AddTransition(new Transition("Chasing", "TakingDefensivePosition", (transition) => shouldDefend));

        // === GettingItem Transitions ===
        enemyAI.AddTransition(new Transition("GettingItem", "Idle", (transition) => isAtTarget));
        enemyAI.AddTransition(new Transition("GettingItem", "Chasing", (transition) => canChase));
        enemyAI.AddTransition(new Transition("GettingItem", "TakingDefensivePosition", (transition) => shouldDefend));

        // === Defensive Transitions ===
        enemyAI.AddTransition(new Transition("TakingDefensivePosition", "Idle", (transition) => isAtTarget));
        enemyAI.AddTransition(new Transition("TakingDefensivePosition", "Chasing", (transition) => canChase));
        enemyAI.AddTransition(new Transition("TakingDefensivePosition", "GettingItem", (transition) => shouldGetItem));
    }

    private void Idle()
    {
        agent.SetDestination(startPos);
        isAtTarget = true;
    }

    private void Chasing()
    {
        if (currentTarget == null)
        {
            isAtTarget = true;
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(chasingLocation);

        float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
        isAtTarget = dist < 5f;
    }

    private void GettingItem()
    {
        agent.isStopped = false;
        agent.SetDestination(itemTarget);

        float dist = Vector3.Distance(transform.position, itemTarget);
        isAtTarget = dist < 0.5f;
    }

    private void Defensive()
    {
        agent.isStopped = false;
        agent.SetDestination(defensivePosition);

        float dist = Vector3.Distance(transform.position, defensivePosition);
        isAtTarget = dist < 1f;
    }

    private void OnDestroy()
    {
        OnBulletBurstFired -= OnBulletHeard;
    }

    private void OnBulletHeard(Vector3 shotOrigin)
    {
        if (Vector3.Distance(transform.position, shotOrigin) <= hearingRadius)
        {
            chasingLocation = shotOrigin;
            heardShots = true;
        }
    }
}