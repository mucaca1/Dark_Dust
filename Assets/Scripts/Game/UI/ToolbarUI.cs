using Mirror;
using UnityEngine;

namespace Game.UI {
    public class ToolbarUI : MonoBehaviour {
        [SerializeField] private GameObject _toolbar = null;
        [SerializeField] private GameManager _gameManager = null;

        private void Start() {
            _gameManager.onChangeActivePlayer += HandleSwapPlayer;
        }
        
        private void OnDestroy() {
            _gameManager.onChangeActivePlayer -= HandleSwapPlayer;
        }

        private void HandleSwapPlayer(int playerId) {
            _toolbar.SetActive(NetworkClient.connection.connectionId == playerId);
        }
    }
}