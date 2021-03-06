using System;
using TMPro;
using UnityEngine;

namespace Game.UI {
    public class StormInfoView : MonoBehaviour {
        [SerializeField] private GameManager _gameManager = null;
        [SerializeField] private TMP_Text _stormValueText = null;
        [SerializeField] private TMP_Text _sandMarkersText = null;
        [SerializeField] private TMP_Text _tornadoCardText = null;
        
        private void Start() {
            _gameManager.onStromTickMarkChanged += HandleStormValue;
            _gameManager.onDustCardSet += HandleSandValue;
            _gameManager.onTornadoCardChanged += HandleTornadoCardValue;
        }

        private void OnDestroy() {
            _gameManager.onStromTickMarkChanged -= HandleStormValue;
            _gameManager.onDustCardSet -= HandleSandValue;
            _gameManager.onTornadoCardChanged -= HandleTornadoCardValue;
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