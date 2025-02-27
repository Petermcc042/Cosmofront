using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class PlacedObjectSO : ScriptableObject
{
    public static Dir GetNextDir(Dir dir)
    {
        switch (dir)
        {
            default:
            case Dir.Down:
                return Dir.Left;
            case Dir.Left:
                return Dir.Up;
            case Dir.Up:
                return Dir.Right;
            case Dir.Right:
                return Dir.Down;
        }
    }

    public enum Dir
    {
        Down,
        Left,
        Up,
        Right,
    }

    public string nameString;
    public string parentNameString;
    public Transform startPrefab;
    public Transform upgradePrefab;
    public Transform finalPrefab;
    public int width;
    public int length;
    public bool isWalkable;
    public bool isBuildable;
    public Vector3 buildingCost = new(0,0,0);
    public int buidingHealth;


    public int GetRotationAngle(Dir dir)
    {
        switch (dir)
        {
            default:
            case Dir.Down:
                return 0;
            case Dir.Left:
                return 90;
            case Dir.Up:
                return 180;
            case Dir.Right:
                return 270;
        }
    }

    public Vector2Int GetRotationOffset(Dir dir)
    {
        switch (dir)
        {
            default:
            case Dir.Down:
                return new Vector2Int(0, 0);
            case Dir.Left:
                return new Vector2Int(0, width);
            case Dir.Up:
                return new Vector2Int(width, length);
            case Dir.Right:
                return new Vector2Int(length, 0);
        }
    }

    public List<Vector2Int> GetGridPositionList(Vector2Int offset, Dir dir)
    {
        List<Vector2Int> gridPositionList = new List<Vector2Int>();

        switch (dir)
        {
            default:
            case Dir.Down:
            case Dir.Up:
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < length; y++)
                    {
                        gridPositionList.Add(offset + new Vector2Int(x, y));
                    }
                }
                break;
            case Dir.Left:
            case Dir.Right:
                for (int x = 0; x < length; x++)
                {
                    for (int y = 0; y < width; y++)
                    {
                        gridPositionList.Add(offset + new Vector2Int(x, y));
                    }
                }
                break;
        }
        return gridPositionList;
    }

    [Header("Menu UI Variables")]
    public string visibleName;

}
