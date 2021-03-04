using System;
using Game;
using Game.Cards.PlaygroundCards;
using Mirror;
using Telepathy;
using UnityEngine;

namespace Network {
    public class Player : NetworkBehaviour {
        [SerializeField] private Renderer _renderer = null;

        [field: SyncVar] public string PlayerName { get; set; } = "PlayerName";

        [field: SyncVar(hook = nameof(UpdatePlayerColor))]
        public Color PlayerColor { get; set; } = Color.black;

        private PlaygroundCard _position = null;

        private void UpdatePlayerColor(Color oldColor, Color newColor) {
            _renderer.material.color = newColor;
        }

        public override void OnStartAuthority() {
            CmdSetStartPosition();
        }

        #region Server

        [Server]
        private void SetStartPosition() {
            GameManager manager = FindObjectOfType<GameManager>();
            _position = manager.GetStartCard();
            transform.position = _position.GetNextPlayerPosition(connectionToClient.connectionId);
        }

        [Server]
        private void SetNewPosition(PlaygroundCard card) {
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (!gameManager.IsPlayerTurn(connectionToClient.connectionId)) return;
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