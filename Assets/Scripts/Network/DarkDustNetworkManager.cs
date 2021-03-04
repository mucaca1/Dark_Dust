using System;
using Game;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Network {
    public class DarkDustNetworkManager : NetworkManager {
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
    }
}