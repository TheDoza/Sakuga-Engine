using Godot;
using SakugaEngine.Resources;
using SakugaEngine.Global;

namespace SakugaEngine
{
    [GlobalClass]
    public partial class StanceManager : Node
    {
        private SakugaActor _owner;
        [Export] public FrameTimer MoveBuffer;

        public bool CanRun;

        public int CurrentStance;
        public int BufferedMove = -1;
        public int CurrentMove = -1;
        public bool CancelBuffer;

        public void Initialize(SakugaActor owner)
        {
            _owner = owner;
            CurrentStance = 0;
        }

        public bool CheckMoveConditions(int index)
        {
            if (!CheckAllowedStateType(index)) return false;

            if (CurrentMove >= 0)
            {
                if (CurrentMove == index && GetMove(index).IgnoreSelf) return false;
                if (GetMove(index).PriorityBuffer && GetMove(index).Priority < GetCurrentMove().Priority) return false;
            }
            if (GetMove(index).AcceptHitReactionStates && GetMove(index).IgnoreKnockdownHits && _owner.HitstunType >= HitstunType.KNOCKDOWN) return false;

            if (_owner.Parameters.SuperGauge.CurrentValue < GetMove(index).SuperGaugeRequired) return false;

            bool isDesiredHealth = _owner.Parameters.Health.CurrentValue >= GetMove(index).HealthRequired.X &&
                _owner.Parameters.Health.CurrentValue <= GetMove(index).HealthRequired.Y;
            if (!isDesiredHealth) return false;

            int distance = GlobalFunctions.Distance(_owner.GetOpponent(0).Body.FixedPosition, _owner.Body.FixedPosition).X;
            bool isValidDistance = distance >= GetMove(index).DistanceArea.X && distance <= GetMove(index).DistanceArea.Y;
            if (!isValidDistance) return false;

            if (!IsCorrectSurface(index)) return false;

            bool isValidState = CheckStatesToIgnore(index);
            if (!isValidState) return false;

            bool isCorrectState = CheckOwnerState(index);
            if (!isCorrectState) return false;

            if (!_owner.Parameters.CompareVariables(GetMove(index).VariablesRequirement)) return false;

            // [FF PATCH: move conditions] optional per-move FrameDataConditions
            if (GetMove(index).Conditions != null)
                foreach (FrameDataCondition condition in GetMove(index).Conditions)
                    if (condition != null && !condition.Check(ref _owner)) return false;
            
            bool AllowedFrameWindow = _owner.StateManager.CurrentStateFrame < GetMove(index).FrameLimit;
            if (GetMove(index).FrameLimit > 0 && !AllowedFrameWindow) return false;
            
            return true;
        }

        public void CheckMoves()
        {
            if (MoveBuffer == null) return;
            if (!CanRun) return;
            if (FunctionFighter.Mechanics.KO.IsKO(_owner)) { BufferedMove = -1; return; } // [FF PATCH: ko] a KO'd fighter ignores its inputs

            // [FF PATCH: training reversal] The training dummy's tech / reversal action starts here, on the same frame a
            // perfectly timed input would. When it does, normal input matching is skipped for this frame.
            bool forced = FunctionFighter.Training.Reversal.Run(_owner, this) ||
                          FunctionFighter.Mechanics.Locomotion.TryHeldTech(_owner, this); // [FF PATCH: tech] hold A/B/C to tech asap

            // [FF PATCH: move priority] When several moves match on the same frame, the highest Priority wins
            // (e.g. B+C throw beats the B and C normals). Ties still go to the move lower in the list, like upstream.
            // Upstream simply took the lowest matching move in the list and ignored Priority here.
            int bestMove = -1;
            for (int i = forced ? -1 : GetMoveListLength() - 1; i >= 0; i--)
            {
                if (GetMove(i).SkipCheck) continue;
                // [FF PATCH: locomotion] walking / crouching (priority 0) can't interrupt a run, skid, step dash or backdash
                if (GetMove(i).Priority <= 0 && FunctionFighter.Mechanics.Locomotion.BlocksBasicMoves(_owner)) continue;
                // [FF PATCH: landing] landing recovery: walks / jumps / backdash locked for a few frames, everything during special landing
                if (FunctionFighter.Mechanics.Locomotion.LandingBlocks(_owner, GetMove(i))) continue;
                if (bestMove >= 0 && GetMove(i).Priority <= GetMove(bestMove).Priority) continue;
                if (!CheckMoveConditions(i)) continue;
                if (_owner.Inputs.CheckMotionInputs(GetMove(i).Inputs, _owner.InputSide(GetMove(i).SideCheck)))
                    bestMove = i;
            }
            if (bestMove >= 0) BufferMove(bestMove, false);

            //if (CurrentMove >= 0)
                //_owner.Parameters.SuperGauge.AddSuperGauge(GetCurrentMove().BuildSuperGauge);

            CheckMoveEndCondition();
            CheckMoveCancel();
            ProcessMoveBuffer();
        }

