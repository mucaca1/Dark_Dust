using System;
using Game.Characters.Ability;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
    public class SpecialAbilityActionUI : MonoBehaviour {
        [SerializeField] private TMP_InputField waterInputField = null;
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

        public void Initialize(AbilityType abilityType) {
            waterInputField.gameObject.SetActive(abilityType == AbilityType.WaterCarrier);
        }

        public int GetInputValue() {
            int val = 0;
            try {
                val = int.Parse(waterInputField.text);
            }
            catch (Exception e) {
                Console.WriteLine(e);
                val = 0;
            }

            return val;
        }
    }
}