using System;
using UnityEngine;
using Unity.Cinemachine;

namespace CheckmateRoyale.Presentation.Cameras
{
    /// <summary>Tunable camera values (blends, FOVs, offsets). Plain serializable so it needs no asset.</summary>
    [Serializable]
    public sealed class CameraTuning
    {
        public float DefaultBlend = 0.35f;
        public float CutBlend = 0.12f;
        public float CommanderFov = 42f;
        public float DuelFov = 30f;
        public float DollyLateral = 2.2f;
        public float DollyHeight = 1.4f;
        public float OrbitRadius = 3.5f;
        public float OrbitHeight = 1.8f;
        public float DutchDegrees = 4f;
    }

    /// <summary>What the rigs need to frame the current move.</summary>
    public readonly struct BeatContext
    {
        public readonly Vector3 Attacker;
        public readonly Vector3 Victim;
        public readonly Vector3 BoardCenter;
        public readonly bool HasVictim;

        public BeatContext(Vector3 attacker, Vector3 victim, Vector3 boardCenter, bool hasVictim)
        {
            Attacker = attacker; Victim = victim; BoardCenter = boardCenter; HasVictim = hasVictim;
        }

        public Vector3 Focus => HasVictim ? Victim : Attacker;
        public Vector3 Midpoint => (Attacker + (HasVictim ? Victim : Attacker)) * 0.5f;
    }

    public interface ICameraRig
    {
        CinemachineCamera Camera { get; }
        void Prepare(in BeatContext ctx, CameraTuning t);
    }

    /// <summary>Base rig: owns a CinemachineCamera and helpers to pose it.</summary>
    public abstract class CameraRig : MonoBehaviour, ICameraRig
    {
        public CinemachineCamera Camera { get; private set; }

        protected virtual void Awake()
        {
            Camera = gameObject.GetComponent<CinemachineCamera>();
            if (Camera == null) Camera = gameObject.AddComponent<CinemachineCamera>();
            Camera.Priority = 0;
        }

        public abstract void Prepare(in BeatContext ctx, CameraTuning t);

        protected void Look(Vector3 eye, Vector3 target, float dutch = 0f)
        {
            transform.position = eye;
            Vector3 dir = target - eye;
            if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward;
            transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 0f, dutch);
        }

        protected void SetFov(float fov)
        {
            LensSettings lens = Camera.Lens;
            lens.FieldOfView = fov;
            Camera.Lens = lens;
        }
    }

    /// <summary>Default orbitable 3/4 view that always frames the whole board.</summary>
    public sealed class CommanderRig : CameraRig
    {
        public override void Prepare(in BeatContext ctx, CameraTuning t)
        {
            SetFov(t.CommanderFov);
            Look(ctx.BoardCenter + new Vector3(0f, 8.5f, -7.5f), ctx.BoardCenter);
        }
    }

    /// <summary>Side dolly along the attacker→target line for Approach beats.</summary>
    public sealed class DollyTrackRig : CameraRig
    {
        public override void Prepare(in BeatContext ctx, CameraTuning t)
        {
            SetFov(t.CommanderFov);
            Vector3 line = ctx.Focus - ctx.Attacker;
            Vector3 lateral = Vector3.Cross(line.normalized, Vector3.up);
            if (lateral.sqrMagnitude < 1e-4f) lateral = Vector3.right;
            Vector3 eye = ctx.Midpoint + lateral.normalized * t.DollyLateral + Vector3.up * t.DollyHeight;
            Look(eye, ctx.Attacker);
        }
    }

    /// <summary>Tight over-the-shoulder of attacker facing victim, for Impact beats.</summary>
    public sealed class DuelOTSRig : CameraRig
    {
        public override void Prepare(in BeatContext ctx, CameraTuning t)
        {
            SetFov(t.DuelFov);
            Vector3 dir = (ctx.Focus - ctx.Attacker);
            if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward;
            dir.Normalize();
            Vector3 eye = ctx.Attacker - dir * 1.4f + Vector3.up * 1.1f + Vector3.Cross(dir, Vector3.up) * 0.5f;
            Look(eye, ctx.Focus + Vector3.up * 0.3f);
        }
    }

    /// <summary>Cranes up-and-back to reveal the threatened king, for Check beats.</summary>
    public sealed class CraneRevealRig : CameraRig
    {
        public override void Prepare(in BeatContext ctx, CameraTuning t)
        {
            SetFov(t.CommanderFov);
            Vector3 eye = ctx.Attacker + new Vector3(0f, 3.5f, -3.5f);
            Look(eye, ctx.Focus);
        }
    }

    /// <summary>180° orbit around the duel at a dutch angle, for Finisher beats.</summary>
    public sealed class OrbitalSloMoRig : CameraRig
    {
        public override void Prepare(in BeatContext ctx, CameraTuning t)
        {
            SetFov(t.DuelFov);
            Vector3 center = ctx.Midpoint;
            Vector3 eye = center + new Vector3(t.OrbitRadius, t.OrbitHeight, -t.OrbitRadius * 0.4f);
            Look(eye, center + Vector3.up * 0.4f, t.DutchDegrees);
        }
    }
}
