using UnityEngine;

public class District : MonoBehaviour
{
    [SerializeField] public int districtId;
    [SerializeField] public Vector3 cameraPosition;
    [SerializeField] public string districtName;
    [SerializeField] public string districtDescription;
    [SerializeField] public Vector3 purchaseCost;
    [SerializeField] public int populationPerSecond;

    private MeshRenderer thisMesh;
    public int upgradeLevel;


    private void Awake()
    {
        thisMesh = GetComponent<MeshRenderer>();
        thisMesh.enabled = false;
        districtName = "District " + districtId;
        districtDescription = "sad;lfkja;dlfkjasd;flkjasd";
        populationPerSecond = districtId * 5;
    }

    public void ShowTile(bool _result)
    {
        thisMesh.enabled = _result;
    }
}
