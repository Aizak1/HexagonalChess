using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mover;

public class GameComponentManager : MonoBehaviour
{
    [SerializeField]
    private GameManager manager;
    [SerializeField]
    private Mover mover;

    private void Update() {
        if(manager.gameState != GameState.InProcessing) {
            mover.enabled = false;
        } else {
            mover.enabled = true;
        }
    }
}
