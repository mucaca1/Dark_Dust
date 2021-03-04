using System;
using Mirror;
using UnityEngine;

namespace Game.Cards.PlaygroundCards {
    public class SandCard : PlaygroundPart {
        [SerializeField] private Renderer softDust = null;
        [SerializeField] private Renderer hardDust = null;

        [SyncVar(hook = nameof(HandleDustValue))]
        private int _dustValue = 1;

        public int DustValue => _dustValue;

        public event Action<SandCard> onDestroy; 

        private void Awake() {
            playgroundCardOffsetY = 0.05f;
        }

        public void AddDust() {
            _dustValue += 1;
        }

        public void RemoveDust(int count = 1) {
            _dustValue -= count;

            if (_dustValue != 0) return;
            
            onDestroy?.Invoke(this);
        }

        private void HandleDustValue(int oldValue, int newDustValue) {
            softDust.gameObject.SetActive(newDustValue == 1);
            hardDust.gameObject.SetActive(newDustValue == 2);
        }
    }
}