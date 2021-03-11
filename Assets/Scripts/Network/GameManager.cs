using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Cards.PlayCards.Tornado;
using Game.Cards.PlaygroundCards;
using Game.Characters;
using Game.Characters.Ability;
using Game.UI;
using Mirror;
using Network;
using UnityEngine;

namespace Game {
    public class GameManager : NetworkBehaviour {
        [Serializable]
        private class TornadoCardSet {
            public int count = 0;
            public TornadoCard cardPrefab = null;
        }

        private class TornadoDeckCardData {
            public TornadoCard cardPrefab = null;
            public int steps = 0;
            public TornadoDirection direction;
            [SyncVar] public string text = "";
        }

        private static AbilityManager abilityManager = new AbilityManager();

        private Player _activePlayer = null;

        [SyncVar] private string _activePlayerName = "";

        [SyncVar(hook = nameof(HandleStepCounter))]
        private int _stepsRemaning = 4;

        [SyncVar] private int _takedItems = 0;
        [SyncVar] private int _takedItemsCount = 0;
        [SerializeField] private Transform playGroundStartTransform = null;
        [SerializeField] private GameObject playgroundCardPrefab = null;
        [SerializeField] private TornadoCardSet[] _tornadoCardsPrefab = new TornadoCardSet[0];

        [SerializeField] private SpecialAbilityActionUI _specialActionMenuPrefab = null;
        [SerializeField] private SelectPlayerUI _selectPlayerPrefab = null;

        private Queue<TornadoDeckCardData> _tornadoCards = new Queue<TornadoDeckCardData>();

        private List<PlaygroundCard> _playgroundCards = new List<PlaygroundCard>();
        private PlaygroundCard _tornado = null;
        private PlaygroundCardData[] _playgroundCardDatas;
        private Queue<Player> _playerOrder = new Queue<Player>();
        private static int _maxSteps = 4;

        [SyncVar(hook = nameof(HandleSandStack))]
        private int _sandStackReaming = 48;

        [SyncVar] private int _actualStormTickMark = 2;
        private int _stromTickMarkValue = 2;
        private Queue<CharacterData> _charactersData = new Queue<CharacterData>();

        private SpecialAbilityActionUI _openedAbilityActionInstance = null;

        public PlaygroundCard Tornado => _tornado;

        public List<PlaygroundCard> PlaygroundCards => _playgroundCards;

        public Player ActivePlayer => _activePlayer;

        public static AbilityManager AbilityManager => abilityManager;

        public string ActivePlayerName => _activePlayerName;

        public int ActualStormTickMark => _actualStormTickMark;

        public event Action<int> onTakedItemsIncrease;
        public event Action<int> onStromTickMarkChanged;
        public event Action<int> onDustCardSet;
        public event Action<int> onTornadoCardChanged;

        public static event Action<int> onAvaibleStepsChanged;
        public static event Action onPlaygroundLoaded;

        public static event Action onDustTurn;

        private static GameManager _gameManager;

        public static GameManager Instance {
            get { return _gameManager; }
        }


        private void Awake() {
            if (_gameManager != null && _gameManager != this) {
                Destroy(this.gameObject);
            }
            else {
                _gameManager = this;
            }
        }

        private void Start() {
            if (isServer) {
                PlaygroundCard.onAddSand += AddSandHandler;
                PlaygroundCard.onRemoveSand += RemoveSandHandler;
                _playgroundCardDatas = Resources.LoadAll<PlaygroundCardData>("");

                Debug.Log(_playgroundCardDatas.Length == 0
                    ? "No playground cards was found. Check Resources folder"
                    : "Playground cards was loaded successfully");

                GenerateNewPlayGround();
            }

            if (isClient) {
                LoadPlayground();
                onPlaygroundLoaded?.Invoke();
            }
        }

        public Vector3 GetPlaygroundStartPosition() {
            return playGroundStartTransform.position;
        }

        public bool IsItemTaked(int index) {
            return (_takedItems & (1 << index)) != 0;
        }

        private void TakeItem(int index) {
            _takedItems |= 1 << index;
            ++_takedItemsCount;
            onTakedItemsIncrease?.Invoke(_takedItemsCount);
        }

        #region Server

        [Server]
        public void StormTickUp() {
            ++_stromTickMarkValue;
            onStromTickMarkChanged?.Invoke(_stromTickMarkValue);
        }

        [Server]
        private void AddSandHandler() {
            --_sandStackReaming;
        }

        [Server]
        private void RemoveSandHandler() {
            ++_sandStackReaming;
        }

