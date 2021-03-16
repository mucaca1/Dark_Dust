using System;
using Game.Cards.PlaygroundCards;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
    public class CardHoverUI : MonoBehaviour {
        [SerializeField] private GameObject _holder = null;
        [SerializeField] private Image _cardImage = null;
        [SerializeField] private TMP_Text _cardName = null;

        private void Start() {
            PlaygroundCard.onCardHover += ShowCard;
        }

        private void OnDestroy() {
            PlaygroundCard.onCardHover -= ShowCard;
        }


        private void ShowCard(PlaygroundCard card) {
            if (card == null) {
                _holder.SetActive(false);
                return;
            }
            _cardImage.sprite = card.IsRevealed ? card.FrontImageSprite : card.BackImageSprite;
            _cardName.text = card.IsRevealed ? card.CardType.ToString() : "Unknow";
            _holder.SetActive(true);
        }
    }
}