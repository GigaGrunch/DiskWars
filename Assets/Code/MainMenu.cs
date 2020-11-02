﻿using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiskWars
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private Button _singleplayerButton;
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;

        public static NetworkMode NetworkMode;

        public void Start()
        {
            _singleplayerButton.onClick.AddListener(OnSingleplayerButtonClicked);
            _hostButton.onClick.AddListener(OnHostButtonClicked);
            _joinButton.onClick.AddListener(OnJoinClicked);
        }

        private void OnSingleplayerButtonClicked()
        {
            NetworkMode = NetworkMode.None;
            SceneManager.LoadScene("GameScene");
        }

        private void OnHostButtonClicked()
        {
            NetworkMode = NetworkMode.Host;
            SceneManager.LoadScene("GameScene");
        }

        private void OnJoinClicked()
        {
            NetworkMode = NetworkMode.Client;
            SceneManager.LoadScene("GameScene");
        }
    }
}
