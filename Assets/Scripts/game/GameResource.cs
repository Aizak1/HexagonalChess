using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameResource : MonoBehaviour
{
    public Dictionary<Cell, Figure> figuresToSetup = new Dictionary<Cell, Figure>();
    public Dictionary<Vector2Int, Vector3Int> coordinatesMatcher
        = new Dictionary<Vector2Int, Vector3Int>();
}
