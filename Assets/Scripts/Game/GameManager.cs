using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Game {
    public class GameManager : NetworkBehaviour {
        [SerializeField] private GameObject playgroundCardPrefab = null;
        private List<PlaygroundCard> _playgroundCards = new List<PlaygroundCard>();
        private PlaygroundCardData[] _playgroundCardDatas;

        [ServerCallback]
        private void Start() {
            _playgroundCardDatas = Resources.LoadAll<PlaygroundCardData>("");

            Debug.Log(_playgroundCardDatas.Length == 0
                ? "No playground cards was found. Check Resources folder"
                : "Playground cards was loaded successfully");

            GenerateNewPlayGround();
        }

        #region Server

        [Server]
        private void GenerateNewPlayGround() {
            if (_playgroundCardDatas == null || _playgroundCardDatas.Length == 0) return;

            foreach (PlaygroundCardData cardData in _playgroundCardDatas) {
                GameObject card = Instantiate(playgroundCardPrefab, Vector3.zero, Quaternion.identity);
                PlaygroundCard newCardData = card.GetComponent<PlaygroundCard>();
                newCardData.SetData(cardData);
                NetworkServer.Spawn(card);
            }
        }

        #endregion
    }
}