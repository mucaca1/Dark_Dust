using System;
using Game;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Network {
    public class DarkDustNetworkManager : NetworkManager {

        [SerializeField] private GameManager _gameManager = null;
        
        public override void OnServerAddPlayer(NetworkConnection conn) {
            base.OnServerAddPlayer(conn);

            // Initialize player
            Player player = conn.identity.GetComponent<Player>();
            player.PlayerName = $"Player {numPlayers}";
            player.PlayerColor = new Color(
                Random.Range(0, 1f),
                Random.Range(0, 1f),
                Random.Range(0, 1f)
            );
        }

        public override void OnServerConnect(NetworkConnection conn) {
            GameObject gameManager = Instantiate(_gameManager.gameObject, Vector3.zero, Quaternion.identity);
            NetworkServer.Spawn(gameManager);
        }
    }
}