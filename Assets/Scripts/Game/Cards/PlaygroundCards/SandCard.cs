using System;
using Mirror;
using UnityEngine;

namespace Game.Cards.PlaygroundCards {
    public class SandCard : MonoBehaviour {
        [SerializeField] private Renderer softDust = null;
        [SerializeField] private Renderer hardDust = null;
        
        public void HandleDustValue(int dustValue) {
            softDust.gameObject.SetActive(dustValue == 1);
            hardDust.gameObject.SetActive(dustValue >= 2);
        }
    }
}