using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
    public class SelectItemUI : MonoBehaviour {
        [SerializeField] private TMP_Text itemNameText = null;
        [SerializeField] private Button useButton = null;
        [SerializeField] private Button giveButton = null;

        public void Initialize(string name) {
            itemNameText.text = name;
        }
    }
}