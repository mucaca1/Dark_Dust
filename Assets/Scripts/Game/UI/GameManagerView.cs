using System;
using Mirror;
using Network;
using TMPro;
using UnityEngine;

namespace Game.UI {
    public class GameManagerView : MonoBehaviour {

        [SerializeField] private TMP_Text _collectedPartsText = null;
        [SerializeField] private TMP_Text _playerWaterText = null;

        private Player _player = null;
        
        private void Start() {
            GameManager gm = FindObjectOfType<GameManager>();
            gm.onTakedItemsIncrease += HandleItemsUpdated;
        }

        private void Update() {
            if (_player == null) {
                _player = NetworkClient.connection?.identity?.GetComponent<Player>();
                _player.Character.onWaterValueChanged += HandlePlayerWater;
            }
        }

        private void OnDestroy() {
            GameManager gm = FindObjectOfType<GameManager>();
            gm.onTakedItemsIncrease -= HandleItemsUpdated;
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