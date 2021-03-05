using System;
using Mirror;
using Network;
using UnityEngine;

namespace Game.Cards.PlaygroundCards {
    public class PlaygroundCard : NetworkBehaviour {
        [SerializeField] private MeshRenderer backImage;
        [SerializeField] private MeshRenderer frontImage;
        [SerializeField] private Transform[] positionToStay;
        [SerializeField] private Transform cardReference = null;
        [SerializeField] private Transform sandSpawnerReference = null;
        [SyncVar] protected string cardName;
        [SyncVar] private bool _isRevealed = false;

        [SerializeField] private GameObject dustCardPrefab;

        private SandCard _sandCard = null;
        private PlaygroundCardType _cardType;
        private CardDirection _cardDirection;

        public PlaygroundCardType CardType => _cardType;
        public CardDirection CardDirection => _cardDirection;

        protected Vector3 position;
        protected Vector3 playgroundStartPosition;
        protected Vector2 indexPosition;

        protected float playgroundCardOffsetY = 0f;
        private float playgroundCardSize = 1f;
        private float playgroundCardOffset = .1f;

        private int[] _stayingPositionPlayer = new int[5];
        public bool IsRevealed => _isRevealed;

        public Vector3 GetPosition() {
            return position;
        }

        public void SetPosition(Vector3 position) {
            this.position = position;
        }

        public Vector2 GetIndexPosition() {
            return indexPosition;
        }

        public void SetIndexPosition(Vector2 position) {
            indexPosition = position;
        }

        public string GetCardName() {
            return cardName;
        }

        public void SetStartPosition(Vector3 startPosition) {
            playgroundStartPosition = startPosition;
        }

        [ServerCallback]
        private void Awake() {
            for (int i = 0; i < _stayingPositionPlayer.Length; i++) {
                _stayingPositionPlayer[i] = -1;
            }
        }

        public void UpdateRotation() {
            if (_isRevealed)
                cardReference.Rotate(0.0f, 0.0f, 180.0f, Space.Self);
        }

        public void ExcavateCard() {
            _isRevealed = true;
        }

        public void UpdatePosition() {
            Vector3 pos = new Vector3(
                playgroundStartPosition.x + playgroundCardOffset + (indexPosition.x * playgroundCardOffset) +
                playgroundCardSize / 2 + (indexPosition.x * playgroundCardSize),
                playgroundStartPosition.y + 0f + playgroundCardOffsetY,
                playgroundStartPosition.z + playgroundCardOffset + (indexPosition.y * playgroundCardOffset) +
                playgroundCardSize / 2 + (indexPosition.y * playgroundCardSize)
            );
            gameObject.transform.position = pos;
            SetPosition(pos);
        }

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


        #region Server

        [Server]
        public bool PlayerStayHere(int playerId) {
            for (int i = 0; i < _stayingPositionPlayer.Length; i++) {
                if (_stayingPositionPlayer[i] == playerId) {
                    return true;
                }
            }

            return false;
        }

        [Server]
        public Vector3 GetNextPlayerPosition(int playerId) {
            for (int i = 0; i < _stayingPositionPlayer.Length; i++) {
                if (_stayingPositionPlayer[i] == -1) {
                    _stayingPositionPlayer[i] = playerId;
                    return positionToStay[i].position;
                }
            }

            return Vector3.zero;
        }

        [Server]
        public void LeavePart(int playerId) {
            for (int i = 0; i < _stayingPositionPlayer.Length; i++) {
                if (_stayingPositionPlayer[i] == playerId)
                    _stayingPositionPlayer[i] = -1;
            }
        }

        [Server]
        public bool CanMoveToThisPart(PlaygroundCard from) {
            if (_sandCard != null) {
                if (_sandCard.DustValue > 1) return false;
            }

            if (from.indexPosition.x != indexPosition.x && from.indexPosition.y != indexPosition.y) return false;

            if (Mathf.Abs(from.indexPosition.x - indexPosition.x) > 1 ||
                Mathf.Abs(from.indexPosition.y - indexPosition.y) > 1) return false;
            return true;
        }

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
            GameObject dust = Instantiate(dustCardPrefab, sandSpawnerReference.position,
                Quaternion.identity);

            SandCard sandCard = dust.GetComponent<SandCard>();
            _sandCard = sandCard;
            sandCard.onDestroy += DestroySand;

            // Refactor append as child
            dust.transform.parent = sandSpawnerReference;

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

        [Server]
        public void SwapCards(PlaygroundCard destinationCard) {
            Vector2 destinationPosition = destinationCard.GetIndexPosition();
            destinationCard.SetIndexPosition(indexPosition);
            indexPosition = destinationPosition;
            
            destinationCard.UpdatePosition();
            UpdatePosition();
        }

        #endregion
    }
}