        [Server]
        private void GenerateNewPlayGround() {
            if (_playgroundCardDatas == null || _playgroundCardDatas.Length == 0) return;

            foreach (PlaygroundCardData cardData in _playgroundCardDatas) {
                for (int i = 0; i < cardData.CardCount; i++) {
                    GameObject card = Instantiate(playgroundCardPrefab, Vector3.zero, Quaternion.identity);
                    PlaygroundCard newCardData = card.GetComponent<PlaygroundCard>();
                    newCardData.name = cardData.name;
                    newCardData.SetData(cardData, playGroundStartTransform.position);
                    if (newCardData.CardType != PlaygroundCardType.Tornado)
                        _playgroundCards.Add(newCardData);
                    else
                        _tornado = newCardData;
                    NetworkServer.Spawn(card);
                }
            }

            // Sort cards.
            _playgroundCards = _playgroundCards.OrderBy(x => Guid.NewGuid()).ToList();

            Stack<PlaygroundCard> stack = new Stack<PlaygroundCard>();
            foreach (PlaygroundCard playgroundCard in _playgroundCards) {
                if (playgroundCard.CardType != PlaygroundCardType.Tornado) {
                    stack.Push(playgroundCard);
                }
                else {
                    _tornado = playgroundCard; // Extract tornado card
                }
            }

            // Set card position.
            for (int i = 0; i < 5; i++) {
                for (int j = 0; j < 5; j++) {
                    // Set tornado in the middle.
                    if (i == 2 && j == 2) {
                        _tornado.SetIndexPosition(new Vector2(i, j));
                        _tornado.UpdatePosition();
                        _tornado.ExcavateCard();
                        continue;
                    }

                    PlaygroundCard card = stack.Pop();
                    card.SetIndexPosition(new Vector2(i, j));
                    card.UpdatePosition();
                    card.ExcavateCard();

                    if (card.CardType == PlaygroundCardType.Start) {
                        card.ExcavateCard();
                    }

                    if ((i == 0 && j == 2) || (i == 1 && j == 1) || (i == 1 && j == 3) ||
                        (i == 2 && j == 0) || (i == 2 && j == 4) || (i == 3 && j == 1) ||
                        (i == 3 && j == 3) || (i == 4 && j == 2)) {
                        card.AddSand();
                    }
                }
            }

            GenerateStormDeck();
        }

        [Server]
        private void LoadCharacterData() {
            _charactersData.Clear();
            List<CharacterData> data = Resources.LoadAll<CharacterData>("Meteorologist")?.ToList();
            if (data == null) {
                throw new Exception("Characters data are missing");
            }

            data = data.OrderBy(x => Guid.NewGuid()).ToList();
            foreach (CharacterData characterData in data) {
                _charactersData.Enqueue(characterData);
            }
        }

        [Server]
        private void GenerateStormDeck() {
            _tornadoCards.Clear();
            int moveDirection = 0;
            int moveCounter = 0;
            List<TornadoDeckCardData> generatedSet = new List<TornadoDeckCardData>();
            foreach (TornadoCardSet cardSet in _tornadoCardsPrefab) {
                for (var i = 0; i < cardSet.count; i++) {
                    TornadoDeckCardData cardData = new TornadoDeckCardData();
                    cardData.cardPrefab = cardSet.cardPrefab;
                    TornadoMove tornadoMove = cardSet.cardPrefab as TornadoMove;
                    if (tornadoMove != null) {
                        cardData.direction = (TornadoDirection) (moveDirection % 4);
                        if (moveDirection++ % 8 == 0)
                            ++moveCounter;
                        cardData.steps = moveCounter;

                        cardData.text = $"Moving {cardData.direction}, {cardData.steps} steps";
                    }
                    else {
                        SunBeatsDown tornadoSun = cardSet.cardPrefab as SunBeatsDown;
                        if (tornadoSun != null) {
                            cardData.text = "Sun Beats Down";
                        }
                        else {
                            cardData.text = "Storm Pick Up";
                        }
                    }


                    generatedSet.Add(cardData);
                }
            }

            // Sort cards.
            generatedSet = generatedSet.OrderBy(x => Guid.NewGuid()).ToList();

            foreach (TornadoDeckCardData card in generatedSet) {
                _tornadoCards.Enqueue(card);
            }

            Debug.Log("Tornado cards has been generated and sorted.");
        }

        [Server]
        public void RegisterPlayerToQueue(Player player) {
            _playerOrder.Enqueue(player);
            if (_activePlayer == null) {
                _activePlayer = _playerOrder.Dequeue();
                _activePlayer.StartTurn();
                _activePlayerName = _activePlayer.PlayerName;
            }
        }

        [Server]
        public CharacterData GetCharacterData() {
            if (_charactersData.Count == 0)
                LoadCharacterData();

            return _charactersData.Dequeue();
        }

        [Server]
        public void DoAction() {
            --_stepsRemaning;
            if (_stepsRemaning != 0) return;
            Debug.Log($"Player {_activePlayer.PlayerName}: End his turn.");
            _activePlayer.EndTurn();
            _playerOrder.Enqueue(_activePlayer);

            onDustTurn?.Invoke();
            StartCoroutine(DesertTurn());
        }

