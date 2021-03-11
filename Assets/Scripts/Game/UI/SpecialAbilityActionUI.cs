using System;
using Game.Cards.PlaygroundCards;
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

        public PlaygroundCard SourceCard { get; private set; } = null;
        public PlaygroundCard DestinationCard { get; private set; } = null;

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

        public void Initialize(AbilityType abilityType, PlaygroundCard source, PlaygroundCard destination, bool canCancel = true) {
            waterInputField.gameObject.SetActive(abilityType == AbilityType.WaterCarrier);
            SourceCard = source;
            DestinationCard = destination;
            if (!canCancel) {
                cancelButton.gameObject.SetActive(false);
                cancelButton.onClick.RemoveListener(HandleCancel);
            }
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