        private void BufferMove(int moveIndex, bool moveCancel)
        {
            BufferedMove = moveIndex;
            CancelBuffer = moveCancel;
            MoveBuffer.Start(GlobalVariables.MoveBufferLength);
        }

        public void ExecuteMove(int moveIndex)
        {
            _owner.Parameters.SuperGauge.CurrentValue -= GetMove(moveIndex).SuperGaugeRequired;

            if (GetMove(moveIndex).SuperFlash > 0 && !_owner.SuperFlashing)
            {
                _owner.GetOpponent(0).StartSuperFlash((uint)GetMove(moveIndex).SuperFlash);
            }

            if (GetMove(moveIndex).MoveState >= 0) 
                _owner.StateManager.PlayState(GetMove(moveIndex).MoveState, true);
            
            _owner.Parameters.ChangeVariablesBehavior(CustomVariableBehaviorTarget.ON_MOVE_ENTER);
            _owner.Parameters.SetVariables(GetMove(moveIndex).VariablesChange);

            CurrentMove = moveIndex;
            BufferedMove = -1;
            CancelBuffer = false;
            MoveBuffer.Stop();

            GD.Print("You executed " + GetCurrentMove().MoveName + "!");
        }

        private void CheckMoveCancel()
        {
            if (CurrentMove < 0) return;
            if (!CanRun) return;
            if (GetCurrentMove().CanCancelTo == null) return;
            if (GetCurrentMove().CanCancelTo.Length <= 0) return;

            foreach (MoveCancelSettings cancel in GetCurrentMove().CanCancelTo)
            {
                if (!CheckCancelConditions(cancel)) continue;
                if (!CheckMoveConditions(cancel.MoveIndex)) continue;
                if (_owner.Inputs.CheckMotionInputs(GetMove(cancel.MoveIndex).Inputs, _owner.InputSide(GetMove(cancel.MoveIndex).SideCheck)))
                {                    
                    BufferMove(cancel.MoveIndex, true);
                    break;
                }
            }
        }

        /// <summary>[FF PATCH: training reversal] Would this move come out right now if its input were pressed?
        /// Same checks as CheckMoves + ProcessMoveBuffer (conditions, state, hitstop/hitstun, LOCK_MOVE, priority).</summary>
        public bool CanExecuteNow(int index)
        {
            if (index < 0 || index >= GetMoveListLength() || GetMove(index) == null) return false;
            if (!CheckMoveConditions(index)) return false;
            var m = GetMove(index);
            if (_owner.StateManager.CurrentStateType() == StateType.COMBAT) return false;
            if (!m.Unlockable && _owner.ContainsFrameProperty(FrameProperties.LOCK_MOVE)) return false;
            if (!m.IgnoreHitstop && _owner.Hitstop.IsRunning()) return false;
            if (!m.IgnoreHitstun && _owner.Hitstun.IsRunning()) return false;
            if (CurrentMove >= 0)
            {
                var c = GetCurrentMove();
                bool ok = c.CanBeOverrided && (c.IgnoreSamePriority ? m.Priority > c.Priority : m.Priority >= c.Priority);
                if (!ok) return false;
            }
            return true;
        }

        public void ProcessMoveBuffer()
        {
            if (BufferedMove < 0) return;
            //if (CurrentMove >= 0 && !GetCurrentMove().CanBeOverrided) return;
            if (!MoveBuffer.IsRunning() || !IsCorrectSurface(BufferedMove))
            {
                BufferedMove = -1;
                GD.Print("Buffer Cleaned!");
                return;
            }
            if (!CancelBuffer && _owner.StateManager.CurrentStateType() == StateType.COMBAT) return;

            if (!GetBufferedMove().Unlockable && _owner.ContainsFrameProperty(FrameProperties.LOCK_MOVE)) return;
            if (!GetBufferedMove().IgnoreHitstop && _owner.Hitstop.IsRunning()) return;
            if (!GetBufferedMove().IgnoreHitstun && _owner.Hitstun.IsRunning()) return;
            
            bool CanOverride = CurrentMove < 0 || CancelBuffer || (GetCurrentMove().CanBeOverrided && 
                                (GetCurrentMove().IgnoreSamePriority ? 
                                GetBufferedMove().Priority > GetCurrentMove().Priority : 
                                GetBufferedMove().Priority >= GetCurrentMove().Priority));

            if (CanOverride)
            {
                ExecuteMove(BufferedMove);
            }
            else GD.Print("Move " + GetBufferedMove().MoveName + " Buffered!");
        }

