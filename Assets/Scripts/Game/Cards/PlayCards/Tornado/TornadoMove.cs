using UnityEngine;

namespace Game.Cards.PlayCards.Tornado {
    public class TornadoMove : TornadoCard {
        [SerializeField] private Sprite frontSite = null;
        [SerializeField] private int _steps = 0;
        [SerializeField] private TornadoDirection _direction;

        public int Steps {
            get => _steps;
            set => _steps = value;
        }

        public TornadoDirection Direction {
            get => _direction;
            set => _direction = value;
        }

        public override void DoAction() {
            throw new System.NotImplementedException();
        }
    }
}