using System;
using Game.Characters;
using Mirror;
using Network;
using TMPro;
using UnityEngine;

namespace Game.UI {
    public class GameManagerView : MonoBehaviour {
        [SerializeField] private TMP_Text _characterNameText = null;
        [SerializeField] private TMP_Text _collectedPartsText = null;
        [SerializeField] private TMP_Text _playerWaterText = null;
        
        private Player _player = null;

        private void Start() {
            GameManager.Instance.onTakedItemsIncrease += HandleItemsUpdated;
            Character.onWaterValueChanged += HandlePlayerWater;
            Character.onCharacterInitialized += HandleCreatedCharacter;
        }

        private void Update() {
            if (_player == null) {
                _player = NetworkClient.connection?.identity?.GetComponent<Player>();
                if (_player != null) {
                    _characterNameText.text = _player.GetComponent<Character>().CharacterName;
                }
            }
        }

        private void OnDestroy() {
            GameManager.Instance.onTakedItemsIncrease -= HandleItemsUpdated;
            Character.onWaterValueChanged -= HandlePlayerWater;
        }

        private void HandleItemsUpdated(int count) {
            _collectedPartsText.text = $"Collected items: {count}/4";
        }

        private void HandlePlayerWater(Character character, int newValue, int maxValue) {
            if (!character.connectionToClient.identity.hasAuthority) return;
            _playerWaterText.text = $"Water: {newValue}/{maxValue}";
        }
        
        private void HandleCreatedCharacter(Character character) {
            if (character.connectionToClient.connectionId != NetworkClient.connection.connectionId) return;

            _characterNameText.text = character.CharacterName;
        }
    }
}