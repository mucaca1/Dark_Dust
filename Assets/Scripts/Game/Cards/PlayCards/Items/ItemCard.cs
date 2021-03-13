using Game.Characters;
using Mirror;
using Network;
using UnityEngine;

namespace Game.Cards.PlayCards.Items {
    public class ItemCard : NetworkBehaviour {

        [SerializeField] private int cardId = -1;

        [SerializeField] private string _cardName;
        [SerializeField] private string _description;

        public int CardId => cardId;

        public virtual void DoSpecialAction() {
            Debug.Log("Special Action");
        }

        public void GiveCardToThePlayer(Player player) {
            ItemCard gm = Instantiate(this, Vector3.zero, Quaternion.identity);
            NetworkServer.Spawn(gm.gameObject, player.connectionToServer);
            
            NetworkServer.Destroy(gameObject);
        }

        public bool CanGiveCardToThePlayer(Player player) {
            return connectionToClient.identity.GetComponent<Character>().Position ==
                   player.GetComponent<Character>().Position;
        }
    }
}