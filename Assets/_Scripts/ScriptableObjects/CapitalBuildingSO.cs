using UnityEngine;

[CreateAssetMenu()]
public class CapitalBuildingSO : ScriptableObject
{
    [SerializeField] public string buildingName;
    [SerializeField] public string buildingDescription;
    [SerializeField] public Vector3 purchaseCost; 
    [SerializeField] public Vector3 cameraPosition; //Vector3(47.8768387,64.6658401,-152.560944)
}
