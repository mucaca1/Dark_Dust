﻿using System;
using Game.Characters.Ability;
using Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
    public class SelectPlayerUI : MonoBehaviour {
        public class Value {
            public int index = -1;
            public GameObject gameObject;
            public string itemName;
            public Color itemColor;
        }

        [SerializeField] private TMP_Text playerNameText = null;
        [SerializeField] private Image playerColor = null;
        [SerializeField] private Button selectButton = null;

        private AbilityType action = AbilityType.Archeologist;
        private int index = -1;
        public event Action<AbilityType, GameObject, int> onValueSelected;

        private GameObject _gameObjectRefference = null;

        private void Start() {
            selectButton.onClick.AddListener(HandleButtonCLick);
        }

        private void OnDestroy() {
            selectButton.onClick.RemoveListener(HandleButtonCLick);
        }

        private void HandleButtonCLick() {
            onValueSelected?.Invoke(action, _gameObjectRefference, index);
        }

        public void Initialize(Value value, AbilityType action) {
            _gameObjectRefference = value.gameObject;
            playerNameText.text = value.itemName;
            playerColor.color = value.itemColor;
            index = value.index;
            this.action = action;
        }
    }
}