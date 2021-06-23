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
        private Text endGameText;

        private void Update() {
            switch (manager.gameState) {

                case GameState.NotStarted:
                    mainMenu.enabled = true;
                    pawnTransformationMenu.enabled = false;
                    endGameCanvas.enabled = false;
                    break;

                case GameState.Paused:
                    if(manager.client != null && manager.isWhiteTeam == manager.isWhiteTurn) {
                        return;
                    }
                    pawnTransformationMenu.enabled = true;
                    mainMenu.enabled = false;
                    endGameCanvas.enabled = false;
                    break;
                case GameState.InProcessing:

                    pawnTransformationMenu.enabled = false;
                    mainMenu.enabled = false;
                    endGameCanvas.enabled = false;

                    break;
                case GameState.Finished:

                    pawnTransformationMenu.enabled = false;
                    mainMenu.enabled = false;
                    endGameCanvas.enabled = true;
                    if (manager.gameResult == GameResult.Draw) {

                        endGameText.text = "Draw";

                    } else if (manager.gameResult == GameResult.WhiteWin) {

                        endGameText.text = "White Win";

                    } else if (manager.gameResult == GameResult.BlackWin) {

                        endGameText.text = "Black Win";

                    }
                    break;
                default:
                    break;
            }
        }

        public void StartNewGame() {
            manager.ResetGame();
            manager.InitializeGame();
        }

        public void TransformPawnToNewFigureUi(string figureTypeName) {
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
        }
        public void ConnectToServerButton() {
            manager.CreateClientPlayer();
        }

        public void Quit() {
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

