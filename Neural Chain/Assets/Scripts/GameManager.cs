using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    //info for each enemy
    private struct AgentState
    {
        public float health;
        public int ammo;
        public Vector3 position;
        public GameObject currentTarget;
    }

    public Camera cam;
    public LayerMask groundMask;           // Only raycast against ground (and items)
    public GameObject playerAgent;
    public GameObject enemyAgent;

    public float navMeshSampleMaxDistance = 10f;

    public GameObject[] playerSpawnPoints;
    public GameObject[] enemySpawnPoints;
    public GameObject[] healthPacks;
    public GameObject[] ammoBoxes;
    public GameObject[] defensivePositions;

    private List<GameObject> playerAgents = new List<GameObject>();
    private List<(GameObject, AgentState)> enemyAgents = new();
    private GameObject currentPlayerAgent;

    private bool isQueueing = false;
    private const float selectionRadius = 0.5f;

    private List<(GameObject agent, Vector3 destination)> queuedCommands = new();
    private GameObject currentQueuedAgent = null;

    public TextMeshProUGUI resultText;


    void Start()
    {
        SpawnAgents();
        cam.transform.position = new Vector3(-35, 20, -45);
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
    }

    void FixedUpdate()
    {
        //Remove any dead agents from their lists
        playerAgents.RemoveAll(player => player == null);
        enemyAgents.RemoveAll(enemy => enemy.Item1 == null);

        //Win condition (Loss condition taken care of below in ManageEnemies())
        if (playerAgents.Count <= 0) resultText.text = "You Win!";
        UpdateEnemies();
        ManageEnemies();
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
            GameObject agentObject = Instantiate(enemyAgent, spawnPos.position, spawnPos.rotation);

            Agent agent = agentObject.GetComponent<Agent>();
            AgentState agentState = new();
            agentState.health = agent.currentHealth;
            agentState.ammo = agent.currentAmmo;
            agentState.position = agentObject.transform.position;
            agentState.currentTarget = agent.currentTarget;

            enemyAgents.Add((agentObject, agentState));
        }

        for (int i = 0; i < defensivePositions.Length; i++)
        {
            defensivePositions[i].SetActive(false);
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

    private void UpdateEnemies()
    {
        for (int i = 0; i < enemyAgents.Count; i++)
        {
            GameObject enemyObj = enemyAgents[i].Item1;
            AgentState agentState = enemyAgents[i].Item2;

            if (enemyObj)
            {
                Agent currentEnemy = enemyObj.GetComponent<Agent>();
                agentState.health = currentEnemy.currentHealth;
                agentState.ammo = currentEnemy.currentAmmo;
                agentState.position = enemyObj.transform.position;
                agentState.currentTarget = currentEnemy.currentTarget;
            }
        }
    }

    private void ManageEnemies()
    {
        if (playerAgents.Count <= 0)
        {
            //If all players dead, all enemies go back to idle
            for (int i = 0; i < enemyAgents.Count; i++)
            {
                resultText.text = "You Lose!";
                GameObject enemyObj = enemyAgents[i].Item1;
                EnemyAgent agent = enemyObj.GetComponent<EnemyAgent>();
                agent.canChase = true;
                agent.heardShots = false;
                agent.shouldGetItem = false;
                agent.shouldDefend = false;
            }
            return;
        }

        float reachableDist = 30f;
        int healthThreshold = 3;
        int ammoThreshold = 6;

        for (int i = 1; i < enemyAgents.Count; i++)
        {
            GameObject enemyObj = enemyAgents[i].Item1;
            AgentState agentState = enemyAgents[i].Item2;

            if (enemyObj)
            {

            }
        }
    }

    public (float distance, int index) NearestHealthPack(GameObject enemyObj)
    {
        float shortestDistance = float.MaxValue;
        int closestIndex = -1;

        for (int i = 0; i < healthPacks.Length; i++)
        {
            GameObject pack = healthPacks[i];
            if (!pack.activeInHierarchy) continue; // skip inactive packs

            float dist = Vector3.Distance(enemyObj.transform.position, pack.transform.position);
            if (dist < shortestDistance)
            {
                shortestDistance = dist;
                closestIndex = i;
            }
        }

        return (shortestDistance, closestIndex);
    }

    public (float distance, int index) NearestAmmoBox(GameObject enemyObj)
    {
        float shortestDistance = float.MaxValue;
        int closestIndex = -1;

        for (int i = 0; i < ammoBoxes.Length; i++)
        {
            GameObject box = ammoBoxes[i];
            if (!box.activeInHierarchy) continue; // skip inactive boxes

            float dist = Vector3.Distance(enemyObj.transform.position, box.transform.position);
            if (dist < shortestDistance)
            {
                shortestDistance = dist;
                closestIndex = i;
            }
        }

        return (shortestDistance, closestIndex);
    }
    
    public (float distance, int index) NearestDefensivePos(GameObject enemyObj)
    {
        float shortestDistance = float.MaxValue;
        int closestIndex = -1;

        for (int i = 0; i < defensivePositions.Length; i++)
        {
            GameObject box = defensivePositions[i];

            float dist = Vector3.Distance(enemyObj.transform.position, box.transform.position);
            if (dist < shortestDistance)
            {
                shortestDistance = dist;
                closestIndex = i;
            }
        }

        return (shortestDistance, closestIndex);
    }
    
}