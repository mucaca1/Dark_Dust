using Mirror;
using UnityEngine;

namespace Game.Cards.PlayCards.Tornado {
    [System.Serializable]
    public class SunBeatsDown : TornadoCard {
        [Server]
        public override void DoAction() {
            GameManager gameManager = FindObjectOfType<GameManager>();
            Debug.Log("Sun Beats Down");
        }
    }
}