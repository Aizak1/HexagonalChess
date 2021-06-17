using mover;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UiController : MonoBehaviour
{
    [SerializeField]
    private GameManager manager;
    [SerializeField]
    private Mover mover;

    [SerializeField]
    private Material hexagonMaterial;
    [SerializeField]
    private Material highlightMaterial;

    public List<Cell> highlightedCells;

    private void Update() {

        if (highlightedCells == null || highlightedCells.Count == 0) {
            if (mover.currentFigure.IsSome()) {
                HighlightFigureMoves(mover.currentFigure.Peel());
            }
        } else {
            if (mover.currentFigure.IsNone()) {
                UnHighlightFigureMoves();
            }
        }

    }

    public void HighlightFigureMoves(Figure figure) {
        highlightedCells = new List<Cell>();
        var cells = FindObjectsOfType<Cell>();
        var figureMoves = manager.GetAllCurrentFigureMoves(figure);

        foreach (var cell in cells) {
            foreach (var move in figureMoves) {
                if (cell.gameCoordinates.x == move.finalX && cell.gameCoordinates.y == move.finalZ) {
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
