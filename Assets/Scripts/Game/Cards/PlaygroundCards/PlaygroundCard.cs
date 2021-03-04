using System;
using Mirror;
using UnityEngine;

namespace Game.Cards.PlaygroundCards {
    public class PlaygroundCard : PlaygroundPart {
        [SerializeField] private MeshRenderer backImage;
        [SerializeField] private MeshRenderer frontImage;

        [SerializeField] private GameObject dustCardPrefab;

        private SandCard _sandCard = null;
        private PlaygroundCardType _cardType;
        private CardDirection _cardDirection;

        public static event Action onDustRemove;

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

        public PlaygroundCardType CardType => _cardType;

        #region Server
        
        [Server]
        public void AddSand() {
            if (_sandCard == null)
                CreateSandCard(this);
            else
                _sandCard.AddDust();
        }

        [Server]
        public void RemoveSand(int count = 1) {
            if (_sandCard == null) return;
            _sandCard.RemoveDust(count);
        }
        
        [Server]
        private void CreateSandCard(PlaygroundCard card) {
            GameManager manager = FindObjectOfType<GameManager>();
            GameObject dust = Instantiate(dustCardPrefab, Vector3.zero,
                Quaternion.identity);

            SandCard sandCard = dust.GetComponent<SandCard>();
            _sandCard = sandCard;
            sandCard.onDestroy += DestroySand;

            sandCard.SetStartPosition(manager.GetPlaygroundStartPosition());
            sandCard.SetIndexPosition(card.GetIndexPosition());
            sandCard.UpdatePosition();

            NetworkServer.Spawn(dust);
        }

        [Server]
        private void DestroySand(SandCard card) {
            _sandCard.onDestroy -= DestroySand;
            _sandCard = null;
            NetworkServer.Destroy(card.gameObject);
        }
        
        [Server]
        public bool IsDustNull() {
            return _sandCard == null;
        }

        #endregion
    }
}