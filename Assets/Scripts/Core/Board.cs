using System;
using System.Collections.Generic;

namespace Warcaby.Core
{
    /// <summary>
    /// Immutable(ish) board state. Clone before mutating for AI search.
    /// </summary>
    public class Board
    {
        public const int Size = 8;
        private readonly PieceType[,] _cells = new PieceType[Size, Size];

        // Piece counts – computed on demand from _cells (no cache to go out of sync)
        public int WhitePieces
        {
            get
            {
                int n = 0;
                for (int r = 0; r < Size; r++)
                    for (int c = 0; c < Size; c++)
                        if (_cells[r, c].IsWhite()) n++;
                return n;
            }
        }

        public int BlackPieces
        {
            get
            {
                int n = 0;
                for (int r = 0; r < Size; r++)
                    for (int c = 0; c < Size; c++)
                        if (_cells[r, c].IsBlack()) n++;
                return n;
            }
        }

        // ─── Constructors ──────────────────────────────────────────────────

        public Board() { }

        private Board(Board source)
        {
            Array.Copy(source._cells, _cells, _cells.Length);
        }

        // ─── Factory ───────────────────────────────────────────────────────

        /// <summary>Creates standard Polish checkers initial position.</summary>
        public static Board CreateInitial()
        {
            var board = new Board();

            // Black pieces occupy top three rows (rows 0-2)
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    if (IsPlayableSquare(row, col))
                    {
                        board._cells[row, col] = PieceType.Black;
                    }
                }
            }

            // White pieces occupy bottom three rows (rows 5-7)
            for (int row = 5; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    if (IsPlayableSquare(row, col))
                    {
                        board._cells[row, col] = PieceType.White;
                    }
                }
            }

            return board;
        }

        // ─── Access ────────────────────────────────────────────────────────

        public PieceType GetPiece(int row, int col) => _cells[row, col];
        public PieceType GetPiece(BoardPosition pos) => _cells[pos.Row, pos.Col];

        public void SetPiece(int row, int col, PieceType type) => SetPiece(new BoardPosition(row, col), type);

        public void SetPiece(BoardPosition pos, PieceType type)
        {
            _cells[pos.Row, pos.Col] = type;
        }

        public bool IsEmpty(BoardPosition pos) => _cells[pos.Row, pos.Col] == PieceType.None;

        // ─── Helpers ───────────────────────────────────────────────────────

        public static bool IsPlayableSquare(int row, int col) => (row + col) % 2 == 1;
        public static bool IsInBounds(int row, int col) => row >= 0 && row < Size && col >= 0 && col < Size;
        public static bool IsInBounds(BoardPosition pos) => IsInBounds(pos.Row, pos.Col);

        /// <summary>Applies a move (mutates in place – clone first for AI).</summary>
        public void ApplyMove(Move move)
        {
            var piece = GetPiece(move.From);

            // Remove captured pieces
            foreach (var cap in move.Captures)
                SetPiece(cap, PieceType.None);

            // Move the piece
            SetPiece(move.From, PieceType.None);

            // Promotion check (Polish rules: promote immediately upon reaching last rank)
            bool promotionRow = (piece.IsWhite() && move.To.Row == 0) ||
                                 (piece.IsBlack() && move.To.Row == Size - 1);
            SetPiece(move.To, promotionRow ? piece.Promote() : piece);
        }

        public Board Clone() => new Board(this);

        /// <summary>Returns all positions occupied by pieces of given player.</summary>
        public IEnumerable<BoardPosition> GetPiecePositions(PlayerColor player)
        {
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    if (_cells[r, c].BelongsTo(player))
                        yield return new BoardPosition(r, c);
        }
    }
}
