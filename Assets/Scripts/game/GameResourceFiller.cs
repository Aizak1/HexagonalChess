using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameResourceFiller : MonoBehaviour
{
    [SerializeField]
    private Cell[] cells;
    [SerializeField]
    private Figure[] figures;

    [SerializeField]
    private GameResource resource;



    private void Awake() {

        for (int i = 0; i < cells.Length; i++) {
            resource.figuresToSetup.Add(cells[i], figures[i]);
        }
    }

}
