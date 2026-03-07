namespace Warcaby.Core
{
    public enum PieceType
    {
        None = 0,
        White = 1,
        Black = 2,
        WhiteKing = 3,
        BlackKing = 4
    }

    public static class PieceTypeExtensions
    {
        public static bool IsWhite(this PieceType type) =>
            type == PieceType.White || type == PieceType.WhiteKing;

        public static bool IsBlack(this PieceType type) =>
            type == PieceType.Black || type == PieceType.BlackKing;

        public static bool IsKing(this PieceType type) =>
            type == PieceType.WhiteKing || type == PieceType.BlackKing;

        public static bool IsEmpty(this PieceType type) => type == PieceType.None;

        public static bool BelongsTo(this PieceType type, PlayerColor player) =>
            player == PlayerColor.White ? type.IsWhite() : type.IsBlack();

        public static PieceType Promote(this PieceType type)
        {
            if (type == PieceType.White) return PieceType.WhiteKing;
            if (type == PieceType.Black) return PieceType.BlackKing;
            return type;
        }
    }

    public enum PlayerColor
    {
        White = 0,
        Black = 1
    }

    public static class PlayerColorExtensions
    {
        public static PlayerColor Opponent(this PlayerColor player) =>
            player == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
    }
}
