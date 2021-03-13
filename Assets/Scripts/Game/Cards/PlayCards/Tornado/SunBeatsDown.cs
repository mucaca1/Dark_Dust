using System.Collections.Generic;
using Game.Cards.PlaygroundCards;
using Game.Characters;
using Mirror;
using UnityEngine;

namespace Game.Cards.PlayCards.Tornado {
    [System.Serializable]
    public class SunBeatsDown : TornadoCard {
        [Server]
        public override void DoAction() {
            
            foreach (PlaygroundCard card in GameManager.Instance.PlaygroundCards) {
                List<Character> charactersOnCard = card.GetCharacters();
                if (charactersOnCard.Count == 0) continue;
                
                if (card.CardType == PlaygroundCardType.Cave || card.CoveredBySolarShield) continue;
                
                foreach (Character character in charactersOnCard) {
                    character.DrinkWater();
                }
            }
            Debug.Log("Sun Beats Down");
        }

        public override string GetString() {
            return "Sun Beats Down";
        }
    }
}