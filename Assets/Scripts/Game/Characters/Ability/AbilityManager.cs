using System.Collections.Generic;
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

        public bool CanUsePlaygroundCardAsPlayer(Character character) {
            return character.Ability == AbilityType.WaterCarrier;
        }

        public bool CanPickUpWater(Character character, PlaygroundCard destination) {
            return (character.Ability == AbilityType.WaterCarrier && character.Position == destination &&
                    destination.CardType == PlaygroundCardType.Water && destination.IsRevealed);
        }

        public bool CanGiveWaterToSomePlayer(Character character, Character[] characters, PlaygroundCard destination) {
            if (character.Ability != AbilityType.WaterCarrier) return false;
            foreach (Character otherCharacter in characters) {
                if (otherCharacter == character) continue;

                if (otherCharacter.Position == destination && (character.Position == destination ||
                                                               destination.CanSeeThisCard(character.Position))) {
                    return true;
                }
            }

            return false;
        }

        public void DoSpecialAction(Character sourceCharacter, AbilityType ability, GameObject selectedObject, int value) {
            switch (ability) {
                case AbilityType.WaterCarrier:
                    if (selectedObject.TryGetComponent(out PlaygroundCard card)) {
                        if (card.CardType == PlaygroundCardType.Water) {
                            if (sourceCharacter.hasAuthority) {
                                sourceCharacter.AddWater(2);
                                GameManager.Instance.DoAction();
                            }
                        }
                    }

                    if (selectedObject.TryGetComponent(out Character destinationCharacter)) {
                        int waterToAdd = sourceCharacter.Water - value;
                        sourceCharacter.RemoveWater(value);
                        GameManager.Instance.CmdAddWater(destinationCharacter, waterToAdd);
                        GameManager.Instance.DoAction();
                    }

                    break;
            }
        }
    }
}