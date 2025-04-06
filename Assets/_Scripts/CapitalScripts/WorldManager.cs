using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class WorldManager : MonoBehaviour
{
    [Header("World UI")]
    [SerializeField] LayerMask regionLayerMask;

    [Header("Region Data")]
    [SerializeField] private GameObject menuUI;
    [SerializeField] private TextMeshProUGUI regionNameUI;
    private string levelToLoad;
    private string regionName;
    private int mapSize;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))// && !IsOverUI())
        {
            CheckBuildingClick();
        }
    }

    // ui method triggered
    public void LoadLevel()
    {
        Debug.Log(mapSize);
        if (mapSize != 0 )
        {
            PrecomputedData.Clear();
            PrecomputedData.Init(mapSize);
            PrecomputedData.InitGrid();
        }

        SceneManager.LoadScene(levelToLoad);
    }

    private void CheckBuildingClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, regionLayerMask))
        {
            WorldRegion region = raycastHit.collider.GetComponent<WorldRegion>();
            if (region != null)
            {
                levelToLoad = region.levelLoad;
                regionName = region.regionName;
                mapSize = region.gridLength;
                OpenMenu();
            }
        }
        else {
            if (IsOverUI()) { return; }
            
            CloseMenu();
        }

        
    }

    private void CloseMenu()
    {
        menuUI.SetActive(false);
    }

    private void OpenMenu()
    {
        menuUI.SetActive(true);
        regionNameUI.text = regionName;
    }

    private static bool IsOverUI()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return true;
        else
            return false;
    }
}