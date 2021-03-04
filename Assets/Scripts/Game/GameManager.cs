using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Cards.PlaygroundCards;
using Mirror;
using Network;
using UnityEngine;

namespace Game {
    public class GameManager : NetworkBehaviour {
        private Player _activePlayer = null;

        [SyncVar] private int _stepsRemaning = 4;
        [SerializeField] private Transform playGroundStartTransform = null;

        [SerializeField] private GameObject playgroundCardPrefab = null;

        private List<PlaygroundCard> _playgroundCards = new List<PlaygroundCard>();
        private PlaygroundCardData[] _playgroundCardDatas;
        private Queue<Player> _playerOrder = new Queue<Player>();
        private static int _maxSteps = 4;

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
                    _playgroundCards.Add(newCardData);
                    NetworkServer.Spawn(card);
                }
            }

            // Sort cards.
            _playgroundCards = _playgroundCards.OrderBy(x => Guid.NewGuid()).ToList();

            Stack<PlaygroundCard> stack = new Stack<PlaygroundCard>();
            PlaygroundCard tornado = null;
            foreach (PlaygroundCard playgroundCard in _playgroundCards) {
                if (playgroundCard.CardType != PlaygroundCardType.Tornado) {
                    stack.Push(playgroundCard);
                }
                else {
                    tornado = playgroundCard; // Extract tornado card
                }
            }

            // Set card position.
            for (int i = 0; i < 5; i++) {
                for (int j = 0; j < 5; j++) {
                    // Set tornado in the middle.
                    if (i == 2 && j == 2) {
                        tornado.SetIndexPosition(new Vector2(i, j));
                        tornado.UpdatePosition();
                        tornado.ExcavateCard();
                        tornado.UpdateRotation();
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