using TMPro;
using UnityEngine;

public class WarehouseMenu : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI attaniumAmountText;
    [SerializeField] private TextMeshProUGUI marcumAmountText;
    [SerializeField] private TextMeshProUGUI imearAmountText;

    // Update is called once per frame
    void Update()
    {
        if (!this.gameObject.activeSelf) {  return; }
        attaniumAmountText.text = SaveSystem.playerData.attaniumTotal.ToString();
        marcumAmountText.text = SaveSystem.playerData.marcumTotal.ToString();
        imearAmountText.text = SaveSystem.playerData.imearTotal.ToString();
    }
}
