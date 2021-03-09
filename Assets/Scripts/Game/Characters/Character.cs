using System;
using System.Collections.Generic;
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

        private PlayerController _Controller = null;

        private List<CharacterAbility> _abilities = new List<CharacterAbility>();

        public event Action<int, int> onWaterValueChanged;
        public event Action<Character> onCharacterDie;

        public List<CharacterAbility> Abilities => _abilities;

        public int MAXWater => _maxWater;

        public string CharacterName => _characterName;

        public string AbilityDescription => _abilityDescription;

        public PlaygroundCard Position => _position;

        public PlayerController Controller => _Controller;

        public override void OnStartAuthority() {
            CmdSetStartPosition();
            _Controller = gameObject.GetComponent<PlayerController>();
        }

        private int RemoveExtraSandAbility() {
            int sand = 0;

            foreach (CharacterAbility ability in _abilities) {
                RemoveSandAbility rms = ability as RemoveSandAbility;
                if (rms == null) continue;
                sand += rms.ExtraSandToRemove;
            }

            return sand;
        }

        private bool CanSeeThisPartAbility(PlaygroundCard destination) {
            foreach (CharacterAbility ability in _abilities) {
                ExploreAbility explore = ability as ExploreAbility;
                if (explore == null) continue;
                if (explore.CanSeeToPart(_position, destination))
                    return true;
            }

            return false;
        }

        private bool CanMoveToThisPartAbility() {
            foreach (CharacterAbility ability in _abilities) {
                IgnoreSandCountAbility explore = ability as IgnoreSandCountAbility;
                if (explore == null) continue;
                return true;
            }

            return false;
        }

        #region Server

        [Server]
        public void InitializePlayer(CharacterData data) {
            _characterName = data.characterName;
            _maxWater = data.water;
            _abilityDescription = data.abilityDescription;

            foreach (string dataAbilityName in data.abilityNames) {
                CharacterAbility ability = Resources.Load<CharacterAbility>("Ability/" + dataAbilityName);
                CharacterAbility gameObject = Instantiate(ability, Vector3.zero, Quaternion.identity);
                _abilities.Add(gameObject);
            }

            _water = _maxWater;
            onWaterValueChanged?.Invoke(_water, _maxWater);
        }

        [Server]
        public void SetWater(int water) {
            _water = Mathf.Max(_maxWater, _water + water);
            onWaterValueChanged?.Invoke(_water, _maxWater);
        }

        [Server]
        public void DrinkWater() {
            _water = Mathf.Max(0, _water - 1);
            onWaterValueChanged?.Invoke(_water, _maxWater);
            if (_water != 0) return;
            onCharacterDie?.Invoke(this);
        }

        [Server]
        private void SetStartPosition() {
            GameManager manager = FindObjectOfType<GameManager>();
            _position = manager.GetStartCard();
            transform.position = _position.GetNextPlayerPosition(this);
        }

        [Server]
        private void SetNewPosition(PlaygroundCard card) {
            if (_position != null)
                _position.LeavePart(this);
            _position = card;
            transform.position = _position.GetNextPlayerPosition(this);
        }

        [Server]
        private void ServerRemoveSand(PlaygroundCard card) {
            card.RemoveSand(1 + RemoveExtraSandAbility());
        }

        [Server]
        private void ServerExcavate(PlaygroundCard card) {
            card.ExcavateCard();
        }

        [Server]
        private void ServerPickUpAPart(PlaygroundCard card) {
            GameManager gameManager = FindObjectOfType<GameManager>();
            gameManager.PickUpAPart(card);
        }

        [Server]
        private void ServerDoAction(PlayerAction action, PlaygroundCard card) {
            if (!connectionToClient.identity.GetComponent<Player>().IsYourTurn) return;
            if (!card.CanCharacterDoAction(this)) return;
            switch (action) {
                case PlayerAction.WALK:
                    SetNewPosition(card);
                    break;
                case PlayerAction.EXCAVATE:
                    ServerExcavate(card);
                    break;
                case PlayerAction.REMOVE_SAND:
                    ServerRemoveSand(card);
                    break;
                case PlayerAction.PICK_UP_A_PART:
                    ServerPickUpAPart(card);
                    break;
            }

            GameManager gameManager = FindObjectOfType<GameManager>();
            gameManager.DoAction();
        }

        [Command]
        private void CmdSetStartPosition() {
            SetStartPosition();
        }

        [Command]
        private void CmdDoAction(PlayerAction action, PlaygroundCard card) {
            ServerDoAction(action, card);
        }

        #endregion

        #region Client

        [Client]
        public void DoAction(PlayerAction action, PlaygroundCard card) {
            Player player = connectionToClient.identity.GetComponent<Player>();
            if (card.CanActivePlayerDoAction(connectionToClient.identity.GetComponent<Player>())) {
                CmdDoAction(action, card);
            }
        }

        #endregion
    }
}