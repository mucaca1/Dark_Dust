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

        public bool CanMoveFromCard(Character character, PlaygroundCard sourceCard, PlaygroundCard destinationCard) {
            if (character.Ability == AbilityType.Climber) return true;
            if (!destinationCard.CanMoveToThisPart()) return false;

            foreach (Character ch in sourceCard.GetCharacters()) {
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

        public void DoSpecialAction(Character sourceCharacter, AbilityType ability, GameObject selectedObject,
            int value, PlaygroundCard source, PlaygroundCard destination) {
            Character selectedCharacter = null;
            switch (ability) {
                case AbilityType.WaterCarrier:
                    if (value == 0) return;
                    if (selectedObject.TryGetComponent(out PlaygroundCard card)) {
                        if (card.CardType == PlaygroundCardType.Water) {
                            if (sourceCharacter.hasAuthority) {
                                sourceCharacter.AddWater(2);
                                GameManager.Instance.DoAction();
                            }
                        }
                    }

                    if (selectedObject.TryGetComponent(out selectedCharacter)) {
                        int waterToAdd = sourceCharacter.Water - value;
                        sourceCharacter.RemoveWater(value);
                        GameManager.Instance.CmdAddWater(selectedCharacter, waterToAdd);
                        GameManager.Instance.DoAction();
                    }

                    break;

                case AbilityType.Climber:
                    if (selectedObject.TryGetComponent(out selectedCharacter)) {
                        GameManager.Instance.CmdMoveCharacter(selectedCharacter, destination);
                        sourceCharacter.CmdDoAction(PlayerAction.WALK, destination); // DoAction contain DoAction.
                    }

                    break;
            }
        }

        public bool HasAuraAbility(Character character) {
            return character.Ability == AbilityType.Archeologist || character.Ability == AbilityType.Explorer;
        }
    }
}