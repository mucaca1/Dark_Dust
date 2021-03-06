using System;
using Game.Characters.Ability;
using Mirror;
using UnityEngine;

namespace Game.Characters {
    public class Character : NetworkBehaviour {
        [SyncVar] private string _characterName;
        [SyncVar] private int _water;
        [SyncVar] private string _abilityDescription;
        
        private int _maxWater;

        private CharacterAbility[] _abilities = new CharacterAbility[0];

        public static event Action<Character> onCharacterDie;

        #region Server

        [Server]
        public void InitializePlayer(CharacterData data) {
            _characterName = data.characterName;
            _maxWater = data.water;
            _abilityDescription = data.abilityDescription;
            _abilities = data.ability;
        }

        [Server]
        public void SetWater(int water) {
            _water = Mathf.Max(_maxWater, _water + water);
        }

        [Server]
        public void DrinkWater() {
            _water = Mathf.Max(0, _water - 1);
            if (_water != 0) return;
            onCharacterDie?.Invoke(this);
        }

        #endregion
    }
}