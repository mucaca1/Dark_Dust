using System;
using Mirror;
using Network;
using TMPro;
using UnityEngine;

namespace Game.UI {
    public class TornadoCardsRemainingUI : MonoBehaviour {

        [SerializeField] private TMP_Text sandDustText = null;
        

        private void Update() {
            int count = GameManager.Instance.TakingStormCardsConstant - GameManager.Instance.TakingStormCards;
            sandDustText.gameObject.SetActive(count != 0);
            if (count != 0) {
                sandDustText.text = $"Dark dust take -{count} card";
            }
        }
    }
}