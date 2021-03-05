using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Cards.PlayCards.Tornado;
using Game.Cards.PlaygroundCards;
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

        private Player _activePlayer = null;

        [SyncVar] private int _stepsRemaning = 4;
        [SyncVar] private int _takedItems = 0;
        [SerializeField] private Transform playGroundStartTransform = null;

        [SerializeField] private GameObject playgroundCardPrefab = null;
        [SerializeField] private TornadoCardSet[] _tornadoCardsPrefab = new TornadoCardSet[0];

        private List<TornadoCard> _tornadoCards = new List<TornadoCard>();

        private List<PlaygroundCard> _playgroundCards = new List<PlaygroundCard>();
        private PlaygroundCard _tornado = null;
        private PlaygroundCardData[] _playgroundCardDatas;
        private Queue<Player> _playerOrder = new Queue<Player>();
        private static int _maxSteps = 4;

        private int _stromTickMark = 2;

        private void Start() {
            if (isServer) {
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
                        _tornado.UpdateRotation();
                        continue;
                    }

                    PlaygroundCard card = stack.Pop();
                    card.SetIndexPosition(new Vector2(i, j));
                    card.UpdatePosition();

                    if (card.CardType == PlaygroundCardType.Start) {
                        card.ExcavateCard();
                        card.UpdateRotation();
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
        private void GenerateStormDeck() {
            int moveDirection = 0;
            int moveCounter = 0;
            foreach (TornadoCardSet cardSet in _tornadoCardsPrefab) {
                while (0 != cardSet.count--) {
                    TornadoMove tornadoMove = cardSet.cardPrefab as TornadoMove;
                    if (tornadoMove != null) {
                        tornadoMove.Direction = (TornadoDirection) (moveDirection % 4);
                        if (moveDirection++ % 4 == 0)
                            ++moveCounter;
                        tornadoMove.Steps = moveCounter;
                    }

                    _tornadoCards.Add(cardSet.cardPrefab);
                }
            }

            // Sort cards.
            _tornadoCards = _tornadoCards.OrderBy(x => Guid.NewGuid()).ToList();
            
            Debug.Log("Tornado cards has been generated and sorted.");
        }

        [Server]
        public void RegisterPlayerToQueue(Player player) {
            _playerOrder.Enqueue(player);
            if (_activePlayer == null) {
                _activePlayer = _playerOrder.Dequeue();
                _activePlayer.StartTurn();
            }
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
            _activePlayer.StartTurn();
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