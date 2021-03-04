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
        [SyncVar][SerializeField] private int _activePlayer = -1;
        [SerializeField] private Transform playGroundStartTransform = null;

        [SerializeField] private GameObject playgroundCardPrefab = null;
        [SerializeField] private GameObject dustCardPrefab;

        private List<PlaygroundCard> _playgroundCards = new List<PlaygroundCard>();
        private PlaygroundCardData[] _playgroundCardDatas;
        private Queue<int> _playerOrder = new Queue<int>();
        
        public static event Action<int> OnChangeActivePlayer;

        private void Start() {
            PlaygroundCard.OnDustNeedToCreate += AddDustCard;
            if (isServer) {
                _playgroundCardDatas = Resources.LoadAll<PlaygroundCardData>("");

                Debug.Log(_playgroundCardDatas.Length == 0
                    ? "No playground cards was found. Check Resources folder"
                    : "Playground cards was loaded successfully");

                GenerateNewPlayGround();
            }

            if (isClient) {
                LoadPlayground();
                RegisterPlayerToQueue(NetworkClient.connection.connectionId);
                if (_activePlayer == -1) {
                    _activePlayer = _playerOrder.Dequeue();
                }
            }
        }

        private void OnDestroy() {
            PlaygroundCard.OnDustNeedToCreate -= AddDustCard;
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
        private void AddDustCard(PlaygroundCard card) {
            GameObject dust = Instantiate(dustCardPrefab, Vector3.zero,
                Quaternion.identity);

            SandCard sandCard = dust.GetComponent<SandCard>();
            sandCard.SetStartPosition(playGroundStartTransform.position);
            sandCard.SetIndexPosition(card.GetIndexPosition());
            sandCard.UpdatePosition();

            NetworkServer.Spawn(dust);
        }

        [Server]
        private void RegisterPlayerToQueue(int playerId) {
            _playerOrder.Enqueue(playerId);
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