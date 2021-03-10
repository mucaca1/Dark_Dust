using UnityEngine;

namespace Game.Characters {
    [CreateAssetMenu(fileName = "NewCharacterName", menuName = "DarkDust/Create New Character", order = 0)]
    public class CharacterData : ScriptableObject {
        public string characterName = "Character Name";

        public int startWater = -1;
        public int maxWater = -1;

        [TextArea] public string abilityDescription = "Ability Description";
    }
}