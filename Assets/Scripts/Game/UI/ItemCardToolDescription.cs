using System;
using System.Security.Cryptography;
using Game.Cards.PlayCards.Items;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI {
    public class ItemCardToolDescription : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        public ItemCard card;

        public static event Action<ItemCard> onHover; 

        public void OnPointerEnter(PointerEventData eventData) {
            onHover?.Invoke(card);
        }

        public void OnPointerExit(PointerEventData eventData) {
            onHover?.Invoke(null);
        }
    }
}