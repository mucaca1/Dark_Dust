using System;
using Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
    public class SelectPlayerUI : MonoBehaviour {
        [SerializeField] private TMP_Text playerNameText = null;
        [SerializeField] private Image playerColor = null;
        [SerializeField] private Button selectButton = null;

        private Player _player = null;
        public event Action<Player> onPlayerSelected;

        private void Start() {
            selectButton.onClick.AddListener(HandleButtonCLick);
        }

        private void HandleButtonCLick() {
            onPlayerSelected?.Invoke(_player);
        }

        public void Initialize(Player player) {
            _player = player;
            playerNameText.text = player.PlayerName;
            playerColor.color = player.PlayerColor;
        }
    }
}