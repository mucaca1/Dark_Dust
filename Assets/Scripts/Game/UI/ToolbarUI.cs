using System;
using Mirror;
using Network;
using TMPro;
using UnityEngine;

namespace Game.UI {
    public class ToolbarUI : MonoBehaviour {
        [SerializeField] private GameObject _toolbar = null;
        [SerializeField] private GameObject _activePlayerToolbar = null;
        [SerializeField] private TMP_Text _activePlayerName = null;

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

        private void HandleSwapPlayer(bool yourTurn, string playerName) {
            _toolbar.SetActive(yourTurn);
            _activePlayerToolbar.SetActive(!yourTurn);
            _activePlayerName.text = playerName + " is in command.";
        }
    }
}