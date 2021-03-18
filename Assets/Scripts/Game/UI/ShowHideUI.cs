using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI {
    public class ShowHideUI : MonoBehaviour, IPointerClickHandler {

        [SerializeField] private GameObject _showBar = null;
        [SerializeField] private GameObject _barToShow = null;

        [SerializeField] private bool _hideShowBar = false;
        private bool _isShowed = false;

        
        public void OnPointerClick(PointerEventData eventData) {
            GameManager.Instance.InitializeGame(false, true);
            _isShowed = !_isShowed;
            _barToShow.SetActive(_isShowed);
        }
    }
}