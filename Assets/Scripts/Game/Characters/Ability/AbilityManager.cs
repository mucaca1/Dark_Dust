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

        public bool CanMoveToCard(Character character) {
            return character.Ability == AbilityType.Climber;
        }

        public bool CanMoveFromCard(Character character, PlaygroundCard card) {
            if (character.Ability == AbilityType.Climber) return true;

            foreach (Character ch in card.GetCharacters()) {
                if (ch.Ability == AbilityType.Climber) {
                    return true;
                }
            }

            return false;
        }
    }
}