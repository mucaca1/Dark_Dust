using System;
using Mirror;
using Network;
using TMPro;
using UnityEngine;

namespace Game.UI {
    public class GameManagerView : MonoBehaviour {
        [SerializeField] private TMP_Text _collectedPartsText = null;
        [SerializeField] private TMP_Text _playerWaterText = null;
        [SerializeField] private GameManager _gameManager = null;

        private Player _player = null;

        private void Start() {
            _gameManager.onTakedItemsIncrease += HandleItemsUpdated;
        }

        private void Update() {
            if (_player == null) {
                _player = NetworkClient.connection?.identity?.GetComponent<Player>();
                if (_player != null)
                    _player.Character.onWaterValueChanged += HandlePlayerWater;
            }
        }

        private void OnDestroy() {
            _gameManager.onTakedItemsIncrease -= HandleItemsUpdated;
            _player.Character.onWaterValueChanged -= HandlePlayerWater;
        }

        private void HandleItemsUpdated(int count) {
            _collectedPartsText.text = $"Collected items: {count}/4";
        }

        private void HandlePlayerWater(int newValue, int maxValue) {
            _playerWaterText.text = $"Water: {newValue}/{maxValue}";
        }
    }
}