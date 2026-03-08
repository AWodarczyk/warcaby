using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Warcaby.Core;

namespace Warcaby.UI
{
    /// <summary>
    /// Self-contained controller for the MainMenu scene.
    /// Stores chosen settings in GameSettings then loads the Game scene.
    /// Attach to a persistent GameObject together with the Canvas.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        public const string GameSceneName = "Game";

        [Header("Mode buttons")]
        [SerializeField] private Button _btnPvP;
        [SerializeField] private Button _btnVsAI;
        [SerializeField] private Button _btnOnline;

        [Header("AI options panel")]
        [SerializeField] private GameObject   _aiOptionsPanel;
        [SerializeField] private TMP_Dropdown _aiColorDropdown;      // 0=Białe, 1=Czarne
        [SerializeField] private TMP_Dropdown _aiDifficultyDropdown; // 0=Łatwy, 1=Normalny, 2=Trudny

        [Header("Online panel")]
        [SerializeField] private GameObject    _onlinePanel;
        [SerializeField] private TMP_InputField _serverAddressInput;
        [SerializeField] private Button         _btnHost;
        [SerializeField] private Button         _btnJoin;

        [Header("Common")]
        [SerializeField] private Button _btnBack;   // Back inside sub-panels
        [SerializeField] private Button _btnQuit;

        // ─── Unity ────────────────────────────────────────────────────────

        private void Start()
        {
            _btnPvP   .onClick.AddListener(OnPvP);
            _btnVsAI  .onClick.AddListener(OnVsAI);
            _btnOnline.onClick.AddListener(OnOnline);

            if (_btnBack != null) _btnBack.onClick.AddListener(ShowMainButtons);
            if (_btnQuit != null) _btnQuit.onClick.AddListener(OnQuit);

            if (_btnHost != null) _btnHost.onClick.AddListener(OnHost);
            if (_btnJoin != null) _btnJoin.onClick.AddListener(OnJoin);

            if (_aiColorDropdown != null)
                _aiColorDropdown.onValueChanged.AddListener(_ => PreviewAIOptions());

            ShowMainButtons();
        }

        // ─── Button handlers ──────────────────────────────────────────────

        private void OnPvP()
        {
            GameSettings.Mode = GameMode.LocalPvP;
            LoadGameScene();
        }

        private void OnVsAI()
        {
            ShowAIOptions();
        }

        public void StartVsAI()
        {
            GameSettings.Mode       = GameMode.VsAI;
            GameSettings.HumanColor = (_aiColorDropdown != null && _aiColorDropdown.value == 1)
                                      ? PlayerColor.Black : PlayerColor.White;
            GameSettings.AIDepth    = AIDepthFromDropdown();
            LoadGameScene();
        }

        private void OnOnline()
        {
            SetPanel(mainButtons: false, ai: false, online: true);
        }

        private void OnHost()
        {
            GameSettings.Mode          = GameMode.OnlineMultiplayer;
            GameSettings.ServerAddress = "localhost";
            var nm = Mirror.NetworkManager.singleton;
            if (nm != null) nm.StartHost();
            LoadGameScene();
        }

        private void OnJoin()
        {
            GameSettings.Mode          = GameMode.OnlineMultiplayer;
            GameSettings.ServerAddress = _serverAddressInput != null
                                         ? _serverAddressInput.text : "localhost";
            var nm = Mirror.NetworkManager.singleton;
            if (nm != null)
            {
                nm.networkAddress = GameSettings.ServerAddress;
                nm.StartClient();
            }
            LoadGameScene();
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ─── Panel visibility ─────────────────────────────────────────────

        private void ShowMainButtons() =>
            SetPanel(mainButtons: true, ai: false, online: false);

        private void ShowAIOptions() =>
            SetPanel(mainButtons: false, ai: true, online: false);

        private void SetPanel(bool mainButtons, bool ai, bool online)
        {
            if (_aiOptionsPanel != null) _aiOptionsPanel.SetActive(ai);
            if (_onlinePanel    != null) _onlinePanel   .SetActive(online);

            // Main buttons: PvP, VsAI, Online are always in the main Panel –
            // hide them when a sub-panel is open
            SetMainButtonsVisible(mainButtons);
        }

        private void SetMainButtonsVisible(bool visible)
        {
            if (_btnPvP    != null) _btnPvP   .gameObject.SetActive(visible);
            if (_btnVsAI   != null) _btnVsAI  .gameObject.SetActive(visible);
            if (_btnOnline != null) _btnOnline.gameObject.SetActive(visible);
        }

        // ─── Helpers ──────────────────────────────────────────────────────

        private void PreviewAIOptions()
        {
            // Could update a label here in the future
        }

        private int AIDepthFromDropdown()
        {
            if (_aiDifficultyDropdown == null) return 5;
            return _aiDifficultyDropdown.value switch
            {
                0 => 2,  // Łatwy
                1 => 5,  // Normalny
                _ => 8   // Trudny
            };
        }

        private static void LoadGameScene()
        {
            SceneManager.LoadScene(GameSceneName);
        }
    }
}
