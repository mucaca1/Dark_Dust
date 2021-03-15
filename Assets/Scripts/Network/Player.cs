using System;
using System.Collections.Generic;
using Game;
using Game.Cards.PlayCards.Items;
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
        [SerializeField] private SelectItemUI _selectItemPrefab = null;

        SyncList<int> _playerCards = new SyncList<int>();

        private SpecialAbilityActionUI _openedAbilityActionInstance = null;

        [SyncVar(hook = nameof(HandleChangePlayer))]
        private bool isYourTurn = false;

        [field: SyncVar] [SerializeField] public string _playerName = "PlayerName";

        [field: SyncVar] public Color PlayerColor { get; set; } = Color.black;

        private PlayerController _controller = null;

        private List<ItemCard> _cards = new List<ItemCard>();

        public event Action<bool, string> onChangeActivePlayer;

        public event Action<int> onItemCardsChanged;

        private AbilityManager _abilityManager = new AbilityManager();

        public AbilityManager AbilityManager => _abilityManager;

        public bool IsYourTurn => isYourTurn;

        public SyncList<int> PlayerCards {
            get => _playerCards;
            set => _playerCards = value;
        }

        public List<ItemCard> Cards => _cards;


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
            _abilityManager.onWeakenStorm += HandleOnWeakenStorm;
            _abilityManager.onShowCards += HandleOnShowCards;
            _abilityManager.onControlOtherCharacter += HandleControlOtherCharacter;

            _playerCards.Callback += HandleItemCardOperationFromServer;

            SelectItemUI.onItemActionSelect += HandleItemAction;
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
        public void RemoveCard(int cardId) {
            if (!_playerCards.Contains(cardId)) return;
            _playerCards.Remove(cardId);
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

        [Command]
        private void CmdInitializeControl() {
            GetComponent<Character>().CharacterInControl.ExtraMoveSteps = 3;
        }


        [Command]
        public void CmdGiveCardToThePlayer(int cardId, Player player) {
            if (GetComponent<Character>().Position != player.GetComponent<Character>().Position) return;
            if (!_playerCards.Contains(cardId)) return;
            _playerCards.Remove(cardId);
            player.PlayerCards.Add(cardId);
        }
        
        [Command]
        private void CmdSetCardAbility(CardAction itemCardAction) {
            GetComponent<Character>().CardAbility = itemCardAction;
        }

        #endregion

        #region Client

        [ClientRpc]
        public void ActionDialogueCloseRpc() {
            HandleSpecialActionDialogueClose();
        }

        [Client]
        private void HandleDustTurn() {
            onChangeActivePlayer?.Invoke(false, "Desert");
        }

        [Client]
        private void HandleChangePlayer(bool oldValue, bool newValue) {
            onChangeActivePlayer?.Invoke(newValue, GameManager.Instance.ActivePlayerName);
        }

        [Client]
        public void ShowSpecialCardDialogue() {
            bool abilityViewWasOpened = _openedAbilityActionInstance != null;

            if (!abilityViewWasOpened) {
                _openedAbilityActionInstance = Instantiate(_specialActionMenuPrefab, Vector3.zero, Quaternion.identity);
                _openedAbilityActionInstance.Initialize();
            }

            foreach (ItemCard itemCard in _cards) {
                SelectItemUI itemSelect =
                    Instantiate(_selectItemPrefab, Vector3.zero, Quaternion.identity);
                itemSelect.transform.parent = _openedAbilityActionInstance.GetActionContentHolderTransform();
                itemSelect.transform.localScale = Vector3.one;
                itemSelect.Initialize(itemCard.CardName, itemCard.CardId);
                itemSelect.gameObject.GetComponentInChildren<ItemCardToolDescription>().card =
                    itemCard;
            }


            _openedAbilityActionInstance.onCancel += HandleItemDialogueClose;
        }

        [Client]
        public void ShowSpecialActionDialogue(AbilityType ability, Character character, PlaygroundCard source,
            PlaygroundCard destination, int index = -1) {
            bool abilityViewWasOpened = _openedAbilityActionInstance != null;

            if (!abilityViewWasOpened) {
                _openedAbilityActionInstance = Instantiate(_specialActionMenuPrefab, Vector3.zero, Quaternion.identity);
                _openedAbilityActionInstance.Initialize(ability, source, destination);
            }

            if (ability == AbilityType.GiveItem) {
                foreach (Character ch in character.Position.GetCharacters()) {
                    if (ch == character) continue;
                    if (ch.Position != source) continue;

                    SelectPlayerUI playerSelect = Instantiate(_selectPlayerPrefab, Vector3.zero, Quaternion.identity);
                    playerSelect.transform.parent = _openedAbilityActionInstance.GetActionContentHolderTransform();
                    playerSelect.transform.localScale = Vector3.one;
                    SelectPlayerUI.Value value = new SelectPlayerUI.Value();
                    value.gameObject = ch.GetComponent<Player>().gameObject;
                    value.itemName = ch.GetComponent<Player>().PlayerName;
                    value.itemColor = ch.GetComponent<Player>().PlayerColor;
                    value.index = index;
                    playerSelect.Initialize(value, ability);
                    playerSelect.onValueSelected += HandleSpecialActionSelectedPlayer;
                }

                _openedAbilityActionInstance.onCancel += HandleSpecialActionDialogueClose;
                return;
            }

            if (ability == AbilityType.UseItem) {
                foreach (Character ch in character.Position.GetCharacters()) {
                    if (ch == character) continue;
                    if (ch.Position != source) continue;

                    SelectPlayerUI playerSelect = Instantiate(_selectPlayerPrefab, Vector3.zero, Quaternion.identity);
                    playerSelect.transform.parent = _openedAbilityActionInstance.GetActionContentHolderTransform();
                    playerSelect.transform.localScale = Vector3.one;
                    SelectPlayerUI.Value value = new SelectPlayerUI.Value();
                    value.gameObject = ch.GetComponent<Player>().gameObject;
                    value.itemName = ch.GetComponent<Player>().PlayerName;
                    value.itemColor = ch.GetComponent<Player>().PlayerColor;
                    value.index = index;
                    playerSelect.Initialize(value, ability);
                    playerSelect.onValueSelected += HandleSpecialActionSelectedPlayer;
                }

                _openedAbilityActionInstance.onCancel += HandleSpecialActionDialogueClose;
                _openedAbilityActionInstance.onCancel += HandleDialogueCloseButton;
                return;
            }

            if (destination.CardType == PlaygroundCardType.Tornado && _abilityManager.CanClockOnTornado(character)) {
                if (abilityViewWasOpened) {
                    HandleSpecialActionDialogueClose();
                    _openedAbilityActionInstance =
                        Instantiate(_specialActionMenuPrefab, Vector3.zero, Quaternion.identity);
                    _openedAbilityActionInstance.Initialize(ability, source, destination, false);

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
                        playerSelect.Initialize(value, ability);
                        playerSelect.onValueSelected += HandleSpecialActionSelectedPlayer;
                    }
                }
            }
            else if (_abilityManager.CanMoveWithCharacter(character)) {
                foreach (Player player in FindObjectsOfType<Player>()) {
                    if (player == this) continue;
                    SelectPlayerUI playerSelect = Instantiate(_selectPlayerPrefab, Vector3.zero, Quaternion.identity);
                    playerSelect.transform.parent = _openedAbilityActionInstance.GetActionContentHolderTransform();
                    playerSelect.transform.localScale = Vector3.one;
                    SelectPlayerUI.Value value = new SelectPlayerUI.Value();
                    value.gameObject = player.gameObject;
                    value.itemName = player.PlayerName;
                    value.itemColor = player.PlayerColor;
                    playerSelect.Initialize(value, ability);
                    playerSelect.onValueSelected += HandleSpecialActionSelectedPlayer;
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
                    playerSelect.Initialize(value, ability);
                    playerSelect.onValueSelected += HandleSpecialActionSelectedPlayer;
                }

                foreach (Character ch in _abilityManager.CanMoveToCard(character)
                    ? character.Position.GetCharacters()
                    : destination.GetCharacters()) {
                    if (ch == character) continue;
                    if (ch.Position != source && !_abilityManager.CanUsePlaygroundCardAsPlayer(character)) continue;

                    SelectPlayerUI playerSelect = Instantiate(_selectPlayerPrefab, Vector3.zero, Quaternion.identity);
                    playerSelect.transform.parent = _openedAbilityActionInstance.GetActionContentHolderTransform();
                    playerSelect.transform.localScale = Vector3.one;
                    SelectPlayerUI.Value value = new SelectPlayerUI.Value();
                    value.gameObject = ch.GetComponent<Player>().gameObject;
                    value.itemName = ch.GetComponent<Player>().PlayerName;
                    value.itemColor = ch.GetComponent<Player>().PlayerColor;
                    value.index = ch.ExtraMoveSteps;
                    playerSelect.Initialize(value, ability);
                    playerSelect.onValueSelected += HandleSpecialActionSelectedPlayer;
                }
            }


            _openedAbilityActionInstance.onCancel += HandleSpecialActionDialogueClose;
            _openedAbilityActionInstance.onCancel += HandleDialogueCloseButton;
        }

        [Client]
        private void HandleSpecialActionSelectedPlayer(AbilityType ability, GameObject selectedObject, int index) {
            if (!hasAuthority) return;
            Character source = NetworkClient.connection.identity.GetComponent<Character>();

            if (ability == AbilityType.Meteorologist && index == -1 && selectedObject == null) {
                ShowSpecialActionDialogue(source.Ability, source, source.Position, GameManager.Instance.Tornado);
            }
            else {
                if (source.ExtraMoveSteps != 0 && ability == AbilityType.Climber) {
                    if (selectedObject.TryGetComponent(out Character character)) {
                        HandleOnPositionChange(source, _openedAbilityActionInstance.DestinationCard);
                        HandleOnPositionChange(character, _openedAbilityActionInstance.DestinationCard);
                        GetComponent<Character>().CmdDoExtraMoveStep();
                    }
                }
                else {
                    _abilityManager.DoSpecialAction(source, ability, selectedObject,
                        _openedAbilityActionInstance.GetInputValue(), _openedAbilityActionInstance.SourceCard,
                        _openedAbilityActionInstance.DestinationCard, index);
                }


                HandleSpecialActionDialogueClose();
            }
        }

        [Client]
        private void HandleItemDialogueClose() {
            _openedAbilityActionInstance.onCancel -= HandleItemDialogueClose;
            Destroy(_openedAbilityActionInstance.gameObject);
            _openedAbilityActionInstance = null;
        }

        [Client]
        private void HandleDialogueCloseButton() {
            if (!hasAuthority) return;

            GetComponent<Character>().CmdResetItemCard();
        }

        [Client]
        public void HandleSpecialActionDialogueClose() {
            if (_openedAbilityActionInstance == null) return;
            foreach (Transform child in _openedAbilityActionInstance.GetActionContentHolderTransform()) {
                if (child.TryGetComponent(out SelectPlayerUI playerUI)) {
                    playerUI.onValueSelected -= HandleSpecialActionSelectedPlayer;
                }

                Destroy(child.gameObject);
            }

            _openedAbilityActionInstance.onCancel -= HandleSpecialActionDialogueClose;
            _openedAbilityActionInstance.onCancel -= HandleDialogueCloseButton;
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
        private void HandleOnWeakenStorm() {
            if (!hasAuthority) return;
            CmdWakenStorm();
        }

        [Client]
        private void HandleOnPositionChange(Character arg1, PlaygroundCard arg2) {
            if (!hasAuthority) return;
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

        [Client]
        private void HandleControlOtherCharacter() {
            CmdInitializeControl();
        }

        [Client]
        public bool CanGiveCardToThePlayer(Player player) {
            return _playerCards.Count != 0 &&
                   GetComponent<Character>().Position == player.GetComponent<Character>().Position;
        }

        [Client]
        public void GiveCardToThePlayer(int cardId, Player player) {
            CmdGiveCardToThePlayer(cardId, player);
        }

        [Client]
        private void HandleItemCardOperationFromServer(SyncList<int>.Operation op, int itemIndex, int oldItem,
            int newItem) {
            onItemCardsChanged?.Invoke(_playerCards.Count);
            if (op == SyncList<int>.Operation.OP_ADD) {
                ItemCard[] cards = Resources.LoadAll<ItemCard>("ItemCards");

                foreach (ItemCard itemCard in cards) {
                    if (newItem == itemCard.CardId) {
                        ItemCard itemCardObject = Instantiate(itemCard, Vector3.zero, Quaternion.identity);
                        _cards.Add(itemCardObject);
                    }
                }
            }

            if (op == SyncList<int>.Operation.OP_REMOVEAT) {
                ItemCard card = null;
                foreach (ItemCard itemCard in _cards) {
                    if (itemCard.CardId == oldItem) {
                        card = itemCard;
                    }
                }

                _cards.Remove(card);
                Destroy(card.gameObject);
            }
        }

        [Client]
        private void HandleItemAction(int cardId, AbilityType abilityType) {
            if (!hasAuthority) return;
            if (abilityType == AbilityType.GiveItem) {
                HandleSpecialActionDialogueClose();
                ShowSpecialActionDialogue(AbilityType.GiveItem, GetComponent<Character>(),
                    GetComponent<Character>().Position, null, cardId);
            }
            else if (abilityType == AbilityType.UseItem) {
                foreach (ItemCard itemCard in _cards) {
                    if (itemCard.CardId == cardId) {
                        CmdSetCardAbility(itemCard.Action);
                    }
                }

                GetComponent<Character>().CmdDoAction(PlayerAction.WALK, GetComponent<Character>().Position, true);
            }
        }

        #endregion
    }
}