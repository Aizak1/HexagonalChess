using System;
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

    public bool IsMoveMatchRools(Move move,Option<Figure>[][] board, bool isWhiteTurn) {

        Figure figure = move.figure;
        Option<Figure> figureToEat = move.figureToEat;

        int delta2dX = Mathf.Abs(figure.x - move.x);
        int delta2dZ = Mathf.Abs(figure.z - move.z);

        var initCoordIn3D = resource.coordinates2dTo3d[new Vector2Int(figure.x, figure.z)];
        var finalCoordIn3D = resource.coordinates2dTo3d[new Vector2Int(move.x, move.z)];

        int delta3dX = Mathf.Abs(initCoordIn3D.x - finalCoordIn3D.x);
        int delta3dY = Mathf.Abs(initCoordIn3D.y - finalCoordIn3D.y);
        int delta3dZ = Mathf.Abs(initCoordIn3D.z - finalCoordIn3D.z);


        if (figure.isWhite != isWhiteTurn) {
            return false;
        }

        if (figureToEat.IsSome() && figureToEat.Peel().isWhite == figure.isWhite) {
            return false;
        }


        switch (figure.type) {
            case FigureType.Pawn:
                float halfOfVertical = (float)board[figure.x].Length / 2;

                if (figureToEat.IsNone()) {

                    if(delta2dX != 0) {
                        return false;
                    }

                    if(figure.isWhite && move.z < figure.z) {
                        return false;
                    }

                    if (!figure.isWhite && move.z > figure.z) {
                        return false;
                    }

                    if (!IsStraightMove(delta3dX, delta3dY, delta3dZ)) {
                        return false;
                    }

                    if(figure.moveCount == 0 && delta2dZ >= halfOfVertical - 1) {
                        return false;
                    }

                    if(figure.moveCount != 0 && delta2dZ > 1) {
                        return false;
                    }

                    if (IsObstacleInDiretion(initCoordIn3D, finalCoordIn3D, board)) {
                        return false;
                    }
                }

                if (figureToEat.IsSome()) {
                    if (delta2dX != 1) {
                        return false;
                    }

                    if (!IsDiagonalMove(delta3dX,delta3dY,delta3dZ)) {
                        return false;
                    }
                }

                break;
            case FigureType.Rook:

                if (!IsStraightMove(delta3dX, delta3dY, delta3dZ)) {
                    return false;
                }

                if (IsObstacleInDiretion(initCoordIn3D, finalCoordIn3D, board )) {
                    return false;
                }

                break;
            case FigureType.Knight:

                if (!IsKnightMove(delta3dX, delta3dY, delta3dZ)){
                    return false;
                }

                break;
            case FigureType.Bishop:

                if (!IsDiagonalMove(delta3dX, delta3dY, delta3dZ)) {
                    return false;
                }
                if (IsObstacleInDiretion(initCoordIn3D, finalCoordIn3D, board)) {
                    return false;
                }

                break;
            case FigureType.Queen:

                if (!IsDiagonalMove(delta3dX, delta3dY, delta3dZ) &&
                    !IsStraightMove(delta3dX,delta3dY,delta3dZ)){
                    return false;
                }

                if (IsObstacleInDiretion(initCoordIn3D, finalCoordIn3D, board)) {
                    return false;
                }

                break;
            case FigureType.King:

                if(delta2dX > 2) {
                    return false;
                }

                if(delta2dX == 0 && delta2dZ > 1) {
                    return false;
                }

                if(delta2dX == 1) {
                    if(!IsStraightMove(delta3dX,delta3dY,delta2dZ)
                        && !IsDiagonalMove(delta3dX, delta3dY, delta3dZ)) {
                        return false;
                    }
                }

                if(delta2dX == 2) {

                    if (!IsDiagonalMove(delta3dX, delta3dY, delta3dZ)) {
                        return false;
                    }

                    if (delta3dX != 1) {
                        return false;
                    }
                }

              break;
            default:
                break;
        }

        return true;
    }

    private bool IsObstacleInDiretion(
        Vector3Int initPos,
        Vector3Int finalPos,
        Option<Figure>[][] board
        ){
        int deltaSignedX = finalPos.x - initPos.x;
        int deltaSignedY = finalPos.y - initPos.y;
        int deltaSignedZ = finalPos.z - initPos.z;

        int deltaUnsignedX = Mathf.Abs(deltaSignedX);
        int deltaUnsignedY = Mathf.Abs(deltaSignedY);
        int deltaUnsignedZ = Mathf.Abs(deltaSignedZ);

        int stepX = 0;
        int stepY = 0;
        int stepZ = 0;

        if(deltaSignedX != 0 && deltaSignedY != 0 && deltaSignedZ != 0) {
            stepX = deltaSignedX / deltaUnsignedX;
            stepY = deltaSignedY / deltaUnsignedY;
            stepZ = deltaSignedZ / deltaUnsignedZ;

            if( deltaUnsignedX == deltaUnsignedY) {
                stepZ *= 2;
            } else if(deltaUnsignedY == deltaUnsignedZ) {
                stepX *= 2;
            } else if(deltaUnsignedX == deltaUnsignedZ) {
                stepY *= 2;
            }

        } else {
            if (deltaSignedX != 0) {
                stepX = deltaSignedX / deltaUnsignedX;
            }
            if (deltaSignedY != 0) {
                stepY = deltaSignedY / deltaUnsignedY;
            }
            if (deltaSignedZ != 0) {
                stepZ = deltaSignedZ / deltaUnsignedZ;
            }
        }

        Vector3Int step = new Vector3Int(stepX, stepY, stepZ);
        var initialPosition = initPos += step;

        while (initialPosition != finalPos) {
            var coordinatesIn2d = resource.coordinates3dTo2d[initialPosition];
            if (board[coordinatesIn2d.x][coordinatesIn2d.y].IsSome()) {
                return true;
            }
            initialPosition += step;

        }

        return false;
    }

    private bool IsDiagonalMove(int delta3dX, int delta3dY, int delta3dZ) {
        if(delta3dX == delta3dY && delta3dZ != 0) {
            return true;
        }

        if(delta3dY == delta3dZ && delta3dX != 0) {
            return true;
        }

        if(delta3dX == delta3dZ && delta3dY != 0) {
            return true;
        }

        return false;
    }

    private bool IsStraightMove(int delta3dX, int delta3dY, int delta3dZ) {
        if(delta3dX != 0 && delta3dY != 0 && delta3dZ != 0) {
            return false;
        }
        return true;
    }

    private bool IsKnightMove(int delta3dX, int delta3dY, int delta3dZ) {
        if (delta3dX == 1 && delta3dY == 2 && delta3dZ == 3) {
            return true;
        }

        if (delta3dX == 1 && delta3dY == 3 && delta3dZ == 2) {
            return true;
        }

        if (delta3dX == 3 && delta3dY == 1 && delta3dZ == 2) {
            return true;
        }

        if (delta3dX == 2 && delta3dY == 1 && delta3dZ == 3) {
            return true;
        }

        if (delta3dX == 2 && delta3dY == 3 && delta3dZ == 1) {
            return true;
        }

        if (delta3dX == 3 && delta3dY == 2 && delta3dZ == 1) {
            return true;
        }

        return false;
    }

    public bool IsCorrectMove(Move move, Option<Figure>[][] board, bool isWhiteTurn) {

        if (!IsMoveMatchRools(move, board,isWhiteTurn)) {
            return false;
        }

        var boardCopy = CreateBoardCopy(board);

        int initX = move.figure.x;
        int initZ = move.figure.z;

        boardCopy[initX][initZ] = Option<Figure>.None();
        boardCopy[move.x][move.z] = Option<Figure>.Some(move.figure);

        boardCopy[move.x][move.z].Peel().x = move.x;
        boardCopy[move.x][move.z].Peel().z = move.z;

        if (IsCheck(boardCopy,isWhiteTurn)) {
            move.figure.x = initX;
            move.figure.z = initZ;
            return false;
        }

        move.figure.x = initX;
        move.figure.z = initZ;
        return true;
    }

    private bool IsCheck(Option<Figure>[][] boardCopy, bool isWhiteTurn) {
        Option<Figure> king = Option<Figure>.None();
        List<Figure> opponentFigures = new List<Figure>();
        for (int i = 0; i < BOARD_VERTICALS_AMOUNT; i++) {
            for (int j = 0; j < boardCopy[i].Length; j++) {

                if (boardCopy[i][j].IsNone()) {
                    continue;
                }

                var figure = boardCopy[i][j].Peel();

                if (figure.isWhite != isWhiteTurn) {
                    opponentFigures.Add(boardCopy[i][j].Peel());
                }

                if(figure.type == FigureType.King && figure.isWhite == isWhiteTurn) {
                    king = Option<Figure>.Some(boardCopy[i][j].Peel());
                }


            }
        }

        if (king.IsNone()) {
            Debug.LogError("Invalid initialize of game");
            return true;
        }

        foreach (var item in opponentFigures) {
            var move = new Move() {
                figure = item,
                figureToEat = king,
                x = king.Peel().x,
                z = king.Peel().z
            };
            if (IsMoveMatchRools(move, boardCopy,!isWhiteTurn)) {
                return true;
            }
        }
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

        var allFigures = FindObjectsOfType<Figure>();
        var moves = GetAllTeamMoves(allFigures);
        if(moves.Count == 0) {
            if (isWhiteTurn) {
                Debug.Log("Black wins");
            } else {
                Debug.Log("White wins");
            }
        }
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

    public List<Move> GetAllTeamMoves(Figure[] allFigures) {
        List<Move> moves = new List<Move>();
        foreach (var item in allFigures) {

            if(item.isWhite != isWhiteTurn) {
                continue;
            }

            for (int i = 0; i < BOARD_VERTICALS_AMOUNT; i++) {
                for (int j = 0; j < board[i].Length; j++) {

                    var move = new Move() {
                        figure = item,
                        figureToEat = board[i][j],
                        x = i,
                        z = j
                    };
                    if (IsCorrectMove(move,board,isWhiteTurn)) {
                        moves.Add(move);
                    }
                }
            }
        }
        return moves;
    }


    private Option<Figure>[][] CreateBoardCopy(Option<Figure>[][] board) {
        var boardCopy = new Option<Figure>[BOARD_VERTICALS_AMOUNT][];

        for (int i = 0; i < BOARD_VERTICALS_AMOUNT; i++) {
            boardCopy[i] = new Option<Figure>[CELLS_IN_VERTICAL_AMOUNT[i]];
        }

        for (int i = 0; i < BOARD_VERTICALS_AMOUNT; i++) {
            for (int j = 0; j < board[i].Length; j++) {
                boardCopy[i][j] = board[i][j];
            }
        }

        return boardCopy;
    }

    public List<Move> GetAllCurrentFigureMoves(Figure figure) {
        List<Move> moves = new List<Move>();
        for (int i = 0; i < BOARD_VERTICALS_AMOUNT; i++) {
            for (int j = 0; j < board[i].Length; j++) {

                var move = new Move() {
                    figure = figure,
                    figureToEat = board[i][j],
                    x = i,
                    z = j,
                };
                if (IsCorrectMove(move,board,isWhiteTurn)) {
                    moves.Add(move);
                }
            }
        }
        return moves;
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
