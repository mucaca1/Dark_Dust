using Game.Characters;
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

        [SerializeField] private TMP_Text _stepsAvaibleText = null;

        [SerializeField] private Button walkButton = null;
        [SerializeField] private Button excavateButton = null;
        [SerializeField] private Button removeSandButton = null;
        [SerializeField] private Button pickUpAPartButton = null;

        [SerializeField] private Toggle _specialActivityToggle = null;

        private Player _player = null;
        private PlayerController _controller = null;

        private void Start() {
            walkButton.onClick.AddListener(WalkAction);
            excavateButton.onClick.AddListener(ExcavateAction);
            removeSandButton.onClick.AddListener(RemoveSandAction);
            pickUpAPartButton.onClick.AddListener(PickUpAPartAction);
            _specialActivityToggle.onValueChanged.AddListener(HandleSpecialAbilityToggle);
            GameManager.onAvaibleStepsChanged += HandleActionCounter;
        }

        private void Update() {
            if (_player == null) {
                _player = NetworkClient.connection?.identity?.GetComponent<Player>();
                if (_player != null) {
                    _player.onChangeActivePlayer += HandleSwapPlayer;
                    _controller = _player.GetComponent<PlayerController>();
                    _specialActivityToggle.gameObject.SetActive(!_player.AbilityManager.HasAuraAbility(_player.GetComponent<Character>()));
                    HandleSwapPlayer(_player.IsYourTurn, GameManager.Instance.ActivePlayerName);
                }
            }
        }

        private void OnDestroy() {
            _player.onChangeActivePlayer -= HandleSwapPlayer;
            GameManager.onAvaibleStepsChanged -= HandleActionCounter;

            walkButton.onClick.RemoveListener(WalkAction);
            excavateButton.onClick.RemoveListener(ExcavateAction);
            removeSandButton.onClick.RemoveListener(RemoveSandAction);
            pickUpAPartButton.onClick.RemoveListener(PickUpAPartAction);
            _specialActivityToggle.onValueChanged.RemoveListener(HandleSpecialAbilityToggle);
        }

        private void WalkAction() {
            if (_controller != null)
                _controller.SetPlayerAction(PlayerAction.WALK);
        }

        private void ExcavateAction() {
            _controller.SetPlayerAction(PlayerAction.EXCAVATE);
        }

        private void RemoveSandAction() {
            _controller.SetPlayerAction(PlayerAction.REMOVE_SAND);
        }

        private void PickUpAPartAction() {
            _controller.SetPlayerAction(PlayerAction.PICK_UP_A_PART);
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

        private void HandleActionCounter(int count) {
            _stepsAvaibleText.text = $"Actions: {count}";
        }

        private void HandleSpecialAbilityToggle(bool value) {
            _controller.SetSpecialAction(value);
        }
    }
}