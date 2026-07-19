using System;
using System.Collections.Generic;
using System.Text;
using static CheckmateRoyale.ChessCore.Types;

namespace CheckmateRoyale.ChessCore
{
    /// <summary>A parsed PGN game: tag roster, the moves, the starting position and the result.</summary>
    public sealed class PgnGame
    {
        public readonly Dictionary<string, string> Tags = new Dictionary<string, string>();
        public readonly List<Move> Moves = new List<Move>();
        public string StartFen = Fen.StartPos;
        public string Result = "*";
    }

    /// <summary>Standard Algebraic Notation encode/decode plus PGN game import/export.</summary>
    public static class Pgn
    {
        private static char PieceLetter(PieceType t) => t switch
        {
            PieceType.Knight => 'N',
            PieceType.Bishop => 'B',
            PieceType.Rook => 'R',
            PieceType.Queen => 'Q',
            PieceType.King => 'K',
            _ => '?'
        };

        // ---- SAN encode ----

        /// <summary>Encode a legal move as SAN (with +/# suffix), for the given position.</summary>
        public static string ToSan(Position pos, Move move)
        {
            var sb = new StringBuilder(8);

            if (move.Flag == MoveFlag.KingCastle) sb.Append("O-O");
            else if (move.Flag == MoveFlag.QueenCastle) sb.Append("O-O-O");
            else
            {
                PieceType pt = pos.Board[move.From].TypeOf();
                if (pt == PieceType.Pawn)
                {
                    if (move.IsCapture)
                        sb.Append((char)('a' + FileOf(move.From))).Append('x');
                    sb.Append(SquareName(move.To));
                    if (move.IsPromotion) sb.Append('=').Append(PieceLetter(move.Promotion));
                }
                else
                {
                    sb.Append(PieceLetter(pt));
                    sb.Append(Disambiguation(pos, move, pt));
                    if (move.IsCapture) sb.Append('x');
                    sb.Append(SquareName(move.To));
                }
            }

            // Check / checkmate suffix.
            pos.MakeMove(move, out StateUndo u);
            if (pos.InCheck(pos.SideToMove))
            {
                Span<Move> buf = stackalloc Move[MoveGenerator.MaxMoves];
                int n = MoveGenerator.GenerateLegal(pos, buf);
                sb.Append(n == 0 ? '#' : '+');
            }
            pos.UnmakeMove(move, u);

            return sb.ToString();
        }

        private static string Disambiguation(Position pos, Move move, PieceType pt)
        {
            Span<Move> buf = stackalloc Move[MoveGenerator.MaxMoves];
            int n = MoveGenerator.GenerateLegal(pos, buf);

            bool ambiguous = false, sameFile = false, sameRank = false;
            for (int i = 0; i < n; i++)
            {
                Move m = buf[i];
                if (m.From == move.From) continue;
                if (m.To != move.To) continue;
                if (pos.Board[m.From].TypeOf() != pt) continue;
                ambiguous = true;
                if (FileOf(m.From) == FileOf(move.From)) sameFile = true;
                if (RankOf(m.From) == RankOf(move.From)) sameRank = true;
            }

            if (!ambiguous) return "";
            if (!sameFile) return ((char)('a' + FileOf(move.From))).ToString();
            if (!sameRank) return ((char)('1' + RankOf(move.From))).ToString();
            return SquareName(move.From);
        }

        // ---- SAN decode ----

        /// <summary>Decode a SAN token into a legal move by matching against generated SAN.</summary>
        public static Move FromSan(Position pos, string san)
        {
            string want = Normalize(san);
            Span<Move> buf = stackalloc Move[MoveGenerator.MaxMoves];
            int n = MoveGenerator.GenerateLegal(pos, buf);
            for (int i = 0; i < n; i++)
            {
                if (Normalize(ToSan(pos, buf[i])) == want) return buf[i];
            }
            throw new ArgumentException($"Illegal or unparseable SAN '{san}' in position {Fen.ToFen(pos)}");
        }

        // Strip check/mate marks and annotation glyphs; normalize castling zeros.
        private static string Normalize(string san)
        {
            var sb = new StringBuilder(san.Length);
            foreach (char c in san)
            {
                if (c == '+' || c == '#' || c == '!' || c == '?') continue;
                sb.Append(c == '0' ? 'O' : c); // accept 0-0 for O-O
            }
            return sb.ToString();
        }

        // ---- game import / export ----

