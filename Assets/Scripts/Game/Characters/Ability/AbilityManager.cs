using System;
using Game.Cards.PlaygroundCards;
using UnityEngine;

namespace Game.Characters.Ability {
    public class AbilityManager {
        public event Action onDoAction;
        public static event Action<Character, int> onChangeWater;
        public static event Action<Character, PlaygroundCard> onPositionChange;
        public event Action onWeakenStorm;
        public event Action<int> onShowCards;

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

        public bool CanClockOnTornado(Character character) {
            return character.Ability == AbilityType.Meteorologist;
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
            int value, PlaygroundCard source, PlaygroundCard destination, int index) {
            Character selectedCharacter = null;
            switch (ability) {
                case AbilityType.WaterCarrier:
                    if (value == 0) return;
                    if (selectedObject.TryGetComponent(out PlaygroundCard card)) {
                        if (card.CardType == PlaygroundCardType.Water) {
                            if (sourceCharacter.hasAuthority) {
                                onChangeWater?.Invoke(sourceCharacter, 2);
                                onDoAction?.Invoke();
                            }
                        }
                    }

                    if (selectedObject.TryGetComponent(out selectedCharacter)) {
                        int waterToAdd = sourceCharacter.Water - value;
                        onChangeWater?.Invoke(sourceCharacter, value);
                        onChangeWater?.Invoke(sourceCharacter, waterToAdd);
                        onDoAction?.Invoke();
                    }

                    break;

                case AbilityType.Climber:
                    if (selectedObject.TryGetComponent(out selectedCharacter)) {
                        onPositionChange?.Invoke(selectedCharacter, destination);
                        onPositionChange?.Invoke(sourceCharacter, destination);
                        onDoAction?.Invoke();
                    }

                    break;
                
                case AbilityType.Meteorologist:
                    if (index == -1) {
                        if (GameManager.Instance.ActualStormTickMark == 0) return;
                        onWeakenStorm?.Invoke();
                    }
                    else {
                        onShowCards?.Invoke(index);
                    }
                    
                    onDoAction?.Invoke();
                    break;
                
                case AbilityType.Navigator:
                    if (selectedObject.TryGetComponent(out selectedCharacter)) {
                        sourceCharacter.CharacterInControl = selectedCharacter;
                        onDoAction?.Invoke();
                    }

                    break;
            }
        }

        public bool HasAuraAbility(Character character) {
            return character.Ability == AbilityType.Archeologist || character.Ability == AbilityType.Explorer;
        }

        public bool CanMoveWithCharacter(Character character) {
            return character.Ability == AbilityType.Navigator;
        }
    }
}