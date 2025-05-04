using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CommandMenu : MonoBehaviour
{
    [Header("Timer Management")]
    [SerializeField] private TextMeshProUGUI committedPopulationText;
    [SerializeField] private Slider populationSlider;

    private void Update()
    {
        if (!this.gameObject.activeSelf) { return; }

        populationSlider.maxValue = SaveSystem.playerData.populationTotal;
        committedPopulationText.text = populationSlider.value.ToString();
    }
}
