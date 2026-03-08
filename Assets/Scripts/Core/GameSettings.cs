using Warcaby.Core;

namespace Warcaby
{
    /// <summary>
    /// Static container for settings chosen in MainMenu.
    /// Survives scene load (no MonoBehaviour needed).
    /// </summary>
    public static class GameSettings
    {
        public static GameMode  Mode        { get; set; } = GameMode.LocalPvP;
        public static PlayerColor HumanColor { get; set; } = PlayerColor.White;
        public static int       AIDepth     { get; set; } = 5;

        // Online
        public static string    ServerAddress { get; set; } = "localhost";

        // Clock (seconds per player; 0 = no clock)
        public static int       ClockSeconds  { get; set; } = 0;
    }
}
