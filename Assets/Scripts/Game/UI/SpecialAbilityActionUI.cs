using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
    public class SpecialAbilityActionUI : MonoBehaviour {
        [SerializeField] private TMP_Text description = null;
        [SerializeField] private GameObject actionContent = null;
        [SerializeField] private Button cancelButton = null;

        public event Action onCancel;

        private void Start() {
            cancelButton.onClick.AddListener(HandleCancel);
        }

        private void OnDestroy() {
            cancelButton.onClick.RemoveListener(HandleCancel);
        }

        private void HandleCancel() {
            onCancel?.Invoke();
        }

        public Transform GetActionContentHolderTransform() {
            return actionContent.transform;
        }
    }
}