using UnityEngine;

namespace Game {
    [CreateAssetMenu(fileName = "new playgroud card", menuName = "DarkDust/Playground Card", order = 0)]
    public class PlaygroundCard : ScriptableObject {
        [SerializeField] private PlaygroundCardType _cardType;
        [SerializeField] private Sprite _frontImage;
        [SerializeField] private Sprite _backImage;
        [SerializeField] private int _cardCount;
    }
}