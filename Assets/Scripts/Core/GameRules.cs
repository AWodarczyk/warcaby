using System.Collections.Generic;

namespace Warcaby.Core
{
    /// <summary>
    /// Generates legal moves according to Polish (standard) checkers rules:
    /// - Mandatory capture (bicie obowiązkowe)
    /// - Kings fly along diagonals (dama lata)
    /// - Promotion after reaching last rank (even mid-capture chain is resolved at end)
    /// </summary>
    public static class GameRules
    {
        private static readonly (int dr, int dc)[] Directions = { (-1, -1), (-1, 1), (1, -1), (1, 1) };

        // ─── Public API ────────────────────────────────────────────────────

        /// <summary>Returns all legal moves for the given player. Captures are mandatory.</summary>
        public static List<Move> GetLegalMoves(Board board, PlayerColor player)
        {
            var captures = GetAllCaptures(board, player);
            if (captures.Count > 0) return captures;
            return GetAllSimpleMoves(board, player);
        }

        public static bool HasAnyMove(Board board, PlayerColor player) =>
            GetLegalMoves(board, player).Count > 0;

        public static GameResult GetResult(Board board, PlayerColor currentPlayer)
        {
            if (!HasAnyMove(board, currentPlayer))
                return currentPlayer == PlayerColor.White ? GameResult.BlackWins : GameResult.WhiteWins;
            if (board.WhitePieces == 0) return GameResult.BlackWins;
            if (board.BlackPieces == 0) return GameResult.WhiteWins;
            return GameResult.InProgress;
        }

        // ─── Simple Moves ──────────────────────────────────────────────────

        private static List<Move> GetAllSimpleMoves(Board board, PlayerColor player)
        {
            var result = new List<Move>();
            foreach (var pos in board.GetPiecePositions(player))
            {
                var piece = board.GetPiece(pos);
                if (piece.IsKing())
                    AddKingSimpleMoves(board, pos, result);
                else
                    AddPawnSimpleMoves(board, pos, player, result);
            }
            return result;
        }

        private static void AddPawnSimpleMoves(Board board, BoardPosition from, PlayerColor player, List<Move> moves)
        {
            int forward = player == PlayerColor.White ? -1 : 1;
            foreach (int dc in new[] { -1, 1 })
            {
                var to = from.Offset(forward, dc);
                if (Board.IsInBounds(to) && board.IsEmpty(to))
                    moves.Add(new Move(from).AddStep(to));
            }
        }

        private static void AddKingSimpleMoves(Board board, BoardPosition from, List<Move> moves)
        {
            foreach (var (dr, dc) in Directions)
            {
                var pos = from.Offset(dr, dc);
                while (Board.IsInBounds(pos) && board.IsEmpty(pos))
                {
                    moves.Add(new Move(from).AddStep(pos));
                    pos = pos.Offset(dr, dc);
                }
            }
        }

        // ─── Captures ─────────────────────────────────────────────────────

        private static List<Move> GetAllCaptures(Board board, PlayerColor player)
        {
            var result = new List<Move>();
            foreach (var pos in board.GetPiecePositions(player))
            {
                var chain = new Move(pos);
                var visited = new HashSet<BoardPosition> { pos };
                ExpandCaptures(board, pos, board.GetPiece(pos), player, chain, visited, result);
            }
            return result;
        }

        private static void ExpandCaptures(
            Board board, BoardPosition from, PieceType piece, PlayerColor player,
            Move chain, HashSet<BoardPosition> visited, List<Move> result)
        {
            bool foundCapture = false;

            if (piece.IsKing())
            {
                foreach (var (dr, dc) in Directions)
                {
                    var enemy = from.Offset(dr, dc);
                    while (Board.IsInBounds(enemy) && board.IsEmpty(enemy))
                        enemy = enemy.Offset(dr, dc);

                    if (!Board.IsInBounds(enemy)) continue;
                    var enemyPiece = board.GetPiece(enemy);
                    if (enemyPiece.IsEmpty() || enemyPiece.BelongsTo(player)) continue;
                    if (visited.Contains(enemy)) continue;

                    // Land anywhere beyond the captured piece
                    var land = enemy.Offset(dr, dc);
                    while (Board.IsInBounds(land) && board.IsEmpty(land))
                    {
                        foundCapture = true;
                        var newChain = chain.AddStep(land, enemy);
                        var newVisited = new HashSet<BoardPosition>(visited) { enemy };

                        // Simulate capture to look for further jumps
                        var tempBoard = board.Clone();
                        tempBoard.SetPiece(enemy, PieceType.None);
                        ExpandCaptures(tempBoard, land, piece, player, newChain, newVisited, result);
                        land = land.Offset(dr, dc);
                    }
                }
            }
            else
            {
                // Pawns can capture in all 4 diagonal directions (Polish rules)
                foreach (var (dr, dc) in Directions)
                {
                    var enemy = from.Offset(dr, dc);
                    if (!Board.IsInBounds(enemy)) continue;
                    var enemyPiece = board.GetPiece(enemy);
                    if (enemyPiece.IsEmpty() || enemyPiece.BelongsTo(player)) continue;
                    if (visited.Contains(enemy)) continue;

                    var land = enemy.Offset(dr, dc);
                    if (!Board.IsInBounds(land) || !board.IsEmpty(land)) continue;

                    foundCapture = true;
                    var newChain = chain.AddStep(land, enemy);
                    var newVisited = new HashSet<BoardPosition>(visited) { enemy };

                    var tempBoard = board.Clone();
                    tempBoard.SetPiece(enemy, PieceType.None);
                    ExpandCaptures(tempBoard, land, piece, player, newChain, newVisited, result);
                }
            }

            if (!foundCapture && chain.IsCapture)
                result.Add(chain);
        }
    }

    public enum GameResult
    {
        InProgress,
        WhiteWins,
        BlackWins,
        Draw
    }
}
