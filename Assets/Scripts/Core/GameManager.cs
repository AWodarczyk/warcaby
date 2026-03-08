using System;
using System.Collections.Generic;
using UnityEngine;
using Warcaby.Core;

namespace Warcaby
{
    public enum GameMode
    {
        LocalPvP,
        VsAI,
        OnlineMultiplayer
    }

    /// <summary>
    /// Central game controller. Coordinates board state, rules, AI, and networking.
    /// Attach to a persistent GameObject in the Game scene.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ─── Events ───────────────────────────────────────────────────────
        public event Action<Board> OnBoardChanged;
        public event Action<PlayerColor> OnTurnChanged;
        public event Action<GameResult> OnGameOver;
        public event Action<Move> OnMoveMade;

        // ─── Inspector ────────────────────────────────────────────────────
        [Header("Settings")]
        [SerializeField] private GameMode _gameMode = GameMode.LocalPvP;
        [SerializeField] private PlayerColor _aiColor = PlayerColor.Black;
        [SerializeField] private int _aiDepth = 5;

        // ─── State ────────────────────────────────────────────────────────
        public Board Board { get; private set; }
        public PlayerColor CurrentPlayer { get; private set; }
        public GameMode Mode => _gameMode;
        public GameResult Result { get; private set; }

        private List<Move> _legalMoves = new();
        private Move _pendingCapture; // selected piece's ongoing capture chain

        // ─── Selection ───────────────────────────────────────────────────
        public BoardPosition? SelectedPosition { get; private set; }
        public List<Move> LegalMoves => _legalMoves;

        // ─── Unity ────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartGame(GameMode mode, PlayerColor humanColor = PlayerColor.White)
        {
            _gameMode = mode;
            if (mode == GameMode.VsAI)
                _aiColor = humanColor.Opponent();

            Board = Board.CreateInitial();
            CurrentPlayer = PlayerColor.White;
            Result = GameResult.InProgress;
            SelectedPosition = null;
            RefreshLegalMoves();

            OnBoardChanged?.Invoke(Board);
            OnTurnChanged?.Invoke(CurrentPlayer);

            if (IsAITurn()) TriggerAI();
        }

        // ─── Input handling ───────────────────────────────────────────────

        /// <summary>Called by InputHandler when the player clicks a board square.</summary>
        public void OnSquareClicked(BoardPosition pos)
        {
            if (Result != GameResult.InProgress) return;
            if (IsAITurn()) return;

            var piece = Board.GetPiece(pos);

            // Select own piece
            if (piece.BelongsTo(CurrentPlayer))
            {
                SelectedPosition = pos;
                OnBoardChanged?.Invoke(Board);
                return;
            }

            // Try to execute a move
            if (SelectedPosition.HasValue)
            {
                var move = FindMove(SelectedPosition.Value, pos);
                if (move != null)
                {
                    ExecuteMove(move);
                    return;
                }
            }

            SelectedPosition = null;
            OnBoardChanged?.Invoke(Board);
        }

        private Move FindMove(BoardPosition from, BoardPosition to)
        {
            foreach (var m in _legalMoves)
                if (m.From == from && m.To == to)
                    return m;
            return null;
        }

        // ─── Move execution ───────────────────────────────────────────────

        public void ExecuteMove(Move move)
        {
            Board.ApplyMove(move);
            SelectedPosition = null;
            OnMoveMade?.Invoke(move);
            OnBoardChanged?.Invoke(Board);

            // Check win
            Result = GameRules.GetResult(Board, CurrentPlayer.Opponent());
            if (Result != GameResult.InProgress)
            {
                OnGameOver?.Invoke(Result);
                return;
            }

            SwitchTurn();
        }

        private void SwitchTurn()
        {
            CurrentPlayer = CurrentPlayer.Opponent();
            RefreshLegalMoves();
            OnTurnChanged?.Invoke(CurrentPlayer);

            if (IsAITurn()) TriggerAI();
        }

        /// <summary>Called externally (e.g. NetworkGameManager) to signal game over.</summary>
        public void TriggerGameOver(GameResult result)
        {
            Result = result;
            OnGameOver?.Invoke(result);
        }

        private void RefreshLegalMoves() =>
            _legalMoves = GameRules.GetLegalMoves(Board, CurrentPlayer);

        // ─── AI ───────────────────────────────────────────────────────────

        private bool IsAITurn() =>
            _gameMode == GameMode.VsAI && CurrentPlayer == _aiColor;

        private void TriggerAI()
        {
            // Run on next frame to allow UI update
            StartCoroutine(AICoroutine());
        }

        private System.Collections.IEnumerator AICoroutine()
        {
            yield return null; // one frame delay
            var ai = new AI.MinimaxAI(_aiDepth);
            var move = ai.GetBestMove(Board, _aiColor);
            if (move != null) ExecuteMove(move);
        }

        // ─── Helpers ──────────────────────────────────────────────────────

        public List<Move> GetMovesFromPosition(BoardPosition pos)
        {
            var result = new List<Move>();
            foreach (var m in _legalMoves)
                if (m.From == pos) result.Add(m);
            return result;
        }
    }
}
