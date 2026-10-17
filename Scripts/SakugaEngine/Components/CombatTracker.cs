using Godot;

namespace SakugaEngine
{
    [GlobalClass]
    public partial class CombatTracker : Node
    {
        private SakugaActor _owner;
        public int HitCombo;
        public int LastDamage;
        public int CurrentCombo;
        public int HighestCombo;
        public int TotalFrames;
        public int FrameData;
        public int FrameAdvantage;
        public int HitFrame;
        public int StunAtHit;
        public int LastHitType;
        public bool invalidHit;
        /// <summary>[FF PATCH: combo display] The hit number at which the combo became invalid (0 = still valid).</summary>
        public int InvalidAtHit;

        public void Initialize(SakugaActor onwer)
        {
            _owner = onwer;
            HitCombo = 0;
            LastDamage = 0;
            CurrentCombo = 0;
            HighestCombo = 0;
            TotalFrames = 0;
            FrameData = 0;
            FrameAdvantage = 0;
            HitFrame = 0;
            StunAtHit = 0;
            LastHitType = 0;
            invalidHit = false;
            InvalidAtHit = 0;
        }
        public void UpdateTrackers(int damage, int hitFrame, int hitStun, int hitType, bool isInvalid = false, bool isCombo = true)
        {
            if (HitCombo == 0) CurrentCombo = 0;
            if (isCombo) HitCombo++;
            if (isInvalid && !invalidHit) InvalidAtHit = HitCombo; // [FF PATCH: combo display]
            if (isInvalid)
                invalidHit = true;
            LastDamage = damage;
            CurrentCombo += damage;
            if (CurrentCombo > HighestCombo)
                HighestCombo = CurrentCombo;

            HitFrame = hitFrame;
            StunAtHit = hitStun;
            LastHitType = hitType;
            FrameAdvantage = HitFrame - StunAtHit;
        }

        public void UpdateFrameData()
        {
            bool corectStateType = _owner.StateManager.CurrentStateType() == Global.StateType.HIT_REACTION || _owner.StateManager.CurrentStateType() == Global.StateType.BLOCKING;
            int selectFrameOrigin = corectStateType ? (int)_owner.Hitstun.TimeLeft :
                                    (_owner.StateManager.GetCurrentState().AnimationData.Duration - _owner.StateManager.CurrentStateFrame);

            FrameData = _owner.StateManager.CurrentStateType() <= Global.StateType.MOVEMENT ? 0 : selectFrameOrigin;
        }

        public void Reset()
        {
            HitCombo = 0;
            //LastDamage = 0;
            //CurrentCombo = 0;
            invalidHit = false;
            InvalidAtHit = 0;
        }
    }
}
