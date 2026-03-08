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
        [SerializeField] private GameObject _aiOptionsPanel;

        [Header("AI Color (segmented)")]
        [SerializeField] private Button _btnAIColorWhite;
        [SerializeField] private Button _btnAIColorBlack;

        [Header("AI Difficulty (segmented)")]
        [SerializeField] private Button _btnDiffEasy;
        [SerializeField] private Button _btnDiffNormal;
        [SerializeField] private Button _btnDiffHard;

        private int _aiColorIndex     = 0; // 0=White 1=Black
        private int _aiDifficultyIndex = 1; // 0=Easy 1=Normal 2=Hard

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

            if (_btnAIColorWhite != null) _btnAIColorWhite.onClick.AddListener(() => SetAIColor(0));
            if (_btnAIColorBlack != null) _btnAIColorBlack.onClick.AddListener(() => SetAIColor(1));
            if (_btnDiffEasy    != null) _btnDiffEasy    .onClick.AddListener(() => SetAIDifficulty(0));
            if (_btnDiffNormal  != null) _btnDiffNormal  .onClick.AddListener(() => SetAIDifficulty(1));
            if (_btnDiffHard    != null) _btnDiffHard    .onClick.AddListener(() => SetAIDifficulty(2));

            SetAIColor(0);
            SetAIDifficulty(1);

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
            GameSettings.HumanColor = _aiColorIndex == 1 ? PlayerColor.Black : PlayerColor.White;
            GameSettings.AIDepth    = _aiDifficultyIndex switch { 0 => 2, 1 => 5, _ => 8 };
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

        // ─── AI segmented controls ────────────────────────────────────────

        private static readonly Color _colorSelected = new Color(0.70f, 0.48f, 0.28f, 1f);
        private static readonly Color _colorNormal   = new Color(0.56f, 0.35f, 0.18f, 1f);

        private void SetAIColor(int idx)
        {
            _aiColorIndex = idx;
            HighlightGroup(idx, _btnAIColorWhite, _btnAIColorBlack);
        }

        private void SetAIDifficulty(int idx)
        {
            _aiDifficultyIndex = idx;
            HighlightGroup(idx, _btnDiffEasy, _btnDiffNormal, _btnDiffHard);
        }

        private static void HighlightGroup(int selectedIdx, params Button[] btns)
        {
            for (int i = 0; i < btns.Length; i++)
            {
                if (btns[i] == null) continue;
                var img = btns[i].GetComponent<Image>();
                if (img != null)
                    img.color = (i == selectedIdx) ? _colorSelected : _colorNormal;
            }
        }

        private static void LoadGameScene()
        {
            SceneManager.LoadScene(GameSceneName);
        }
    }
}
