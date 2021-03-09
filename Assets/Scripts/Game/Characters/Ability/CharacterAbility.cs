using System.Collections.Generic;
using UnityEngine;

namespace Game.Characters.Ability {
    public abstract class CharacterAbility : MonoBehaviour {

        private string _specialDescription = "Desc";

        public string SpecialDescription => _specialDescription;
        
        public abstract List<Character> GetAllEnabledCharacters();
    }
}