using FSM;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAgent : Agent
{
    public float hearingRadius = 50f;
    private NavMeshAgent agent;
    public bool heardShots = false;
    private Vector3 chasingLocation;
    private StateMachine enemyAI;

    // These flags control state transitions and are toggled by GameManager.
    public bool canChase;
    public bool shouldGetItem;
    public bool shouldDefend;
    public Vector3 itemTarget;
    private Transform startPos;
    public Transform defensivePosition;

    //Avoid state jittering
    private float stateEnterTime;
    private float minStateDuration = 2f;
    private bool HasMinDurationElapsed() => Time.time - stateEnterTime >= minStateDuration;

    private string currentState;


    protected override void Start()
    {
        base.Start();

        startPos = transform;
        OnBulletBurstFired += OnBulletHeard;
        agent = gameObject.GetComponent<NavMeshAgent>();

        CreateFSM();
        canChase = true;
        shouldGetItem = false;
        shouldDefend = false;
    }

    protected override void Update()
    {
        base.Update();
        enemyAI.OnLogic();
    }

    private void CreateFSM()
    {
        enemyAI = new StateMachine(this, needsExitTime: false);

        enemyAI.AddState("Idle", new State(
            onEnter: (state) => { stateEnterTime = Time.time; Idle(); Debug.Log(currentState); },
            onLogic: (state) => RotateIdle()
        ));

        enemyAI.AddState("Chasing", new State(
            onEnter: (state) => { stateEnterTime = Time.time; Chasing(); Debug.Log(currentState); },
            onLogic: (state) => StopIfInRange()
        ));

        enemyAI.AddState("GettingItem", new State(
            onEnter: (state) => { stateEnterTime = Time.time; GettingItem(); Debug.Log(currentState); }
        ));

        enemyAI.AddState("DefensivePosition", new State(
            onEnter: (state) => { stateEnterTime = Time.time; Defensive(); Debug.Log(currentState); },
            onLogic: (state) => RotateDefensive()
        ));

        // === Idle Transitions ===
        enemyAI.AddTransition(new Transition("Idle", "Chasing", (transition) => HasMinDurationElapsed() && canChase && heardShots && !shouldGetItem && !shouldDefend));
        enemyAI.AddTransition(new Transition("Idle", "GettingItem", (transition) => HasMinDurationElapsed() && shouldGetItem && !canChase && !shouldDefend));
        enemyAI.AddTransition(new Transition("Idle", "TakingDefensivePosition", (transition) => HasMinDurationElapsed() && shouldDefend && !shouldGetItem && !canChase));

        // === Chasing Transitions ===
        enemyAI.AddTransition(new Transition("Chasing", "Idle", (transition) => HasMinDurationElapsed() && !shouldGetItem && !shouldDefend));
        enemyAI.AddTransition(new Transition("Chasing", "GettingItem", (transition) => HasMinDurationElapsed() && shouldGetItem && !canChase && !shouldDefend));
        enemyAI.AddTransition(new Transition("Chasing", "TakingDefensivePosition", (transition) => HasMinDurationElapsed() && shouldDefend && !canChase && !shouldGetItem));

        // === GettingItem Transitions ===
        enemyAI.AddTransition(new Transition("GettingItem", "Idle", (transition) => HasMinDurationElapsed() && !shouldGetItem && !shouldDefend));
        enemyAI.AddTransition(new Transition("GettingItem", "Chasing", (transition) => HasMinDurationElapsed() && canChase && !shouldGetItem && !shouldDefend));
        enemyAI.AddTransition(new Transition("GettingItem", "TakingDefensivePosition", (transition) => HasMinDurationElapsed() && shouldDefend && !shouldGetItem && !canChase));

        // === Defensive Transitions ===
        enemyAI.AddTransition(new Transition("TakingDefensivePosition", "Idle", (transition) => HasMinDurationElapsed() && !shouldGetItem && !shouldDefend));
        enemyAI.AddTransition(new Transition("TakingDefensivePosition", "Chasing", (transition) => HasMinDurationElapsed() && canChase && !shouldGetItem && !shouldDefend));
        enemyAI.AddTransition(new Transition("TakingDefensivePosition", "GettingItem", (transition) => HasMinDurationElapsed() && shouldGetItem && !shouldDefend && !canChase));

        // Set start state
        enemyAI.SetStartState("Idle");
        // Trigger
        enemyAI.OnEnter();
    }

    private void Idle()
    {
        currentState = "Idle";
        agent.isStopped = false;
        agent.updateRotation = true;
        agent.SetDestination(startPos.position);
    }

    private void RotateIdle()
    {
        currentState = "RotateIdle";

        if (Vector3.Distance(transform.position, startPos.position) <= 2f)
        {
            agent.updateRotation = false;

            Vector3 flatForward = startPos.forward;
            flatForward.y = 0;
            Quaternion lookRotation = Quaternion.LookRotation(flatForward);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                lookRotation,
                agent.angularSpeed * 2 * Time.deltaTime
            );
        }
    }

    private void Chasing()
    {
        currentState = "Chasing";
        agent.isStopped = false;
        agent.updateRotation = true;
        heardShots = false;
        agent.SetDestination(chasingLocation);
    }

    private void StopIfInRange()
    {
        currentState = "StopIfInRange";

        //Chase new target
        if (heardShots)
        {
            agent.SetDestination(chasingLocation);
            heardShots = false;
        }
        
        //Stop if in range
        if (Vector3.Distance(transform.position, chasingLocation) <= 5f)
        {
            agent.isStopped = true;
        }
    }

    private void GettingItem()
    {
        currentState = "GettingItem";
        
        agent.isStopped = false;
        agent.updateRotation = true;
        agent.SetDestination(itemTarget);
    }

    private void Defensive()
    {
        currentState = "Defensive";

        agent.isStopped = false;
        agent.updateRotation = true;
        agent.SetDestination(defensivePosition.position);
    }

    private void RotateDefensive()
    {
        currentState = "RotateDefensive";

        if (Vector3.Distance(transform.position, defensivePosition.position) <= 2f)
        {
            agent.updateRotation = false;

            Vector3 flatForward = defensivePosition.forward;
            flatForward.y = 0;
            Quaternion lookRotation = Quaternion.LookRotation(flatForward);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                lookRotation,
                agent.angularSpeed * 2 * Time.deltaTime
            );
        }
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