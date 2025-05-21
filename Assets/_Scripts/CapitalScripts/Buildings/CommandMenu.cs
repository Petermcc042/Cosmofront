using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CommandMenu : MonoBehaviour
{
    [SerializeField] private PopulationManager populationManager;

    [Header("Timer Management")]
    [SerializeField] private TextMeshProUGUI committedPopulationText;
    [SerializeField] private Slider populationSlider;
    private float storedValue;

    private void Update()
    {
        if (!this.gameObject.activeSelf) { return; }

        populationSlider.maxValue = SaveSystem.playerData.populationTotal;
        committedPopulationText.text = populationSlider.value.ToString();
        storedValue = populationSlider.value;
    }

    // called from UI button
    public void OpenWorldView()
    {
        populationManager.PickTroops((int)storedValue);
        SceneManager.LoadScene("3_WorldView");
    }
}
