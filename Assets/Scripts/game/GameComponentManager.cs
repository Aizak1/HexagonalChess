using UnityEngine;
using mover;

namespace game {
    public class GameComponentManager : MonoBehaviour {
        [SerializeField]
        private GameManager manager;
        [SerializeField]
        private Mover mover;

        private void Update() {
            if (manager.gameState != GameState.InProcessing) {
                mover.enabled = false;
            } else {
                mover.enabled = true;
            }
        }
    }
}

