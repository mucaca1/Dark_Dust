using System;
using Game.Cards.PlaygroundCards;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Network {
    public class PlayerController : MonoBehaviour {
        [SerializeField] private LayerMask _layerMask = new LayerMask();

        private Camera _mainCamera;

        private void Start() {
            _mainCamera = Camera.main;
        }

        private void Update() {
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;

            Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _layerMask)) return;
            if (hit.collider.TryGetComponent<PlaygroundCard>(out PlaygroundCard card)) {
                Player player = NetworkClient.connection.identity.GetComponent<Player>();
                player.CmdGoToPosition(card);
            }
        }
    }
}