using UnityEngine;

public class CometTail : MonoBehaviour
{
    private TrailRenderer trailRenderer;

    void Start()
    {
        // Add a Trail Renderer component to the GameObject
        trailRenderer = gameObject.AddComponent<TrailRenderer>();

        // Configure the trail renderer properties
        trailRenderer.time = 0.3f; // Duration the trail stays visible (in seconds)
        trailRenderer.startWidth = 1.0f; // Width of the trail at the start
        trailRenderer.endWidth = 0.1f; // Width of the trail at the end
        trailRenderer.material = new Material(Shader.Find("Sprites/Default")); // Use a default material
        trailRenderer.startColor = new Color(1f, 0.5f, 0f, 1f); // Orange start color
        trailRenderer.endColor = new Color(1f, 0f, 0f, 0.5f); // Fades to red and transparent

        // Optional: Set trail texture or gradient for a more dynamic look
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.yellow, 0.0f),
                new GradientColorKey(Color.red, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        trailRenderer.colorGradient = gradient;
    }
}