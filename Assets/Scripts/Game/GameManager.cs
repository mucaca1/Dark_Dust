using Mirror;
using UnityEngine;

namespace Game {
    public class GameManager : NetworkBehaviour {
        private PlaygroundCard[] _playgroundCards;

        [ServerCallback]
        private void Start() {
            _playgroundCards = Resources.LoadAll<PlaygroundCard>("");

            Debug.Log(_playgroundCards.Length == 0
                ? "No playground cards was found. Check Resources folder"
                : "Playground cards was loaded successfully");
        }
    }
}