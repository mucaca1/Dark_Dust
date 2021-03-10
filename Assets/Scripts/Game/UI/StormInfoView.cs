using System;
using TMPro;
using UnityEngine;

namespace Game.UI {
    public class StormInfoView : MonoBehaviour {
        [SerializeField] private TMP_Text _stormValueText = null;
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
        

        private void HandleStormValue(int value) {
            _stormValueText.text = $"Storm tick: {value}";
        }

        private void HandleSandValue(int value) {
            _sandMarkersText.text = $"Sand markers remaning: {value}";
        }

        private void HandleTornadoCardValue(int value) {
            _tornadoCardText.text = $"Storm cards in deck: {value}";
        }
    }
}