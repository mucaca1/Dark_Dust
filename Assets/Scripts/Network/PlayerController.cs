using System;
using Game;
using Game.Cards.PlaygroundCards;
using Mirror;
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
                Player player = GetComponent<Player>();

                switch (_action) {
                    case PlayerAction.WALK:
                        player.GoToPosition(card);
                        break;
                    case PlayerAction.EXCAVATE:
                        player.Excavate(card);
                        break;
                    case PlayerAction.REMOVE_SAND:
                        player.RemoveSand(card);
                        break;
                    case PlayerAction.PICK_UP_A_PART:
                        player.PickUpAPart(card);
                        break;
                }
                
            }
        }

        public void SetPlayerAction(PlayerAction action) {
            _action = action;
        }
    }
}