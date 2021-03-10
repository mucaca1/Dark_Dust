using Game;
using Game.Cards.PlaygroundCards;
using Game.Characters;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Network {
    public class PlayerController : MonoBehaviour {
        [SerializeField] private LayerMask _layerMask = new LayerMask();

        [SerializeField] private PlayerAction _action = PlayerAction.WALK;
        private bool _specialAction = false;

        private Camera _mainCamera;

        public PlayerAction Action => _action;

        private void Start() {
            _mainCamera = Camera.main;
        }

        private void Update() {
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;

            if (EventSystem.current.IsPointerOverGameObject()) {
                Debug.Log("UI HOVER");
                return;
            }
            
            Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _layerMask)) return;
            if (hit.collider.TryGetComponent(out PlaygroundCard card)) {
                Character character = GetComponent<Character>();

                character.DoAction(_action, card, _specialAction);
            }
        }

        public void SetPlayerAction(PlayerAction action) {
            _action = action;
        }
        
        public void SetSpecialAction(bool action) {
            _specialAction = action;
        }
    }
}