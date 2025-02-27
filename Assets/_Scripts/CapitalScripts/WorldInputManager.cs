using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class WorldInputManager : MonoBehaviour
{
    [Header("World UI")]
    [SerializeField] LayerMask regionLayerMask;

    [Header("Region Data")]
    [SerializeField] private GameObject menuUI;
    [SerializeField] private TextMeshProUGUI regionNameUI;
    private string levelToLoad;
    private string regionName;

    // Update is called once per frame
    void Update()
    {
        CheckBuildingClick();

        if (Input.GetMouseButtonDown(0))// && !IsOverUI())
        {
            LoadLevel();
        }
    }

    private void LoadLevel()
    {
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
                OpenMenu();
            }
        }
        else
        {
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