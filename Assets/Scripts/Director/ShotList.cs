using System.IO;

namespace CheckmateRoyale.Director
{
    /// <summary>One timed beat of a directed sequence. Carries the cues presentation will fire.</summary>
    public struct Beat
    {
        public BeatType Type;
        public float Duration;        // seconds (presentation-domain, off-clock)
        public CameraRig Camera;
        public byte AnimationIntentId;
        public byte VfxTier;          // 0..3
        public float SlowMoFactor;    // 1.0 = normal speed; < 1 = slow motion
        public byte AudioStingerId;
    }

    /// <summary>
    /// The Director's output for one move: an ordered set of beats plus the metadata that
    /// produced it. Serializes to a versioned, byte-exact blob so replays re-render
    /// identically and golden tests can diff it.
    /// </summary>
    public sealed class ShotList
    {
        public const int SchemaVersion = 1;

        public int Ply;
        public ModeDial Dial;
        public int DramaScoreValue;
        public DramaTag[] Tags = System.Array.Empty<DramaTag>();
        public Beat[] Beats = System.Array.Empty<Beat>();

        public float TotalDuration
        {
            get { float t = 0; foreach (var b in Beats) t += b.Duration; return t; }
        }

        public byte[] ToBytes()
        {
            using var ms = new MemoryStream(128);
            using var w = new BinaryWriter(ms);
            w.Write(SchemaVersion);
            w.Write(Ply);
            w.Write((byte)Dial);
            w.Write(DramaScoreValue);

            w.Write((byte)Tags.Length);
            foreach (var t in Tags) w.Write((byte)t);

            w.Write((byte)Beats.Length);
            foreach (var b in Beats)
            {
                w.Write((byte)b.Type);
                w.Write(b.Duration);
                w.Write((byte)b.Camera);
                w.Write(b.AnimationIntentId);
                w.Write(b.VfxTier);
                w.Write(b.SlowMoFactor);
                w.Write(b.AudioStingerId);
            }
            w.Flush();
            return ms.ToArray();
        }

        public static ShotList FromBytes(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var r = new BinaryReader(ms);
            int ver = r.ReadInt32();
            if (ver != SchemaVersion) throw new IOException($"Unsupported ShotList schema {ver}");
            var s = new ShotList
            {
                Ply = r.ReadInt32(),
                Dial = (ModeDial)r.ReadByte(),
                DramaScoreValue = r.ReadInt32()
            };
            int tagCount = r.ReadByte();
            s.Tags = new DramaTag[tagCount];
            for (int i = 0; i < tagCount; i++) s.Tags[i] = (DramaTag)r.ReadByte();

            int beatCount = r.ReadByte();
            s.Beats = new Beat[beatCount];
            for (int i = 0; i < beatCount; i++)
            {
                s.Beats[i] = new Beat
                {
                    Type = (BeatType)r.ReadByte(),
                    Duration = r.ReadSingle(),
                    Camera = (CameraRig)r.ReadByte(),
                    AnimationIntentId = r.ReadByte(),
                    VfxTier = r.ReadByte(),
                    SlowMoFactor = r.ReadSingle(),
                    AudioStingerId = r.ReadByte()
                };
            }
            return s;
        }

        public bool UsesSlowMo()
        {
            foreach (var b in Beats) if (b.SlowMoFactor < 1.0f) return true;
            return false;
        }
    }
}
