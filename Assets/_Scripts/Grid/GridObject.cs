using UnityEngine;

public class GridObject
{
    public int x;
    public int z;
    private PlacedObject placedObject;
    public bool isWalkable;
    public bool isBaseArea;
    public bool isPathfindingArea;
    public bool isBuilding;
    public bool isTraversable;

    public GridObject(int x, int z)
    {
        this.x = x;
        this.z = z;
        isWalkable = true;
        isBaseArea = false;
        isBuilding = false;
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

    public override string ToString()
    {
        return x + ":" + z + "\n" + placedObject;
    }
}
