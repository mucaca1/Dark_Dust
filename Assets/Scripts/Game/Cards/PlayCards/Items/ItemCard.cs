using UnityEngine;

namespace Game.Cards.PlayCards.Items {
    public class ItemCard : MonoBehaviour {
        [SerializeField] private int cardId = -1;

        [SerializeField] private string _cardName;
        [SerializeField] private string _description;
        [SerializeField] private CardAction action;

        public int CardId => cardId;

        public string CardName => _cardName;

        public string Description => _description;

        public CardAction Action => action;

        public virtual void DoSpecialAction() {
            Debug.Log("Special Action");
        }
    }
}