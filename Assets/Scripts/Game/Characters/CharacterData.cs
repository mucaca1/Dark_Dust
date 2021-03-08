﻿using Game.Characters.Ability;
using UnityEngine;

namespace Game.Characters {
    [CreateAssetMenu(fileName = "NewCharacterName", menuName = "DarkDust/Create New Character", order = 0)]
    public class CharacterData : ScriptableObject {

        public string characterName = "Character Name";
        
        public int water = -1;

        public string[] abilityNames;

        [TextArea]
        public string abilityDescription = "Ability Description";
    }
}