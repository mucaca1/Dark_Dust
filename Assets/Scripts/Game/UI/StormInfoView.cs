using System;
using TMPro;
using UnityEngine;

namespace Game.UI {
    public class StormInfoView : MonoBehaviour {
        [SerializeField] private TMP_Text _stormValueText = null;
        [SerializeField] private TMP_Text _stormCardCountText = null;
        [SerializeField] private TMP_Text _sandMarkersText = null;
        [SerializeField] private TMP_Text _tornadoCardText = null;
        
        private void Start() {
            GameManager.Instance.onStromTickMarkChanged += HandleStormValue;
            GameManager.Instance.onDustCardSet += HandleSandValue;
            GameManager.Instance.onTornadoCardChanged += HandleTornadoCardValue;
        }

        private void OnDestroy() {
            GameManager.Instance.onStromTickMarkChanged -= HandleStormValue;
            GameManager.Instance.onDustCardSet -= HandleSandValue;
            GameManager.Instance.onTornadoCardChanged -= HandleTornadoCardValue;
        }
        

        public void HandleStormValue(int value, int maxValue, int cardCount) {
            _stormValueText.text = $"Storm tick: {value}/{maxValue}";
            _stormCardCountText.text = $"Cards in desert turn: {cardCount}";
        }

        private void HandleSandValue(int value) {
            _sandMarkersText.text = $"Sand markers remaining: {value}";
        }

        private void HandleTornadoCardValue(int value) {
            _tornadoCardText.text = $"Storm cards in deck: {value}";
        }
    }
}