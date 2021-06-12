using UnityEngine;
using vjp;

namespace mover {
    public class Mover : MonoBehaviour {

        [SerializeField]
        private GameManager manager;

        private Option<Figure> currentFigure;
        private const float FIGURES_Y_POSITION = 0.3f;
        private RaycastHit hit;


        private void Update() {

            if (!Input.GetMouseButtonDown(0)) {
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out hit)) {
                return;
            }

            if (currentFigure.IsNone()) {
                var picked = hit.transform.gameObject.GetComponent<Figure>();
                if (picked == null) {
                    currentFigure = Option<Figure>.None();
                    return;
                }
                currentFigure = Option<Figure>.Some(picked);
                currentFigure.Peel().transform.position += Vector3.up;

            } else {
                var cellComponent = hit.collider.gameObject.GetComponent<Cell>();

                if(cellComponent == null) {
                    currentFigure.Peel().transform.position -= Vector3.up;
                    currentFigure = Option<Figure>.None();
                    return;
                }

                var gameFinalX = cellComponent.gameCoordinates.x;
                var gameFinalZ = cellComponent.gameCoordinates.y;

                var worldFinalX = cellComponent.transform.position.x;
                var worldFinalZ = cellComponent.transform.position.z;

                if (!manager.IsCorrectMove(currentFigure.Peel(), gameFinalX, gameFinalZ)) {
                    currentFigure.Peel().transform.position -= Vector3.up;
                    currentFigure = Option<Figure>.None();
                    return;
                }

                var finalPosition = new Vector3(worldFinalX, FIGURES_Y_POSITION, worldFinalZ);
                currentFigure.Peel().transform.position = finalPosition;
                currentFigure = Option<Figure>.None();
            }

        }
    }
}
