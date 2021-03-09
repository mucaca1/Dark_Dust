using Game;
using Game.Cards.PlaygroundCards;
using Game.Characters;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Network {
    public class PlayerController : MonoBehaviour {
        [SerializeField] private LayerMask _layerMask = new LayerMask();

        private PlayerAction _action = PlayerAction.WALK;

        private Camera _mainCamera;

        public PlayerAction Action => _action;

        private void Start() {
            _mainCamera = Camera.main;
        }

        private void Update() {
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;

            Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _layerMask)) return;
            if (hit.collider.TryGetComponent<PlaygroundCard>(out PlaygroundCard card)) {
                Character character = GetComponent<Character>();

                character.DoAction(_action, card);
            }
        }

        public void SetPlayerAction(PlayerAction action) {
            _action = action;
        }
    }
}