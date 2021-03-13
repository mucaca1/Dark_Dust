using System.Collections.Generic;
using Game;
using Mirror;
using UnityEngine;

namespace Network {
    public class DarkDustNetworkManager : NetworkManager {

        private List<Player> _players = new List<Player>();

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
            
            _players.Add(player);

            GameManager.Instance.RegisterPlayerToQueue(player);
        }
    }
}