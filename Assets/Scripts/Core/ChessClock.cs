using System;

namespace Warcaby.Core
{
    /// <summary>
    /// Two-player chess-style countdown clock.
    /// Call Tick(deltaTime) every frame from GameManager.Update().
    /// </summary>
    public class ChessClock
    {
        public float WhiteRemaining { get; private set; }
        public float BlackRemaining { get; private set; }
        public bool  Running        { get; private set; }

        /// <summary>Fired once when the active player's time reaches zero.</summary>
        public event Action<PlayerColor> OnTimeExpired;

        private PlayerColor _active;
        private bool        _fired;

        public ChessClock(float secondsPerPlayer)
        {
            WhiteRemaining = secondsPerPlayer;
            BlackRemaining = secondsPerPlayer;
        }

        /// <summary>Switch the active side and (re)start ticking.</summary>
        public void SetActive(PlayerColor player)
        {
            _active = player;
            Running = true;
        }

        public void Stop() => Running = false;

        public void Tick(float dt)
        {
            if (!Running || _fired) return;

            if (_active == PlayerColor.White)
                WhiteRemaining -= dt;
            else
                BlackRemaining -= dt;

            float remaining = _active == PlayerColor.White ? WhiteRemaining : BlackRemaining;
            if (remaining <= 0f)
            {
                Running = false;
                _fired  = true;
                OnTimeExpired?.Invoke(_active);
            }
        }

        /// <summary>Formats seconds as "M:SS".</summary>
        public static string Format(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int m = (int)(seconds / 60f);
            int s = (int)(seconds % 60f);
            return $"{m}:{s:D2}";
        }
    }
}
