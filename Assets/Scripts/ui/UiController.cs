using cell;
using figure;
using game;
using net;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ui {
    public class UiController : MonoBehaviour {
        [SerializeField]
        private GameManager manager;
        [SerializeField]
        private GameResource resource;

        [SerializeField]
        private Material hexagonMaterial;
        [SerializeField]
        private Material highlightMaterial;

        public List<Cell> highlightedCells;

        [SerializeField]
        private Canvas mainMenu;
        [SerializeField]
        private Canvas pawnTransformationMenu;
        [SerializeField]
        private Canvas endGameCanvas;
        [SerializeField]
        private Canvas gameMenu;


        [SerializeField]
        private Canvas connectingMenu;
        [SerializeField]
        private Canvas waitingMenu;


        [SerializeField]
        private Text endGameText;
        [SerializeField]
        private InputField ipInputField;

        private void Update() {
            switch (manager.gameState) {

                case GameState.NotStarted:
                    EnableCanvas(mainMenu);
                    break;

                case GameState.Paused:
                    if(manager.client != null && manager.isWhiteTeam == manager.isWhiteTurn) {
                        return;
                    }

                    EnableCanvas(pawnTransformationMenu);
                    break;
                case GameState.InProcessing:

                    EnableCanvas(gameMenu);

                    break;
                case GameState.Finished:

                    EnableCanvas(endGameCanvas);
                    if (manager.gameResult == GameResult.Draw) {

                        endGameText.text = "Draw";

                    } else if (manager.gameResult == GameResult.WhiteWin) {

                        endGameText.text = "White Win";

                    } else if (manager.gameResult == GameResult.BlackWin) {

                        endGameText.text = "Black Win";

                    }
                    break;
                case GameState.Waiting:
                    EnableCanvas(waitingMenu);
                    break;

                case GameState.Connecting:
                    EnableCanvas(connectingMenu);
                    break;
                default:
                    break;
            }
        }

        private void EnableCanvas(Canvas canvas) {
            connectingMenu.enabled = false;
            waitingMenu.enabled = false;
            endGameCanvas.enabled = false;
            pawnTransformationMenu.enabled = false;
            mainMenu.enabled = false;
            gameMenu.enabled = false;

            canvas.enabled = true;
        }

        public void HotSeatButton() {
            manager.InitializeGame();
        }

        public void TransformPawnToNewFigureButton(string figureTypeName) {
            if (!Enum.TryParse(figureTypeName, out FigureType figureType)) {
                Debug.LogError("Invalid enum type");
                return;
            }
            manager.TransformPawnToNewFigure(figureType);
            if(manager.client != null) {
                string msg = $"TRANSFORM|{(int)figureType}";
                manager.client.Send(msg);
            }


        }

        public void HostServerButton() {
            manager.CreateHostPlayer();
            manager.gameState = GameState.Waiting;
        }
        public void ConnectToServerButton() {
            manager.CreateClientPlayer(ipInputField.text);
        }

        public void ConnectMenuButton() {
            manager.gameState = GameState.Connecting;
        }

        public void MainMenuButton() {
            manager.ResetGame();
            manager.gameState = GameState.NotStarted;
        }

        public void QuitButton() {
            Application.Quit();
        }

        public void HighlightFigureMoves(Figure figure) {
            highlightedCells = new List<Cell>();
            var cells = FindObjectsOfType<Cell>();
            var figureMoves = manager.GetAllCurrentFigureMoves(figure);

            foreach (var cell in cells) {
                foreach (var move in figureMoves) {
                    var gameCoord = cell.gameCoordinates;
                    if (gameCoord.x == move.finalX && gameCoord.y == move.finalY) {
                        cell.gameObject.GetComponent<MeshRenderer>().material = highlightMaterial;
                        highlightedCells.Add(cell);
                    }
                }
            }
        }

        public void UnHighlightFigureMoves() {
            foreach (var item in highlightedCells) {
                item.GetComponent<MeshRenderer>().material = hexagonMaterial;
            }
            highlightedCells = new List<Cell>();
        }


    }
}

