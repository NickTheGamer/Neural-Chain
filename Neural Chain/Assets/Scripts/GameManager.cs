using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public Camera cam;
    public LayerMask groundMask;           // Only raycast against ground
    public GameObject playerAgent;
    public GameObject enemyAgent;

    public float navMeshSampleMaxDistance = 10f;

    public Transform[] playerSpawnPoints;
    public Transform[] enemySpawnPoints;

    private List<GameObject> playerAgents = new List<GameObject>();
    private List<GameObject> enemyAgents = new List<GameObject>();
    private GameObject currentPlayerAgent;

    void Update()
    {
        CheckClick();
    }

    void Start()
    {
        SpawnAgents();
    }

    private void SpawnAgents()
    {
        for (int i = 0; i < playerSpawnPoints.Length; i++)
        {
            Transform spawnPos = playerSpawnPoints[i % playerSpawnPoints.Length];
            GameObject agent = Instantiate(playerAgent, spawnPos.position, spawnPos.rotation);
            playerAgents.Add(agent);
        }

        for (int i = 0; i < enemySpawnPoints.Length; i++)
        {
            Transform spawnPos = enemySpawnPoints[i % enemySpawnPoints.Length];
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

                if (clickedObject.CompareTag("PlayerAgent"))
                {
                    currentPlayerAgent = clickedObject;
                    return;
                }
            }

            if (Physics.Raycast(ray, out RaycastHit groundHit, 100f, groundMask))
            {
                if (NavMesh.SamplePosition(groundHit.point, out NavMeshHit navHit, navMeshSampleMaxDistance, NavMesh.AllAreas))
                {
                    if (currentPlayerAgent != null)
                    {
                        NavMeshAgent agent = currentPlayerAgent.GetComponent<NavMeshAgent>();
                        agent.SetDestination(navHit.position);
                    }
                }
            }
        }
    }
}
