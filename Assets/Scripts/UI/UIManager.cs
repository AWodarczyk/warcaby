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

        [Header("AI Color (segmented)")]
        [SerializeField] private Button _btnAIColorWhite;
        [SerializeField] private Button _btnAIColorBlack;

        [Header("AI Difficulty (segmented)")]
        [SerializeField] private Button _btnDiffEasy;
        [SerializeField] private Button _btnDiffNormal;
        [SerializeField] private Button _btnDiffHard;

        private int _aiColorIndex     = 0; // 0=White 1=Black
        private int _aiDifficultyIndex = 1; // 0=Easy 1=Normal 2=Hard

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

            // AI segmented controls
            if (_btnAIColorWhite  != null) _btnAIColorWhite .onClick.AddListener(() => SetAIColor(0));
            if (_btnAIColorBlack  != null) _btnAIColorBlack .onClick.AddListener(() => SetAIColor(1));
            if (_btnDiffEasy      != null) _btnDiffEasy     .onClick.AddListener(() => SetAIDifficulty(0));
            if (_btnDiffNormal    != null) _btnDiffNormal   .onClick.AddListener(() => SetAIDifficulty(1));
            if (_btnDiffHard      != null) _btnDiffHard     .onClick.AddListener(() => SetAIDifficulty(2));

            SetAIColor(0);
            SetAIDifficulty(1);

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
            _mainMenuPanel?.SetActive(active == _mainMenuPanel);
            _gamePanel?.SetActive(active == _gamePanel);
            _gameOverPanel?.SetActive(active == _gameOverPanel);
            _onlinePanel?.SetActive(active == _onlinePanel);
        }

        // ─── Game start ───────────────────────────────────────────────────

        private void StartGame(GameMode mode)
        {
            PlayerColor humanColor = PlayerColor.White;
            if (mode == GameMode.VsAI)
            {
                humanColor = _aiColorIndex == 0 ? PlayerColor.White : PlayerColor.Black;
                GameSettings.AIDepth = _aiDifficultyIndex switch
                {
                    0 => 2,
                    1 => 5,
                    _ => 8
                };
            }

            GameManager.Instance.StartGame(mode, humanColor);
            SetActivePanel(_gamePanel);
            UpdateHUD();
        }

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

        // ─── Event handlers ───────────────────────────────────────────────

        private void HandleBoardChanged(Board board) => UpdateHUD(board);

        private void HandleTurnChanged(PlayerColor player)
        {
            string name = player == PlayerColor.White ? "Białe" : "Czarne";
            if (_turnLabel != null) _turnLabel.text = $"Ruch: {name}";
        }

        private void HandleGameOver(GameResult result)
        {
            string msg = result switch
            {
                GameResult.WhiteWins => "Białe wygrywają!",
                GameResult.BlackWins => "Czarne wygrywają!",
                GameResult.Draw      => "Remis!",
                _                    => ""
            };
            if (_gameOverLabel != null) _gameOverLabel.text = msg;
            SetActivePanel(_gameOverPanel);
        }

        private void UpdateHUD(Board board = null)
        {
            board ??= GameManager.Instance?.Board;
            if (board == null) return;
            int w = board.WhitePieces;
            int b = board.BlackPieces;
            if (_whitePiecesLabel != null) _whitePiecesLabel.text = $"Białe: {w}";
            if (_blackPiecesLabel != null) _blackPiecesLabel.text = $"Czarne: {b}";
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
            // Activate the Toast root GO (background Image); _toastLabel lives on its child.
            var toastRoot = _toastLabel.transform.parent != null
                ? _toastLabel.transform.parent.gameObject
                : _toastLabel.gameObject;
            _toastLabel.text = message;
            toastRoot.SetActive(true);
            CancelInvoke(nameof(HideToast));
            Invoke(nameof(HideToast), 3f);
        }

        private void HideToast()
        {
            var toastRoot = _toastLabel.transform.parent != null
                ? _toastLabel.transform.parent.gameObject
                : _toastLabel.gameObject;
            toastRoot.SetActive(false);
        }
    }
}
