using Mirror;
using UnityEngine;

namespace Game.Cards.PlayCards.Tornado {
    [System.Serializable]
    public abstract class TornadoCard : NetworkBehaviour {
        [SerializeField] protected Sprite backSite = null;
        public abstract void DoAction();
        public abstract string GetString();
    }
}