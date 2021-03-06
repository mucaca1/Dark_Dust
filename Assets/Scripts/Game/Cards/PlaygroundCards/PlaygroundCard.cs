using System;
using System.Collections.Generic;
using Game.Characters;
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

        [SyncVar(hook = nameof(UpdateRotation))]
        private bool _isRevealed = false;

        [SyncVar(hook = nameof(HandleSandCount))]
        private int _sandCount = 0;

        [SerializeField] private SandCard dustCardPrefab;

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

        private Character[] _stayingPositionPlayer = new Character[5];
        public bool IsRevealed => _isRevealed;

        public static event Action onAddSand;
        public static event Action onRemoveSand;

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
                _stayingPositionPlayer[i] = null;
            }
        }

        public void UpdateRotation(bool oldValue, bool newValue) {
            if (newValue)
                cardReference.Rotate(0.0f, 0.0f, 180.0f, Space.Self);
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
            
            for (var i = 0; i < _stayingPositionPlayer.Length; i++) {
                if (_stayingPositionPlayer[i] == null) continue;

                _stayingPositionPlayer[i].gameObject.transform.position = positionToStay[i].position;
            }
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
        public void ExcavateCard() {
            _isRevealed = true;
        }

        [Server]
        public bool IsCharacterHere(Character character) {
            for (int i = 0; i < _stayingPositionPlayer.Length; i++) {
                if (_stayingPositionPlayer[i] == character) {
                    return true;
                }
            }

            return false;
        }

        [Server]
        public Vector3 GetNextPlayerPosition(Character character) {
            for (int i = 0; i < _stayingPositionPlayer.Length; i++) {
                if (_stayingPositionPlayer[i] == null) {
                    _stayingPositionPlayer[i] = character;
                    return positionToStay[i].position;
                }
            }

            return Vector3.zero;
        }

        [Server]
        public void LeavePart(Character character) {
            for (int i = 0; i < _stayingPositionPlayer.Length; i++) {
                if (_stayingPositionPlayer[i] == character)
                    _stayingPositionPlayer[i] = null;
            }
        }

        [Server]
        public bool CanMoveToThisPart(PlaygroundCard from) {
            if (_sandCount > 1) return false;

            if (from.indexPosition.x != indexPosition.x && from.indexPosition.y != indexPosition.y) return false;

            if (Mathf.Abs(from.indexPosition.x - indexPosition.x) > 1 ||
                Mathf.Abs(from.indexPosition.y - indexPosition.y) > 1) return false;
            return true;
        }


        [Server]
        public void RemoveSand(int count = 1) {
            _sandCount = Mathf.Max(0, _sandCount - count);
            onRemoveSand?.Invoke();
        }

        [Server]
        public void AddSand() {
            ++_sandCount;
            onAddSand?.Invoke();
        }

        [Server]
        public bool IsDust() {
            return _sandCount != 0;
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

        #region Client

        [Client]
        private void HandleSandCount(int oldDustCount, int newDustCount) {
            if (newDustCount == 1) {
                GameManager manager = FindObjectOfType<GameManager>();
                GameObject dust = Instantiate(dustCardPrefab.gameObject, sandSpawnerReference.position,
                    Quaternion.identity);
                // Refactor append as child
                dust.transform.parent = sandSpawnerReference;
            }

            foreach (Transform children in sandSpawnerReference) {
                SandCard sandCard = children.GetComponent<SandCard>();
                sandCard.HandleDustValue(newDustCount);
            }
        }

        #endregion
    }
}