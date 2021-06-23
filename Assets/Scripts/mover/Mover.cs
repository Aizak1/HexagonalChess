using cell;
using figure;
using game;
using move;
using ui;
using UnityEngine;
using vjp;

namespace mover {
    public class Mover : MonoBehaviour {

        [SerializeField]
        private GameManager manager;
        [SerializeField]
        private UiController uiController;

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

                if (picked == null || !manager.IsCorrectSelect(picked)) {
                    currentFigure = Option<Figure>.None();
                    return;
                }

                currentFigure = Option<Figure>.Some(picked);
                currentFigure.Peel().transform.position += Vector3.up;
                uiController.HighlightFigureMoves(picked);

            } else {

                var cellComponent = hit.collider.gameObject.GetComponent<Cell>();

                if(cellComponent == null) {
                    currentFigure.Peel().transform.position -= Vector3.up;
                    currentFigure = Option<Figure>.None();
                    uiController.UnHighlightFigureMoves();
                    return;
                }

                var gameFinalX = cellComponent.gameCoordinates.x;
                var gameFinalY = cellComponent.gameCoordinates.y;

                Move move = new Move {
                    figure = currentFigure.Peel(),
                    figureToEat = manager.board[gameFinalX][gameFinalY],

                    initX = currentFigure.Peel().x,
                    initY = currentFigure.Peel().y,

                    finalX = gameFinalX,
                    finalY = gameFinalY,
                };


                if (!manager.IsCorrectMove(move,manager.board,manager.isWhiteTurn)) {
                    currentFigure.Peel().transform.position -= Vector3.up;
                    currentFigure = Option<Figure>.None();

                    uiController.UnHighlightFigureMoves();
                    return;
                }

                manager.MakeMove(move);
                currentFigure = Option<Figure>.None();
                uiController.UnHighlightFigureMoves();
            }

        }
    }
}
