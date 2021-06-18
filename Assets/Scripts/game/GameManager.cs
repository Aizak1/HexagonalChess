using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using vjp;


public enum GameState {
    Paused,
    InProcessing
}

public class GameManager : MonoBehaviour
{

    [SerializeField]
    private GameResource resource;

    private const int BOARD_VERTICALS_AMOUNT = 9;
    private readonly int[] CELLS_IN_VERTICAL_AMOUNT = new int[] { 6, 7, 8, 9, 10, 9, 8, 7, 6 };

    private const float FIGURES_Y_POSITION = 0.3f;

    private readonly Vector2Int LEFT_WHITE_ROOK_POS = new Vector2Int(0, 0);
    private readonly Vector2Int RIGHT_WHITE_ROOK_POS = new Vector2Int(8, 0);
    private readonly Vector2Int LEFT_BLACK_ROOK_POS = new Vector2Int(0, 5);
    private readonly Vector2Int RIGHT_BLACK_ROOK_POS = new Vector2Int(8, 5);


    public Option<Figure>[][] board = new Option<Figure>[9][];
    public Move previousMove;

    public bool isWhiteTurn;
    public GameState gameState;


    private void Start() {
        InitializeGame();
    }

    public void MakeMove(Move move) {

        previousMove = move;
        if (IsCastling(move, board, isWhiteTurn)) {
            MakeCastling(move, board, isWhiteTurn);
        }

        Figure figure = move.figure;
        Option<Figure> figureToEat = move.figureToEat;
        figure.moveCount++;

        board[move.initX][move.initZ] = Option<Figure>.None();

        figure.x = move.finalX;
        figure.z = move.finalZ;

        if (figureToEat.IsSome()) {
            Destroy(figureToEat.Peel().gameObject);
            board[figureToEat.Peel().x][figureToEat.Peel().z] = Option<Figure>.None();
        }
        board[move.finalX][move.finalZ] = Option<Figure>.Some(figure);
        MoveFigurePosition(figure);
        isWhiteTurn = !isWhiteTurn;

        var moves = GetAllTeamMoves(board, isWhiteTurn);
        if (moves.Count == 0) {

            if (!IsCheck(board, isWhiteTurn)) {
                Debug.Log("Draw");
                return;
            }

            if (isWhiteTurn) {
                Debug.Log("Black wins");
                gameState = GameState.Paused;
            } else {
                Debug.Log("White wins");
                gameState = GameState.Paused;
            }
        }
    }

    private void MoveFigurePosition(Figure figure) {
        var cellWorldCoord = resource.coordinates2dToWorld[new Vector2Int(figure.x, figure.z)];
        var finalPosition = new Vector3(cellWorldCoord.x, FIGURES_Y_POSITION, cellWorldCoord.z);
        figure.transform.position = finalPosition;
    }

    private void MakeCastling(Move move, Option<Figure>[][] board, bool isWhiteTurn) {
        var delta2dX = Mathf.Abs(move.finalX - move.initX);

        Option<Figure> rookCell;
        Figure rook;

        if (delta2dX == 2) {
            if (move.figure.isWhite) {
                rookCell = board[RIGHT_WHITE_ROOK_POS.x][RIGHT_WHITE_ROOK_POS.y];
                rook = rookCell.Peel();

                rook.x = 5;
                rook.z = 0;
                board[RIGHT_WHITE_ROOK_POS.x][RIGHT_WHITE_ROOK_POS.y] = Option<Figure>.None();

            } else {
                rookCell = board[LEFT_BLACK_ROOK_POS.x][LEFT_BLACK_ROOK_POS.y];
                rook = rookCell.Peel();

                rook.x = 3;
                rook.z = 8;

                board[LEFT_BLACK_ROOK_POS.x][LEFT_BLACK_ROOK_POS.y] = Option<Figure>.None();
            }
        } else {
            if (move.figure.isWhite) {
                rookCell = board[LEFT_WHITE_ROOK_POS.x][LEFT_WHITE_ROOK_POS.y];
                rook = rookCell.Peel();

                rook.x = 2;
                rook.z = 0;

                board[2][0] = Option<Figure>.Some(rook);
                board[LEFT_WHITE_ROOK_POS.x][LEFT_WHITE_ROOK_POS.y] = Option<Figure>.None();
            } else {

                rookCell = board[RIGHT_BLACK_ROOK_POS.x][RIGHT_BLACK_ROOK_POS.y];

                rook = rookCell.Peel();

                rook.x = 6;
                rook.z = 7;
                board[RIGHT_BLACK_ROOK_POS.x][RIGHT_BLACK_ROOK_POS.y] = Option<Figure>.None();

            }
        }
        board[rook.x][rook.z] = Option<Figure>.Some(rook);
        MoveFigurePosition(rook);

    }