        /// <summary>Export a game to PGN text (seven-tag roster first, then movetext).</summary>
        public static string Write(PgnGame game)
        {
            var sb = new StringBuilder(512);
            string[] roster = { "Event", "Site", "Date", "Round", "White", "Black", "Result" };
            foreach (string key in roster)
                sb.Append('[').Append(key).Append(" \"")
                  .Append(game.Tags.TryGetValue(key, out string v) ? v : (key == "Result" ? game.Result : "?"))
                  .Append("\"]\n");
            if (game.StartFen != Fen.StartPos)
            {
                sb.Append("[SetUp \"1\"]\n");
                sb.Append("[FEN \"").Append(game.StartFen).Append("\"]\n");
            }
            sb.Append('\n');

            var pos = Fen.Parse(game.StartFen);
            int moveNo = pos.FullmoveNumber;
            bool whiteToMove = pos.SideToMove == Color.White;
            var line = new StringBuilder(256);
            foreach (Move m in game.Moves)
            {
                if (whiteToMove) line.Append(moveNo).Append(". ");
                else if (line.Length == 0) line.Append(moveNo).Append("... ");
                line.Append(ToSan(pos, m)).Append(' ');
                pos.MakeMove(m, out _);
                if (!whiteToMove) moveNo++;
                whiteToMove = !whiteToMove;
            }
            line.Append(game.Result);
            sb.Append(WrapAt80(line.ToString()));
            sb.Append('\n');
            return sb.ToString();
        }

        /// <summary>Parse PGN text into a <see cref="PgnGame"/> (single game).</summary>
        public static PgnGame Parse(string pgn)
        {
            var game = new PgnGame();
            var lines = pgn.Replace("\r\n", "\n").Split('\n');
            var moveText = new StringBuilder();

            foreach (string raw in lines)
            {
                string s = raw.Trim();
                if (s.Length == 0) continue;
                if (s[0] == '[' && s.EndsWith("]"))
                {
                    int sp = s.IndexOf(' ');
                    int q1 = s.IndexOf('"');
                    int q2 = s.LastIndexOf('"');
                    if (sp > 0 && q1 > 0 && q2 > q1)
                    {
                        string key = s.Substring(1, sp - 1);
                        string val = s.Substring(q1 + 1, q2 - q1 - 1);
                        game.Tags[key] = val;
                    }
                }
                else moveText.Append(s).Append(' ');
            }

            if (game.Tags.TryGetValue("FEN", out string fen)) game.StartFen = fen;
            if (game.Tags.TryGetValue("Result", out string res)) game.Result = res;

            var pos = Fen.Parse(game.StartFen);
            foreach (string token in Tokenize(moveText.ToString()))
            {
                if (token == "1-0" || token == "0-1" || token == "1/2-1/2" || token == "*")
                {
                    game.Result = token;
                    break;
                }
                Move m = FromSan(pos, token);
                game.Moves.Add(m);
                pos.MakeMove(m, out _);
            }
            return game;
        }

        // Split movetext into SAN tokens, dropping move numbers, comments, NAGs and variations.
        private static IEnumerable<string> Tokenize(string movetext)
        {
            var tokens = new List<string>();
            int i = 0, n = movetext.Length;
            while (i < n)
            {
                char c = movetext[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }
                if (c == '{') { while (i < n && movetext[i] != '}') i++; i++; continue; }
                if (c == '(') { int depth = 1; i++; while (i < n && depth > 0) { if (movetext[i] == '(') depth++; else if (movetext[i] == ')') depth--; i++; } continue; }
                if (c == '$') { i++; while (i < n && char.IsDigit(movetext[i])) i++; continue; }

                int start = i;
                while (i < n && !char.IsWhiteSpace(movetext[i]) && movetext[i] != '{' && movetext[i] != '(') i++;
                string tok = movetext.Substring(start, i - start);

                // Strip a leading "12." / "12..." move number.
                int dot = tok.LastIndexOf('.');
                if (dot >= 0) tok = tok.Substring(dot + 1);
                if (tok.Length == 0) continue;
                if (tok.Length == 1 && char.IsDigit(tok[0])) continue;
                tokens.Add(tok);
            }
            return tokens;
        }

        private static string WrapAt80(string text)
        {
            var words = text.Split(' ');
            var sb = new StringBuilder(text.Length + 16);
            int lineLen = 0;
            foreach (string w in words)
            {
                if (w.Length == 0) continue;
                if (lineLen > 0 && lineLen + 1 + w.Length > 80) { sb.Append('\n'); lineLen = 0; }
                else if (lineLen > 0) { sb.Append(' '); lineLen++; }
                sb.Append(w); lineLen += w.Length;
            }
            return sb.ToString();
        }
    }
}
