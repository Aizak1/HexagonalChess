using figure;
using move;
using net;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using vjp;
using mover;

namespace game {
    public enum GameState {
        NotStarted,
        Paused,
        InProcessing,
        Finished
    }

    public enum GameResult {
        Draw,
        WhiteWin,
        BlackWin
    }

    public class GameManager : MonoBehaviour {

        [SerializeField]
        private GameResource resource;

        public Client client;
        public Server server;

        private const int BOARD_VERTICALS_AMOUNT = 9;

        private readonly int[] CELLS_IN_VERTICAL_AMOUNT
            = new int[] { 6, 7, 8, 9, 10, 9, 8, 7, 6 };

        private const float FIGURES_Y_POSITION = 0.3f;

        private readonly Vector2Int LEFT_WHITE_ROOK_POS = new Vector2Int(0, 0);
        private readonly Vector2Int RIGHT_WHITE_ROOK_POS = new Vector2Int(8, 0);
        private readonly Vector2Int LEFT_BLACK_ROOK_POS = new Vector2Int(0, 5);
        private readonly Vector2Int RIGHT_BLACK_ROOK_POS = new Vector2Int(8, 5);

        private readonly Vector2Int LEFT_WHITE_ROOK_CASTLING_POS = new Vector2Int(2, 0);
        private readonly Vector2Int RIGHT_WHITE_ROOK_CASTLING_POS = new Vector2Int(5, 0);
        private readonly Vector2Int LEFT_BLACK_ROOK_CASTLING_POS = new Vector2Int(3, 8);
        private readonly Vector2Int RIGHT_BLACK_ROOK_CASTLING_POS = new Vector2Int(6, 7);

        private const int LONG_CASTLING_DELTA = 3;
        private const int SHORT_CASTLING_DELTA = 2;

        private const int STRAIGHT_MOVE_THIRD_COORD_DELTA = 0;
        private const int DIAGONAL_MOVE_THIRD_COORD_DELTA = 2;

        private const int KING_MAX_2D_X_DELTA = 3;


        public Option<Figure>[][] board = new Option<Figure>[BOARD_VERTICALS_AMOUNT][];
        public Move previousMove;

        public bool isWhiteTurn;
        public bool isWhiteTeam;
        public GameState gameState;
        public GameResult gameResult;


        private void Start() {
            gameState = GameState.NotStarted;
        }

        public void MakeMove(Move move) {
            Figure figure = board[move.initX][move.initY].Peel();
            Option<Figure> figureToEat = board[move.finalX][move.finalY];
            if (IsEnPassant(move, previousMove)) {
                figureToEat = board[previousMove.finalX][previousMove.finalY];
            }

            previousMove = move;

            if (IsCastling(move, board, isWhiteTurn)) {
                MakeCastling(move, board);
            }

            figure.moveCount++;

            board[move.initX][move.initY] = Option<Figure>.None();

            figure.x = move.finalX;
            figure.y = move.finalY;

            if (figureToEat.IsSome()) {
                Destroy(figureToEat.Peel().gameObject);
                board[figureToEat.Peel().x][figureToEat.Peel().y] = Option<Figure>.None();
            }
            board[move.finalX][move.finalY] = Option<Figure>.Some(figure);
            MoveFigurePosition(figure);
            isWhiteTurn = !isWhiteTurn;

            if (client == null) {
                isWhiteTeam = !isWhiteTeam;
            }

            if (IsPawnRichEndOfTheBoard(move, board)) {
                gameState = GameState.Paused;
                return;
            }

            var moves = GetAllTeamMoves(board, isWhiteTurn);
            if (moves.Count == 0) {

                gameResult = CalculateGameResult(board, isWhiteTurn);
                gameState = GameState.Finished;
            }

            if (client != null && isWhiteTeam != isWhiteTurn) {
                string sendData =
                    $"MOVE|{move.initX}|{move.initY}|{move.finalX}|{move.finalY}";
                Debug.Log(sendData);
                client.Send(sendData);

            }


        }

        public GameResult CalculateGameResult(Option<Figure>[][] board, bool isWhiteTurn) {
            if (!IsCheck(board, isWhiteTurn)) {
                return GameResult.Draw;
            }

            if (isWhiteTurn) {
                return GameResult.BlackWin;
            } else {
                return GameResult.WhiteWin;
            }

        }

