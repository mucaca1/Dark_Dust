using System;
using Mirror;
using UnityEngine;

namespace Game.Cards.PlaygroundCards {
    public class PlaygroundCard : PlaygroundPart {
        [SerializeField] private MeshRenderer backImage;
        [SerializeField] private MeshRenderer frontImage;

        private SandCard _sandCard = null;
        private PlaygroundCardType _cardType;
        private CardDirection _cardDirection;
        
        public static event Action<PlaygroundCard> onDustNeedToCreate;

        public void SetData(PlaygroundCardData cardData, Vector3 startPosition) {
            playgroundStartPosition = startPosition;
            SetData(cardData);
        }

        public void SetData(PlaygroundCardData cardData) {
            cardName = cardData.name;
            backImage.material.mainTexture = cardData.BackImage.texture;
            frontImage.material.mainTexture = cardData.FrontImage.texture;
            _cardType = cardData.CardType;
            _cardDirection = cardData.CardDirection;
        }

        public void SetData(PlaygroundCard cardData) {
            backImage.material.mainTexture = cardData.backImage.material.mainTexture;
            frontImage.material.mainTexture = cardData.frontImage.material.mainTexture;
            _cardType = cardData._cardType;
            _cardDirection = cardData._cardDirection;
        }

        public void AddSand() {
            if (_sandCard == null)
                onDustNeedToCreate?.Invoke(this);
            else
                _sandCard.AddDust();
        }

        public void RemoveSand(int count = 1) {
            if (_sandCard == null) return;
            _sandCard.RemoveDust();
        }

        public PlaygroundCardType CardType => _cardType;
    }
}