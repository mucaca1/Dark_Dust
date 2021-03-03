using UnityEngine;

namespace Game {
    [CreateAssetMenu(fileName = "DarkDust", menuName = "new playgroud card", order = 0)]
    public class PlaygroundCard : ScriptableObject {
        private PlaygroundCardType _cardType;
        private Sprite _frontImage;
        private Sprite _backImage;
        private int _cardCount;
    }
}