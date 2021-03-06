using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Cards.PlayCards.Tornado;
using Game.Cards.PlaygroundCards;
using Game.Characters;
using Mirror;
using Network;
using NUnit.Framework;
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
        }

        private Player _activePlayer = null;

        [SyncVar] private int _stepsRemaning = 4;
        [SyncVar] private int _takedItems = 0;
        [SerializeField] private Transform playGroundStartTransform = null;

        [SerializeField] private Character _characterPrefab = null;
        [SerializeField] private GameObject playgroundCardPrefab = null;
        [SerializeField] private TornadoCardSet[] _tornadoCardsPrefab = new TornadoCardSet[0];

        private Queue<TornadoDeckCardData> _tornadoCards = new Queue<TornadoDeckCardData>();

        private List<PlaygroundCard> _playgroundCards = new List<PlaygroundCard>();
        private PlaygroundCard _tornado = null;
        private PlaygroundCardData[] _playgroundCardDatas;
        private Queue<Player> _playerOrder = new Queue<Player>();
        private static int _maxSteps = 4;
        [SyncVar] private int _sandStackReaming = 48;

        private int _stromTickMark = 2;
        private Queue<CharacterData> _charactersData = new Queue<CharacterData>(); 

        public PlaygroundCard Tornado => _tornado;

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
            }
        }

        public Vector3 GetPlaygroundStartPosition() {
            return playGroundStartTransform.position;
        }

        private bool IsItemTaked(int index) {
            return (_takedItems & (1 << index)) != 0;
        }

        private void TakeItem(int index) {
            _takedItems |= 1 << index;
        }

        #region Server

        [Server]
        public void StormTickUp() {
            ++_stromTickMark;
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
            if (player.Character == null) {
                CharacterData characterData = GetCharacterData();
                GameObject characterObj = Instantiate(_characterPrefab.gameObject, Vector3.zero, Quaternion.identity);

                Character character = characterObj.GetComponent<Character>();
                character.InitializePlayer(characterData);
                player.Character = character;
                NetworkServer.Spawn(characterObj, connectionToClient);
            }
            _playerOrder.Enqueue(player);
            if (_activePlayer == null) {
                _activePlayer = _playerOrder.Dequeue();
                _activePlayer.StartTurn();
            }
        }

        [Server]
        private CharacterData GetCharacterData() {
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
            _activePlayer = _playerOrder.Dequeue();
            _stepsRemaning = _maxSteps;

            StartCoroutine(DesertTurn());

            _activePlayer.StartTurn();
        }

        [Server]
        private IEnumerator DesertTurn() {
            Debug.Log("Desert is in command");
            int pickUpCards = _stromTickMark;
            for (int i = 0; i < pickUpCards; i++) {
                if (_tornadoCards.Count == 0)
                    GenerateStormDeck();
                TornadoDeckCardData tornadoCard = _tornadoCards.Dequeue();
                GameObject card = Instantiate(tornadoCard.cardPrefab.gameObject, Vector3.zero, Quaternion.identity);
                if (card.TryGetComponent(out TornadoMove move)) {
                    move.Steps = tornadoCard.steps;
                    move.Direction = tornadoCard.direction;
                }

                card.GetComponent<TornadoCard>().DoAction();
                Destroy(card);
                yield return new WaitForSeconds(1);
            }
        }

        [Server]
        public bool IsPlayerTurn(Player player) {
            return player == _activePlayer;
        }

        [Server]
        public bool TryPickUpAPart(PlaygroundCard destinationCard) {
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
                        return true;
                    }
                }
            }

            return false;
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
            }
        }

        #endregion
    }
}