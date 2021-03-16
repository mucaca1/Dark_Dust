using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Network;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyMenu : MonoBehaviour {

    [SerializeField] private GameObject lobbyUI = null;
    [SerializeField] private Button startGameButton = null;
    [SerializeField] private TMP_Text[] playerNameTexts = new TMP_Text[5];

    private void Start() {
        DarkDustNetworkManager.ClientOnConnected += HandleClientOnConnected;
        Player.AuthorityOnPartyOwnerStateUpdated += AuthorityHandlePartyOwnerStateUpdated;
        Player.ClientOnInfoUpdated += ClientHandleInfoUpdated;
    }

    private void OnDestroy() {
        DarkDustNetworkManager.ClientOnConnected -= HandleClientOnConnected;
        Player.AuthorityOnPartyOwnerStateUpdated -= AuthorityHandlePartyOwnerStateUpdated;
        Player.ClientOnInfoUpdated -= ClientHandleInfoUpdated;
    }

    private void ClientHandleInfoUpdated() {
        List<Player> players = ((DarkDustNetworkManager) NetworkManager.singleton).Players;

        for (int i = 0; i < players.Count; i++) {
            playerNameTexts[i].text = players[i].PlayerName;
        }

        for (int i = players.Count; i < playerNameTexts.Length; i++) {
            playerNameTexts[i].text = "Waiting For Player...";
        }

        startGameButton.interactable = players.Count >= 1;
    }
    
    private void AuthorityHandlePartyOwnerStateUpdated(bool value) {
        startGameButton.gameObject.SetActive(value);
    }

    public void StartGame() {
        NetworkClient.connection.identity.GetComponent<Player>().CmdStartGame();
    }

    private void HandleClientOnConnected() {
        lobbyUI.SetActive(true);
    }

    public void LeaveLobby() {
        if (NetworkServer.active && NetworkClient.isConnected) {
            NetworkManager.singleton.StopHost();
        }
        else {
            NetworkManager.singleton.StopClient();

            SceneManager.LoadScene(0);
        }
    }
}