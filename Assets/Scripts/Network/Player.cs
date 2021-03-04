using System;
using Game;
using Game.Cards.PlaygroundCards;
using Mirror;
using UnityEngine;

namespace Network {
    public class Player : NetworkBehaviour {
        [SerializeField] private Renderer _renderer = null;

        [SyncVar(hook = nameof(HandleChangePlayer))]
        private bool _yourTurn = false;

        [field: SyncVar] public string PlayerName { get; set; } = "PlayerName";

        [field: SyncVar(hook = nameof(UpdatePlayerColor))]
        public Color PlayerColor { get; set; } = Color.black;

        private PlaygroundCard _position = null;

        public event Action<bool> onChangeActivePlayer;

        private void UpdatePlayerColor(Color oldColor, Color newColor) {
            _renderer.material.color = newColor;
        }

        public override void OnStartAuthority() {
            CmdSetStartPosition();
        }

        private void HandleChangePlayer(bool oldValue, bool newValue) {
            onChangeActivePlayer?.Invoke(newValue);
        }

        #region Server

        [Server]
        public void EndTurn() {
            _yourTurn = false;
        }

        [Server]
        public void StartTurn() {
            _yourTurn = true;
        }

        [Server]
        private void SetStartPosition() {
            GameManager manager = FindObjectOfType<GameManager>();
            _position = manager.GetStartCard();
            transform.position = _position.GetNextPlayerPosition(connectionToClient.connectionId);
        }

        [Server]
        private void SetNewPosition(PlaygroundCard card) {
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (!gameManager.IsPlayerTurn(this)) return;
            if (_position != null) {
                if (!card.CanMoveToThisPart(_position)) return;
                _position.LeavePart(connectionToClient.connectionId);
            }

            _position = card;
            transform.position = _position.GetNextPlayerPosition(connectionToClient.connectionId);
            gameManager.DoAction();
        }

        [Command]
        private void CmdSetStartPosition() {
            SetStartPosition();
        }

        [Command]
        public void CmdGoToPosition(PlaygroundCard card) {
            SetNewPosition(card);
        }

        #endregion

        #region Client

        [Client]
        public void GoToPosition(PlaygroundCard card) {
            if (!hasAuthority) return;

            CmdGoToPosition(card);
        }

        #endregion
    }
}