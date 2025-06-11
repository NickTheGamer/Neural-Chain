using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(LineRenderer))]

public class AgentPreviewPath : MonoBehaviour
{
    private NavMeshAgent agent;
    private LineRenderer lineRenderer;
    private NavMeshPath previewPath;

    public Material ghostPathMaterial;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        lineRenderer = GetComponent<LineRenderer>();
        previewPath = new NavMeshPath();

        lineRenderer.positionCount = 0;
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;
        lineRenderer.material = ghostPathMaterial;
    }

    public void ShowPreviewPath(Vector3 destination)
    {
        if (NavMesh.CalculatePath(agent.transform.position, destination, NavMesh.AllAreas, previewPath))
        {
            lineRenderer.positionCount = previewPath.corners.Length;
            lineRenderer.SetPositions(previewPath.corners);
        }
    }

    public void ClearPreviewPath()
    {
        lineRenderer.positionCount = 0;
    }
}