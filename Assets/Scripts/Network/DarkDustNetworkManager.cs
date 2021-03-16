using System;
using System.Collections.Generic;
using Game;
using Game.Characters;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace Network {
    public class DarkDustNetworkManager : NetworkManager {
        private List<Player> _players = new List<Player>();
        private bool isGameInProgress = false;

        public static event Action ClientOnConnected;
        public static event Action ClientOnDisconnected;

        public List<Player> Players => _players;

        #region Server

        public override void OnServerConnect(NetworkConnection conn) {
            if (!isGameInProgress) return;

            conn.Disconnect();
        }

        public override void OnServerDisconnect(NetworkConnection conn) {
            Player player = conn.identity.GetComponent<Player>();

            _players.Remove(player);

            base.OnServerDisconnect(conn);
        }

        public override void OnStopServer() {
            _players.Clear();
            isGameInProgress = false;
        }

        public void StartGame() {
            if (_players.Count < 1) return;

            isGameInProgress = true;

            ServerChangeScene("Game");
        }

        public override void OnServerAddPlayer(NetworkConnection conn) {
            base.OnServerAddPlayer(conn);

            // Initialize player
            Player player = conn.identity.GetComponent<Player>();
            _players.Add(player);
            player.PlayerName = $"Player {numPlayers}";
            player.PlayerColor = new Color(
                Random.Range(0, 1f),
                Random.Range(0, 1f),
                Random.Range(0, 1f)
            );

            player.SetPartyOwner(_players.Count == 1);
        }

        public override void OnServerSceneChanged(string sceneName) {
            if (SceneManager.GetActiveScene().name.StartsWith("Game")) {
                foreach (Player player in _players) {
                    player.GetComponent<Character>().ServerStartGame();
                    GameManager.Instance.RegisterPlayerToQueue(player);
                }
            }
        }

        #endregion

        #region Client

        public override void OnClientConnect(NetworkConnection conn) {
            base.OnClientConnect(conn);
            ClientOnConnected?.Invoke();
        }

        public override void OnClientDisconnect(NetworkConnection conn) {
            base.OnClientDisconnect(conn);
            ClientOnDisconnected?.Invoke();
        }

        public override void OnStopClient() {
            _players.Clear();
        }

        #endregion
    }
}