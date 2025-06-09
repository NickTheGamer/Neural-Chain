using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public Camera cam;
    public LayerMask groundMask;           // Only raycast against ground

    public float navMeshSampleMaxDistance = 10f;

    void Update()
    {
        CheckClick();
    }

    private void CheckClick()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundMask))
            {
                // Optional: Debug click
                Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f);

                // Find the nearest point on the NavMesh to where we clicked
                if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, navMeshSampleMaxDistance, NavMesh.AllAreas))
                {
                    Debug.Log(navHit.position);
                }
            }
        }
    }
}
