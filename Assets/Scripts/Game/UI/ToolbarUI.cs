using System;
using Mirror;
using Network;
using UnityEngine;

namespace Game.UI {
    public class ToolbarUI : MonoBehaviour {
        [SerializeField] private GameObject _toolbar = null;

        private Player _player = null;


        private void Update() {
            if (_player == null) {
                _player = NetworkClient.connection?.identity?.GetComponent<Player>();
                if (_player != null) {
                    _player.onChangeActivePlayer += HandleSwapPlayer;
                }
                
            }
        }

        private void OnDestroy() {
            _player.onChangeActivePlayer -= HandleSwapPlayer;
        }

        private void HandleSwapPlayer(bool yourTurn) {
            _toolbar.SetActive(yourTurn);
        }
    }
}