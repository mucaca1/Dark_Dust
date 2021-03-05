using System;
using Mirror;
using UnityEngine;

namespace Game.Cards.PlaygroundCards {
    public class SandCard : NetworkBehaviour {
        [SerializeField] private Renderer softDust = null;
        [SerializeField] private Renderer hardDust = null;

        [SyncVar(hook = nameof(HandleDustValue))]
        private int _dustValue = 1;

        public int DustValue => _dustValue;

        public event Action<SandCard> onDestroy;
        public static event Action onAddSand;
        public static event Action onRemoveSand;

        [ServerCallback]
        private void Start() {
            onAddSand?.Invoke();
        }

        public void AddDust() {
            _dustValue += 1;
            onAddSand?.Invoke();
        }

        public void RemoveDust(int count = 1) {
            _dustValue -= count;
            onRemoveSand?.Invoke();
            if (_dustValue != 0) return;
            
            onDestroy?.Invoke(this);
        }

        private void HandleDustValue(int oldValue, int newDustValue) {
            softDust.gameObject.SetActive(newDustValue == 1);
            hardDust.gameObject.SetActive(newDustValue >= 2);
        }
    }
}