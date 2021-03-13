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
using Random = System.Random;

namespace Game {
    public class GameManager : NetworkBehaviour {
        [Serializable]
        private class CardSet {
            public int count = 0;
            public TornadoCard cardPrefab = null;
        }

        private class TornadoDeckCardData {
            public TornadoCard cardPrefab = null;
            public int steps = 0;
            public TornadoDirection direction;
            [SyncVar] public string text = "";
        }

        private Player _activePlayer = null;

        [SyncVar] private string _activePlayerName = "";

        [SyncVar(hook = nameof(HandleStepCounter))]
        private int _stepsRemaning = 4;

        [SyncVar] private int _takedItems = 0;
        [SyncVar] private int _takedItemsCount = 0;
        public SyncList<string> futureCards = new SyncList<string>();
        [SerializeField] private Transform playGroundStartTransform = null;
        [SerializeField] private GameObject playgroundCardPrefab = null;
        [SerializeField] private CardSet[] _tornadoCardsPrefab = new CardSet[0];
        private Queue<int> _itemsCards = new Queue<int>();

        private Queue<TornadoDeckCardData> _tornadoCards = new Queue<TornadoDeckCardData>();

        private List<PlaygroundCard> _playgroundCards = new List<PlaygroundCard>();
        private PlaygroundCard _tornado = null;
        private PlaygroundCardData[] _playgroundCardDatas;
        private Queue<Player> _playerOrder = new Queue<Player>();
        private static int _maxSteps = 4;

        [SyncVar(hook = nameof(HandleSandStack))]
        private int _sandStackReaming = 48;

        [SyncVar] private int _actualStormTickMark = 2;
        [SyncVar] private int _stromTickMarkValue = 2;
        private Queue<CharacterData> _charactersData = new Queue<CharacterData>();

        public PlaygroundCard Tornado => _tornado;

        public List<PlaygroundCard> PlaygroundCards => _playgroundCards;

        public Player ActivePlayer => _activePlayer;

        public string ActivePlayerName => _activePlayerName;

        public int ActualStormTickMark => _actualStormTickMark;
        public int StormTickMarkValue => _stromTickMarkValue;

        public event Action<int> onTakedItemsIncrease;
        public event Action<int> onStromTickMarkChanged;
        public event Action<int> onDustCardSet;
        public event Action<int> onTornadoCardChanged;

        public static event Action<int> onAvaibleStepsChanged;
        public static event Action onPlaygroundLoaded;

        private event Action getTornadoNextCards;

        public static event Action onDustTurn;

        private static GameManager _gameManager;

        public static GameManager Instance {
            get { return _gameManager; }
        }


        private void Awake() {
            if (_gameManager != null && _gameManager != this) {
                Destroy(gameObject);
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
                GenerateItemCards();
                getTornadoNextCards += ServerGetTornadoCards;
            }

            if (isClient) {
                LoadPlayground();
                onPlaygroundLoaded?.Invoke();
            }
        }

        private void OnDestroy() {
            if (isServer) {
                getTornadoNextCards -= ServerGetTornadoCards;
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
        private void GenerateItemCards() {
            List<int> seed = new List<int>();
            for (int i = 2; i < 14; i++) {
                seed.Add((int)(i / 2));
            }
            var shuffledcards = seed.OrderBy(a => Guid.NewGuid()).ToList();
            
            foreach (int i in shuffledcards) {
                _itemsCards.Enqueue(i);
            }
        }

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
            List<CharacterData> data = Resources.LoadAll<CharacterData>("")?.ToList();
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
            foreach (CardSet cardSet in _tornadoCardsPrefab) {
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
        public void ServerWeakStormCard() {
            _actualStormTickMark = Mathf.Max(_actualStormTickMark - 1, 0);
        }

        [Server]
        public void ServerGetTornadoCards() {
            TornadoDeckCardData[] t = _tornadoCards.ToArray();
            for (int i = 0; i < t.Length; i++) {
                futureCards.Add(t[i].text);
                if (i == 2) break;
            }
        }
        
        [Server]
        public int GetNextItemCard() {
            return _itemsCards.Dequeue();
        }

        [Server]
        public void MoveCardToTheBottom(int cardIndex) {
            futureCards.Clear();
            List<TornadoDeckCardData> t = _tornadoCards.ToList();

            TornadoDeckCardData card = t[cardIndex];
            t.Remove(card);
            t.Add(card);
            _tornadoCards.Clear();
            foreach (TornadoDeckCardData tornadoDeckCardData in t) {
                _tornadoCards.Enqueue(tornadoDeckCardData);
            }
        }

        private void CmdGetTornadoCards() {
            ServerGetTornadoCards();
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

        #endregion
    }
}