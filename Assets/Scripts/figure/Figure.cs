using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace figure {
    public enum FigureType {
        Pawn,
        Rook,
        Knight,
        Bishop,
        Queen,
        King

    }

    public class Figure : MonoBehaviour {
        public int x;
        public int y;

        public int moveCount;

        public bool isWhite;
        public FigureType type;
    }
}

