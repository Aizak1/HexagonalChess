using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FigureType {
    Pawn,
    Knight,
    Bishop,
    Queen,
    King

}

public class Figure : MonoBehaviour
{
    public int x;
    public int z;

    public int moveCount;

    public bool isWhite;
    public FigureType type;
}
