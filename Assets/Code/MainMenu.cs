using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiskWars
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private Button _singleplayerButton;
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;

        public void Start()
        {
            _singleplayerButton.onClick.AddListener(StartSingleplayer);
            _hostButton.onClick.AddListener(HostGame);
            _joinButton.onClick.AddListener(JoinGame);
        }

        private void StartSingleplayer()
        {
            SceneManager.LoadScene("GameScene");
        }

        private void HostGame()
        {
            SceneManager.LoadScene("GameScene");
        }

        private void JoinGame()
        {
            SceneManager.LoadScene("GameScene");
        }
    }
}
