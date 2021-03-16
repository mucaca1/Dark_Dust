using System;
using Game.Cards.PlaygroundCards;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
    public class ShowCardActionUI : MonoBehaviour {
        [SerializeField] private GameObject holder = null;
        [SerializeField] private Image _cardImage = null;
        [SerializeField] private Button _button = null;

        private void Start() {
            _button.onClick.AddListener(HandleButtonAction);
            holder.SetActive(false);
        }

        private void OnDestroy() {
            _button.onClick.RemoveListener(HandleButtonAction);
        }

        private void HandleButtonAction() {
            holder.SetActive(false);
        }
        
        public void Initialize(PlaygroundCard card) {
            _cardImage.sprite = card.FrontImageSprite;
            holder.SetActive(true);
        }
    }
}