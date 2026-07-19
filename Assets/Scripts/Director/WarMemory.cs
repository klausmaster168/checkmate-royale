using System.Collections.Generic;
using System.IO;
using CheckmateRoyale.ChessCore;
using static CheckmateRoyale.ChessCore.Types;

namespace CheckmateRoyale.Director
{
    /// <summary>Facts about the current move derived from the war's history.</summary>
    public readonly struct NarrativeFacts
    {
        public readonly bool Revenge;
        public readonly bool Rampage;
        public readonly bool LastOfTypeFalls;
        public readonly bool FirstBlood;
        public readonly int Bonus; // 0..100

        public NarrativeFacts(bool revenge, bool rampage, bool lastOfType, bool firstBlood, int bonus)
        {
            Revenge = revenge; Rampage = rampage; LastOfTypeFalls = lastOfType;
            FirstBlood = firstBlood; Bonus = bonus;
        }
    }

    /// <summary>
    /// Per-game memory of the battle: a stable identity for every piece (surviving moves
    /// and promotions), a kill map, per-piece rampage timing, check counts and the biggest
    /// eval swing so far. Serializes to a compact byte[] (&lt;=2KB) for replay storage.
    /// </summary>
    public sealed class WarMemory
    {
        private const byte SerVersion = 1;
        private const int NoCapture = -1000;

        public struct Kill
        {
            public byte KillerId, VictimId, KillerColor, VictimColor, VictimType;
            public short Ply;
        }

        private readonly byte[] _pieceIdAt = new byte[64]; // 0 = empty
        private byte _nextId = 1;
        private readonly int[] _lastCapturePlyById = new int[33];
        private readonly int[] _checkCount = new int[2];
        private readonly List<Kill> _kills = new List<Kill>(40);

        public int BiggestSwingCp { get; private set; }
        public int CaptureCount { get; private set; }
        public int LastCaptureSquare { get; private set; } = -1;
        public int LastCapturePly { get; private set; } = NoCapture;

        public IReadOnlyList<Kill> Kills => _kills;
        public int CheckCount(Color c) => _checkCount[(int)c];

        public WarMemory() { for (int i = 0; i < 33; i++) _lastCapturePlyById[i] = NoCapture; }

        /// <summary>Assign a stable id to every piece on the starting position (ascending square order).</summary>
        public void Init(Position start)
        {
            for (int i = 0; i < 64; i++) _pieceIdAt[i] = 0;
            _nextId = 1;
            for (int sq = 0; sq < 64; sq++)
                if (start.Board[sq] != Piece.None) _pieceIdAt[sq] = _nextId++;
        }

        private static int CaptureSquare(in Move m, Color mover) =>
            m.IsEnPassant ? (mover == Color.White ? m.To - 8 : m.To + 8) : m.To;

        /// <summary>Read narrative facts for a move WITHOUT mutating memory (call before <see cref="RecordMove"/>).</summary>
        public NarrativeFacts Evaluate(in Move m, Position before, int ply)
        {
            if (!m.IsCapture)
                return new NarrativeFacts(false, false, false, false, 0);

            Color mover = before.SideToMove;
            Color victimColor = mover.Opposite();
            int capSq = CaptureSquare(m, mover);
            byte capturerId = _pieceIdAt[m.From];
            byte victimId = _pieceIdAt[capSq];
            PieceType victimType = before.Board[capSq].TypeOf();

            bool firstBlood = CaptureCount == 0;

            bool revenge = false;
            for (int i = 0; i < _kills.Count; i++)
                if (_kills[i].KillerId == victimId && (Color)_kills[i].VictimColor == mover) { revenge = true; break; }

            int last = _lastCapturePlyById[capturerId];
            bool rampage = last != NoCapture && (ply - last) <= 6;

            bool lastOfType = Bitboards.PopCount(before.PieceBB(victimColor, victimType)) == 1;

            int bonus = (revenge ? 40 : 0) + (rampage ? 25 : 0) + (lastOfType ? 20 : 0);
            if (bonus > 100) bonus = 100;
            return new NarrativeFacts(revenge, rampage, lastOfType, firstBlood, bonus);
        }

        public bool IsRecapture(in Move m, int ply) =>
            m.IsCapture && m.To == LastCaptureSquare && ply == LastCapturePly + 1;

