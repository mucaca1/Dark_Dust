using System;
using System.Collections.Generic;
using Game.Characters;
using Game.Characters.Ability;
using Mirror;
using Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Cards.PlaygroundCards {
    public class PlaygroundCard : NetworkBehaviour {
        [SerializeField] private TMP_Text _sandCounterText = null;
        [SerializeField] private GameObject _hoverMark = null;
        [SerializeField] private MeshRenderer backImage;
        [SerializeField] private MeshRenderer frontImage;
        [SerializeField] private Transform[] positionToStay;
        [SerializeField] private Transform cardReference = null;
        [SerializeField] private Transform sandSpawnerReference = null;
        [SyncVar] protected string cardName;

        [SyncVar(hook = nameof(UpdateRotation))]
        private bool _isRevealed = false;

        [SyncVar(hook = nameof(HandleSandCount))]
        private int _sandCount = 0;

        [SerializeField] private SandCard dustCardPrefab;

        private PlaygroundCardType _cardType;
        private CardDirection _cardDirection;

        [SyncVar] protected Vector3 position;
        protected Vector3 playgroundStartPosition;
        [SyncVar] protected Vector2 indexPosition;

        public PlaygroundCardType CardType => _cardType;
        public CardDirection CardDirection => _cardDirection;
        public int SandCount => _sandCount;

        protected float playgroundCardOffsetY = 0f;
        private float playgroundCardSize = 1f;
        private float playgroundCardOffset = .1f;

        private Character[] _stayingPositionPlayer = new Character[5];
        public bool IsRevealed => _isRevealed;

        public static event Action onAddSand;
        public static event Action onRemoveSand;

        public Vector3 GetPosition() {
            return position;
        }

        public void SetPosition(Vector3 position) {
            this.position = position;
        }

        public Vector2 GetIndexPosition() {
            return indexPosition;
        }

        public void SetIndexPosition(Vector2 position) {
            indexPosition = position;
        }

        public string GetCardName() {
            return cardName;
        }

        public void SetStartPosition(Vector3 startPosition) {
            playgroundStartPosition = startPosition;
        }

        [ServerCallback]
        private void Awake() {
            for (int i = 0; i < _stayingPositionPlayer.Length; i++) {
                _stayingPositionPlayer[i] = null;
            }
        }

        public void UpdateRotation(bool oldValue, bool newValue) {
            if (newValue)
                cardReference.Rotate(0.0f, 0.0f, 180.0f, Space.Self);
        }

        public void UpdatePosition() {
            Vector3 pos = new Vector3(
                playgroundStartPosition.x + playgroundCardOffset + (indexPosition.x * playgroundCardOffset) +
                playgroundCardSize / 2 + (indexPosition.x * playgroundCardSize),
                playgroundStartPosition.y + 0f + playgroundCardOffsetY,
                playgroundStartPosition.z + playgroundCardOffset + (indexPosition.y * playgroundCardOffset) +
                playgroundCardSize / 2 + (indexPosition.y * playgroundCardSize)
            );
            gameObject.transform.position = pos;
            SetPosition(pos);

            for (var i = 0; i < _stayingPositionPlayer.Length; i++) {
                if (_stayingPositionPlayer[i] == null) continue;

                _stayingPositionPlayer[i].gameObject.transform.position = positionToStay[i].position;
            }
        }

        public void SetData(PlaygroundCardData cardData, Vector3 startPosition) {
            playgroundStartPosition = startPosition;
            SetData(cardData);
        }

        public void SetData(PlaygroundCardData cardData) {
            cardName = cardData.name;
            backImage.material.mainTexture = cardData.BackImage.texture;
            frontImage.material.mainTexture = cardData.FrontImage.texture;
            _cardType = cardData.CardType;
            _cardDirection = cardData.CardDirection;
        }

        public void SetData(PlaygroundCard cardData) {
            backImage.material.mainTexture = cardData.backImage.material.mainTexture;
            frontImage.material.mainTexture = cardData.frontImage.material.mainTexture;
            _cardType = cardData._cardType;
            _cardDirection = cardData._cardDirection;
        }

        public List<Character> GetCharacters() {
            List<Character> characters = new List<Character>();
            for (var i = 0; i < _stayingPositionPlayer.Length; i++) {
                if (_stayingPositionPlayer[i] == null) continue;

                characters.Add(_stayingPositionPlayer[i]);
            }

            return characters;
        }


        public bool CanActivePlayerDoAction(Character character, bool isOwnerCharacter) {
            if (!character.gameObject.GetComponent<Player>().IsYourTurn && isOwnerCharacter) return false;

            if (!isOwnerCharacter) {
                return CanCharacterDoMoveAction(character,
                    character.GetComponent<PlayerController>().SpecialAction);
            }

            return CanCharacterDoAction(character.GetComponent<PlayerController>().Action, character);
        }

        public bool CanCharacterDoAction(PlayerAction action, Character character) {
            switch (action) {
                case PlayerAction.WALK:
                    return CanCharacterDoMoveAction(character,
                        character.GetComponent<PlayerController>().SpecialAction);
                case PlayerAction.EXCAVATE:
                    return CanCharacterExcavate(character);
                case PlayerAction.REMOVE_SAND:
                    return CanCharacterDoRemoveSandAction(character);
                case PlayerAction.PICK_UP_A_PART:
                    return CanCharacterPickUpAPart(character);
            }

            return false;
        }

        public bool CanCharacterDoMoveAction(Character character, bool useSpecialAction) {
            Player player = character.GetComponent<Player>();
            if (useSpecialAction &&
                player.AbilityManager.CanClockOnTornado(character)) {
                return _cardType == PlaygroundCardType.Tornado;
            }

            if (_cardType == PlaygroundCardType.Tornado) {
                return false;
            }

            if (useSpecialAction && character.GetComponent<Player>().AbilityManager.CanMoveWithCharacter(character)) {
                foreach (Character character1 in GetCharacters()) {
                    if (character == character1) continue;
                    return true;
                }

                return false;
            }

            if (character.Position == this) {
                if (useSpecialAction) {
                    return (player.AbilityManager.CanPickUpWater(character, this) ||
                            player.AbilityManager.CanGiveWaterToSomePlayer(character,
                                FindObjectsOfType<Character>(), this));
                }

                return false;
            }

            if (character.Ability == AbilityType.WaterCarrier && useSpecialAction) {
                return (player.AbilityManager.CanPickUpWater(character, this) ||
                        player.AbilityManager.CanGiveWaterToSomePlayer(character, FindObjectsOfType<Character>(),
                            this));
            }

            if (_cardType == PlaygroundCardType.Cave && character.Position.CardType == PlaygroundCardType.Cave) {
                return CanMoveToThisPart() || player.AbilityManager.CanMoveToCard(character);
            }

            if (!(CanSeeThisCard(character.Position) ||
                  player.AbilityManager.CanMoveHorizontal(character, this))) return false;
            if (!(CanMoveToThisPart() || player.AbilityManager.CanMoveToCard(character)) &&
                (
                    character.Position.CanMoveToThisPart() ||
                    player.AbilityManager.CanMoveFromCard(character, character.Position, this)
                )) return false;

            return true;
        }

        public bool CanCharacterDoRemoveSandAction(Character character) {
            if (_cardType == PlaygroundCardType.Tornado) return false;
            if (!(CanSeeThisCard(character.Position) ||
                  GetComponent<Player>().AbilityManager.CanMoveHorizontal(character, this))) return false;
            return _sandCount > 0;
        }

        public bool CanCharacterExcavate(Character character) {
            if (_cardType == PlaygroundCardType.Tornado) return false;
            return IsCharacterHere(character) && !_isRevealed && _sandCount == 0;
        }

        public bool CanCharacterPickUpAPart(Character character) {
            if (_cardType == PlaygroundCardType.Tornado) return false;
            PlaygroundCardType[] type = new[] {
                PlaygroundCardType.Compass, PlaygroundCardType.Engine, PlaygroundCardType.Helm,
                PlaygroundCardType.Propeller
            };

            foreach (PlaygroundCardType itemType in type) {
                PlaygroundCard horizontalCard = null;
                PlaygroundCard verticalCard = null;
                foreach (PlaygroundCard card in GameManager.Instance.PlaygroundCards) {
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
                if (horizontalCard.GetIndexPosition().y == GetIndexPosition().y &&
                    verticalCard.GetIndexPosition().x == GetIndexPosition().x) {
                    if (!GameManager.Instance.IsItemTaked(itemType.GetHashCode())) {
                        return character.Position == this;
                    }
                }
            }

            return false;
        }

        public bool CanMoveToThisPart() {
            return _sandCount <= 1;
        }

        #region Server

        [Server]
        public void ExcavateCard(Character who = null) {
            _isRevealed = true;
            if (_cardType == PlaygroundCardType.Water) {
                foreach (Character character in GetCharacters()) {
                    character.AddWater(2);
                }
            }

            if (who != null) {
                if (_cardType == PlaygroundCardType.Components || _cardType == PlaygroundCardType.Cave) {
                    // Add card for player
                    who.GetComponent<Player>().PlayerCards.Add(GameManager.Instance.GetNextItemCard());
                }
            }
        }


        public bool IsCharacterHere(Character character) {
            return character.Position == this;
        }

        [Server]
        public Vector3 GetNextPlayerPosition(Character character) {
            for (int i = 0; i < _stayingPositionPlayer.Length; i++) {
                if (_stayingPositionPlayer[i] == null) {
                    _stayingPositionPlayer[i] = character;
                    return positionToStay[i].position;
                }
            }

            return Vector3.zero;
        }

        [Server]
        public void LeavePart(Character character) {
            for (int i = 0; i < _stayingPositionPlayer.Length; i++) {
                if (_stayingPositionPlayer[i] == character)
                    _stayingPositionPlayer[i] = null;
            }
        }

        public bool CanSeeThisCard(PlaygroundCard from) {
            if (from.indexPosition.x != indexPosition.x && from.indexPosition.y != indexPosition.y) return false;

            if (Mathf.Abs(from.indexPosition.x - indexPosition.x) > 1 ||
                Mathf.Abs(from.indexPosition.y - indexPosition.y) > 1) return false;
            return true;
        }


        [Server]
        public void RemoveSand(int count = 1) {
            _sandCount = Mathf.Max(0, _sandCount - count);
            onRemoveSand?.Invoke();
        }

        [Server]
        public void AddSand() {
            ++_sandCount;
            onAddSand?.Invoke();
        }

        [Server]
        public bool IsDust() {
            return _sandCount != 0;
        }

        [Server]
        public void SwapCards(PlaygroundCard destinationCard) {
            Vector2 destinationPosition = destinationCard.GetIndexPosition();
            destinationCard.SetIndexPosition(indexPosition);
            indexPosition = destinationPosition;

            destinationCard.UpdatePosition();
            UpdatePosition();

            foreach (Character character in destinationCard.GetCharacters()) {
                character.SetNewPosition(destinationCard);
            }

            foreach (Character character in GetCharacters()) {
                character.SetNewPosition(this);
            }
        }

        #endregion

        #region Client

        [Client]
        private void HandleSandCount(int oldDustCount, int newDustCount) {
            if (newDustCount == 1) {
                GameObject dust = Instantiate(dustCardPrefab.gameObject, sandSpawnerReference.position,
                    Quaternion.identity);
                // Refactor append as child
                dust.transform.parent = sandSpawnerReference;
            }

            foreach (Transform children in sandSpawnerReference) {
                SandCard sandCard = children.GetComponent<SandCard>();
                sandCard.HandleDustValue(newDustCount);
            }

            _sandCounterText.text = newDustCount.ToString();
            _sandCounterText.gameObject.SetActive(newDustCount > 2);
        }

        [ClientCallback]
        void OnMouseOver() {
            Character character = NetworkClient.connection.identity.GetComponent<Character>();
            Color color =
                CanActivePlayerDoAction(character.CharacterInControl, character.CharacterInControl == character)
                    ? Color.green
                    : Color.red;
            _hoverMark.GetComponentInChildren<Image>().color = color;
            _hoverMark.SetActive(true);
        }

        [ClientCallback]
        void OnMouseExit() {
            _hoverMark.SetActive(false);
        }

        #endregion
    }
}