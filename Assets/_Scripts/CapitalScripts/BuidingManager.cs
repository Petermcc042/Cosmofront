using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuidingManager : MonoBehaviour
{
    [Header("World UI")]
    [SerializeField] LayerMask buildingLayerMask;
    [SerializeField] List<CapitalBuilding> capitalBuildings;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))// && !IsOverUI())
        {
            CheckBuildingClick();
        }
    }

    private void CheckBuildingClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, buildingLayerMask))
        {
            CapitalBuilding building = raycastHit.collider.GetComponent<CapitalBuilding>();
            if (building != null)
            {
                building.OpenMenu();
            }
        }
        else
        {
            if (IsOverUI()) { return; }

            for (int i = 0; i < capitalBuildings.Count; i++)
            {
                capitalBuildings[i].CloseMenu();
            }
        }
    }

    private static bool IsOverUI()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return true;
        else
            return false;
    }
}
