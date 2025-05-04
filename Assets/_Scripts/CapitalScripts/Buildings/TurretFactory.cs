using UnityEngine;

public class TurretFactory : MonoBehaviour
{
    [SerializeField] UpgradeManager upgradeManager;

    private void OnEnable()
    {
        upgradeManager.RunLines();
    }
}
