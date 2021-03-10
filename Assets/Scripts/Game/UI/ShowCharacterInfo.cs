using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI {
    public class ShowCharacterInfo : MonoBehaviour, IPointerClickHandler {
        [SerializeField] private GameObject _characterInfoUI = null;

        private bool _isActivate = false;

        public void OnPointerClick(PointerEventData eventData) {
            _isActivate = !_isActivate;
            _characterInfoUI.SetActive(_isActivate);
        }
    }
}