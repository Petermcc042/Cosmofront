using UnityEngine;

public class OperationManager : MonoBehaviour
{
    [SerializeField] private BuildingManager buildingManager;
    [SerializeField] private DistrictManager districtManager;
    [SerializeField] private CameraMovement cameraMovement;
    [SerializeField] private PopulationManager populationManager;

    private bool showingDistrict;

    private void Update()
    {
        cameraMovement.MoveCam();
        populationManager.UpdatePopulation();

        showingDistrict = districtManager.UpdateDistrictTiles();

        if (showingDistrict) 
        {
            buildingManager.CloseMenus();

            if (Input.GetMouseButtonDown(0))// && !IsOverUI())
            {
                districtManager.CheckDistrictClick();
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))// && !IsOverUI())
            {
                buildingManager.CheckBuildingClick();
            }

            
        }

        if (Input.GetKeyDown(KeyCode.Escape)) 
        { 
            buildingManager.CloseMenus(); 
            districtManager.CloseMenus();
        }
    }

}