        private void MoveFigurePosition(Figure figure) {
            var cellWorldCoord = resource.converter2dToWorld[new Vector2Int(figure.x, figure.y)];
            var finalPos = new Vector3(cellWorldCoord.x, FIGURES_Y_POSITION, cellWorldCoord.z);
            figure.transform.position = finalPos;
        }

        public bool IsCorrectSelect(Figure figure) {
            if(figure.isWhite != isWhiteTeam) {
                return false;
            }
            return true;
        }

        public bool IsCorrectMove(Move move, Option<Figure>[][] board, bool isWhiteTurn) {

            if (!IsCorrectMovePattern(move, board, isWhiteTurn)) {
                return false;
            }

            var boardCopy = CreateBoardCopy(board);

            int initX = move.initX;
            int initY = move.initY;

            Figure figure = board[initX][initY].Peel();

            boardCopy[move.finalX][move.finalY] = Option<Figure>.Some(figure);
            boardCopy[initX][initY] = Option<Figure>.None();

            boardCopy[move.finalX][move.finalY].Peel().x = move.finalX;
            boardCopy[move.finalX][move.finalY].Peel().y = move.finalY;

            if (IsCheck(boardCopy, isWhiteTurn)) {
                boardCopy[move.finalX][move.finalY].Peel().x = initX;
                boardCopy[move.finalX][move.finalY].Peel().y = initY;
                return false;
            }

            boardCopy[move.finalX][move.finalY].Peel().x = initX;
            boardCopy[move.finalX][move.finalY].Peel().y = initY;
            return true;
        }

