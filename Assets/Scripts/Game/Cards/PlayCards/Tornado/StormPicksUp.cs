using Mirror;
using UnityEngine;

namespace Game.Cards.PlayCards.Tornado {
    [System.Serializable]
    public class StormPicksUp : TornadoCard {
        [Server]
        public override void DoAction() {
            GameManager.Instance.StormTickUp();
            Debug.Log("Storm Pick Up");
        }

        public override string GetString() {
            return "Storm Pick Up";
        }
    }
}