using System;
using Game;
using Game.Cards.PlaygroundCards;
using Game.Characters;
using Game.Characters.Ability;
using Game.UI;
using Mirror;
using UnityEngine;

namespace Network {
    public class Player : NetworkBehaviour {
        [SerializeField] private SpecialAbilityActionUI _specialActionMenuPrefab = null;
        [SerializeField] private SelectPlayerUI _selectPlayerPrefab = null;

        private SpecialAbilityActionUI _openedAbilityActionInstance = null;

        [SyncVar(hook = nameof(HandleChangePlayer))]
        private bool isYourTurn = false;

        [field: SyncVar] [SerializeField] public string _playerName = "PlayerName";

        [field: SyncVar] public Color PlayerColor { get; set; } = Color.black;

        private PlayerController _controller = null;

        public event Action<bool, string> onChangeActivePlayer;

        private AbilityManager _abilityManager = new AbilityManager();

        public AbilityManager AbilityManager => _abilityManager;

        public bool IsYourTurn => isYourTurn;


        public string PlayerName {
            get => _playerName;
            set => _playerName = value;
        }

        [ClientCallback]
        private void Start() {
            GameManager.Instance.futureCards.Callback += HandleShowedCard;
            GameManager.onDustTurn += HandleDustTurn;
            _abilityManager.onDoAction += HandleOnDoAction;
            AbilityManager.onChangeWater += HandleOnChangeWater;
            AbilityManager.onPositionChange += HandleOnPositionChange;
            _abilityManager.onWeakenStorm += HandleOnWeakenStrom;
            _abilityManager.onShowCards += HandleOnShowCards;
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

        [Command]
        private void CmdWakenStorm() {
            GameManager.Instance.ServerWeakStormCard();
        }

        [Command]
        private void CmdDoAction() {
            GameManager.Instance.DoAction();
        }

        [Command]
        private void CmdShowNextCards() {
            GameManager.Instance.ServerGetTornadoCards();
        }

        [Command]
        private void CmdMoveCharacter(Character character, PlaygroundCard playgroundCard) {
            character.ServerDoAction(PlayerAction.WALK, playgroundCard, false);
        }

        [Command]
        private void CmdMoveCardToBottom(int cardIndex) {
            if (cardIndex == 3) {
                GameManager.Instance.futureCards.Clear();
            }
            else {
                GameManager.Instance.MoveCardToTheBottom(cardIndex);
            }
        }

        #endregion

        #region Client

        [Client]
        private void HandleDustTurn() {
            onChangeActivePlayer?.Invoke(false, "Desert");
        }

        [Client]
        private void HandleChangePlayer(bool oldValue, bool newValue) {
            onChangeActivePlayer?.Invoke(newValue, GameManager.Instance.ActivePlayerName);
        }

        [Client]
        public void ShowSpecialActionDialogue(Character character, PlaygroundCard source, PlaygroundCard destination) {
            bool abilityViewWasOpened = _openedAbilityActionInstance != null;

            if (!abilityViewWasOpened) {
                _openedAbilityActionInstance = Instantiate(_specialActionMenuPrefab, Vector3.zero, Quaternion.identity);
                _openedAbilityActionInstance.Initialize(character.Ability, source, destination);
            }

            if (destination.CardType == PlaygroundCardType.Tornado && _abilityManager.CanClockOnTornado(character)) {
                if (abilityViewWasOpened) {
                    HandleSpecialActionDialogueClose();
                    _openedAbilityActionInstance =
                        Instantiate(_specialActionMenuPrefab, Vector3.zero, Quaternion.identity);
                    _openedAbilityActionInstance.Initialize(character.Ability, source, destination, false);

                    CmdShowNextCards();
                }
                else {
                    for (int i = 0; i < 2; i++) {
                        SelectPlayerUI playerSelect =
                            Instantiate(_selectPlayerPrefab, Vector3.zero, Quaternion.identity);
                        playerSelect.transform.parent = _openedAbilityActionInstance.GetActionContentHolderTransform();
                        playerSelect.transform.localScale = Vector3.one;
                        SelectPlayerUI.Value value = new SelectPlayerUI.Value();
                        value.gameObject = i % 2 == 0 ? destination.gameObject : null;
                        value.itemName = i % 2 == 0 ? "Skip one tornado card" : "Show tornado cards";
                        value.itemColor = i % 2 == 0 ? Color.black : Color.gray;
                        playerSelect.Initialize(value, character.Ability);
                        playerSelect.onValueSelected += HandleSpecialActionSelectedPlayer;
                    }
                }
            }
            else {
                if (_abilityManager.CanUsePlaygroundCardAsPlayer(character) && character.Position == destination &&
                    destination.CardType == PlaygroundCardType.Water) {
                    SelectPlayerUI playerSelect = Instantiate(_selectPlayerPrefab, Vector3.zero, Quaternion.identity);
                    playerSelect.transform.parent = _openedAbilityActionInstance.GetActionContentHolderTransform();
                    playerSelect.transform.localScale = Vector3.one;
                    SelectPlayerUI.Value value = new SelectPlayerUI.Value();
                    value.gameObject = source.gameObject;
                    value.itemName = source.CardType.ToString();
                    value.itemColor = Color.black;
                    playerSelect.Initialize(value, character.Ability);
                    playerSelect.onValueSelected += HandleSpecialActionSelectedPlayer;
                }

                foreach (Character ch in FindObjectsOfType<Character>()) {
                    if (ch == character) continue;
                    if (ch.Position != source && !_abilityManager.CanUsePlaygroundCardAsPlayer(character)) continue;

                    SelectPlayerUI playerSelect = Instantiate(_selectPlayerPrefab, Vector3.zero, Quaternion.identity);
                    playerSelect.transform.parent = _openedAbilityActionInstance.GetActionContentHolderTransform();
                    playerSelect.transform.localScale = Vector3.one;
                    SelectPlayerUI.Value value = new SelectPlayerUI.Value();
                    value.gameObject = ch.GetComponent<Player>().gameObject;
                    value.itemName = ch.GetComponent<Player>().PlayerName;
                    value.itemColor = ch.GetComponent<Player>().PlayerColor;
                    playerSelect.Initialize(value, character.Ability);
                    playerSelect.onValueSelected += HandleSpecialActionSelectedPlayer;
                }
            }


            _openedAbilityActionInstance.onCancel += HandleSpecialActionDialogueClose;
        }

        [Client]
        private void HandleSpecialActionSelectedPlayer(AbilityType ability, GameObject selectedObject, int index) {
            Character source = NetworkClient.connection.identity.GetComponent<Character>();
            if (ability == AbilityType.Meteorologist && index == -1 && selectedObject == null) {
                ShowSpecialActionDialogue(source, source.Position, GameManager.Instance.Tornado);
            }
            else {
                _abilityManager.DoSpecialAction(source, ability, selectedObject,
                    _openedAbilityActionInstance.GetInputValue(), _openedAbilityActionInstance.SourceCard,
                    _openedAbilityActionInstance.DestinationCard, index);

                HandleSpecialActionDialogueClose();
            }
        }

        [Client]
        private void HandleSpecialActionDialogueClose() {
            if (_openedAbilityActionInstance == null) return;
            foreach (Transform child in _openedAbilityActionInstance.GetActionContentHolderTransform()) {
                child.GetComponent<SelectPlayerUI>().onValueSelected -= HandleSpecialActionSelectedPlayer;
                Destroy(child.gameObject);
            }

            _openedAbilityActionInstance.onCancel -= HandleSpecialActionDialogueClose;
            Destroy(_openedAbilityActionInstance.gameObject);
            _openedAbilityActionInstance = null;
        }

        [Client]
        private void HandleShowedCard(SyncList<string>.Operation op, int itemIndex, string oldItem, string newItem) {
            if (!NetworkClient.connection.identity.GetComponent<Player>().IsYourTurn) return;
            if (op != SyncList<string>.Operation.OP_ADD) return;
            if (_openedAbilityActionInstance == null) return;
            SelectPlayerUI playerSelect =
                Instantiate(_selectPlayerPrefab, Vector3.zero, Quaternion.identity);
            playerSelect.transform.parent = _openedAbilityActionInstance.GetActionContentHolderTransform();
            playerSelect.transform.localScale = Vector3.one;
            SelectPlayerUI.Value value = new SelectPlayerUI.Value();
            value.index = itemIndex;
            value.gameObject = null;
            value.itemName = newItem;
            value.itemColor = Color.gray;
            playerSelect.Initialize(value, AbilityType.Meteorologist);
            playerSelect.onValueSelected += HandleSpecialActionSelectedPlayer;

            if (itemIndex == 2) {
                SelectPlayerUI emptySelect = Instantiate(_selectPlayerPrefab, Vector3.zero, Quaternion.identity);
                emptySelect.transform.parent = _openedAbilityActionInstance.GetActionContentHolderTransform();
                emptySelect.transform.localScale = Vector3.one;
                SelectPlayerUI.Value emptyValue = new SelectPlayerUI.Value();
                emptyValue.index = 3;
                emptyValue.gameObject = null;
                emptyValue.itemName = "Do not move card";
                emptyValue.itemColor = Color.gray;
                emptySelect.Initialize(emptyValue, AbilityType.Meteorologist);
                emptySelect.onValueSelected += HandleSpecialActionSelectedPlayer;
            }
        }

        [Client]
        private void HandleOnShowCards(int cardIndex) {
            if (!hasAuthority) return;
            CmdMoveCardToBottom(cardIndex);
        }

        [Client]
        private void HandleOnWeakenStrom() {
            if (!hasAuthority) return;
            CmdWakenStorm();
        }

        [Client]
        private void HandleOnPositionChange(Character arg1, PlaygroundCard arg2) {
            CmdMoveCharacter(arg1, arg2);
        }

        [Client]
        private void HandleOnChangeWater(Character arg1, int arg2) {
            if (!hasAuthority) return;

            arg1.AddWater(arg2);
        }

        [Client]
        private void HandleOnDoAction() {
            if (!hasAuthority && !IsYourTurn) return;
            CmdDoAction();
        }

        #endregion
    }
}