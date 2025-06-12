using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using FSM;

public class GameManager : MonoBehaviour
{
    public Camera cam;
    public LayerMask groundMask;           // Only raycast against ground
    public GameObject playerAgent;
    public GameObject enemyAgent;

    public float navMeshSampleMaxDistance = 10f;

    public GameObject[] playerSpawnPoints;
    public GameObject[] enemySpawnPoints;

    private List<GameObject> playerAgents = new List<GameObject>();
    private List<GameObject> enemyAgents = new List<GameObject>();
    private GameObject currentPlayerAgent;

    private bool isQueueing = false;
    private const float selectionRadius = 0.5f;

    private List<(GameObject agent, Vector3 destination)> queuedCommands = new();
    private GameObject currentQueuedAgent = null;

    private StateMachine enemyOverlord;

    //info for each enemy
    private struct AgentState
    {
        public float health;
        public int ammo;
        public Vector3 position;
        public GameObject currentTarget;
        public bool canChase;
        public bool isAlive;
    }
    private AgentState[] agentStates;


    void Start()
    {
        //CreateFSM();
        SpawnAgents();
    }

    void Update()
    {
        isQueueing = Keyboard.current.shiftKey.isPressed;

        //Pause everything except camera
        Time.timeScale = isQueueing ? 0f : 1f;
        
        if (!isQueueing && queuedCommands.Count > 0)
        {
            ExecuteQueuedCommands();
        }

        CheckClick();
        //UpdateEnemies();
    }

    private void SpawnAgents()
    {
        for (int i = 0; i < playerSpawnPoints.Length; i++)
        {
            GameObject point = playerSpawnPoints[i];
            point.SetActive(false);
            Transform spawnPos = point.transform;
            GameObject agent = Instantiate(playerAgent, spawnPos.position, spawnPos.rotation);
            playerAgents.Add(agent);
        }

        for (int i = 0; i < enemySpawnPoints.Length; i++)
        {
            GameObject point = enemySpawnPoints[i];
            point.SetActive(false);
            Transform spawnPos = point.transform;
            GameObject agent = Instantiate(enemyAgent, spawnPos.position, spawnPos.rotation);
            enemyAgents.Add(agent);
        }
    }

    private void CheckClick()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                GameObject clickedObject = hit.collider.gameObject;

                if (isQueueing)
                {
                    // Direct selection
                    if (clickedObject.CompareTag("PlayerAgent"))
                    {
                        currentQueuedAgent = clickedObject;
                        return;
                    }

                    // Soft selection
                    GameObject softAgent = CheckLeeway(hit);
                    if (softAgent != null)
                    {
                        currentQueuedAgent = softAgent;
                        return;
                    }

                    // Queue command
                    // Fast bitshift for layer comparison
                    if (groundMask == (groundMask | (1 << hit.collider.gameObject.layer)) &&
                        currentQueuedAgent != null &&
                        NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, navMeshSampleMaxDistance, NavMesh.AllAreas))
                    {
                        queuedCommands.Add((currentQueuedAgent, navHit.position));

                        AgentPreviewPath preview = currentQueuedAgent.GetComponent<AgentPreviewPath>();
                        if (preview != null) preview.ShowPreviewPath(navHit.position);

                        currentQueuedAgent = null;
                    }

                    return;
                }
                else
                {
                    // Direct selection
                    if (clickedObject.CompareTag("PlayerAgent"))
                    {
                        currentPlayerAgent = clickedObject;
                        return;
                    }

                    // Soft selection
                    GameObject softAgent = CheckLeeway(hit);
                    if (softAgent != null)
                    {
                        currentPlayerAgent = softAgent;
                        return;
                    }

                    if (groundMask == (groundMask | (1 << hit.collider.gameObject.layer)) &&
                        currentPlayerAgent != null &&
                        NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, navMeshSampleMaxDistance, NavMesh.AllAreas))
                    {
                        currentPlayerAgent.GetComponent<NavMeshAgent>().SetDestination(navHit.position);
                    }
                }
            }
        }
    }

    private GameObject CheckLeeway(RaycastHit hit)
    {
        //Chooses closest agent if close together
        Collider[] nearby = Physics.OverlapSphere(hit.point, selectionRadius);

        GameObject closest = null;
        float minDist = float.MaxValue;

        foreach (Collider col in nearby)
        {
            if (col.CompareTag("PlayerAgent"))
            {
                float dist = Vector3.Distance(hit.point, col.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = col.gameObject;
                }
            }
        }

        return closest;
    }

    private void ExecuteQueuedCommands()
    {
        //Execute all stored commands while time was stopped
        foreach (var command in queuedCommands)
        {
            NavMeshAgent agent = command.agent.GetComponent<NavMeshAgent>();
            agent.SetDestination(command.destination);

            // Clear preview
            var preview = command.agent.GetComponent<AgentPreviewPath>();
            if (preview != null) preview.ClearPreviewPath();
        }

        queuedCommands.Clear();
        currentQueuedAgent = null;
    }

    // private void CreateFSM()
    // {
    //     enemyOverlord = new StateMachine(this, needsExitTime: false);
    // }

    // private void UpdateEnemies()
    // {
    //     for (int i = 0; i < enemySpawnPoints.Length; i++)
    //     {
    //         Agent currentEnemy = enemyAgents[i].GetComponent<Agent>();
    //         AgentState agentState = agentStates[i];
    //         if (agentState.isAlive)
    //         {
    //             agentState.health = currentEnemy.currentHealth;
    //             agentState.ammo = currentEnemy.currentAmmo;

    //         }
    //     }
    // }
}

// private struct AgentState
//     {
//         public float health;
//         public int ammo;
//         public Vector3 position;
//         public GameObject currentTarget;
//         public bool canChase;
//         public bool isAlive;
//     }

// // Add Searching state
// harvesting_fsm.AddState("Searching", new State(onLogic: (state) => SearchingForResource()));
// // Add Gathering/harvesting state
// harvesting_fsm.AddState("Gathering", new State(onLogic: (state) => GatheringResource()));
// // Add Dropping off state
// harvesting_fsm.AddState("DroppingOff", new State(onLogic: (state) => DroppingOffResource()));

// // ----- C. Define transitions between states -----

// // Transition from Searching state to Gathering state.
// // Transition happens when a crystal has been identified.
// harvesting_fsm.AddTransition(new Transition(
//     "Searching", // from state
//     "Gathering", // to state
//     (transition) => currentCrystal != null // condition that has to be met before transition happens
// ));