using System;
using System.Text;
using static CheckmateRoyale.ChessCore.Types;

namespace CheckmateRoyale.ChessCore
{
    /// <summary>Forsyth–Edwards Notation: exact round-trip parse and emit.</summary>
    public static class Fen
    {
        public const string StartPos = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        private static char PieceChar(Piece p) => p switch
        {
            Piece.WP => 'P', Piece.WN => 'N', Piece.WB => 'B', Piece.WR => 'R', Piece.WQ => 'Q', Piece.WK => 'K',
            Piece.BP => 'p', Piece.BN => 'n', Piece.BB => 'b', Piece.BR => 'r', Piece.BQ => 'q', Piece.BK => 'k',
            _ => '.'
        };

        private static Piece CharPiece(char c) => c switch
        {
            'P' => Piece.WP, 'N' => Piece.WN, 'B' => Piece.WB, 'R' => Piece.WR, 'Q' => Piece.WQ, 'K' => Piece.WK,
            'p' => Piece.BP, 'n' => Piece.BN, 'b' => Piece.BB, 'r' => Piece.BR, 'q' => Piece.BQ, 'k' => Piece.BK,
            _ => Piece.None
        };

        /// <summary>Parse a FEN into a fresh <see cref="Position"/> with a valid hash.</summary>
        public static Position Parse(string fen)
        {
            if (string.IsNullOrWhiteSpace(fen)) throw new ArgumentException("Empty FEN.", nameof(fen));
            var parts = fen.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4) throw new ArgumentException($"Malformed FEN: '{fen}'", nameof(fen));

            var pos = new Position();

            int rank = 7, file = 0;
            foreach (char c in parts[0])
            {
                if (c == '/') { rank--; file = 0; }
                else if (char.IsDigit(c)) { file += c - '0'; }
                else
                {
                    Piece p = CharPiece(c);
                    if (p == Piece.None) throw new ArgumentException($"Bad piece '{c}' in FEN.", nameof(fen));
                    pos.AddPiece(SquareOf(file, rank), p);
                    file++;
                }
            }

            pos.SideToMove = parts[1] == "b" ? Color.Black : Color.White;

            int rights = 0;
            if (parts[2] != "-")
            {
                foreach (char c in parts[2])
                {
                    switch (c)
                    {
                        case 'K': rights |= CastleRight.WhiteKing; break;
                        case 'Q': rights |= CastleRight.WhiteQueen; break;
                        case 'k': rights |= CastleRight.BlackKing; break;
                        case 'q': rights |= CastleRight.BlackQueen; break;
                    }
                }
            }
            pos.Castling = rights;

            pos.EnPassant = parts[3] == "-" ? -1 : ParseSquare(parts[3]);
            pos.HalfmoveClock = parts.Length > 4 && int.TryParse(parts[4], out int hm) ? hm : 0;
            pos.FullmoveNumber = parts.Length > 5 && int.TryParse(parts[5], out int fm) ? fm : 1;

            pos.Hash = pos.ComputeHash();
            return pos;
        }

        /// <summary>Emit the full FEN string for a position.</summary>
        public static string ToFen(Position pos)
        {
            var sb = new StringBuilder(80);
            for (int rank = 7; rank >= 0; rank--)
            {
                int empty = 0;
                for (int file = 0; file < 8; file++)
                {
                    Piece p = pos.Board[SquareOf(file, rank)];
                    if (p == Piece.None) { empty++; continue; }
                    if (empty > 0) { sb.Append(empty); empty = 0; }
                    sb.Append(PieceChar(p));
                }
                if (empty > 0) sb.Append(empty);
                if (rank > 0) sb.Append('/');
            }

            sb.Append(pos.SideToMove == Color.White ? " w " : " b ");

            if (pos.Castling == 0) sb.Append('-');
            else
            {
                if ((pos.Castling & CastleRight.WhiteKing) != 0) sb.Append('K');
                if ((pos.Castling & CastleRight.WhiteQueen) != 0) sb.Append('Q');
                if ((pos.Castling & CastleRight.BlackKing) != 0) sb.Append('k');
                if ((pos.Castling & CastleRight.BlackQueen) != 0) sb.Append('q');
            }

            sb.Append(' ');
            sb.Append(pos.EnPassant == -1 ? "-" : SquareName(pos.EnPassant));
            sb.Append(' ').Append(pos.HalfmoveClock);
            sb.Append(' ').Append(pos.FullmoveNumber);
            return sb.ToString();
        }
    }
}
