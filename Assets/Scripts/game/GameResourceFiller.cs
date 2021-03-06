using cell;
using figure;
using net;
using UnityEngine;

namespace game {
    public class GameResourceFiller : MonoBehaviour {
        [SerializeField]
        private Cell[] cells;
        [SerializeField]
        private Figure[] figures;

        [SerializeField]
        private Figure[] whiteModelsForTransformation;

        [SerializeField]
        private Figure[] blackModelsForTransformation;

        [SerializeField]
        private Server serverPrefab;

        [SerializeField]
        private Client clientPrefab;

        [SerializeField]
        private GameResource resource;



        private void Awake() {

            resource.serverPrefab = serverPrefab;
            resource.clientPrefab = clientPrefab;

            for (int i = 0; i < cells.Length; i++) {
                resource.figuresToSetup.Add(cells[i], figures[i]);
            }

            for (int i = 0; i < whiteModelsForTransformation.Length; i++) {
                var model = whiteModelsForTransformation[i];
                resource.whiteModelsForTransformation.Add(model.type, model);
            }

            for (int i = 0; i < blackModelsForTransformation.Length; i++) {
                var model = blackModelsForTransformation[i];
                resource.blackModelsForTransformation.Add(model.type, model);
            }

            var allCells = FindObjectsOfType<Cell>();
            for (int i = 0; i < allCells.Length; i++) {

                var coordinatesIn2d = allCells[i].gameCoordinates;
                var coordinateIn3d = allCells[i].coordinatesIn3D;
                var worldCoordinates = allCells[i].transform.position;

                resource.converter2dTo3d.Add(coordinatesIn2d, coordinateIn3d);
                resource.converter3dTo2d.Add(coordinateIn3d, coordinatesIn2d);
                resource.converter2dToWorld.Add(coordinatesIn2d, worldCoordinates);
            }
        }

    }
}

