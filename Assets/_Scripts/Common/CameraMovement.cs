using Unity.VisualScripting;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float moveSpeed = 5f; // Adjust this to control camera movement speed
    public float zoomSpeed = 5f; // Adjust this to control camera zoom speed
    public float minHeight = 5f; // Minimum camera height
    public float maxHeight = 50f; // Maximum camera height
    public float maxZoomStep = 2f; // Maximum zoom change per frame

    void Update()
    {
        MoveCam();
    }

    private void MoveCam()
    {
        // Get keyboard input for camera movement
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Calculate the new position based on input
        Vector3 newPosition = transform.position + new Vector3(horizontalInput, 0f, verticalInput) * moveSpeed * Time.deltaTime;

        // Clamp Y-axis movement to stay within the height range
        if (transform.position.y <= minHeight && newPosition.y < transform.position.y)
            newPosition.y = transform.position.y; // Stop downward movement at min height

        if (transform.position.y >= maxHeight && newPosition.y > transform.position.y)
            newPosition.y = transform.position.y; // Stop upward movement at max height

        // Update the camera's position while keeping the Y-axis constrained
        transform.position = new Vector3(newPosition.x, transform.position.y, newPosition.z);

        // Zoom in and out with the mouse scroll wheel
        float zoomInput = Input.GetAxis("Mouse ScrollWheel");
        bool isShiftPressed = Input.GetKey(KeyCode.LeftShift);
        int zoomToggle = isShiftPressed ? 6 : 1;

        // Calculate the zoom delta
        float zoomDelta = zoomInput * zoomSpeed * Time.deltaTime * zoomToggle;

        // Clamp the maximum zoom step
        zoomDelta = Mathf.Clamp(zoomDelta, -maxZoomStep, maxZoomStep);

        // Calculate the new camera position with zoom
        Vector3 zoomedPosition = transform.position + transform.forward * zoomDelta;

        // Clamp the zoomed position Y to stay within the height range
        zoomedPosition.y = Mathf.Clamp(zoomedPosition.y, minHeight, maxHeight);

        // Update the camera's position with zoom
        transform.position = zoomedPosition;
    }

}
