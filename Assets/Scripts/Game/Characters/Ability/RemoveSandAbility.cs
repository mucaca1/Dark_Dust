using System.Collections.Generic;

namespace Game.Characters.Ability {
    public class RemoveSandAbility : CharacterAbility {
        private int _extraSandToRemove = 1;

        public int ExtraSandToRemove => _extraSandToRemove;
    }
}