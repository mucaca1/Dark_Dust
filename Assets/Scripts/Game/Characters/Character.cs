using System;
using Game.Cards.PlaygroundCards;
using Game.Characters.Ability;
using Mirror;
using Network;
using UnityEngine;

namespace Game.Characters {
    public class Character : NetworkBehaviour {
        [SerializeField] private Renderer renderer;
        [SyncVar] private string _characterName;

        [SyncVar(hook = nameof(HandleWaterValueChanged))]
        private int _water;

        [SyncVar(hook = nameof(HandleCharacterInitialize))]
        private bool _initialized = false;

        [SyncVar] private int extraMoveSteps = 0;

        [SyncVar] private int _maxWater;
        [SyncVar] private string _abilityDescription = "";

        [SyncVar] private int _abilityCode;
        private AbilityType _ability;

        [SyncVar(hook = nameof(HandleIndexPosition))] [SerializeField]
        private Vector2 _positionIndex = Vector2.zero;

        [SerializeField] private PlaygroundCard _position = null;
        [SerializeField] private Character _characterInControl = null;

        public static event Action<Character> onCharacterInitialized;
        public static event Action<Character, int, int> onWaterValueChanged;

        private static event Action<Character, PlaygroundCard> onMoveCharacterWithExtraStep;
        public event Action<Character> onCharacterDie;

        public int ExtraMoveSteps {
            get => extraMoveSteps;
            set => extraMoveSteps = value;
        }

        public int MAXWater => _maxWater;

        public int Water => _water;

        public string CharacterName => _characterName;

        public string AbilityDescription => _abilityDescription;

        public AbilityType Ability => (AbilityType) _abilityCode;

        public PlaygroundCard Position => _position;

        public Character CharacterInControl {
            get => _characterInControl;
            set => _characterInControl = value;
        }

        public override void OnStartClient() {
            GameManager.onPlaygroundLoaded += HandlePlaygroundLoaded;
        }

        public override void OnStopClient() {
            GameManager.onPlaygroundLoaded -= HandlePlaygroundLoaded;
        }

        private void Start() {
            if (isServer) {
                // Initialize character data
                CharacterData data = GameManager.Instance.GetCharacterData();
                _characterName = data.characterName;
                _maxWater = data.maxWater;
                _ability = data.ability;
                _abilityCode = _ability.GetHashCode();
                _abilityDescription = data.abilityDescription;
                ServerAddWater(data.startWater);
                SetStartPosition();
                _initialized = true;
            }

            if (isClient) {
                renderer.material.color = gameObject.GetComponent<Player>().PlayerColor;
                CharacterInControl = this;
                Character.onMoveCharacterWithExtraStep += HandleMoveCharacterWithExtraStep;
            }
        }

        #region Server

        [Server]
        public void ServerAddWater(int water) {
            _water = Mathf.Min(_maxWater, _water + water);
        }

        [Server]
        public void ServerRemoveWater(int water) {
            _water = Mathf.Min(_maxWater, _water - water);
        }

        [Server]
        public void DrinkWater() {
            _water = Mathf.Max(0, _water - 1);
            if (_water != 0) return;
            onCharacterDie?.Invoke(this);
        }

        [Server]
        private void SetStartPosition() {
            _position = GameManager.Instance.ServerGetStartCard();
            _positionIndex = _position.GetIndexPosition();
            transform.position = _position.GetNextPlayerPosition(this);
        }

        [Server]
        public void SetNewPosition(PlaygroundCard card) {
            if (_position != null)
                _position.LeavePart(this);
            _position = card;
            _positionIndex = _position.GetIndexPosition();
            transform.position = _position.GetNextPlayerPosition(this);
        }

        [Server]
        private void ServerRemoveSand(PlaygroundCard card) {
            card.RemoveSand(1 + GetComponent<Player>().AbilityManager.RemoveExtraSandAbility(this));
        }

        [Server]
        private void ServerExcavate(PlaygroundCard card) {
            card.ExcavateCard();
        }

        [Server]
        private void ServerPickUpAPart(PlaygroundCard card) {
            GameManager.Instance.PickUpAPart(card);
        }

        [Server]
        public void ServerDoAction(PlayerAction action, PlaygroundCard card, bool isAction) {
            if (!connectionToClient.identity.GetComponent<Player>().IsYourTurn && isAction) return;
            if (!card.CanCharacterDoAction(action, this)) return;
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

            if (isAction)
                GameManager.Instance.DoAction();
        }

        [Command]
        public void CmdDoAction(PlayerAction action, PlaygroundCard card, bool isAction) {
            ServerDoAction(action, card, isAction);
        }

        [Command]
        public void AddWater(int water) {
            ServerAddWater(water);
        }

        [Command]
        public void RemoveWater(int water) {
            ServerRemoveWater(water);
        }
        
        [Command]
        private void CmdDoActionWithCharacter(Character character, PlaygroundCard card) {
            character.CharacterInControl.SetNewPosition(card);
            --character._characterInControl.extraMoveSteps;
            if (_characterInControl.extraMoveSteps != 0) return;
            GameManager.Instance.DoAction();
            character.CharacterInControl = character;
        }

        [Command]
        public void CmdDoExtraMoveStep() {
            --_characterInControl.extraMoveSteps;
            if (_characterInControl.extraMoveSteps != 0) return;
            GameManager.Instance.DoAction();
            CharacterInControl = this;
        }

        #endregion

        #region Client

        [Client]
        public void HandlePlaygroundLoaded() {
            _position = GameManager.Instance.GetPlaygroundCardFromIndex(_positionIndex);
        }

        [Client]
        public void DoAction(PlayerAction action, PlaygroundCard card, bool specialAction) {
            if (!hasAuthority) return;
            if (!GetComponent<Player>().IsYourTurn) return;

            if (!card.CanActivePlayerDoAction(_characterInControl, this == _characterInControl)) return;
            if (specialAction && action == PlayerAction.WALK) {
                GetComponent<Player>().ShowSpecialActionDialogue(_characterInControl, Position, card);
            }
            else {
                if (_characterInControl.extraMoveSteps == 0) {
                    CmdDoAction(action, card, true);
                }
                else {
                    onMoveCharacterWithExtraStep?.Invoke(this, card);
                }
            }
        }

        [Client]
        public void HandleIndexPosition(Vector2 oldIndexPosition, Vector2 newIndexPosition) {
            _position = GameManager.Instance.GetPlaygroundCardFromIndex(newIndexPosition);
        }

        [Client]
        private void HandleWaterValueChanged(int oldWaterValue, int newWaterValue) {
            onWaterValueChanged?.Invoke(this, newWaterValue, _maxWater);
        }

        [Client]
        private void HandleCharacterInitialize(bool oldValue, bool newValue) {
            if (newValue) {
                onCharacterInitialized?.Invoke(this);
            }
        }

        [Client]
        private void HandleMoveCharacterWithExtraStep(Character character, PlaygroundCard card) {
            if (!hasAuthority) return;
            CmdDoActionWithCharacter(character, card);
        }

        #endregion
    }
}