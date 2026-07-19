namespace CheckmateRoyale.Director
{
    /// <summary>
    /// Rations spectacle so peaks stay rare. Slow-motion is token-gated: a fixed pool that
    /// regenerates slowly, so the tenth capture still lands. Deterministic given the ply
    /// history. Finishers are allowed only for mate (enforced by the Shot Planner).
    /// </summary>
    public sealed class EscalationBudget
    {
        public const int Cap = 5;
        private const int RegenEveryPlies = 10;

        private int _tokens;
        private int _lastRegenPly;

        public EscalationBudget(int startTokens = Cap)
        {
            _tokens = startTokens;
            _lastRegenPly = 0;
        }

        public int Tokens => _tokens;

        /// <summary>Tokens that WOULD be available at <paramref name="ply"/>, without mutating (pure).</summary>
        public int PeekAvailable(int ply)
        {
            int t = _tokens, last = _lastRegenPly;
            while (ply - last >= RegenEveryPlies) { if (t < Cap) t++; last += RegenEveryPlies; }
            return t;
        }

        /// <summary>Advance regeneration to <paramref name="ply"/> and optionally spend one slow-mo token.</summary>
        public void Commit(int ply, bool spendSlowMo)
        {
            while (ply - _lastRegenPly >= RegenEveryPlies) { if (_tokens < Cap) _tokens++; _lastRegenPly += RegenEveryPlies; }
            if (spendSlowMo && _tokens > 0) _tokens--;
        }

        public EscalationBudget Clone()
        {
            var b = new EscalationBudget(_tokens);
            b._lastRegenPly = _lastRegenPly;
            return b;
        }
    }
}
