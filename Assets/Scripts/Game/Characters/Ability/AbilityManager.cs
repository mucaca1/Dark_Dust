using Game.Cards.PlaygroundCards;
using UnityEngine;

namespace Game.Characters.Ability {
    public class AbilityManager {
        public int RemoveExtraSandAbility(Character character) {
            if (character.Ability == AbilityType.Archeologist) {
                return 1;
            }

            return 0;
        }

        public bool CanMoveHorizontal(Character character, PlaygroundCard destination) {
            if (character.Ability != AbilityType.Explorer) return false;
            
            return Mathf.Abs(character.Position.GetIndexPosition().x - destination.GetIndexPosition().x) == 1 &&
                   Mathf.Abs(character.Position.GetIndexPosition().y - destination.GetIndexPosition().y) == 1;
        }
    }
}