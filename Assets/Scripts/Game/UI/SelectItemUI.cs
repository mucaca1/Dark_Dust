using System;
using Game.Characters.Ability;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
    public class SelectItemUI : MonoBehaviour {
        [SerializeField] private TMP_Text itemNameText = null;
        [SerializeField] private Button useButton = null;
        [SerializeField] private Button giveButton = null;

        private int cardId = -1;

        public static event Action<int, AbilityType> onItemActionSelect; 

        public void Initialize(string name, int id) {
            itemNameText.text = name;
            cardId = id;
        }

        private void Start() {
            useButton.onClick.AddListener(HandleOnUse);
            giveButton.onClick.AddListener(HandleOnGive);
        }

        private void OnDestroy() {
            
            useButton.onClick.RemoveListener(HandleOnUse);
            giveButton.onClick.RemoveListener(HandleOnGive);
        }

        private void HandleOnUse() {
            onItemActionSelect?.Invoke(cardId, AbilityType.UseItem);
        }

        private void HandleOnGive() {
            onItemActionSelect?.Invoke(cardId, AbilityType.GiveItem);
        }
    }
}