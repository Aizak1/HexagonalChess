using UnityEngine;
using vjp;

namespace mover {
    public class Mover : MonoBehaviour {

        [SerializeField]
        private GameManager manager;

        private const float FIGURES_Y_POSITION = 0.3f;
        private RaycastHit hit;

        public Option<Figure> currentFigure;

        private void Update() {

            if (!Input.GetMouseButtonDown(0)) {
                return;
            }

            manager.ChangeCollidersState();

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

                Move move = new Move {
                    figure = currentFigure.Peel(),
                    figureToEat = manager.board[gameFinalX][gameFinalZ],
                    x = gameFinalX,
                    z = gameFinalZ
                };

                if (!manager.IsCorrectMove(move)) {
                    currentFigure.Peel().transform.position -= Vector3.up;
                    currentFigure = Option<Figure>.None();
                    return;
                }

                manager.MakeMove(move);

                var worldFinalX = cellComponent.transform.position.x;
                var worldFinalZ = cellComponent.transform.position.z;
                var finalPosition = new Vector3(worldFinalX, FIGURES_Y_POSITION, worldFinalZ);
                currentFigure.Peel().transform.position = finalPosition;
                currentFigure = Option<Figure>.None();
            }

        }
    }
}
