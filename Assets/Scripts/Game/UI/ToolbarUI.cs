using System;
using Mirror;
using Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
    public class ToolbarUI : MonoBehaviour {
        [SerializeField] private GameObject _toolbar = null;
        [SerializeField] private GameObject _activePlayerToolbar = null;
        [SerializeField] private TMP_Text _activePlayerName = null;

        [SerializeField] private Button walkButton = null;
        [SerializeField] private Button excavateButton = null;
        [SerializeField] private Button removeSandButton = null;
        [SerializeField] private Button pickUpAPartButton = null;

        private Player _player = null;

        private void Start() {
            walkButton.onClick.AddListener(WalkAction);
            excavateButton.onClick.AddListener(ExcavateAction);
            removeSandButton.onClick.AddListener(RemoveSandAction);
            pickUpAPartButton.onClick.AddListener(PickUpAPartAction);
        }

        private void Update() {
            if (_player == null) {
                _player = NetworkClient.connection?.identity?.GetComponent<Player>();
                if (_player != null) {
                    _player.onChangeActivePlayer += HandleSwapPlayer;
                    HandleSwapPlayer(_player.IsYourTurn, _player.PlayerName);
                }
            }
        }

        private void OnDestroy() {
            _player.onChangeActivePlayer -= HandleSwapPlayer;
        }

        private void WalkAction() {
            _player.Character.gameObject.GetComponent<PlayerController>().SetPlayerAction(PlayerAction.WALK);
        }

        private void ExcavateAction() {
            _player.Character.gameObject.GetComponent<PlayerController>().SetPlayerAction(PlayerAction.EXCAVATE);
        }

        private void RemoveSandAction() {
            _player.Character.gameObject.GetComponent<PlayerController>().SetPlayerAction(PlayerAction.REMOVE_SAND);
        }

        private void PickUpAPartAction() {
            _player.Character.gameObject.GetComponent<PlayerController>().SetPlayerAction(PlayerAction.PICK_UP_A_PART);
        }

        private void HandleSwapPlayer(bool yourTurn, string playerName) {
            _toolbar.SetActive(yourTurn);
            _activePlayerToolbar.SetActive(!yourTurn);
            _activePlayerName.text = playerName + " is in command.";

            if (yourTurn) {
                walkButton.interactable = false;
                excavateButton.interactable = true;
                removeSandButton.interactable = true;
                pickUpAPartButton.interactable = true;
                WalkAction();
            }
        }
    }
}