        public bool IsCorrectSurface(int index)
        {
            return (GetMove(index).UseOnGround && _owner.Body.IsOnGround) ||
                (GetMove(index).UseOnAir && !_owner.Body.IsOnGround && _owner.Body.FixedPosition.Y >= GetMove(index).MinimumHeight) ||
                (GetMove(index).UseOnGround && GetMove(index).UseOnAir);
        }

        public bool CheckAllowedStateType(int moveIndex)
        {
            bool notLockedState =  _owner.StateManager.GetCurrentState().Type != StateType.LOCKED;
            bool checkNullState = GetMove(moveIndex).AcceptNullStates && _owner.StateManager.GetCurrentState().Type == StateType.NULL;
            bool checkMovementState = GetMove(moveIndex).AcceptMovementStates && _owner.StateManager.GetCurrentState().Type == StateType.MOVEMENT;
            bool checkCombatState = GetMove(moveIndex).AcceptCombatStates && _owner.StateManager.GetCurrentState().Type == StateType.COMBAT;
            bool checkBlockingState = GetMove(moveIndex).AcceptBlockingStates && _owner.StateManager.GetCurrentState().Type == StateType.BLOCKING;
            // [FF PATCH: burst] AcceptHardKnockdownHits lets a move (Burst) come out of hard knockdown hits too (wall bounce...)
            bool checkHitReactionState = GetMove(moveIndex).AcceptHitReactionStates && _owner.StateManager.GetCurrentState().Type == StateType.HIT_REACTION &&
                (_owner.HitstunType < HitstunType.HARD_KNOCKDOWN || GetMove(moveIndex).AcceptHardKnockdownHits && _owner.HitstunType == HitstunType.HARD_KNOCKDOWN);

            return notLockedState && (checkNullState || checkMovementState || checkCombatState || checkBlockingState || checkHitReactionState);
        }

        private void CheckMoveEndCondition()
        {
            if (CurrentMove < 0) return;

            switch (GetCurrentMove().MoveEnd)
            {
                case MoveEndCondition.STATE_END:
                    if (_owner.StateManager.CurrentState != GetCurrentMove().MoveState)
                        EndMove();
                    break;
                case MoveEndCondition.RELEASE_BUTTON:
                    if (_owner.Inputs.CheckInputEnd(GetCurrentMove().Inputs, _owner.InputSide(GetCurrentMove().SideCheck)))
                        EndMove();
                    break;
                case MoveEndCondition.STATE_TYPE_CHANGE:
                    if (_owner.StateManager.CurrentStateType() != _owner.Data.States[GetCurrentMove().MoveState].Type)
                        EndMove();
                    break;
                case MoveEndCondition.ON_FALL:
                    if (_owner.Body.IsFalling)
                        EndMove();
                    break;
            }
        }

        public void EndMove()
        {
            if (GetCurrentMove().MoveEndState >= 0)
                _owner.StateManager.PlayState(GetCurrentMove().MoveEndState, false);

            CurrentMove = -1;
            _owner.Parameters.ChangeVariablesBehavior(CustomVariableBehaviorTarget.ON_MOVE_EXIT);
        }

        public void CallStance(int NextStance)
        {
            CurrentStance = NextStance;
            CurrentMove = -1;
            BufferedMove = -1;
            CancelBuffer = false;
            MoveBuffer.Stop();
        }

        public void Clear()
        {
            CurrentMove = -1;
            BufferedMove = -1;
            CancelBuffer = false;
            if (!GetCurrentStance().IsDamagePersistent) CurrentStance = 0;
            MoveBuffer.Stop();
        }

        public bool CheckOwnerState(int index)
        {
            if (GetMove(index).IsSequenceFromStates == null) return true;
            if (GetMove(index).IsSequenceFromStates.Length <= 0) return true;
            
            foreach (int possibleStates in GetMove(index).IsSequenceFromStates)
            {
                if (_owner.StateManager.CurrentState == possibleStates)
                    return true;
            }
            return false;
        }

