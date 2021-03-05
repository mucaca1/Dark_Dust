using System;
using Game.Cards.PlaygroundCards;
using Mirror;
using Network;
using UnityEngine;

namespace Game.Cards {
    public class PlaygroundPart : NetworkBehaviour {
        [SerializeField] private Transform[] positionToStay;
        [SyncVar] protected string cardName;
        [SyncVar] private bool _isRevealed = false;

        protected Vector3 position;
        protected Vector3 playgroundStartPosition;
        protected Vector2 indexPosition;

        protected float playgroundCardOffsetY = 0f;
        private float playgroundCardSize = 1f;
        private float playgroundCardOffset = .1f;

        private int[] _stayingPositionPlayer = new int[5];
        public bool IsRevealed => _isRevealed;

        [ServerCallback]
        private void Awake() {
            for (int i = 0; i < _stayingPositionPlayer.Length; i++) {
                _stayingPositionPlayer[i] = -1;
            }
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

        public Vector3 GetPosition() {
            return position;
        }

        public void SetPosition(Vector3 position) {
            position = position;
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

        public void UpdateRotation() {
            if (_isRevealed)
                gameObject.transform.Rotate(0.0f, 0.0f, 180.0f, Space.Self);
        }

        public void ExcavateCard() {
            _isRevealed = true;
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

        #endregion
    }
}