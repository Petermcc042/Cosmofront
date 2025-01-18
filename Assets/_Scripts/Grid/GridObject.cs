using UnityEngine;

public class GridObject
{
    public GridXZ<GridObject> grid;
    public int x;
    public int z;
    private PlacedObject placedObject;

    public int gCost;
    public int fCost;
    public int dCost;
    public int hCost;

    public GridObject cameFromNode;
    public bool isWalkable;
    public bool isBaseArea;

    public GridObject(GridXZ<GridObject> grid, int x, int z)
    {
        this.grid = grid;
        this.x = x;
        this.z = z;
        isWalkable = true;
        isBaseArea = false;
    }

    public void CalculateFCost() {
        fCost = gCost + hCost + dCost;
    }

    public void SetPlacedObject(PlacedObject _placedObject)
    {
        this.placedObject = _placedObject;
    }

    public PlacedObject GetPlacedObject()
    {
        return placedObject;
    }

    public void ClearPlacedObject()
    {
        placedObject = null;
    }

    public bool CanBuildBase()
    {
        // Return true if there is no placed object or if the placed object is buildable and the area is designated for base
        return (placedObject == null || placedObject.placedObjectSO.isBuildable) && isBaseArea;
    }

    public bool CanBuild()
    {
        return placedObject == null || placedObject.placedObjectSO.isBuildable;
    }


    public bool IsWalkable()
    {
        // Return true if placedObject is null or if it is walkable
        return placedObject == null || placedObject.placedObjectSO.isWalkable;
    }

    public override string ToString()
    {
        return x + ":" + z + "\n" + placedObject;
    }
}