        public bool CheckStatesToIgnore(int index)
        {
            if (GetMove(index).IgnoreStates == null) return true;
            if (GetMove(index).IgnoreStates.Length <= 0) return true;
            
            foreach (int possibleStates in GetMove(index).IgnoreStates)
            {
                if (_owner.StateManager.CurrentState == possibleStates)
                    return false;
            }
            return true;
        }

        private bool CheckCancelConditions(MoveCancelSettings cancelSettings)
        {
            bool isKaraCancel = ((_owner.CancelConditions & cancelSettings.Conditions) & CancelCondition.KARA_CANCEL) > 0;
            if ((_owner.CancelConditions & cancelSettings.Conditions) > 0 && ((isKaraCancel && _owner.StateManager.CurrentStateFrame < GlobalVariables.KaraCancelWindow) ||
            (!isKaraCancel && _owner.StateManager.CurrentStateFrame >= cancelSettings.FrameThreshold.X && _owner.StateManager.CurrentStateFrame <= cancelSettings.FrameThreshold.Y)))
            {
                return true;
            }
            return false;
        }

        public int GetBlockState(HitType Type, byte state)
        {
            if (_owner.StateManager.CurrentStateType() == StateType.HIT_REACTION) return -1;
            if (_owner.StateManager.CurrentStateType() == StateType.LOCKED) return -1;
            if (_owner.ContainsFrameProperty(FrameProperties.NO_GUARD)) return -1; // [FF PATCH: locomotion] prejump, run skid
            if (_owner.StateManager.CurrentStateType() == StateType.COMBAT && _owner.StateManager.CurrentStateFrame >= GlobalVariables.KaraCancelWindow) return -1;
            if (GetCurrentStance().BlockReactions == null || GetCurrentStance().BlockReactions.Length == 0) return -1;
            for (int i = 0; i < GetCurrentStance().BlockReactions.Length; i++)
            {
                BlockSettings block = GetCurrentStance().BlockReactions[i];
                if (_owner.Body.IsOnGround && block.ReferenceStance == StanceSelect.AIR) continue;
                if (!_owner.Body.IsOnGround && block.ReferenceStance != StanceSelect.AIR) continue;
                if (!ValidHitType(Type, block.BlockType)) continue;
                if (block.InputToCheck == null) continue;
                // [FF PATCH: barrier, training guard] A barrier that's up guards without holding back, and the training dummy
                // can be set to guard on its own; both use the block that matches the fighter's stance (or the hit, for the dummy).
                StanceSelect autoGuard = FunctionFighter.Mechanics.GuardOverride.GetGuardStance(_owner, Type);
                bool autoGuarding = (int)autoGuard >= 0;
                if (autoGuarding ? block.ReferenceStance != autoGuard
                                 : !_owner.Inputs.CheckMotionInputs(block.InputToCheck, _owner.InputSide(InputSideCheck.SIDE_RELATIVE))) continue;

                switch (state)
                {
                    case 0:
                        return block.EnterState;
                    case 1:
                        return block.StunState;
                    case 2:
                        return block.GuardCrushState;
                }
            }
            return -1;
        }

        public int ExitBlockState(StanceSelect stance)
        {
            if (GetCurrentStance().BlockReactions == null || GetCurrentStance().BlockReactions.Length == 0) return -1;
            for (int i = 0; i < GetCurrentStance().BlockReactions.Length; i++)
            {
                BlockSettings block = GetCurrentStance().BlockReactions[i];
                if (!IsValidStance(block.ReferenceStance, stance)) continue;

                return block.ExitState;
            }
            return -1;
        }

        private bool IsValidStance(StanceSelect stance, StanceSelect compare)
        {
            return stance == compare;
        }

        private bool ValidHitType(HitType hitType, BlockType blockType)
        {
            if (hitType == HitType.UNBLOCKABLE) return false;
            if (hitType == HitType.HIGH && (blockType & BlockType.HIGH) > 0) return true;
            if (hitType == HitType.MEDIUM && (blockType & BlockType.MEDIUM) > 0) return true;
            if (hitType == HitType.LOW && (blockType & BlockType.LOW) > 0) return true;

            return false;
        }

        public FighterStance GetStance(int index) => _owner.Data.Stances[index];
        public FighterStance GetCurrentStance() => GetStance(CurrentStance);
        public MoveSettings GetMove(int index) => GetCurrentStance().Moves[index];
        public MoveSettings GetCurrentMove() => GetMove(CurrentMove);
        public MoveSettings GetBufferedMove() => GetMove(BufferedMove);
        public int GetMoveListLength() => GetCurrentStance().Moves.Length;
    }
}