        public bool IsCorrectMovePattern(Move move, Option<Figure>[][] board, bool isWhiteTurn) {

            Figure figure = board[move.initX][move.initY].Peel();
            Option<Figure> figureToEat = board[move.finalX][move.finalY];

            int delta2dX = Mathf.Abs(move.initX - move.finalX);
            int delta2dY = Mathf.Abs(move.initY - move.finalY);

            var initCoordIn3D = resource.converter2dTo3d[new Vector2Int(move.initX, move.initY)];
            var finalCoordIn3D =
                resource.converter2dTo3d[new Vector2Int(move.finalX, move.finalY)];

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

                    if (figure.isWhite && move.finalY < move.initY) {
                        return false;
                    }

                    if (!figure.isWhite && move.finalY > move.initY) {
                        return false;
                    }

                    if (figureToEat.IsNone()) {

                        if (IsEnPassant(move,previousMove)) {
                            return true;
                        }

                        if (delta2dX != 0) {
                            return false;
                        }

                        if (!IsStraightMove(absDeltaIn3d)) {
                            return false;
                        }

                        if (figure.moveCount == 0 && delta2dY >= halfOfVertical - 1) {
                            return false;
                        }

                        if (figure.moveCount != 0 && delta2dY > 1) {
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

                    if (IsObstacleInDiretion(initCoordIn3D, finalCoordIn3D, board)) {
                        return false;
                    }

                    break;

                case FigureType.Knight:

                    if (!IsKnightMove(absDeltaIn3d)) {
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

                    if (delta2dX > KING_MAX_2D_X_DELTA) {
                        return false;
                    }

                    if (delta2dX == 0 && delta2dY > 1) {
                        return false;
                    }

                    if (delta2dX == 1) {
                        if (!IsStraightMove(absDeltaIn3d) && !IsDiagonalMove(absDeltaIn3d)) {
                            return false;
                        }
                    }

                    if (delta2dX == 2) {

                        if (!IsCastling(move, board, isWhiteTurn)) {
                            if (!IsDiagonalMove(absDeltaIn3d)) {
                                return false;
                            }

                            if (delta3dX != 1) {
                                return false;
                            }
                        }
                    }

                    if (delta2dX == KING_MAX_2D_X_DELTA) {
                        if (!IsCastling(move, board, isWhiteTurn)) {
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
            ) {
            int deltaSignedX = finalPos.x - initPos.x;
            int deltaSignedY = finalPos.y - initPos.y;
            int deltaSignedZ = finalPos.z - initPos.z;

            int deltaUnsignedX = Mathf.Abs(deltaSignedX);
            int deltaUnsignedY = Mathf.Abs(deltaSignedY);
            int deltaUnsignedZ = Mathf.Abs(deltaSignedZ);

            int stepX = 0;
            int stepY = 0;
            int stepZ = 0;

            if (deltaSignedX != 0 && deltaSignedY != 0 && deltaSignedZ != 0) {
                stepX = deltaSignedX / deltaUnsignedX;
                stepY = deltaSignedY / deltaUnsignedY;
                stepZ = deltaSignedZ / deltaUnsignedZ;

                if (deltaUnsignedX == deltaUnsignedY) {
                    stepZ *= DIAGONAL_MOVE_THIRD_COORD_DELTA;

                } else if (deltaUnsignedY == deltaUnsignedZ) {
                    stepX *= DIAGONAL_MOVE_THIRD_COORD_DELTA;

                } else if (deltaUnsignedX == deltaUnsignedZ) {
                    stepY *= DIAGONAL_MOVE_THIRD_COORD_DELTA;

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
                var coordinatesIn2d = resource.converter3dTo2d[initialPosition];
                if (board[coordinatesIn2d.x][coordinatesIn2d.y].IsSome()) {
                    return true;
                }
                initialPosition += step;

            }

            return false;
        }

        private bool IsDiagonalMove(Vector3Int absDelta3d) {
            if (absDelta3d.x == absDelta3d.y && absDelta3d.z != STRAIGHT_MOVE_THIRD_COORD_DELTA) {
                return true;
            }

            if (absDelta3d.y == absDelta3d.z && absDelta3d.x != STRAIGHT_MOVE_THIRD_COORD_DELTA) {
                return true;
            }

            if (absDelta3d.x == absDelta3d.z && absDelta3d.y != STRAIGHT_MOVE_THIRD_COORD_DELTA) {
                return true;
            }

            return false;
        }

        private bool IsStraightMove(Vector3Int absDelta3d) {
            if (absDelta3d.x != STRAIGHT_MOVE_THIRD_COORD_DELTA
                && absDelta3d.y != STRAIGHT_MOVE_THIRD_COORD_DELTA
                && absDelta3d.z != STRAIGHT_MOVE_THIRD_COORD_DELTA
                ) {
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

            if (previousMove == null) {
                return false;
            }

            var prevMoveFigure = board[previousMove.finalX][previousMove.finalY].Peel();

            if (prevMoveFigure.moveCount != 1) {
                return false;
            }

            if (prevMoveFigure.type != FigureType.Pawn) {
                return false;
            }

            if (previousMove.finalX != move.finalX) {
                return false;
            }


            var delta2dY = Mathf.Abs(previousMove.finalY - previousMove.initY);

            if (delta2dY == 1) {
                return false;
            }

            var currentPos = new Vector2Int(move.initX, move.initY);
            var initCoordIn3D =
                resource.converter2dTo3d[new Vector2Int(currentPos.x, currentPos.y)];

            Vector2Int eatPos;

            if (prevMoveFigure.isWhite) {
                eatPos = new Vector2Int(previousMove.finalX, previousMove.finalY - 1);
            } else {
                eatPos = new Vector2Int(previousMove.finalX, previousMove.finalY + 1);
            }

            if(move.finalY != eatPos.y) {
                return false;
            }

            var finalCoordIn3D = resource.converter2dTo3d[new Vector2Int(eatPos.x, eatPos.y)];

            int delta3dX = Mathf.Abs(initCoordIn3D.x - finalCoordIn3D.x);
            int delta3dY = Mathf.Abs(initCoordIn3D.y - finalCoordIn3D.y);
            int delta3dZ = Mathf.Abs(initCoordIn3D.z - finalCoordIn3D.z);

            Vector3Int absDeltaIn3d = new Vector3Int(delta3dX, delta3dY, delta3dZ);

            if (!IsDiagonalMove(absDeltaIn3d)) {
                return false;
            }

            return true;
        }

        private bool IsCastling(Move move, Option<Figure>[][] board, bool isWhiteTurn) {

            Figure figure = board[move.initX][move.initY].Peel();

            if (figure.moveCount != 0) {
                return false;
            }

            if (figure.type != FigureType.King) {
                return false;
            }

            var delta2dX = Mathf.Abs(move.finalX - move.initX);

            if (delta2dX != SHORT_CASTLING_DELTA && delta2dX != LONG_CASTLING_DELTA) {
                return false;
            }

            Option<Figure> rookCell;

            if (delta2dX == SHORT_CASTLING_DELTA) {

                if (figure.isWhite) {
                    rookCell = board[RIGHT_WHITE_ROOK_POS.x][RIGHT_WHITE_ROOK_POS.y];
                } else {
                    rookCell = board[LEFT_BLACK_ROOK_POS.x][LEFT_BLACK_ROOK_POS.y];
                }

            } else {

                if (figure.isWhite) {
                    rookCell = board[LEFT_WHITE_ROOK_POS.x][LEFT_WHITE_ROOK_POS.y];
                } else {
                    rookCell = board[RIGHT_BLACK_ROOK_POS.x][RIGHT_BLACK_ROOK_POS.y];
                }

            }

            if (figure.isWhite) {
                if (delta2dX == SHORT_CASTLING_DELTA && move.finalX < move.initX) {
                    return false;
                }
                if (delta2dX == LONG_CASTLING_DELTA && move.finalX > move.initX) {
                    return false;
                }
            } else {
                if (delta2dX == LONG_CASTLING_DELTA && move.finalX < move.initX) {
                    return false;
                }
                if (delta2dX == SHORT_CASTLING_DELTA && move.finalX > move.initX) {
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


            var rookCordIn3d = resource.converter2dTo3d[new Vector2Int(rook.x, rook.y)];
            var moveInitCordIn3d =
                resource.converter2dTo3d[new Vector2Int(move.initX, move.initY)];
            var moveFinalCordIn3d =
                resource.converter2dTo3d[new Vector2Int(move.finalX, move.finalY)];

            var moveDelta = moveFinalCordIn3d - moveInitCordIn3d;

            moveDelta.x = Mathf.Abs(moveDelta.x);
            moveDelta.y = Mathf.Abs(moveDelta.y);
            moveDelta.z = Mathf.Abs(moveDelta.z);

            if (!IsStraightMove(moveDelta)) {
                return false;
            }

            if (IsObstacleInDiretion(moveInitCordIn3d, rookCordIn3d, board)) {
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

        private void MakeCastling(Move move, Option<Figure>[][] board) {
            var delta2dX = Mathf.Abs(move.finalX - move.initX);

            Option<Figure> rookCell;
            Figure figure = board[move.initX][move.initY].Peel();
            Figure rook;

            if (delta2dX == SHORT_CASTLING_DELTA) {
                if (figure.isWhite) {

                    rookCell = board[RIGHT_WHITE_ROOK_POS.x][RIGHT_WHITE_ROOK_POS.y];
                    rook = rookCell.Peel();

                    rook.x = RIGHT_WHITE_ROOK_CASTLING_POS.x;
                    rook.y = RIGHT_WHITE_ROOK_CASTLING_POS.y;
                    board[RIGHT_WHITE_ROOK_POS.x][RIGHT_WHITE_ROOK_POS.y] = Option<Figure>.None();


                } else {

                    rookCell = board[LEFT_BLACK_ROOK_POS.x][LEFT_BLACK_ROOK_POS.y];
                    rook = rookCell.Peel();

                    rook.x = LEFT_BLACK_ROOK_CASTLING_POS.x;
                    rook.y = LEFT_BLACK_ROOK_CASTLING_POS.y;

                    board[LEFT_BLACK_ROOK_POS.x][LEFT_BLACK_ROOK_POS.y] = Option<Figure>.None();

                }
            } else {

                if (figure.isWhite) {

                    rookCell = board[LEFT_WHITE_ROOK_POS.x][LEFT_WHITE_ROOK_POS.y];
                    rook = rookCell.Peel();

                    rook.x = LEFT_WHITE_ROOK_CASTLING_POS.x;
                    rook.y = LEFT_WHITE_ROOK_CASTLING_POS.y;

                    board[LEFT_WHITE_ROOK_POS.x][LEFT_WHITE_ROOK_POS.y] = Option<Figure>.None();

                } else {

                    rookCell = board[RIGHT_BLACK_ROOK_POS.x][RIGHT_BLACK_ROOK_POS.y];

                    rook = rookCell.Peel();

                    rook.x = RIGHT_BLACK_ROOK_CASTLING_POS.x;
                    rook.y = RIGHT_BLACK_ROOK_CASTLING_POS.y;
                    board[RIGHT_BLACK_ROOK_POS.x][RIGHT_BLACK_ROOK_POS.y] = Option<Figure>.None();

                }
            }

            board[rook.x][rook.y] = Option<Figure>.Some(rook);
            MoveFigurePosition(rook);

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

                    if (figure.type == FigureType.King && figure.isWhite == isWhiteTurn) {
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

                    initX = item.x,
                    initY = item.y,

                    finalX = king.Peel().x,
                    finalY = king.Peel().y
                };
                if (IsCorrectMovePattern(move, board, !isWhiteTurn)) {
                    return true;
                }
            }
            return false;

        }


        public void TransformPawnToNewFigure(FigureType figureType) {
            Figure pawnInTheEnd = board[previousMove.finalX][previousMove.finalY].Peel();
            Figure newFigure;
            var position = pawnInTheEnd.transform.position;
            var rotation = pawnInTheEnd.transform.rotation;

            if (pawnInTheEnd.isWhite) {
                var model = resource.whiteModelsForTransformation[figureType];
                newFigure = Instantiate(model, position, rotation, transform);

            } else {
                var model = resource.blackModelsForTransformation[figureType];
                newFigure = Instantiate(model, position, rotation, transform);
            }

            newFigure.x = pawnInTheEnd.x;
            newFigure.y = pawnInTheEnd.y;

            board[pawnInTheEnd.x][pawnInTheEnd.y] = Option<Figure>.Some(newFigure);
            Destroy(pawnInTheEnd.gameObject);
        }


        private bool IsPawnRichEndOfTheBoard(Move move, Option<Figure>[][] board) {
            Figure figure = board[move.finalX][move.finalY].Peel();
            if (figure.type != FigureType.Pawn) {
                return false;
            }

            if (figure.isWhite && move.finalY != board[move.finalX].Length - 1) {
                return false;
            }

            if (!figure.isWhite && move.finalY != 0) {
                return false;
            }

            return true;
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

        public List<Move> GetAllTeamMoves(Option<Figure>[][] board, bool isWhiteTurn) {
            List<Move> moves = new List<Move>();
            List<Figure> currentTurnFigures = new List<Figure>();

            for (int i = 0; i < BOARD_VERTICALS_AMOUNT; i++) {
                for (int j = 0; j < board[i].Length; j++) {
                    if (board[i][j].IsNone()) {
                        continue;
                    }

                    if (board[i][j].Peel().isWhite == isWhiteTurn) {
                        currentTurnFigures.Add(board[i][j].Peel());
                    }
                }
            }

            foreach (var item in currentTurnFigures) {
                for (int i = 0; i < BOARD_VERTICALS_AMOUNT; i++) {
                    for (int j = 0; j < board[i].Length; j++) {

                        var move = new Move() {

                            initX = item.x,
                            initY = item.y,

                            finalX = i,
                            finalY = j
                        };
                        if (IsCorrectMove(move, board, isWhiteTurn)) {
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

                        initX = figure.x,
                        initY = figure.y,

                        finalX = i,
                        finalY = j,
                    };
                    if (IsCorrectMove(move, board, isWhiteTurn)) {
                        moves.Add(move);
                    }
                }
            }
            return moves;
        }

        public void InitializeGame() {

            gameState = GameState.InProcessing;
            isWhiteTurn = true;
            if(client == null) {
                isWhiteTeam = true;
            }

            foreach (var item in resource.figuresToSetup) {
                var cell = item.Key;

                var cellPosition = cell.transform.position;

                var position = new Vector3(cellPosition.x, FIGURES_Y_POSITION, cellPosition.z);
                var rotation = Quaternion.identity;
                var figure = Instantiate(item.Value, position, rotation, transform);

                figure.x = cell.gameCoordinates.x;
                figure.y = cell.gameCoordinates.y;

                board[figure.x][figure.y] = Option<Figure>.Some(figure);

            }
        }

        public void ResetGame() {
            previousMove = null;
            var figuresInGame = FindObjectsOfType<Figure>();
            board = new Option<Figure>[BOARD_VERTICALS_AMOUNT][];
            for (int i = 0; i < BOARD_VERTICALS_AMOUNT; i++) {
                board[i] = new Option<Figure>[CELLS_IN_VERTICAL_AMOUNT[i]];
            }
            foreach (var item in figuresInGame) {
                Destroy(item.gameObject);
            }
        }

    }

}
