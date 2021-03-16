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
            Character.onCharacterInitialized += HandleCreatedCharacter;


            _player = NetworkClient.connection.identity.GetComponent<Player>();

            _characterNameText.text = _player.GetComponent<Character>().CharacterName;
            _playerWaterText.text =
                $"Water: {_player.GetComponent<Character>().Water}/{_player.GetComponent<Character>().MAXWater}";
        }

        private void OnDestroy() {
            GameManager.Instance.onTakedItemsIncrease -= HandleItemsUpdated;
            Character.onWaterValueChanged -= HandlePlayerWater;
            Character.onCharacterInitialized -= HandleCreatedCharacter;
        }

        private void HandleItemsUpdated(int count) {
            _collectedPartsText.text = $"Collected items: {count}/4";
        }

        private void HandlePlayerWater(Character character, int newValue, int maxValue) {
            if (!character.GetComponent<Player>().hasAuthority) return;
            _playerWaterText.text = $"Water: {newValue}/{maxValue}";
        }

        private void HandleCreatedCharacter(Character character) {
            if (!character.GetComponent<Player>().hasAuthority) return;

            _characterNameText.text = character.CharacterName;
            Character.onWaterValueChanged += HandlePlayerWater;
        }
    }
}