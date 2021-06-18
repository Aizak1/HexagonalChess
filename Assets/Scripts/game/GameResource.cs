using cell;
using figure;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game {
    public class GameResource : MonoBehaviour {
        public Dictionary<Cell, Figure> figuresToSetup = new Dictionary<Cell, Figure>();

        public Dictionary<Vector2Int, Vector3Int> converter2dTo3d
            = new Dictionary<Vector2Int, Vector3Int>();

        public Dictionary<Vector3Int, Vector2Int> converter3dTo2d
            = new Dictionary<Vector3Int, Vector2Int>();

        public Dictionary<Vector2Int, Vector3> converter2dToWorld
            = new Dictionary<Vector2Int, Vector3>();

        public Dictionary<FigureType, Figure> whiteModelsForTransformation
            = new Dictionary<FigureType, Figure>();

        public Dictionary<FigureType, Figure> blackModelsForTransformation
            = new Dictionary<FigureType, Figure>();

    }
}

