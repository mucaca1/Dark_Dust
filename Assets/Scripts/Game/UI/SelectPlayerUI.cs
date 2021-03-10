using System;
using Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
    public class SelectPlayerUI : MonoBehaviour {
        public class Value {
            public GameObject gameObject;
            public string itemName;
            public Color itemColor;
        }
        
        [SerializeField] private TMP_Text playerNameText = null;
        [SerializeField] private Image playerColor = null;
        [SerializeField] private Button selectButton = null;
        public event Action<GameObject> onValueSelected;

        private GameObject _gameObjectRefference = null;

        private void Start() {
            selectButton.onClick.AddListener(HandleButtonCLick);
        }

        private void OnDestroy() {
            selectButton.onClick.RemoveListener(HandleButtonCLick);
        }

        private void HandleButtonCLick() {
            onValueSelected?.Invoke(_gameObjectRefference);
        }

        public void Initialize(Value value) {
            _gameObjectRefference = value.gameObject;
            playerNameText.text = value.itemName;
            playerColor.color = value.itemColor;
        }
    }
}