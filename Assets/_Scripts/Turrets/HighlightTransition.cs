using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlightTransition : MonoBehaviour
{
    [SerializeField] private Material material;
    // Set the target transparency value
    public float targetAlpha = 0.5f;
    public float toggleSpeed = 2.0f; // How fast to toggle between transparent and target alpha

    private Color originalColor;
    private bool isFadingIn = true; // Whether we are increasing the transparency (fading in)

    void Start()
    {
        // Store the original color of the material
        originalColor = material.color;

        // Set initial transparency to fully transparent
        SetAlpha(0f);
    }

    void Update()
    {
        // Get the current alpha value of the material
        float currentAlpha = material.color.a;

        // Determine whether to fade in or out based on the current alpha value
        if (isFadingIn)
        {
            // Fade in (increase alpha)
            currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * toggleSpeed);

            // If the alpha has reached the target, start fading out
            if (Mathf.Abs(currentAlpha - targetAlpha) < 0.01f)
                isFadingIn = false;
        }
        else
        {
            // Fade out (decrease alpha)
            currentAlpha = Mathf.Lerp(currentAlpha, 0f, Time.deltaTime * toggleSpeed);

            // If the alpha has reached zero, start fading in
            if (Mathf.Abs(currentAlpha - 0f) < 0.01f)
                isFadingIn = true;
        }

        // Set the new alpha value on the material
        SetAlpha(currentAlpha);
    }

    // Helper method to set the alpha value of the material
    private void SetAlpha(float alpha)
    {
        Color color = material.color;
        color.a = alpha;
        material.color = color;
    }
}
