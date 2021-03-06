using System;
using Game.Cards.PlaygroundCards;
using Game.Characters.Ability;
using Mirror;
using Network;
using UnityEngine;

namespace Game.Characters {
    public class Character : NetworkBehaviour {
        [SyncVar] private string _characterName;
        [SyncVar] private int _water;
        private string _abilityDescription;
        
        [SyncVar] private int _maxWater;
        
        private PlaygroundCard _position = null;

        private CharacterAbility[] _abilities = new CharacterAbility[0];

        public static event Action<Character> onCharacterDie;

        public int MAXWater => _maxWater;

        public string CharacterName => _characterName;

        public string AbilityDescription => _abilityDescription;

        public override void OnStartAuthority() {
            CmdSetStartPosition();
        }

        #region Server

        [Server]
        public void InitializePlayer(CharacterData data) {
            _characterName = data.characterName;
            _maxWater = data.water;
            _abilityDescription = data.abilityDescription;
            _abilities = data.ability;
        }

        [Server]
        public void SetWater(int water) {
            _water = Mathf.Max(_maxWater, _water + water);
        }

        [Server]
        public void DrinkWater() {
            _water = Mathf.Max(0, _water - 1);
            if (_water != 0) return;
            onCharacterDie?.Invoke(this);
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
            if (!connectionToClient.identity.GetComponent<Player>().IsYourTurn) return;
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
            if (!connectionToClient.identity.GetComponent<Player>().IsYourTurn) return;
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
            if (!connectionToClient.identity.GetComponent<Player>().IsYourTurn) return;
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
            if (!connectionToClient.identity.GetComponent<Player>().IsYourTurn) return;
            if (!card.PlayerStayHere(connectionToClient.connectionId)) return;
            if (gameManager.TryPickUpAPart(card))
                gameManager.DoAction();
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
            if (card.CardType == PlaygroundCardType.Tornado) return;
            if (!hasAuthority) return;

            CmdGoToPosition(card);
        }

        #endregion
        
        #region Client

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