using Mirror;
using UnityEngine;

namespace Game.Cards.PlayCards.Tornado {
    public abstract class TornadoCard : NetworkBehaviour {
        
        [SerializeField] protected Sprite backSite = null;

        public abstract void DoAction();
    }
}