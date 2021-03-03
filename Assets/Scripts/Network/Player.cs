using Mirror;
using UnityEngine;

namespace Network {
    public class Player : NetworkBehaviour {
        [field: SyncVar] public string PlayerName { get; set; } = "PlayerName";

        [field: SyncVar] public Color PlayerColor { get; set; } = Color.black;
    }
}