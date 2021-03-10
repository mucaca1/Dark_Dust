using System;
using Game.Characters;
using Mirror;
using Network;
using TMPro;
using UnityEngine;

namespace Game.UI {
    public class CharacterInfoView : MonoBehaviour {
        [SerializeField] private TMP_Text characterName = null;
        [SerializeField] private TMP_Text waterCapacity = null;
        [SerializeField] private TMP_Text abilityDescription = null;

        private Player _player = null;

        private void Update() {
            if (_player == null) {
                _player = NetworkClient.connection?.identity?.GetComponent<Player>();
            }

            if (_player != null) {
                Character character = _player.GetComponent<Character>();
                characterName.text = character.CharacterName;
                waterCapacity.text = $"Water capacity: {character.MAXWater}";
                abilityDescription.text = character.AbilityDescription;
            }
        }
    }
}