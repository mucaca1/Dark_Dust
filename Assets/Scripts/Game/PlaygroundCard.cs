using Mirror;
using UnityEngine;

namespace Game {
    public class PlaygroundCard : NetworkBehaviour {

        [SerializeField] private MeshRenderer backImage;
        [SerializeField] private MeshRenderer frontImage;
        
        private bool _isRevealed = false;
        private int _sandCount = 0;
        private PlaygroundCardType _cardType;
        private CardDirection _cardDirection;
        
        
        public void SetData(PlaygroundCardData cardData) {
            backImage.material.mainTexture = cardData.BackImage.texture;
            frontImage.material.mainTexture = cardData.FrontImage.texture;
            _cardType = cardData.CardType;
            _cardDirection = cardData.CardDirection;
        }
    }
}