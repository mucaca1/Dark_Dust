using UnityEngine;

namespace Game {
    [CreateAssetMenu(fileName = "new playgroud card", menuName = "DarkDust/Playground Card", order = 0)]
    public class PlaygroundCard : ScriptableObject {
        private PlaygroundCardType _cardType;
        private Sprite _frontImage;
        private Sprite _backImage;
        private int _cardCount;
    }
}