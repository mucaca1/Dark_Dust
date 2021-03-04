using UnityEngine;

namespace Game.Cards.PlayCards.Tornado {
    public class TornadoCard {
        [SerializeField] private Sprite backSite = null;
        [SerializeField] private Sprite frontSite = null;
        
        private CardDirection _direction;
        private int _steps = 0;
    }
}