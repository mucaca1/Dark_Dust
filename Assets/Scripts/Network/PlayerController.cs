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

        private void Start() {
            _mainCamera = Camera.main;
        }

        private void Update() {
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;

            Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _layerMask)) return;
            if (hit.collider.TryGetComponent<PlaygroundCard>(out PlaygroundCard card)) {
                Character character = GetComponent<Character>();

                switch (_action) {
                    case PlayerAction.WALK:
                        character.GoToPosition(card);
                        break;
                    case PlayerAction.EXCAVATE:
                        character.Excavate(card);
                        break;
                    case PlayerAction.REMOVE_SAND:
                        character.RemoveSand(card);
                        break;
                    case PlayerAction.PICK_UP_A_PART:
                        character.PickUpAPart(card);
                        break;
                }
            }
        }

        public void SetPlayerAction(PlayerAction action) {
            _action = action;
        }
    }
}