        [Server]
        private void StartNewTurn() {
            _actualStormTickMark = _stromTickMarkValue;
            _activePlayer = _playerOrder.Dequeue();
            _stepsRemaning = _maxSteps;
            _activePlayer.StartTurn();
            _activePlayerName = _activePlayer.PlayerName;
        }

        [Server]
        private IEnumerator DesertTurn() {
            Debug.Log("Desert is in command");
            int pickUpCards = _actualStormTickMark;
            for (int i = 0; i < pickUpCards; i++) {
                if (_tornadoCards.Count == 0)
                    GenerateStormDeck();
                TornadoDeckCardData tornadoCard = _tornadoCards.Dequeue();
                onTornadoCardChanged?.Invoke(_tornadoCards.Count);
                GameObject card = Instantiate(tornadoCard.cardPrefab.gameObject, Vector3.zero, Quaternion.identity);
                if (card.TryGetComponent(out TornadoMove move)) {
                    move.Steps = tornadoCard.steps;
                    move.Direction = tornadoCard.direction;
                }

                card.GetComponent<TornadoCard>().DoAction();
                Destroy(card);
                yield return new WaitForSeconds(1);
            }

            StartNewTurn();
        }

        [Server]
        public bool IsPlayerTurn(Player player) {
            return player == _activePlayer;
        }

        [Server]
        public void PickUpAPart(PlaygroundCard destinationCard) {
            PlaygroundCardType[] type = new[] {
                PlaygroundCardType.Compass, PlaygroundCardType.Engine, PlaygroundCardType.Helm,
                PlaygroundCardType.Propeller
            };

            foreach (PlaygroundCardType itemType in type) {
                PlaygroundCard horizontalCard = null;
                PlaygroundCard verticalCard = null;
                foreach (PlaygroundCard card in _playgroundCards) {
                    if (!card.IsRevealed) continue;
                    if (card.CardType != itemType) continue;
                    if (card.CardDirection == CardDirection.Horizontal) {
                        horizontalCard = card;
                    }
                    else if (card.CardDirection == CardDirection.Vertical) {
                        verticalCard = card;
                    }

                    if (horizontalCard != null && verticalCard != null) break;
                }

                if (horizontalCard == null || verticalCard == null) continue;
                if (horizontalCard.GetIndexPosition().y == destinationCard.GetIndexPosition().y &&
                    verticalCard.GetIndexPosition().x == destinationCard.GetIndexPosition().x) {
                    if (!IsItemTaked(itemType.GetHashCode())) {
                        TakeItem(itemType.GetHashCode());
                    }
                }
            }
        }

        [Server]
        public void MoveTornadoToDestination(PlaygroundCard destination) {
            _tornado.SwapCards(destination);
        }

        [Server]
        public PlaygroundCard GetCardAtIndex(Vector2 position) {
            foreach (PlaygroundCard playgroundCard in _playgroundCards) {
                if (position.Equals(playgroundCard.GetIndexPosition()))
                    return playgroundCard;
            }

            return null;
        }

        [Server]
        public PlaygroundCard ServerGetStartCard() {
            foreach (PlaygroundCard card in _playgroundCards) {
                if (card.CardType == PlaygroundCardType.Start)
                    return card;
            }

            return null;
        }

        [Server]
        private void ServerRemoveWater(Character character, int water) {
            character.ServerRemoveWater(water);
        }

        [Server]
        private void ServerAddWater(Character character, int water) {
            character.ServerAddWater(water);
        }

        [Server]
        private void ServerMoveCharacter(Character character, PlaygroundCard card) {
            character.SetNewPosition(card);
        }

        [Server]
        private void ServerWeakStormCard() {
            _actualStormTickMark = Mathf.Max(_actualStormTickMark - 1, 0);
        }

        [Command]
        public void CmdWeakenStormCard() {
            ServerWeakStormCard();
        }

        public void CmdAddWater(Character character, int water) {
            ServerAddWater(character, water);
        }


        public void CmdRemoveWater(Character character, int water) {
            ServerRemoveWater(character, water);
        }

        public void CmdMoveCharacter(Character character, PlaygroundCard card) {
            ServerMoveCharacter(character, card);
        }

        #endregion

        #region Client

        [Client]
        public PlaygroundCard GetStartCard() {
            foreach (PlaygroundCard card in _playgroundCards) {
                if (card.CardType == PlaygroundCardType.Start)
                    return card;
            }

            return null;
        }