    public bool IsCorrectMove(Move move, Option<Figure>[][] board, bool isWhiteTurn) {

        if (IsEnPassant(move, previousMove)) {
            move.figureToEat = Option<Figure>.Some(previousMove.figure);
        }

        if (!IsCorrectMovePattern(move, board, isWhiteTurn)) {
            return false;
        }

        var boardCopy = CreateBoardCopy(board);

        int initX = move.initX;
        int initZ = move.initZ;


        if (move.figureToEat.IsSome()) {
            var figureToEat = move.figureToEat.Peel();
            boardCopy[figureToEat.x][figureToEat.z] = Option<Figure>.None();
        }


        boardCopy[initX][initZ] = Option<Figure>.None();
        boardCopy[move.finalX][move.finalZ] = Option<Figure>.Some(move.figure);

        boardCopy[move.finalX][move.finalZ].Peel().x = move.finalX;
        boardCopy[move.finalX][move.finalZ].Peel().z = move.finalZ;

        if (IsCheck(boardCopy, isWhiteTurn)) {
            boardCopy[move.finalX][move.finalZ].Peel().x = initX;
            boardCopy[move.finalX][move.finalZ].Peel().z = initZ;
            return false;
        }

        boardCopy[move.finalX][move.finalZ].Peel().x = initX;
        boardCopy[move.finalX][move.finalZ].Peel().z = initZ;
        return true;
    }

