using Godot;
using SakugaEngine.GameState;
using SakugaEngine.Global;
using MessagePack;

namespace SakugaEngine
{
    [GlobalClass]
    public partial class SakugaVFX : SakugaNode
    {
        private SakugaActor _master;
        [ExportCategory("Settings")]
        [Export] private int Duration;
        [Export] private AnimationPlayer Player;
        [Export] private Node3D Graphics;
        [Export] private string AnimationName;
        [Export] private SoundQueue Sound;
        /// <summary>[FF PATCH: signature color] Tint every Sprite3D under Graphics with the owner's DataContainer.SignatureColor
        /// (draw the texture in white / greys). Visual only.</summary>
        [Export] public bool TintWithOwnerColor;
        private bool tinted;
        public bool AttachedToOwner;
        public Vector2I FixedPosition;
        public int Frame;
        public int Side;
        /// <summary>[FF PATCH: vfx transform] Rotation in degrees (facing right; mirrored for the other side) and size in
        /// percent, set per spawn by SpawnVFXAction. Saved with the VFX state.</summary>
        public int RotationDegrees;
        public int ScalePercent = 100;

        SakugaVFX actor;
        private SakugaVFXState State;

        public void SetMaster(SakugaActor newMaster) => _master = newMaster;
        public SakugaActor GetMaster() => _master;

        public override void Render()
        {
            Graphics.Visible = IsActive;
            Vector2I pos = FixedPosition;
            if (AttachedToOwner) pos = _master.Body.FixedPosition + new Vector2I(FixedPosition.X, FixedPosition.Y);
            Graphics.GlobalPosition = GlobalFunctions.ToScaledVector3(pos);
            Graphics.GlobalRotation = Vector3.Zero;
            // [FF PATCH: vfx transform] mirrored rotation for the left-facing side, then the per-spawn size
            Graphics.RotationDegrees = new Vector3(0, 0, RotationDegrees * (Side < 0 ? -1 : 1));
            float size = ScalePercent / 100f;
            Graphics.Scale = new Vector3(Side * size, size, size);
            if (TintWithOwnerColor && !tinted && _master != null && _master.Data != null) ApplyTint(Graphics, _master.Data.SignatureColor);
            
            Player.Play(AnimationName);
            Player.Seek((Duration - Frame) / (float)GlobalVariables.TicksPerSecond, true);
        }

        // [FF PATCH: signature color]
        private void ApplyTint(Node node, Color color)
        {
            if (node is Sprite3D sprite) sprite.Modulate = color;
            foreach (Node child in node.GetChildren()) ApplyTint(child, color);
            tinted = true;
        }

        public override void Initialize(uint id)
        {
            base.Initialize(id);
            actor = this;
            FixedPosition = Vector2I.Zero;
            Side = 0;
            Frame = -1;
            RotationDegrees = 0;
            ScalePercent = 100;
        }

        public void Reset()
        {
            IsActive = StartActive;
            FixedPosition = Vector2I.Zero;
            Side = 0;
            Frame = -1;
            RotationDegrees = 0;
            ScalePercent = 100;
        }

        public void Spawn(Vector2I origin, int side, bool attached) => Spawn(origin, side, attached, 0, 100);

        /// <summary>[FF PATCH: vfx transform] Spawn with a rotation (degrees, facing right) and size (percent).</summary>
        public void Spawn(Vector2I origin, int side, bool attached, int rotationDegrees, int scalePercent)
        {
            RotationDegrees = rotationDegrees;
            ScalePercent = scalePercent > 0 ? scalePercent : 100;
            AttachedToOwner = attached;
            FixedPosition = origin;
            Side = side;
            Frame = Duration;
            if (AttachedToOwner)
            {
                FixedPosition -= new Vector2I(_master.Body.FixedPosition.X, _master.Body.FixedPosition.Y);
            } 
            if (Sound != null)
            {
                Sound.SimpleQueueSound();
            }
            IsActive = true;
        }

        public override void LateTick()
        {
            if (!IsActive) return;

            Frame--;
            if (Frame < 0) { IsActive = false;  return; }
        }

        public override byte[] GetStateData()
        {
            State.GetStateData(ref actor);
            return MessagePackSerializer.Serialize(State);
        }
		public override void SetStateData(byte[] stateBuffer)
        {
            State = MessagePackSerializer.Deserialize<SakugaVFXState>(stateBuffer);
            State.SetStateData(ref actor);
        }
    }
}
