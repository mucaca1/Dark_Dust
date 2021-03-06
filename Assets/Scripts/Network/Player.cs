using System;
using Game;
using Game.Cards.PlaygroundCards;
using Game.Characters;
using Mirror;
using UnityEngine;

namespace Network {
    public class Player : NetworkBehaviour {
        [SerializeField] private GameObject _characterPrefab = null;

        [SyncVar(hook = nameof(HandleChangePlayer))]
        private bool isYourTurn = false;

        [field: SyncVar] public string PlayerName { get; set; } = "PlayerName";

        [field: SyncVar] public Color PlayerColor { get; set; } = Color.black;
        public Character Character { get; set; } = null;

        public event Action<bool, string> onChangeActivePlayer;

        public bool IsYourTurn => isYourTurn;

        [ServerCallback]
        private void Start() {
            CreateCharacter();
        }

        #region Server

        [Server]
        private void CreateCharacter() {
            if (Character == null) {
                GameManager gm = FindObjectOfType<GameManager>();
                CharacterData characterData = gm.GetCharacterData();
                GameObject characterObj = Instantiate(_characterPrefab.gameObject, Vector3.zero, Quaternion.identity);

                Character character = characterObj.GetComponent<Character>();
                character.InitializePlayer(characterData);
                Character = character;
                NetworkServer.Spawn(characterObj, connectionToClient);
            }
        }

        [Server]
        public void EndTurn() {
            isYourTurn = false;
        }

        [Server]
        public void StartTurn() {
            isYourTurn = true;
        }

        #endregion

        #region Client

        [Client]
        private void HandleChangePlayer(bool oldValue, bool newValue) {
            onChangeActivePlayer?.Invoke(newValue, PlayerName);
        }

        #endregion
    }
}