        [Client]
        private void LoadPlayground() {
            PlaygroundCard[] card = FindObjectsOfType<PlaygroundCard>();

            foreach (PlaygroundCard playgroundCard in card) {
                PlaygroundCardData data = Resources.Load<PlaygroundCardData>(playgroundCard.GetCardName());
                playgroundCard.SetData(data);
                if (playgroundCard.CardType == PlaygroundCardType.Tornado) {
                    _tornado = playgroundCard;
                }
                else {
                    _playgroundCards.Add(playgroundCard);
                }
            }
        }

        [Client]
        private void HandleSandStack(int oldValue, int newValue) {
            onDustCardSet?.Invoke(newValue);
        }

        [Client]
        public PlaygroundCard GetPlaygroundCardFromIndex(Vector2 index) {
            foreach (PlaygroundCard card in _playgroundCards) {
                if (card.GetIndexPosition().Equals(index)) {
                    return card;
                }
            }

            return null;
        }

        [Client]
        private void HandleStepCounter(int oldValue, int newCounterValue) {
            onAvaibleStepsChanged?.Invoke(newCounterValue);
        }

        [Client]
        public void ShowSpecialActionDialogue(Character character, PlaygroundCard source, PlaygroundCard destination) {
            bool abilityViewWasOpened = _openedAbilityActionInstance != null;

            if (!abilityViewWasOpened) {
                _openedAbilityActionInstance = Instantiate(_specialActionMenuPrefab, Vector3.zero, Quaternion.identity);
                _openedAbilityActionInstance.Initialize(character.Ability, source, destination);
            }

            if (destination.CardType == PlaygroundCardType.Tornado && abilityManager.CanClockOnTornado(character)) {
                if (abilityViewWasOpened) {
                    HandleSpecialActionDialogueClose();
                    _openedAbilityActionInstance =
                        Instantiate(_specialActionMenuPrefab, Vector3.zero, Quaternion.identity);
                    _openedAbilityActionInstance.Initialize(character.Ability, source, destination);

                    for (int i = 0; i < 3; i++) {
                        SelectPlayerUI playerSelect =
                            Instantiate(_selectPlayerPrefab, Vector3.zero, Quaternion.identity);
                        playerSelect.transform.parent = _openedAbilityActionInstance.GetActionContentHolderTransform();
                        playerSelect.transform.localScale = Vector3.one;
                        SelectPlayerUI.Value value = new SelectPlayerUI.Value();
                        value.index = i;
                        value.gameObject = _tornadoCards.ToArray()[i].cardPrefab.gameObject;
                        value.itemName = _tornadoCards.ToArray()[i].text;
                        value.itemColor = Color.gray;
                        playerSelect.Initialize(value, character.Ability);
                        playerSelect.onValueSelected += HandleSpecialActionSelectedPlayer;
                    }

                    SelectPlayerUI emptySelect = Instantiate(_selectPlayerPrefab, Vector3.zero, Quaternion.identity);
                    emptySelect.transform.parent = _openedAbilityActionInstance.GetActionContentHolderTransform();
                    emptySelect.transform.localScale = Vector3.one;
                    SelectPlayerUI.Value emptyValue = new SelectPlayerUI.Value();
                    emptyValue.index = 4;
                    emptyValue.gameObject = null;
                    emptyValue.itemName = "Do not move card";
                    emptyValue.itemColor = Color.gray;
                    emptySelect.Initialize(emptyValue, character.Ability);
                    emptySelect.onValueSelected += HandleSpecialActionSelectedPlayer;
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
                if (abilityManager.CanUsePlaygroundCardAsPlayer(character) && character.Position == destination &&
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
                    if (ch.Position != source && !AbilityManager.CanUsePlaygroundCardAsPlayer(character)) continue;

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
        private void HandleSpecialActionDialogueClose() {
            foreach (Transform child in _openedAbilityActionInstance.GetActionContentHolderTransform()) {
                child.GetComponent<SelectPlayerUI>().onValueSelected -= HandleSpecialActionSelectedPlayer;
                Destroy(child.gameObject);
            }

            _openedAbilityActionInstance.onCancel -= HandleSpecialActionDialogueClose;
            Destroy(_openedAbilityActionInstance.gameObject);
            _openedAbilityActionInstance = null;
        }

        [Client]
        private void HandleSpecialActionSelectedPlayer(AbilityType ability, GameObject selectedObject, int index) {
            Character source = NetworkClient.connection.identity.GetComponent<Character>();
            if (ability == AbilityType.Meteorologist && index == -1 && selectedObject == null) {
                ShowSpecialActionDialogue(source, source.Position, _tornado);
            }
            else {
                AbilityManager.DoSpecialAction(source, ability, selectedObject,
                    _openedAbilityActionInstance.GetInputValue(), _openedAbilityActionInstance.SourceCard,
                    _openedAbilityActionInstance.DestinationCard, index);

                HandleSpecialActionDialogueClose();
            }
        }

        #endregion
    }
}