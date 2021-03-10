using Game.Cards.PlaygroundCards;
using UnityEngine;

namespace Game.Cards.PlayCards.Tornado {
    [System.Serializable]
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
            Debug.Log($"Tornado is moving to the {_direction} and for {_steps} steps.");

            int x = 0;
            int y = 0;
            switch (_direction) {
                case TornadoDirection.Up:
                    y = 1;
                    break;
                case TornadoDirection.Right:
                    x = 1;
                    break;
                case TornadoDirection.Down:
                    y = -1;
                    break;
                case TornadoDirection.Left:
                    x = -1;
                    break;
            }
            
            for (var i = 0; i < _steps; i++) {
                PlaygroundCard playgroundCard = GameManager.Instance.GetCardAtIndex(GameManager.Instance.Tornado.GetIndexPosition() + new Vector2(x, y));
                if (playgroundCard == null) continue;
                playgroundCard.AddSand();
                GameManager.Instance.MoveTornadoToDestination(playgroundCard);
            }
        }
    }
}