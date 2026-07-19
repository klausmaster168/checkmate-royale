using System;
using CheckmateRoyale.ChessCore;
using CheckmateRoyale.ChessCore.Util;

namespace CheckmateRoyale.Director
{
    /// <summary>Structural facts about a move, derived once and shared by scorer and planner.</summary>
    public readonly struct MoveFacts
    {
        public readonly bool IsCapture, IsCheck, IsMate, IsCastle, IsPromotion, IsEnPassant;

        public MoveFacts(bool isCapture, bool isCheck, bool isMate, bool isCastle, bool isPromotion, bool isEnPassant)
        {
            IsCapture = isCapture; IsCheck = isCheck; IsMate = isMate;
            IsCastle = isCastle; IsPromotion = isPromotion; IsEnPassant = isEnPassant;
        }

        /// <summary>A quiet move is neither a capture, a mate, nor a promotion/castle special.</summary>
        public bool IsQuiet => !IsCapture && !IsMate && !IsCastle && !IsPromotion;

        public static MoveFacts From(in DirectorInput input)
        {
            Position after = input.After;
            bool isCheck = after.InCheck(after.SideToMove);
            bool isMate = false;
            if (isCheck)
            {
                Span<Move> buf = stackalloc Move[MoveGenerator.MaxMoves];
                isMate = MoveGenerator.GenerateLegal(after, buf) == 0;
            }
            Move m = input.Move;
            return new MoveFacts(m.IsCapture, isCheck, isMate, m.IsCastle, m.IsPromotion, m.IsEnPassant);
        }
    }
    /// <summary>Player-facing pacing setting (the "Mode Dial").</summary>
    public enum ModeDial : byte { Cinema = 0, Battle = 1, Blitz = 2, Auto = 3 }

    /// <summary>Coarse game phase by material, used for flavour selection.</summary>
    public enum GamePhase : byte { Opening = 0, Middlegame = 1, Endgame = 2 }

    /// <summary>Narrative/quality tags the Director attaches to a move.</summary>
    public enum DramaTag : byte
    {
        Blunder = 0, Brilliant = 1, Revenge = 2, Rampage = 3,
        Desperate = 4, Quiet = 5, Decisive = 6, FirstBlood = 7
    }

    /// <summary>Kinds of camera rig the Shot Planner can request (executed by Cinemachine later).</summary>
    public enum CameraRig : byte
    {
        Commander = 0, DollyTrack = 1, DuelOTS = 2, CraneReveal = 3, OrbitalSloMo = 4
    }

    /// <summary>Beat types in a directed sequence.</summary>
    public enum BeatType : byte
    {
        Confirm = 0, March = 1, Approach = 2, Impact = 3,
        Fall = 4, Victor = 5, Return = 6, CraneReveal = 7, Finisher = 8
    }

    /// <summary>Snapshot of the mover's clock at move time (seconds are display-domain, not wall-clock).</summary>
    public readonly struct ClockState
    {
        public readonly int SecondsLeft;
        public readonly bool IsLowTime;
        public ClockState(int secondsLeft, bool isLowTime)
        {
            SecondsLeft = secondsLeft; IsLowTime = isLowTime;
        }
        public static readonly ClockState Untimed = new ClockState(int.MaxValue, false);
    }

    /// <summary>Centipawn and "spice" piece values used by the Drama Scorer.</summary>
    internal static class Values
    {
        // Centipawns for static material / sacrifice reasoning.
        public static int Cp(PieceType t) => t switch
        {
            PieceType.Pawn => 100,
            PieceType.Knight => 300,
            PieceType.Bishop => 320,
            PieceType.Rook => 500,
            PieceType.Queen => 900,
            _ => 0
        };

        // "Spice" values for the materialEvent drama component (per the phase spec).
        public static int Spice(PieceType t) => t switch
        {
            PieceType.Pawn => 10,
            PieceType.Knight => 28,
            PieceType.Bishop => 30,
            PieceType.Rook => 45,
            PieceType.Queen => 85,
            _ => 0
        };
    }

    /// <summary>Deterministic per-move PRNG factory: stable for a given (seed, ply).</summary>
    internal static class MoveRandom
    {
        public static Xoshiro256 For(ulong directorSeed, int ply)
        {
            ulong s = directorSeed ^ unchecked((ulong)(uint)ply * 0x9E3779B97F4A7C15UL);
            return new Xoshiro256(s);
        }
    }
}
