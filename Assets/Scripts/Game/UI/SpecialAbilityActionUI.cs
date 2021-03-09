using Game.Characters.Ability;
using TMPro;
using UnityEngine;

namespace Game.UI {
    public class SpecialAbilityActionUI : MonoBehaviour {

        [SerializeField] private TMP_Text _description = null;

        public void Initialize(CharacterAbility ability) {
            _description.text = ability.SpecialDescription;
            
            
        }
    }
}