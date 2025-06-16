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
    private Vector3 startPos;
    private Quaternion startRot;
    public Transform defensivePosition;
    private bool debugState = false;
    private string currentState;


    protected override void Start()
    {
        base.Start();

        startPos = transform.position;
        startRot = transform.rotation;
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
            onEnter: (state) => { Idle(); if (debugState) Debug.Log(currentState); },
            onLogic: (state) => RotateIdle()
        ));

        enemyAI.AddState("Chasing", new State(
            onEnter: (state) => { Chasing(); if (debugState) Debug.Log(currentState); },
            onLogic: (state) => StopIfInRange()
        ));

        enemyAI.AddState("GettingItem", new State(
            onEnter: (state) => { GettingItem(); if (debugState) Debug.Log(currentState); }
        ));

        enemyAI.AddState("DefensivePosition", new State(
            onEnter: (state) => { Defensive(); if (debugState) Debug.Log(currentState); },
            onLogic: (state) => RotateDefensive()
        ));

        // === Idle Transitions ===
        enemyAI.AddTransition(new Transition("Idle", "Chasing", (transition) => canChase && heardShots && !shouldGetItem && !shouldDefend));
        enemyAI.AddTransition(new Transition("Idle", "GettingItem", (transition) => shouldGetItem && !canChase && !shouldDefend));
        enemyAI.AddTransition(new Transition("Idle", "DefensivePosition", (transition) => shouldDefend && !shouldGetItem && !canChase));

        // === Chasing Transitions ===
        enemyAI.AddTransition(new Transition("Chasing", "Idle", (transition) => !shouldGetItem && !shouldDefend && !heardShots));
        enemyAI.AddTransition(new Transition("Chasing", "GettingItem", (transition) => shouldGetItem && !canChase && !shouldDefend));
        enemyAI.AddTransition(new Transition("Chasing", "DefensivePosition", (transition) => shouldDefend && !canChase && !shouldGetItem));

        // === GettingItem Transitions ===
        enemyAI.AddTransition(new Transition("GettingItem", "Idle", (transition) => canChase && !shouldGetItem && !shouldDefend));
        enemyAI.AddTransition(new Transition("GettingItem", "DefensivePosition", (transition) => shouldDefend && !shouldGetItem && !canChase));

        // === Defensive Transitions ===
        enemyAI.AddTransition(new Transition("DefensivePosition", "Idle", (transition) => canChase && !shouldGetItem && !shouldDefend));

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
        agent.SetDestination(startPos);
    }

    private void RotateIdle()
    {
        currentState = "RotateIdle";

        if (Vector3.Distance(transform.position, startPos) <= 2f)
        {
            agent.updateRotation = false;

            Vector3 flatForward = startRot * Vector3.forward;
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
        agent.SetDestination(chasingLocation);
    }

    private void StopIfInRange()
    {
        currentState = "StopIfInRange";

        //Stop if in range
        if (Vector3.Distance(transform.position, chasingLocation) <= 3f)
        {
            agent.isStopped = true;
            heardShots = false;
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
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HealthPack"))
        {
            currentHealth = maxHealth;
            other.gameObject.SetActive(false);
            shouldGetItem = false;
            canChase = true;
            shouldDefend = false;
        }

        else if (other.CompareTag("AmmoBox"))
        {
            currentAmmo = maxAmmo;
            other.gameObject.SetActive(false);
            shouldGetItem = false;
            canChase = true;
            shouldDefend = false;
        }
    }
}