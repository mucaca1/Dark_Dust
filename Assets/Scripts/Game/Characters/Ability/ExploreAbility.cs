using Game.Cards.PlaygroundCards;
using UnityEngine;

namespace Game.Characters.Ability {
    public class ExploreAbility : CharacterAbility {

        public bool CanSeeToPart(PlaygroundCard source, PlaygroundCard destination) {
            return Mathf.Abs(source.GetIndexPosition().x - destination.GetIndexPosition().x) == 1 &&
                   Mathf.Abs(source.GetIndexPosition().y - destination.GetIndexPosition().y) == 1;
        }
    }
}