using System;
using Game;
using Game.Cards.PlaygroundCards;
using Mirror;
using UnityEngine;

namespace Network {
    public class Player : NetworkBehaviour {
        [SerializeField] private Renderer _renderer = null;

        [SyncVar(hook = nameof(HandleChangePlayer))]
        private bool isYourTurn = false;

        [field: SyncVar] public string PlayerName { get; set; } = "PlayerName";

        [field: SyncVar(hook = nameof(UpdatePlayerColor))]
        public Color PlayerColor { get; set; } = Color.black;

        private PlaygroundCard _position = null;

        public event Action<bool, string> onChangeActivePlayer;

        public bool IsYourTurn => isYourTurn;

        private void UpdatePlayerColor(Color oldColor, Color newColor) {
            _renderer.material.color = newColor;
        }

        public override void OnStartAuthority() {
            CmdSetStartPosition();
        }

        private void HandleChangePlayer(bool oldValue, bool newValue) {
            onChangeActivePlayer?.Invoke(newValue, PlayerName);
        }

        #region Server

        [Server]
        public void EndTurn() {
            isYourTurn = false;
        }

        [Server]
        public void StartTurn() {
            isYourTurn = true;
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

        [Server]
        private void ServerRemoveSand(PlaygroundCard card) {
            if (card.CardType == PlaygroundCardType.Tornado) return;
            if (card.IsRevealed) return;
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (!gameManager.IsPlayerTurn(this)) return;
            if (_position == null) return;

            if (!card.CanMoveToThisPart(_position)) return;

            card.RemoveSand(1);
            gameManager.DoAction();
        }

        [Server]
        private void ServerExcavate(PlaygroundCard card) {
            if (card.CardType == PlaygroundCardType.Tornado) return;
            if (card.IsRevealed) return;
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (!gameManager.IsPlayerTurn(this)) return;
            if (_position == null) return;

            if (!card.PlayerStayHere(connectionToClient.connectionId)) return;

            card.ExcavateCard();

            gameManager.DoAction();
        }

        [Server]
        private void ServerPickUpAPart(PlaygroundCard card) {
            if (card.CardType == PlaygroundCardType.Tornado) return;
            if (!card.IsRevealed) return;

            GameManager gameManager = FindObjectOfType<GameManager>();
            if (!gameManager.IsPlayerTurn(this)) return;
            if (!card.PlayerStayHere(connectionToClient.connectionId)) return;
            if (gameManager.TryPickUpAPart(card))
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

        [Command]
        private void CmdRemoveSand(PlaygroundCard card) {
            ServerRemoveSand(card);
        }

        [Command]
        private void CmdExcavate(PlaygroundCard card) {
            ServerExcavate(card);
        }

        [Command]
        private void CmdPickUpAPart(PlaygroundCard card) {
            ServerPickUpAPart(card);
        }

        #endregion

        #region Client

        [Client]
        public void GoToPosition(PlaygroundCard card) {
            if (card.CardType == PlaygroundCardType.Tornado) return;
            if (!hasAuthority) return;

            CmdGoToPosition(card);
        }

        [Client]
        public void RemoveSand(PlaygroundCard card) {
            if (card.CardType == PlaygroundCardType.Tornado) return;
            if (!hasAuthority) return;

            CmdRemoveSand(card);
        }

        [Client]
        public void Excavate(PlaygroundCard card) {
            if (card.CardType == PlaygroundCardType.Tornado) return;
            if (card.IsRevealed) return;
            CmdExcavate(card);
        }

        [Client]
        public void PickUpAPart(PlaygroundCard card) {
            if (card.CardType == PlaygroundCardType.Tornado) return;
            if (!card.IsRevealed) return;
            CmdPickUpAPart(card);
        }

        #endregion
    }
}