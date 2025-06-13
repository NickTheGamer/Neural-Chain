using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed;
    public float scrollSpeed = 4f;
    private float yHeight = 20f;
    private float minHeight = 10f;
    private float maxHeight = 40f;

    [Header("Bounds")]
    public Vector2 xBounds = new Vector2(-75f, 75f);
    public Vector2 zBounds = new Vector2(-75f, 75f);

    private Vector2 movementInput;

    private void OnMove(InputValue value)
    {
        movementInput = value.Get<Vector2>();
    }

    private void OnZoom(InputValue value)
    {
        Vector2 scroll = value.Get<Vector2>();
        float scrollDelta = scroll.y;

        yHeight -= scrollDelta * scrollSpeed;
        yHeight = Mathf.Clamp(yHeight, minHeight, maxHeight);
        moveSpeed = yHeight / 2;
    }

    
    void Start()
    {
        // Lock the camera's rotation to look straight down
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    void Update()
    {
        Vector3 move = new Vector3(movementInput.x, 0f, movementInput.y) * moveSpeed * Time.unscaledDeltaTime;

        Vector3 targetPos = transform.position + move;
        targetPos.y = yHeight;
        targetPos.x = Mathf.Clamp(targetPos.x, xBounds.x, xBounds.y);
        targetPos.z = Mathf.Clamp(targetPos.z, zBounds.x, zBounds.y);

        transform.position = targetPos;
    }

    void OnDrawGizmosSelected()
    {
        //Foor debugging
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(
            new Vector3((xBounds.x + xBounds.y) / 2, yHeight, (zBounds.x + zBounds.y) / 2),
            new Vector3(xBounds.y - xBounds.x, 0.1f, zBounds.y - zBounds.x));
    }
}