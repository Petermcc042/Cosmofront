using UnityEngine;

public class RotateMap : MonoBehaviour
{
    public float rotationSpeed = 5f;

    private Vector3 lastMousePosition;

    void Update()
    {
        // Mouse Drag Rotation
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;

            // Rotate around the Y-axis for horizontal movement, and X-axis for vertical movement
            transform.Rotate(Vector3.up, delta.x * rotationSpeed * Time.deltaTime, Space.World);
            transform.Rotate(Vector3.right, -delta.y * rotationSpeed * Time.deltaTime, Space.Self);
        }

        // WASD Rotation
        float horizontal = Input.GetAxis("Horizontal"); // A, D or Left, Right arrow keys
        float vertical = Input.GetAxis("Vertical"); // W, S or Up, Down arrow keys

        transform.Rotate(Vector3.up, -horizontal * rotationSpeed * Time.deltaTime, Space.World);
        transform.Rotate(Vector3.right, vertical * rotationSpeed * Time.deltaTime, Space.Self);
    }
}