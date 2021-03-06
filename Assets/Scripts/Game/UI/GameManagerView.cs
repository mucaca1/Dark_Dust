using System;
using TMPro;
using UnityEngine;

namespace Game.UI {
    public class GameManagerView : MonoBehaviour {

        [SerializeField] private TMP_Text _collectedPartsText = null;

        private void Start() {
            GameManager gm = FindObjectOfType<GameManager>();
            gm.onTakedItemsIncrease += HandleItemsUpdated;
        }

        private void OnDestroy() {
            GameManager gm = FindObjectOfType<GameManager>();
            gm.onTakedItemsIncrease -= HandleItemsUpdated;
        }

        private void HandleItemsUpdated(int count) {
            _collectedPartsText.text = $"Collected items: {count}/4";
        }
    }
}