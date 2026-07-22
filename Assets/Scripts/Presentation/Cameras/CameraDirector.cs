using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using CheckmateRoyale.Director;

namespace CheckmateRoyale.Presentation.Cameras
{
    /// <summary>
    /// Drives one CinemachineBrain across five priority-blended rigs, switching per beat.
    /// Skipping hard-cuts back to the Commander view. Inert when there is no camera (tests).
    /// </summary>
    public sealed class CameraDirector : MonoBehaviour
    {
        public CameraTuning Tuning = new CameraTuning();

        private const int ActivePriority = 20;
        private const int IdlePriority = 5;

        private CinemachineBrain _brain;
        private PieceViewRegistry _registry;
        private BoardView _board;
        private SequencePlayer _player;

        private CommanderRig _commander;
        private DollyTrackRig _dolly;
        private DuelOTSRig _duel;
        private CraneRevealRig _crane;
        private OrbitalSloMoRig _orbital;

        public void Init(Camera mainCam, PieceViewRegistry registry, BoardView board, SequencePlayer player)
        {
            _registry = registry;
            _board = board;
            _player = player;

            if (mainCam == null) return; // headless / tests: nothing to drive

            _brain = mainCam.GetComponent<CinemachineBrain>();
            if (_brain == null) _brain = mainCam.gameObject.AddComponent<CinemachineBrain>();
            _brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, Tuning.DefaultBlend);

            _commander = Make<CommanderRig>("CommanderCam");
            _dolly = Make<DollyTrackRig>("DollyTrackCam");
            _duel = Make<DuelOTSRig>("DuelOTSCam");
            _crane = Make<CraneRevealRig>("CraneRevealCam");
            _orbital = Make<OrbitalSloMoRig>("OrbitalSloMoCam");

            BeatContext ctx = DefaultContext();
            _commander.Prepare(ctx, Tuning);
            Activate(_commander);

            _player.BeatStarted += OnBeat;
            _player.SequenceSkipped += OnSkip;
        }

        private T Make<T>(string name) where T : CameraRig
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rig = go.AddComponent<T>();
            rig.Camera.Priority = IdlePriority;
            return rig;
        }

        private void OnBeat(BeatType type, int index)
        {
            if (_brain == null) return;
            BeatContext ctx = BuildContext();
            CameraRig rig = type switch
            {
                BeatType.Approach => _dolly,
                BeatType.Impact => _duel,
                BeatType.CraneReveal => _crane,
                BeatType.Finisher => _orbital,
                _ => _commander // Confirm / March / Fall / Victor / Return
            };
            rig.Prepare(ctx, Tuning);
            Activate(rig);
        }

        private void OnSkip()
        {
            if (_brain == null) return;
            _commander.Prepare(DefaultContext(), Tuning);
            Activate(_commander);
        }

        public CameraRig ActiveRig { get; private set; }
        public string ActiveRigName => ActiveRig != null ? ActiveRig.GetType().Name : "none";

        // Every rig activated this game — lets tests verify beat->rig mapping regardless of frame dt.
        private readonly HashSet<string> _activated = new HashSet<string>();
        public IReadOnlyCollection<string> ActivatedRigs => _activated;

        private void Activate(CameraRig rig)
        {
            ActiveRig = rig;
            _activated.Add(rig.GetType().Name);
            _commander.Camera.Priority = rig == _commander ? ActivePriority : IdlePriority;
            _dolly.Camera.Priority = rig == _dolly ? ActivePriority : IdlePriority;
            _duel.Camera.Priority = rig == _duel ? ActivePriority : IdlePriority;
            _crane.Camera.Priority = rig == _crane ? ActivePriority : IdlePriority;
            _orbital.Camera.Priority = rig == _orbital ? ActivePriority : IdlePriority;
        }

        private BeatContext BuildContext()
        {
            if (_player == null || !_player.HasCurrent) return DefaultContext();

            PieceViewRegistry.MoveVisual v = _player.CurrentMove.Visual;
            Vector3 boardCenter = _board != null ? _board.transform.position : Vector3.zero;

            Vector3 attacker = v.Mover != null
                ? v.Mover.transform.position
                : (_board != null ? _board.SquareToWorld(_player.CurrentMove.Move.To) : Vector3.zero);

            bool hasVictim = v.Captured != null;
            Vector3 victim = hasVictim ? v.Captured.transform.position : attacker;

            return new BeatContext(attacker, victim, boardCenter, hasVictim);
        }

        private BeatContext DefaultContext()
        {
            Vector3 c = _board != null ? _board.transform.position : Vector3.zero;
            return new BeatContext(c, c, c, false);
        }
    }
}
