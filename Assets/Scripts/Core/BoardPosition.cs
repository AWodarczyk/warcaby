using System;

namespace Warcaby.Core
{
    [Serializable]
    public struct BoardPosition : IEquatable<BoardPosition>
    {
        public int Row;
        public int Col;

        public BoardPosition(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public bool IsValid(int boardSize = 8) =>
            Row >= 0 && Row < boardSize && Col >= 0 && Col < boardSize;

        public bool Equals(BoardPosition other) => Row == other.Row && Col == other.Col;
        public override bool Equals(object obj) => obj is BoardPosition other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Row, Col);
        public static bool operator ==(BoardPosition a, BoardPosition b) => a.Equals(b);
        public static bool operator !=(BoardPosition a, BoardPosition b) => !a.Equals(b);
        public override string ToString() => $"({Row}, {Col})";

        public BoardPosition Offset(int dRow, int dCol) => new BoardPosition(Row + dRow, Col + dCol);
    }
}