    public bool IsCorrectMovePattern(Move move,Option<Figure>[][] board, bool isWhiteTurn) {

        Figure figure = move.figure;
        Option<Figure> figureToEat = move.figureToEat;

        int delta2dX = Mathf.Abs(move.initX - move.finalX);
        int delta2dZ = Mathf.Abs(move.initZ - move.finalZ);

        var initCoordIn3D = resource.coordinates2dTo3d[new Vector2Int(move.initX, move.initZ)];
        var finalCoordIn3D = resource.coordinates2dTo3d[new Vector2Int(move.finalX, move.finalZ)];

        int delta3dX = Mathf.Abs(initCoordIn3D.x - finalCoordIn3D.x);
        int delta3dY = Mathf.Abs(initCoordIn3D.y - finalCoordIn3D.y);
        int delta3dZ = Mathf.Abs(initCoordIn3D.z - finalCoordIn3D.z);

        Vector3Int absDeltaIn3d = new Vector3Int(delta3dX, delta3dY, delta3dZ);


        if (figure.isWhite != isWhiteTurn) {
            return false;
        }

        if (figureToEat.IsSome() && figureToEat.Peel().isWhite == figure.isWhite) {
            return false;
        }


        switch (figure.type) {
            case FigureType.Pawn:
                float halfOfVertical = (float)board[figure.x].Length / 2;

                if (figure.isWhite && move.finalZ < move.initZ) {
                    return false;
                }

                if (!figure.isWhite && move.finalZ > move.initZ) {
                    return false;
                }

                if (figureToEat.IsNone()) {

                    if(delta2dX != 0) {
                        return false;
                    }

                    if (!IsStraightMove(absDeltaIn3d)) {
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

                    if (!IsDiagonalMove(absDeltaIn3d)) {
                        return false;
                    }
                }

                break;
            case FigureType.Rook:

                if (!IsStraightMove(absDeltaIn3d)) {
                    return false;
                }

                if (IsObstacleInDiretion(initCoordIn3D, finalCoordIn3D, board )) {
                    return false;
                }

                break;
            case FigureType.Knight:

                if (!IsKnightMove(absDeltaIn3d)){
                    return false;
                }

                break;
            case FigureType.Bishop:

                if (!IsDiagonalMove(absDeltaIn3d)) {
                    return false;
                }
                if (IsObstacleInDiretion(initCoordIn3D, finalCoordIn3D, board)) {
                    return false;
                }

                break;
            case FigureType.Queen:

                if (!IsDiagonalMove(absDeltaIn3d) && !IsStraightMove(absDeltaIn3d)) {
                    return false;
                }

                if (IsObstacleInDiretion(initCoordIn3D, finalCoordIn3D, board)) {
                    return false;
                }

                break;
            case FigureType.King:

                if(delta2dX > 3) {
                    return false;
                }

                if(delta2dX == 0 && delta2dZ > 1) {
                    return false;
                }

                if(delta2dX == 1) {
                    if(!IsStraightMove(absDeltaIn3d) && !IsDiagonalMove(absDeltaIn3d)) {
                        return false;
                    }
                }

                if(delta2dX == 2) {

                    if (!IsCastling(move, board, isWhiteTurn)) {
                        if (!IsDiagonalMove(absDeltaIn3d)) {
                            return false;
                        }

                        if (delta3dX != 1) {
                            return false;
                        }
                    }
                }

                if(delta2dX == 3) {
                    if (!IsCastling(move,board,isWhiteTurn)) {
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

    private bool IsDiagonalMove(Vector3Int absDelta3d) {
        if(absDelta3d.x == absDelta3d.y && absDelta3d.z != 0) {
            return true;
        }

        if(absDelta3d.y == absDelta3d.z && absDelta3d.x != 0) {
            return true;
        }

        if(absDelta3d.x == absDelta3d.z && absDelta3d.y != 0) {
            return true;
        }

        return false;
    }

    private bool IsStraightMove(Vector3Int absDelta3d) {
        if(absDelta3d.x != 0 && absDelta3d.y != 0 && absDelta3d.z != 0) {
            return false;
        }
        return true;
    }

    private bool IsKnightMove(Vector3Int absDelta3d) {
        if (absDelta3d.x == 1 && absDelta3d.y == 2 && absDelta3d.z == 3) {
            return true;
        }

        if (absDelta3d.x == 1 && absDelta3d.y == 3 && absDelta3d.z == 2) {
            return true;
        }

        if (absDelta3d.x == 3 && absDelta3d.y == 1 && absDelta3d.z == 2) {
            return true;
        }

        if (absDelta3d.x == 2 && absDelta3d.y == 1 && absDelta3d.z == 3) {
            return true;
        }

        if (absDelta3d.x == 2 && absDelta3d.y == 3 && absDelta3d.z == 1) {
            return true;
        }

        if (absDelta3d.x == 3 && absDelta3d.y == 2 && absDelta3d.z == 1) {
            return true;
        }

        return false;
    }

    private bool IsEnPassant(Move move, Move previousMove) {

        if(previousMove == null) {
            return false;
        }

        if (previousMove.figure.moveCount != 1) {
            return false;
        }

        if (previousMove.figure.type != FigureType.Pawn) {
            return false;
        }

        if(previousMove.finalX != move.finalX) {
            return false;
        }

        var delta2dZ = Mathf.Abs(previousMove.finalZ - previousMove.initZ);

        if(delta2dZ == 1) {
            return false;
        }

        var currentPos = new Vector2Int(move.initX, move.initZ);
        var initCoordIn3D = resource.coordinates2dTo3d[new Vector2Int(currentPos.x,currentPos.y)];

        Vector2Int eatPos;

        if (previousMove.figure.isWhite) {
             eatPos = new Vector2Int(previousMove.finalX, previousMove.finalZ - 1);
        } else {
             eatPos = new Vector2Int(previousMove.finalX, previousMove.finalZ + 1);
        }

        var finalCoordIn3D = resource.coordinates2dTo3d[new Vector2Int(eatPos.x,eatPos.y)];

        int delta3dX = Mathf.Abs(initCoordIn3D.x - finalCoordIn3D.x);
        int delta3dY = Mathf.Abs(initCoordIn3D.y - finalCoordIn3D.y);
        int delta3dZ = Mathf.Abs(initCoordIn3D.z - finalCoordIn3D.z);

        Vector3Int absDeltaIn3d = new Vector3Int(delta3dX, delta3dY, delta3dZ);

        if (!IsDiagonalMove(absDeltaIn3d)) {
            return false;
        }

        return true;
    }

    private bool IsCastling(Move move,Option<Figure>[][] board,bool isWhiteTurn) {
        if(move.figure.moveCount != 0) {
            return false;
        }

        if(move.figure.type != FigureType.King) {
            return false;
        }

        var delta2dX = Mathf.Abs(move.finalX - move.initX);

        if(delta2dX != 2 && delta2dX != 3) {
            return false;
        }

        Option<Figure> rookCell;

        if(delta2dX == 2) {
            if (move.figure.isWhite) {
                rookCell = board[RIGHT_WHITE_ROOK_POS.x][RIGHT_WHITE_ROOK_POS.y];
            } else {
                rookCell = board[LEFT_BLACK_ROOK_POS.x][LEFT_BLACK_ROOK_POS.y];
            }
        } else {
            if (move.figure.isWhite) {
                rookCell = board[LEFT_WHITE_ROOK_POS.x][LEFT_WHITE_ROOK_POS.y];
            } else {
                rookCell = board[RIGHT_BLACK_ROOK_POS.x][RIGHT_BLACK_ROOK_POS.y];
            }
        }

        if (move.figure.isWhite) {
            if(delta2dX == 2 && move.finalX < move.initX) {
                return false;
            }
            if(delta2dX == 3 && move.finalX > move.initX) {
                return false;
            }
        } else {
            if (delta2dX == 3 && move.finalX < move.initX) {
                return false;
            }
            if (delta2dX == 2 && move.finalX > move.initX) {
                return false;
            }
        }


        if (rookCell.IsNone()) {
            return false;
        }

        if (rookCell.Peel().type != FigureType.Rook) {
            return false;
        }

        var rook = rookCell.Peel();

        if (rookCell.Peel().moveCount > 0) {
            return false;
        }


        var rookCordIn3d = resource.coordinates2dTo3d[new Vector2Int(rook.x,rook.z)];
        var moveInitCordIn3d = resource.coordinates2dTo3d[new Vector2Int(move.initX, move.initZ)];
        var moveFinalCordIn3d =
            resource.coordinates2dTo3d[new Vector2Int(move.finalX, move.finalZ)];

        var moveDelta = moveFinalCordIn3d - moveInitCordIn3d;

        moveDelta.x = Mathf.Abs(moveDelta.x);
        moveDelta.y = Mathf.Abs(moveDelta.y);
        moveDelta.z = Mathf.Abs(moveDelta.z);

        if (!IsStraightMove(moveDelta)) {
            return false;
        }

        if (IsObstacleInDiretion(moveInitCordIn3d,rookCordIn3d,board)) {
            return false;
        }

        if (IsObstacleInDiretion(moveInitCordIn3d, moveFinalCordIn3d, board)) {
            return false;
        }

        if (IsCheck(board, isWhiteTurn)) {
            return false;
        }

        return true;
    }

    private bool IsCheck(Option<Figure>[][] board, bool isWhiteTurn) {
        Option<Figure> king = Option<Figure>.None();
        List<Figure> opponentFigures = new List<Figure>();
        for (int i = 0; i < BOARD_VERTICALS_AMOUNT; i++) {
            for (int j = 0; j < board[i].Length; j++) {

                if (board[i][j].IsNone()) {
                    continue;
                }

                var figure = board[i][j].Peel();

                if (figure.isWhite != isWhiteTurn) {
                    opponentFigures.Add(board[i][j].Peel());
                }

                if(figure.type == FigureType.King && figure.isWhite == isWhiteTurn) {
                    king = Option<Figure>.Some(board[i][j].Peel());
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

                initX = item.x,
                initZ = item.z,

                finalX = king.Peel().x,
                finalZ = king.Peel().z
            };
            if (IsCorrectMovePattern(move, board,!isWhiteTurn)) {
                return true;
            }
        }
        return false;

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

    public List<Move> GetAllTeamMoves(Option<Figure>[][] board,bool isWhiteTurn) {
        List<Move> moves = new List<Move>();
        List<Figure> currentTurnFigures = new List<Figure>();

        for (int i = 0; i < BOARD_VERTICALS_AMOUNT; i++) {
            for (int j = 0; j < board[i].Length; j++) {
                if (board[i][j].IsNone()) {
                    continue;
                }

                if(board[i][j].Peel().isWhite == isWhiteTurn) {
                    currentTurnFigures.Add(board[i][j].Peel());
                }
            }
        }

        foreach (var item in currentTurnFigures) {
            for (int i = 0; i < BOARD_VERTICALS_AMOUNT; i++) {
                for (int j = 0; j < board[i].Length; j++) {

                    var move = new Move() {
                        figure = item,
                        figureToEat = board[i][j],

                        initX = item.x,
                        initZ = item.z,

                        finalX = i,
                        finalZ = j
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

                    initX = figure.x,
                    initZ = figure.z,

                    finalX = i,
                    finalZ = j,
                };
                if (IsCorrectMove(move,board,isWhiteTurn)) {
                    moves.Add(move);
                }
            }
        }
        return moves;
    }

    public void InitializeGame() {

        gameState = GameState.InProcessing;
        isWhiteTurn = true;

        for (int i = 0; i < BOARD_VERTICALS_AMOUNT; i++) {
            board[i] = new Option<Figure>[CELLS_IN_VERTICAL_AMOUNT[i]];
        }

        foreach (var item in resource.figuresToSetup) {
            var cell = item.Key;

            var cellPosition = cell.transform.position;

            var position = new Vector3(cellPosition.x, FIGURES_Y_POSITION, cellPosition.z);
            var rotation = Quaternion.identity;
            var figure = Instantiate(item.Value, position, rotation, transform);

            figure.x = cell.gameCoordinates.x;
            figure.z = cell.gameCoordinates.y;

            board[figure.x][figure.z] = Option<Figure>.Some(figure);

        }
    }

}
