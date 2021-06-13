using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using vjp;

public class GameManager : MonoBehaviour
{

    [SerializeField]
    private GameResource resource;

    private const int BOARD_VERTICALS_AMOUNT = 9;
    private readonly int[] CELLS_IN_VERTICAL_AMOUNT = new int[] { 6, 7, 8, 9, 10, 9, 8, 7, 6 };

    private const float FIGURES_Y_ON_INSTANTIATE = 0.3f;

    public Option<Figure>[][] board = new Option<Figure>[9][];

    public bool isWhiteTurn;


    private void Start() {
        InitializeGame();
    }

    public bool IsCorrectSelect(Figure figure) {

        if(figure.isWhite != isWhiteTurn) {
            return false;
        }

        return true;
    }

    public bool IsCorrectMove(Move move) {

        Figure figure = move.figure;
        Option<Figure> figureToEat = move.figureToEat;

        int deltaX = Mathf.Abs(figure.x - move.x);
        int deltaZ = Mathf.Abs(figure.z - move.z);

        int initialVerticalLength = board[figure.x].Length;
        int finalVerticalLength = board[move.x].Length;

        bool isVertGreater = (finalVerticalLength - initialVerticalLength) > 0;

        float halfOfVertical = (float)board[figure.x].Length / 2;

        if (figure.isWhite != isWhiteTurn) {
            return false;
        }

        switch (figure.type) {
            case FigureType.Pawn:

                if (figure.isWhite && move.z < figure.z) {
                    return false;
                }

                if (!figure.isWhite && move.z > figure.z) {
                    return false;
                }

                if (figure.moveCount != 0 && figureToEat.IsNone() && deltaZ > 1) {
                    return false;
                }

                if (figure.moveCount == 0 && deltaZ >= halfOfVertical - 1) {
                    return false;
                }

                if (figureToEat.IsNone() && deltaX > 0) {
                    return false;
                }

                if (figureToEat.IsSome() && deltaX != 1) {
                    return false;
                }

                if (figureToEat.IsSome() && figureToEat.Peel().isWhite == figure.isWhite) {
                    return false;
                }

                if (figureToEat.IsSome() && !isVertGreater && deltaZ != 1 && move.z > figure.z) {
                    return false;
                }

                if (figureToEat.IsSome() && !isVertGreater && deltaZ != 2 && move.z < figure.z) {
                    return false;
                }

                if (figureToEat.IsSome() && isVertGreater && deltaZ != 2 && move.z > figure.z) {
                    return false;
                }

                if (figureToEat.IsSome() && isVertGreater && deltaZ != 1 && move.z < figure.z) {
                    return false;
                }

                if (IsObstacleInDirection(figure, move.x, move.z)) {
                    return false;
                }

                break;
            case FigureType.Rook:
                break;
            case FigureType.Knight:
                break;
            case FigureType.Bishop:
                break;
            case FigureType.Queen:
                break;
            case FigureType.King:
                break;
            default:
                break;
        }

        return true;
    }

    private bool IsObstacleInDirection(Figure figure, int finalX, int finalZ) {

        int initialX = figure.x;
        int initialZ = figure.z;

        int stepX = 0;
        int stepZ = 0;

        if (finalZ < initialZ) {
            stepZ = -1;
        } else if (finalZ > initialZ) {
            stepZ = 1;
        }


        if (finalX < initialX) {
            stepX = -1;
        } else if (finalX > initialX) {
            stepX = 1;
        }

        do {
            for (int j = initialZ + stepZ; j != finalZ; j+=stepZ) {
                if (board[initialX][j].IsSome()) {
                    return true;
                }
            }
            initialX+=stepX;
        } while (initialX != finalX );


        return false;
    }

    public void MakeMove(Move move) {
        Figure figure = move.figure;
        Option<Figure> figureToEat = move.figureToEat;
        figure.moveCount++;

        board[figure.x][figure.z] = Option<Figure>.None();

        figure.x = move.x;
        figure.z = move.z;

        if (figureToEat.IsSome()) {
            Destroy(figureToEat.Peel().gameObject);
        }
        board[move.x][move.z] = Option<Figure>.Some(figure);

        isWhiteTurn = !isWhiteTurn;
    }

    public void ChangeCollidersState() {
        for (int i = 0; i < BOARD_VERTICALS_AMOUNT; i++) {
            for (int j = 0; j < board[i].Length; j++) {
                if (board[i][j].IsNone()) {
                    continue;
                }
                bool isTurnFigure = board[i][j].Peel().isWhite == isWhiteTurn;
                board[i][j].Peel().gameObject.GetComponent<Collider>().enabled = isTurnFigure;
            }
        }
    }

    public void InitializeGame() {

        isWhiteTurn = true;

        for (int i = 0; i < BOARD_VERTICALS_AMOUNT; i++) {
            board[i] = new Option<Figure>[CELLS_IN_VERTICAL_AMOUNT[i]];
        }

        foreach (var item in resource.figuresToSetup) {
            var cell = item.Key;

            var cellPosition = cell.transform.position;

            var position = new Vector3(cellPosition.x, FIGURES_Y_ON_INSTANTIATE, cellPosition.z);
            var rotation = Quaternion.identity;
            var figure = Instantiate(item.Value, position, rotation, transform);

            figure.x = cell.gameCoordinates.x;
            figure.z = cell.gameCoordinates.y;

            board[figure.x][figure.z] = Option<Figure>.Some(figure);

        }
    }

}
