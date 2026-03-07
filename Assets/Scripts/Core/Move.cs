using System;
using System.Collections.Generic;

namespace Warcaby.Core
{
    /// <summary>
    /// Represents a single move or a multi-capture chain.
    /// </summary>
    [Serializable]
    public class Move
    {
        /// <summary>Ordered list of positions visited (first = from, last = destination).</summary>
        public List<BoardPosition> Path { get; }

        /// <summary>Positions of captured pieces (in order of capture).</summary>
        public List<BoardPosition> Captures { get; }

        public BoardPosition From => Path[0];
        public BoardPosition To => Path[^1];

        public bool IsCapture => Captures != null && Captures.Count > 0;

        public Move(BoardPosition from)
        {
            Path = new List<BoardPosition> { from };
            Captures = new List<BoardPosition>();
        }

        /// <summary>Copy constructor for building multi-capture chains.</summary>
        public Move(Move source)
        {
            Path = new List<BoardPosition>(source.Path);
            Captures = new List<BoardPosition>(source.Captures);
        }

        public Move AddStep(BoardPosition next, BoardPosition? captured = null)
        {
            var m = new Move(this);
            m.Path.Add(next);
            if (captured.HasValue) m.Captures.Add(captured.Value);
            return m;
        }

        public override string ToString()
        {
            var steps = string.Join(" -> ", Path);
            return IsCapture ? $"[CAPTURE] {steps}" : steps;
        }
    }
}
