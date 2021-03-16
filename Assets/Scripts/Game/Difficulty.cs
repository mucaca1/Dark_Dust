using System.Collections.Generic;
using UnityEngine;

namespace Game {
    [CreateAssetMenu(fileName = "New Difficulty", menuName = "DarkDust/Difficulty", order = 0)]
    public class Difficulty : ScriptableObject {
        public int players = 0;
        public int[] dustPower = new int[0];
    }
}