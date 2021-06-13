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


    private void Start() {
        InitializeGame();
    }

    public bool IsCorrectMove(Figure figure, int x, int z) {
        return true;
    }

    public void InitializeGame() {

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
