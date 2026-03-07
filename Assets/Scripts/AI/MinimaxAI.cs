using System;
using System.Collections.Generic;
using Warcaby.Core;

namespace Warcaby.AI
{
    /// <summary>
    /// Minimax AI with Alpha-Beta pruning.
    /// Positive score = good for AI, negative = good for opponent.
    /// </summary>
    public class MinimaxAI
    {
        private readonly int _maxDepth;

        // Piece values
        private const int PawnValue = 100;
        private const int KingValue = 300;

        // Positional bonus table for pawns (8x8, viewed from Black side)
        private static readonly int[,] PawnBonus =
        {
            { 0,  0,  0,  0,  0,  0,  0,  0 },
            { 5,  5,  5,  5,  5,  5,  5,  5 },
            { 3,  3,  4,  6,  6,  4,  3,  3 },
            { 2,  2,  3,  5,  5,  3,  2,  2 },
            { 1,  1,  2,  3,  3,  2,  1,  1 },
            { 1,  1,  1,  2,  2,  1,  1,  1 },
            { 0,  0,  0,  1,  1,  0,  0,  0 },
            { 0,  0,  0,  0,  0,  0,  0,  0 }
        };

        public MinimaxAI(int depth = 5) => _maxDepth = depth;

        public Move GetBestMove(Board board, PlayerColor aiPlayer)
        {
            Move bestMove = null;
            int bestScore = int.MinValue;

            var moves = GameRules.GetLegalMoves(board, aiPlayer);
            if (moves.Count == 0) return null;

            foreach (var move in moves)
            {
                var next = board.Clone();
                next.ApplyMove(move);
                int score = -Minimax(next, _maxDepth - 1, int.MinValue + 1, int.MaxValue,
                                     aiPlayer.Opponent(), aiPlayer);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            return bestMove;
        }

        private int Minimax(Board board, int depth, int alpha, int beta,
                             PlayerColor currentPlayer, PlayerColor aiPlayer)
        {
            var result = GameRules.GetResult(board, currentPlayer);
            if (result != GameResult.InProgress)
            {
                if (result == GameResult.Draw) return 0;
                // If the current player lost, that's bad for whoever just moved (opponent of currentPlayer)
                bool aiWon = (result == GameResult.WhiteWins && aiPlayer == PlayerColor.White) ||
                              (result == GameResult.BlackWins && aiPlayer == PlayerColor.Black);
                return aiWon ? 10000 + depth : -10000 - depth;
            }

            if (depth == 0) return Evaluate(board, aiPlayer);

            var moves = GameRules.GetLegalMoves(board, currentPlayer);
            bool maximizing = currentPlayer == aiPlayer;
            int best = maximizing ? int.MinValue + 1 : int.MaxValue;

            foreach (var move in moves)
            {
                var next = board.Clone();
                next.ApplyMove(move);
                int score = Minimax(next, depth - 1, alpha, beta, currentPlayer.Opponent(), aiPlayer);

                if (maximizing)
                {
                    best = Math.Max(best, score);
                    alpha = Math.Max(alpha, score);
                }
                else
                {
                    best = Math.Min(best, score);
                    beta = Math.Min(beta, score);
                }

                if (beta <= alpha) break; // prune
            }

            return best;
        }

        // ─── Evaluation ───────────────────────────────────────────────────

        private int Evaluate(Board board, PlayerColor aiPlayer)
        {
            int score = 0;

            for (int r = 0; r < Board.Size; r++)
            {
                for (int c = 0; c < Board.Size; c++)
                {
                    var piece = board.GetPiece(r, c);
                    if (piece == PieceType.None) continue;

                    int value = GetPieceValue(piece, r, c, aiPlayer);
                    if (piece.BelongsTo(aiPlayer)) score += value;
                    else score -= value;
                }
            }

            return score;
        }

        private static int GetPieceValue(PieceType piece, int row, int col, PlayerColor aiPlayer)
        {
            if (piece.IsKing()) return KingValue;

            int bonus;
            if (piece.IsBlack())
                bonus = PawnBonus[row, col];
            else
                bonus = PawnBonus[Board.Size - 1 - row, Board.Size - 1 - col];

            // Flip for AI perspective
            if (aiPlayer == PlayerColor.White && piece.IsWhite()) return PawnValue + bonus;
            if (aiPlayer == PlayerColor.Black && piece.IsBlack()) return PawnValue + bonus;
            return PawnValue + bonus;
        }
    }
}
