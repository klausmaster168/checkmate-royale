using System;
using static CheckmateRoyale.ChessCore.Types;

namespace CheckmateRoyale.ChessCore
{
    /// <summary>A time control: base time, Fischer increment and (optional) US delay, in ms.</summary>
    public readonly struct TimeControl
    {
        public readonly int BaseMs;
        public readonly int IncrementMs;
        public readonly int DelayMs;

        public TimeControl(int baseMs, int incrementMs, int delayMs = 0)
        {
            BaseMs = baseMs; IncrementMs = incrementMs; DelayMs = delayMs;
        }

        public static readonly TimeControl Bullet1_0 = new TimeControl(60_000, 0);
        public static readonly TimeControl Blitz3_2 = new TimeControl(180_000, 2_000);
        public static readonly TimeControl Rapid10_5 = new TimeControl(600_000, 5_000);
    }

    /// <summary>
    /// Pure chess clock. Time is driven entirely by injected elapsed-millisecond values
    /// (never wall-clock) so it is fully deterministic and testable. Presentation must
    /// never pause this for animations — that is the OFF-CLOCK CINEMA law.
    /// </summary>
    public sealed class Clock
    {
        private readonly TimeControl _tc;
        private readonly long[] _remaining = new long[2];

        public Clock(TimeControl tc)
        {
            _tc = tc;
            _remaining[0] = tc.BaseMs;
            _remaining[1] = tc.BaseMs;
        }

        public long Remaining(Color c) => _remaining[(int)c];
        public bool HasFlagged(Color c) => _remaining[(int)c] <= 0;

        /// <summary>
        /// Register that <paramref name="side"/> spent <paramref name="thinkMs"/> on their move.
        /// Applies delay then increment. Returns true if the side flagged (ran out of time).
        /// </summary>
        public bool Press(Color side, long thinkMs)
        {
            if (thinkMs < 0) throw new ArgumentOutOfRangeException(nameof(thinkMs));
            int i = (int)side;

            long deduction = Math.Max(0, thinkMs - _tc.DelayMs); // US delay: first DelayMs is free
            bool flagged = deduction > _remaining[i];
            _remaining[i] -= deduction;
            if (_remaining[i] < 0) _remaining[i] = 0;

            if (!flagged) _remaining[i] += _tc.IncrementMs; // no increment on a flag fall
            return flagged;
        }
    }
}