        /// <summary>Advance memory to reflect a committed move (updates identity, kills, streaks).</summary>
        public void RecordMove(in Move m, Position before, int ply, bool isCheck, int signedSwingCp)
        {
            Color mover = before.SideToMove;
            byte capturerId = _pieceIdAt[m.From];

            if (m.IsCapture)
            {
                int capSq = CaptureSquare(m, mover);
                byte victimId = _pieceIdAt[capSq];
                PieceType victimType = before.Board[capSq].TypeOf();
                _kills.Add(new Kill
                {
                    KillerId = capturerId,
                    VictimId = victimId,
                    KillerColor = (byte)mover,
                    VictimColor = (byte)mover.Opposite(),
                    VictimType = (byte)victimType,
                    Ply = (short)ply
                });
                _pieceIdAt[capSq] = 0;
                CaptureCount++;
                LastCaptureSquare = m.To;
                LastCapturePly = ply;
                _lastCapturePlyById[capturerId] = ply;
            }

            // Move the identity from -> to (promotion keeps the same id).
            _pieceIdAt[m.From] = 0;
            _pieceIdAt[m.To] = capturerId;

            // Castling rook hop.
            switch (m.Flag)
            {
                case MoveFlag.KingCastle when mover == Color.White: MoveId(7, 5); break;
                case MoveFlag.KingCastle: MoveId(63, 61); break;
                case MoveFlag.QueenCastle when mover == Color.White: MoveId(0, 3); break;
                case MoveFlag.QueenCastle: MoveId(56, 59); break;
            }

            if (isCheck) _checkCount[(int)mover]++;
            if (System.Math.Abs(signedSwingCp) > System.Math.Abs(BiggestSwingCp)) BiggestSwingCp = signedSwingCp;
        }

        private void MoveId(int from, int to) { _pieceIdAt[to] = _pieceIdAt[from]; _pieceIdAt[from] = 0; }

        // ---- serialization ----

        public byte[] ToBytes()
        {
            using var ms = new MemoryStream(256);
            using var w = new BinaryWriter(ms);
            w.Write(SerVersion);
            w.Write(_nextId);
            w.Write(_pieceIdAt);
            w.Write(_checkCount[0]); w.Write(_checkCount[1]);
            w.Write(BiggestSwingCp);
            w.Write(CaptureCount);
            w.Write(LastCaptureSquare);
            w.Write(LastCapturePly);
            for (int i = 0; i < 33; i++) w.Write(_lastCapturePlyById[i]);
            w.Write(_kills.Count);
            foreach (var k in _kills)
            {
                w.Write(k.KillerId); w.Write(k.VictimId); w.Write(k.KillerColor);
                w.Write(k.VictimColor); w.Write(k.VictimType); w.Write(k.Ply);
            }
            w.Flush();
            return ms.ToArray();
        }

        public static WarMemory FromBytes(byte[] data)
        {
            var wm = new WarMemory();
            using var ms = new MemoryStream(data);
            using var r = new BinaryReader(ms);
            byte ver = r.ReadByte();
            if (ver != SerVersion) throw new IOException($"Unsupported WarMemory version {ver}");
            wm._nextId = r.ReadByte();
            var ids = r.ReadBytes(64);
            System.Array.Copy(ids, wm._pieceIdAt, 64);
            wm._checkCount[0] = r.ReadInt32(); wm._checkCount[1] = r.ReadInt32();
            wm.BiggestSwingCp = r.ReadInt32();
            wm.CaptureCount = r.ReadInt32();
            wm.LastCaptureSquare = r.ReadInt32();
            wm.LastCapturePly = r.ReadInt32();
            for (int i = 0; i < 33; i++) wm._lastCapturePlyById[i] = r.ReadInt32();
            int killCount = r.ReadInt32();
            for (int i = 0; i < killCount; i++)
            {
                wm._kills.Add(new Kill
                {
                    KillerId = r.ReadByte(), VictimId = r.ReadByte(), KillerColor = r.ReadByte(),
                    VictimColor = r.ReadByte(), VictimType = r.ReadByte(), Ply = r.ReadInt16()
                });
            }
            return wm;
        }
    }
}
