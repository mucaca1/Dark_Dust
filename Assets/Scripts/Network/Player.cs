using System;
using Game;
using Game.Cards.PlaygroundCards;
using Game.Characters;
using Mirror;
using UnityEngine;

namespace Network {
    public class Player : NetworkBehaviour {

        [SyncVar(hook = nameof(HandleChangePlayer))]
        private bool isYourTurn = false;

        [field: SyncVar] [SerializeField] public string _playerName = "PlayerName";

        [field: SyncVar] public Color PlayerColor { get; set; } = Color.black;

        private PlayerController _controller = null;

        public event Action<bool, string> onChangeActivePlayer;

        public bool IsYourTurn => isYourTurn;


        public string PlayerName {
            get => _playerName;
            set => _playerName = value;
        }

        [ClientCallback]
        private void Start() {
            GameManager.onDustTurn += HandleDustTurn;
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

        #endregion
    }
}