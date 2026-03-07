using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Warcaby.Core;

namespace Warcaby.UI
{
    /// <summary>
    /// Central UI controller. Handles main menu, in-game HUD and end-game overlay.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] private GameObject _mainMenuPanel;
        [SerializeField] private GameObject _gamePanel;
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private GameObject _onlinePanel;

        [Header("Main Menu")]
        [SerializeField] private Button _btnPvP;
        [SerializeField] private Button _btnVsAI;
        [SerializeField] private Button _btnOnline;
        [SerializeField] private TMP_Dropdown _aiColorDropdown;
        [SerializeField] private TMP_Dropdown _aiDifficultyDropdown;

        [Header("In-Game HUD")]
        [SerializeField] private TextMeshProUGUI _turnLabel;
        [SerializeField] private TextMeshProUGUI _whitePiecesLabel;
        [SerializeField] private TextMeshProUGUI _blackPiecesLabel;
        [SerializeField] private Button _btnResign;

        [Header("Game Over")]
        [SerializeField] private TextMeshProUGUI _gameOverLabel;
        [SerializeField] private Button _btnPlayAgain;
        [SerializeField] private Button _btnMainMenu;

        [Header("Online")]
        [SerializeField] private TMP_InputField _serverAddressInput;
        [SerializeField] private Button _btnHost;
        [SerializeField] private Button _btnJoin;

        [Header("Toast")]
        [SerializeField] private TextMeshProUGUI _toastLabel;

        private GameManager _gm;

        // ─── Unity ────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _btnPvP.onClick.AddListener(() => StartGame(GameMode.LocalPvP));
            _btnVsAI.onClick.AddListener(() => StartGame(GameMode.VsAI));
            _btnOnline.onClick.AddListener(ShowOnlinePanel);
            _btnResign.onClick.AddListener(OnResign);
            _btnPlayAgain.onClick.AddListener(OnPlayAgain);
            _btnMainMenu.onClick.AddListener(ShowMainMenu);
            _btnHost.onClick.AddListener(OnHost);
            _btnJoin.onClick.AddListener(OnJoin);

            ShowMainMenu();
        }

        private void OnEnable()
        {
            if (GameManager.Instance == null) return;
            _gm = GameManager.Instance;
            _gm.OnBoardChanged += HandleBoardChanged;
            _gm.OnTurnChanged += HandleTurnChanged;
            _gm.OnGameOver += HandleGameOver;
        }

        private void OnDisable()
        {
            if (_gm == null) return;
            _gm.OnBoardChanged -= HandleBoardChanged;
            _gm.OnTurnChanged -= HandleTurnChanged;
            _gm.OnGameOver -= HandleGameOver;
        }

        // ─── Panel navigation ─────────────────────────────────────────────

        public void ShowMainMenu()
        {
            SetActivePanel(_mainMenuPanel);
        }

        private void ShowOnlinePanel()
        {
            SetActivePanel(_onlinePanel);
        }

        private void SetActivePanel(GameObject active)
        {
            _mainMenuPanel.SetActive(active == _mainMenuPanel);
            _gamePanel.SetActive(active == _gamePanel);
            _gameOverPanel.SetActive(active == _gameOverPanel);
            _onlinePanel.SetActive(active == _onlinePanel);
        }

        // ─── Game start ───────────────────────────────────────────────────

        private void StartGame(GameMode mode)
        {
            PlayerColor humanColor = PlayerColor.White;
            if (mode == GameMode.VsAI)
            {
                humanColor = _aiColorDropdown.value == 0 ? PlayerColor.White : PlayerColor.Black;
                int depth = _aiDifficultyDropdown.value switch
                {
                    0 => 2,
                    1 => 5,
                    _ => 8
                };
                // GameManager exposes AI depth via a property or method
            }

            GameManager.Instance.StartGame(mode, humanColor);
            SetActivePanel(_gamePanel);
            UpdateHUD();
        }

        // ─── Event handlers ───────────────────────────────────────────────

        private void HandleBoardChanged(Board board) => UpdateHUD();

        private void HandleTurnChanged(PlayerColor player)
        {
            string name = player == PlayerColor.White ? "Białe" : "Czarne";
            _turnLabel.text = $"Ruch: {name}";
        }

        private void HandleGameOver(GameResult result)
        {
            _gameOverLabel.text = result switch
            {
                GameResult.WhiteWins => "Białe wygrywają!",
                GameResult.BlackWins => "Czarne wygrywają!",
                GameResult.Draw      => "Remis!",
                _                    => ""
            };
            SetActivePanel(_gameOverPanel);
        }

        private void UpdateHUD()
        {
            if (GameManager.Instance == null) return;
            var board = GameManager.Instance.Board;
            _whitePiecesLabel.text = $"Białe: {board.WhitePieces}";
            _blackPiecesLabel.text = $"Czarne: {board.BlackPieces}";
        }

        private void OnResign()
        {
            var loser = GameManager.Instance.CurrentPlayer;
            var result = loser == PlayerColor.White ? GameResult.BlackWins : GameResult.WhiteWins;
            HandleGameOver(result);
        }

        private void OnPlayAgain()
        {
            StartGame(GameManager.Instance.Mode);
        }

        // ─── Online ───────────────────────────────────────────────────────

        private void OnHost()
        {
            var nm = Mirror.NetworkManager.singleton;
            if (nm != null) nm.StartHost();
            StartGame(GameMode.OnlineMultiplayer);
        }

        private void OnJoin()
        {
            var nm = Mirror.NetworkManager.singleton;
            if (nm != null)
            {
                nm.networkAddress = _serverAddressInput.text;
                nm.StartClient();
            }
            SetActivePanel(_gamePanel);
        }

        // ─── Toast ────────────────────────────────────────────────────────

        public void ShowMessage(string message)
        {
            _toastLabel.text = message;
            _toastLabel.gameObject.SetActive(true);
            CancelInvoke(nameof(HideToast));
            Invoke(nameof(HideToast), 3f);
        }

        private void HideToast() => _toastLabel.gameObject.SetActive(false);
    }
}
