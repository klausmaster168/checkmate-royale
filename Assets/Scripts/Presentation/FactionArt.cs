using UnityEngine;
using CC = CheckmateRoyale.ChessCore;

namespace CheckmateRoyale.Presentation
{
    /// <summary>
    /// Optional per-faction art: a prefab per piece type (e.g. imported KayKit/Quaternius/Mixamo
    /// models) plus a team tint. Any slot left null falls back to the primitive placeholder, so
    /// the game runs with zero art and upgrades piece-by-piece as prefabs are assigned — no code
    /// changes anywhere. This is the drop-in seam for the free-asset art pass.
    /// </summary>
    [CreateAssetMenu(menuName = "Checkmate Royale/Faction Art", fileName = "FactionArt")]
    public sealed class FactionArt : ScriptableObject
    {
        public string DisplayName = "Faction";
        public Color TeamTint = Color.white;
        public bool ApplyTint = false;

        [Header("Piece prefabs (leave null to use the placeholder primitive)")]
        public GameObject Pawn;
        public GameObject Knight;
        public GameObject Bishop;
        public GameObject Rook;
        public GameObject Queen;
        public GameObject King;

        public GameObject PrefabFor(CC.PieceType type) => type switch
        {
            CC.PieceType.Pawn => Pawn,
            CC.PieceType.Knight => Knight,
            CC.PieceType.Bishop => Bishop,
            CC.PieceType.Rook => Rook,
            CC.PieceType.Queen => Queen,
            CC.PieceType.King => King,
            _ => null
        };
    }
}
