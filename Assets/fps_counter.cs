using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class FPS_counter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fpsGUI;

    // Start is called before the first frame update
    void Start()
    {
        fpsGUI.text = "0";
        StartCoroutine(UpdateFPSText());
    }

    IEnumerator UpdateFPSText()
    {
        while (true)
        {
            // Calculate frames per second
            float fps = Mathf.Round(1f / Time.deltaTime);

            // Update the text
            fpsGUI.text = fps.ToSafeString();

            // Wait for 0.2 seconds
            yield return new WaitForSeconds(0.2f);
        }
    }
}
