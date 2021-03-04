using System;
using UnityEngine;

namespace Game.Cards.PlaygroundCards {
    public class SandCard : PlaygroundPart {
        [SerializeField] private Renderer softDust = null;
        [SerializeField] private Renderer hardDust = null;

        private int _dustValue = 1;

        public int DustValue => _dustValue;

        private void Awake() {
            playgroundCardOffsetY = 0.05f;
        }

        public void AddDust() {
            _dustValue += 1;
        }

        public void RemoveDust(int count = 1) {
            _dustValue -= count;

            if (_dustValue < 0) Debug.Log("TODO Destroy");
        }
    }
}