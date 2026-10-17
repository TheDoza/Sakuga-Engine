using System;
using System.Collections.Generic;
using Godot;
using SakugaEngine.Game;
using SakugaEngine.UI;

namespace FunctionFighter.Training
{
    /// <summary>
    /// Training Settings page. Lives in Sakuga_UI.tscn as "TrainingMenu" (next to PauseMenu). In Training it adds a
    /// "Training" button to the pause menu that opens this page. Up/Down picks a row, Left/Right changes it,
    /// Accept runs an action row, Back returns to the pause menu. Changes are saved right away.
    ///
    /// The pause menu also gets a Quick Character Select row above its buttons (QuickCharacterSelect).
    ///
    /// Hotkeys while playing Training: F8 record the dummy, F9 play the recording, F11 reset positions,
    /// F5 save state, F6 load state, F2 frame advance, F3 step a frame
    /// (keyboard 1 defaults: Save State / Load State / Frame Advance / Step Frame in the button config).
    /// Back button + direction resets to a chosen spot (5 mid, 4 left corner P1 inside, 1 left corner P2 inside,
    /// 2 mid sides switched, 3 right corner P1 inside, 6 right corner P2 inside).
    /// </summary>
    public partial class TrainingMenu : Control
    {
        [Export] public int FontSize = 16;
        [Export] public Color HighlightColor = new Color(0.9f, 0.58f, 0f);
        [Export] public Color HeaderColor = new Color(0.55f, 0.8f, 1f);

        class Row
        {
            public string Name;
            public Func<string> Value;      // null = header or action
            public Action<int> Change;      // left/right
            public Action Activate;         // accept
            public bool Header;
            public bool PageTitle;
        }

        readonly List<Row> _rows = new List<Row>();
        readonly List<Label> _nameLabels = new List<Label>();
        readonly List<Label> _valueLabels = new List<Label>();
        Label _status;
        int _selected;
        bool _open;
        bool _applied;
        PauseMenu _pause;
        Button _pauseButton;

        bool IsTraining => TrainingController.Active;

        public override void _Ready()
        {
            Visible = false;
            MouseFilter = MouseFilterEnum.Ignore;
            FunctionFighter.UI.PauseOverlay.Blocking = false;
            TrainingSettings.Load();
            BuildRows();
            BuildUI();
        }

        // ------------------------------------------------------------ rows

        static string OnOff(bool b) => b ? "On" : "Off";
        static T Cycle<T>(T v, int d) where T : Enum
        {
            var values = (T[])Enum.GetValues(typeof(T));
            int i = Array.IndexOf(values, v);
            return values[((i + d) % values.Length + values.Length) % values.Length];
        }
        static int CycleHealth(int v, int d)
        {
            int i = Array.IndexOf(TrainingSettings.HealthSteps, v);
            if (i < 0) i = 0;
            int n = TrainingSettings.HealthSteps.Length;
            return TrainingSettings.HealthSteps[((i + d) % n + n) % n];
        }

        static string MeterName(MeterMode m) => m == MeterMode.RefillAfterCombo ? "Refill After Combo" : m.ToString();
        static string SlotName(int slot)
        {
            int frames = TrainingController.SlotLength(slot);
            return (slot + 1) + (frames > 0 ? $"  ({frames / 60f:0.0} s)" : "  (Empty)");
        }
        static string DummyName(DummyMode m) => m == DummyMode.CPU ? "CPU" : DummyMovement.IsMovementMode(m) ? DummyMovement.Name(m) : m.ToString();
        static string GuardName(GuardMode m)
        {
            switch (m)
            {
                case GuardMode.AfterFirstHit: return "After First Hit";
                case GuardMode.FirstHitOnly: return "First Hit Only";
                case GuardMode.BlockXHits: return "Block " + TrainingSettings.GuardHits + (TrainingSettings.GuardHits == 1 ? " Hit" : " Hits");
                case GuardMode.SwitchStandToCrouch: return "Switch: Stand > Crouch";
                case GuardMode.SwitchCrouchToStand: return "Switch: Crouch > Stand";
                default: return m.ToString();
            }
        }
        static string ReversalName(ReversalMode m) => m == ReversalMode.Action ? "Action" : m == ReversalMode.Recording ? "Play Recording" : "Off";
        static string AirTechName(AirTechMode m) => m.ToString();
        static string GroundTechName(GroundTechMode m) =>
            m == GroundTechMode.ForwardRoll ? "Forward Roll" : m == GroundTechMode.BackRoll ? "Back Roll" : m == GroundTechMode.Emergency ? "Emergency Tech" : m.ToString();
        static string PlayOnResetName(PlayOnResetMode m) => m == PlayOnResetMode.Immediately ? "Immediately" : m == PlayOnResetMode.AfterRoundCall ? "After READY / GO!" : "Off";
        static string DelayName(int d) => d == 0 ? "Frame 1" : "+" + d + (d == 1 ? " frame" : " frames");
        static SakugaEngine.SakugaActor DummyActor()
        {
            var f = GameManager.Instance?.GetFighters();
            return f != null && f.Length > TrainingController.DummyIndex ? f[TrainingController.DummyIndex] : null;
        }
        static string ActionName()
        {
            var a = Reversal.SelectedAction(DummyActor());
            return a == null ? "(none)" : a.Label;
        }
        static string CounterName(CounterMode m) =>
            m == CounterMode.Counter ? "Always Counter" : m == CounterMode.BigCounter ? "Always Big Counter" : m == CounterMode.Off ? "Never" : "Normal";
        static string PositionName(ResetPosition p)
        {
            switch (p)
            {
                case ResetPosition.LeftCorner: return "Dummy Left Corner";
                case ResetPosition.RightCorner: return "Dummy Right Corner";
                case ResetPosition.P1LeftCorner: return "P1 Left Corner";
                case ResetPosition.P1RightCorner: return "P1 Right Corner";
                case ResetPosition.CenterSwapped: return "Center, Sides Switched";
                default: return "Center";
            }
        }
        static string FcName(FunctionalCounterMode m) =>
            m == FunctionalCounterMode.Default ? "Default (" + OnOff(Mechanics.CounterSettings.Get().FunctionalCounterEnabled) + ")" : m.ToString();

        void BuildRows()
        {
            _rows.Clear();
            Header("DUMMY SETTINGS");
            Option("Dummy", () => DummyName(TrainingSettings.Dummy), d => TrainingSettings.Dummy = Cycle(TrainingSettings.Dummy, d));
            Option("Counter Hit", () => CounterName(TrainingSettings.Counter), d => TrainingSettings.Counter = Cycle(TrainingSettings.Counter, d));
            Option("Functional Counter", () => FcName(TrainingSettings.FunctionalCounter), d => TrainingSettings.FunctionalCounter = Cycle(TrainingSettings.FunctionalCounter, d));
            Option("Dummy Barrier", () => TrainingSettings.DummyBarrier.ToString(), d => TrainingSettings.DummyBarrier = Cycle(TrainingSettings.DummyBarrier, d));

            Header("GUARD & TECH");
            Option("Guard", () => GuardName(TrainingSettings.Guard), d => TrainingSettings.Guard = Cycle(TrainingSettings.Guard, d));
            Option("Guard Hits  (Block X Hits)", () => TrainingSettings.GuardHits.ToString(),
                d => TrainingSettings.GuardHits = ((TrainingSettings.GuardHits - 1 + d) % TrainingSettings.MaxGuardHits + TrainingSettings.MaxGuardHits) % TrainingSettings.MaxGuardHits + 1);
            Option("Air Tech", () => AirTechName(TrainingSettings.AirTech), d => TrainingSettings.AirTech = Cycle(TrainingSettings.AirTech, d));
            Option("Ground Tech", () => GroundTechName(TrainingSettings.GroundTech), d => TrainingSettings.GroundTech = Cycle(TrainingSettings.GroundTech, d));
            Option("Tech Delay", () => DelayName(TrainingSettings.TechDelay),
                d => TrainingSettings.TechDelay = ((TrainingSettings.TechDelay + d) % (TrainingSettings.MaxTechDelay + 1) + TrainingSettings.MaxTechDelay + 1) % (TrainingSettings.MaxTechDelay + 1));

            Header("REVERSAL OPTIONS");
            Option("Reversal", () => ReversalName(TrainingSettings.Reversal), d => TrainingSettings.Reversal = Cycle(TrainingSettings.Reversal, d));
            Option("Action", ActionName, d => Reversal.CycleAction(DummyActor(), d));
            Option("On Wakeup", () => OnOff(TrainingSettings.ReversalOnWakeup), d => TrainingSettings.ReversalOnWakeup = !TrainingSettings.ReversalOnWakeup);
            Option("On Block", () => OnOff(TrainingSettings.ReversalOnBlock), d => TrainingSettings.ReversalOnBlock = !TrainingSettings.ReversalOnBlock);
            Option("On Hit", () => OnOff(TrainingSettings.ReversalOnHit), d => TrainingSettings.ReversalOnHit = !TrainingSettings.ReversalOnHit);
            Option("On Air Tech", () => OnOff(TrainingSettings.ReversalOnAirTech), d => TrainingSettings.ReversalOnAirTech = !TrainingSettings.ReversalOnAirTech);
            Option("Delay", () => DelayName(TrainingSettings.ReversalDelay),
                d => TrainingSettings.ReversalDelay = ((TrainingSettings.ReversalDelay + d) % (TrainingSettings.MaxReversalDelay + 1) + TrainingSettings.MaxReversalDelay + 1) % (TrainingSettings.MaxReversalDelay + 1));

            Header("HEALTH & METERS");
            Option("Health Refill", () => OnOff(TrainingSettings.HealthRefill), d => TrainingSettings.HealthRefill = !TrainingSettings.HealthRefill);
            Option("P1 Health", () => TrainingSettings.P1HealthPercent + "%", d => TrainingSettings.P1HealthPercent = CycleHealth(TrainingSettings.P1HealthPercent, d));
            Option("Dummy Health", () => TrainingSettings.DummyHealthPercent + "%", d => TrainingSettings.DummyHealthPercent = CycleHealth(TrainingSettings.DummyHealthPercent, d));
            Option("Super Meter", () => MeterName(TrainingSettings.SuperMode), d => TrainingSettings.SuperMode = Cycle(TrainingSettings.SuperMode, d));
            Option("Super Refill Amount", () => TrainingSettings.SuperRefillPercent + "%",
                d => TrainingSettings.SuperRefillPercent = ((TrainingSettings.SuperRefillPercent + d * TrainingSettings.SuperRefillStep) % 110 + 110) % 110);
            Option("Barrier", () => MeterName(TrainingSettings.BarrierMode), d => TrainingSettings.BarrierMode = Cycle(TrainingSettings.BarrierMode, d));
            Option("Burst", () => MeterName(TrainingSettings.BurstMode), d => TrainingSettings.BurstMode = Cycle(TrainingSettings.BurstMode, d));

            Header("DISPLAY");
            Option("Input History", () => OnOff(TrainingSettings.ShowInputHistory), d => { TrainingSettings.ShowInputHistory = !TrainingSettings.ShowInputHistory; ApplyDisplay(); });
            Option("Frame Meter", () => OnOff(TrainingSettings.ShowFrameMeter), d => TrainingSettings.ShowFrameMeter = !TrainingSettings.ShowFrameMeter);
            Option("P1 Input Display", () => OnOff(TrainingSettings.ShowP1Inputs), d => TrainingSettings.ShowP1Inputs = !TrainingSettings.ShowP1Inputs);
            Option("P2 Input Display", () => OnOff(TrainingSettings.ShowP2Inputs), d => TrainingSettings.ShowP2Inputs = !TrainingSettings.ShowP2Inputs);
            Option("Training Data", () => OnOff(TrainingSettings.ShowTrainingInfo), d => { TrainingSettings.ShowTrainingInfo = !TrainingSettings.ShowTrainingInfo; ApplyDisplay(); });
            Option("Advantage Hitsparks", () => OnOff(TrainingSettings.ShowAdvantage), d => TrainingSettings.ShowAdvantage = !TrainingSettings.ShowAdvantage);
            Option("State Flashes", () => OnOff(TrainingSettings.ShowStateFlash), d => TrainingSettings.ShowStateFlash = !TrainingSettings.ShowStateFlash);
            Option("Punish Indicator", () => OnOff(TrainingSettings.ShowPunish), d => TrainingSettings.ShowPunish = !TrainingSettings.ShowPunish);
            Option("Reversal Indicator", () => OnOff(TrainingSettings.ShowReversal), d => TrainingSettings.ShowReversal = !TrainingSettings.ShowReversal);
            Option("Hitboxes", () => OnOff(GameManager.Instance != null && GameManager.Instance.ShowHitboxes),
                d => { if (GameManager.Instance != null) GameManager.Instance.ShowHitboxes = !GameManager.Instance.ShowHitboxes; });

            Header("RECORDING");
            Option("Slot", () => SlotName(TrainingController.RecordSlot),
                d => TrainingSettings.RecordSlot = ((TrainingSettings.RecordSlot + d) % TrainingController.Slots + TrainingController.Slots) % TrainingController.Slots);
            Option("Playback", () => TrainingSettings.PlaybackRandom ? "Random Slot" : "This Slot", d => TrainingSettings.PlaybackRandom = !TrainingSettings.PlaybackRandom);
            AddAction("Record Dummy  (F8)", () => { TrainingController.ToggleRecording(); Resume(); },
                () => TrainingController.Recording == RecordState.Recording || TrainingController.Recording == RecordState.Countdown ? "Recording..." : (TrainingController.RecordedFrames > 0 ? TrainingController.RecordedFrames + " frames" : "Empty"));
            AddAction("Play Recording  (F9)", () => { TrainingController.TogglePlayback(); Resume(); }, null);
            AddAction("Clear Slot", () => TrainingController.ClearRecording(), null);
            AddAction("Clear All Slots", () => TrainingController.ClearAllRecordings(), null);
            Option("Countdown  (3-2-1)", () => OnOff(TrainingSettings.RecordCountdown), d => TrainingSettings.RecordCountdown = !TrainingSettings.RecordCountdown);
            Option("Play on Reset", () => PlayOnResetName(TrainingSettings.PlayOnReset), d => TrainingSettings.PlayOnReset = Cycle(TrainingSettings.PlayOnReset, d));

            Header("RESET");
            Option("Reset Position", () => PositionName(TrainingSettings.Position), d => TrainingSettings.Position = Cycle(TrainingSettings.Position, d));
            AddAction("Reset Now  (F11 / Back)", () => { ResetNow(); Resume(); }, null);
            AddAction("Save State", () => { SaveStates.Save(); Resume(); }, () => SaveStates.HasState ? "Saved" : "Empty");
            AddAction("Load State", () => { if (SaveStates.Load()) Resume(); }, null);
            AddAction("Frame Advance", () => { FrameAdvance.Toggle(); Resume(); }, () => FrameAdvance.Frozen ? "Frozen" : "Off");
            AddAction("Restore Default Settings", () => { TrainingSettings.Defaults(); ApplyDisplay(); }, null);
            AddAction("Back", Close, null);
        }

        void Header(string name) => _rows.Add(new Row { Name = name, Header = true });

        // ------------------------------------------------------------ pages
        // Each header starts a page. The page name sits at the top (row 0): highlight it and press Left/Right, or use
        // the shoulder buttons / PageUp-PageDown from anywhere.

        readonly List<List<Row>> _pages = new List<List<Row>>();
        readonly List<string> _pageNames = new List<string>();
        int _page;

        void BuildPages()
        {
            _pages.Clear(); _pageNames.Clear();
            foreach (var r in _rows)
            {
                if (r.Header) { _pageNames.Add(r.Name); _pages.Add(new List<Row>()); continue; }
                if (_pages.Count == 0) { _pageNames.Add("TRAINING"); _pages.Add(new List<Row>()); }
                _pages[^1].Add(r);
            }
            ShowPage(_page);
        }

        void ShowPage(int page)
        {
            _page = ((page % _pages.Count) + _pages.Count) % _pages.Count;
            _rows.Clear();
            _rows.Add(new Row
            {
                Name = _pageNames[_page],
                PageTitle = true,
                Value = () => (_page + 1) + " / " + _pages.Count,
                Change = d => { ShowPage(_page + d); _selected = 0; },
            });
            _rows.AddRange(_pages[_page]);
            _top = 0;
        }
        void Option(string name, Func<string> value, Action<int> change) => _rows.Add(new Row { Name = name, Value = value, Change = change });
        void AddAction(string name, System.Action activate, Func<string> value) => _rows.Add(new Row { Name = name, Activate = activate, Value = value });

        // ------------------------------------------------------------ UI

        /// <summary>Rows shown at once; the list scrolls to keep the selected row in view.</summary>
        [Export] public int VisibleRows = 26;
        int _top;
        Label _moreUp, _moreDown;

        void BuildUI()
        {
            SetAnchorsPreset(LayoutPreset.FullRect);
            const int width = 500, rowH = 21, pad = 14;
            BuildPages();
            int longest = 0;
            foreach (var pg in _pages) longest = Math.Max(longest, pg.Count + 1);
            int shown = Math.Min(VisibleRows, longest);
            int height = shown * rowH + pad * 2 + 70;

            var panel = new ColorRect { Color = new Color(0.05f, 0.06f, 0.09f, 0.92f), MouseFilter = MouseFilterEnum.Ignore };
            panel.SetAnchorsPreset(LayoutPreset.Center);
            panel.Position = new Vector2(-width / 2, -height / 2);
            panel.Size = new Vector2(width, height);
            panel.AnchorLeft = panel.AnchorRight = 0.5f;
            panel.AnchorTop = panel.AnchorBottom = 0.5f;
            panel.OffsetLeft = -width / 2; panel.OffsetRight = width / 2;
            panel.OffsetTop = -height / 2; panel.OffsetBottom = height / 2;
            AddChild(panel);

            var title = MakeLabel("TRAINING SETTINGS", 24, HorizontalAlignment.Center);
            title.Position = new Vector2(0, 8);
            title.Size = new Vector2(width, 32);
            panel.AddChild(title);

            for (int i = 0; i < shown; i++)
            {
                float y = 46 + i * rowH;
                var n = MakeLabel("", FontSize, HorizontalAlignment.Left);
                n.Position = new Vector2(pad, y);
                n.Size = new Vector2(width * 0.55f, rowH);
                panel.AddChild(n);
                _nameLabels.Add(n);

                var v = MakeLabel("", FontSize, HorizontalAlignment.Right);
                v.Position = new Vector2(width * 0.45f, y);
                v.Size = new Vector2(width * 0.55f - pad, rowH);
                panel.AddChild(v);
                _valueLabels.Add(v);
            }

            _moreUp = MakeLabel("more above", 11, HorizontalAlignment.Right);
            _moreUp.Position = new Vector2(0, 30); _moreUp.Size = new Vector2(width - pad, 14);
            _moreUp.Modulate = new Color(1, 1, 1, 0.55f);
            panel.AddChild(_moreUp);
            _moreDown = MakeLabel("more below", 11, HorizontalAlignment.Right);
            _moreDown.Position = new Vector2(0, 46 + shown * rowH); _moreDown.Size = new Vector2(width - pad, 14);
            _moreDown.Modulate = new Color(1, 1, 1, 0.55f);
            panel.AddChild(_moreDown);

            _status = MakeLabel("Up/Down: choose   Left/Right: change   LB/RB, PgUp/PgDn: page", 13, HorizontalAlignment.Center);
            _status.Position = new Vector2(0, height - 28);
            _status.Size = new Vector2(width, 20);
            _status.Modulate = new Color(1, 1, 1, 0.7f);
            panel.AddChild(_status);

            _selected = 0;
            Refresh();
        }

        Label MakeLabel(string text, int size, HorizontalAlignment align)
        {
            var l = new Label { Text = text, HorizontalAlignment = align, VerticalAlignment = VerticalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
            l.AddThemeFontSizeOverride("font_size", size);
            return l;
        }

        void Refresh()
        {
            int shown = _nameLabels.Count;
            // Keep the selected row (and the header just above it) in view.
            if (_selected < _top + 1) _top = Math.Max(0, _selected - 1);
            if (_selected >= _top + shown) _top = _selected - shown + 1;
            _top = Mathf.Clamp(_top, 0, Math.Max(0, _rows.Count - shown));
            for (int k = 0; k < shown; k++)
            {
                int i = _top + k;
                if (i >= _rows.Count) { _nameLabels[k].Text = ""; _valueLabels[k].Text = ""; continue; }
                var r = _rows[i];
                bool sel = i == _selected;
                var n = _nameLabels[k];
                n.Text = r.Name;
                n.Position = new Vector2(14 + (r.Header || r.PageTitle ? 0 : 12), n.Position.Y);
                n.AddThemeFontSizeOverride("font_size", r.Header ? FontSize - 2 : r.PageTitle ? FontSize + 2 : FontSize);
                if (r.PageTitle) n.Text = (sel ? "<  " : "") + r.Name + (sel ? "  >" : "");
                string value = r.Value != null ? r.Value() : "";
                if (r.Change != null && !r.PageTitle) value = sel ? "<  " + value + "  >" : value;
                _valueLabels[k].Text = value;
                var c = r.Header ? HeaderColor : sel ? HighlightColor : r.PageTitle ? HeaderColor : Colors.White;
                n.AddThemeColorOverride("font_color", c);
                _valueLabels[k].AddThemeColorOverride("font_color", c);
            }
            if (_moreUp != null) _moreUp.Visible = _top > 0;
            if (_moreDown != null) _moreDown.Visible = _top + shown < _rows.Count;
        }

        int NextSelectable(int from, int dir)
        {
            int i = from;
            for (int n = 0; n < _rows.Count; n++)
            {
                i = ((i + dir) % _rows.Count + _rows.Count) % _rows.Count;
                if (!_rows[i].Header) return i;
            }
            return 0;
        }

        // ------------------------------------------------------------ open / close

        int _stepHeld;

        public override void _Process(double delta)
        {
            FrameAdvance.RenderTick();
            if (!IsTraining) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.GetFighters() == null) return;

            if (!_applied)
            {
                _applied = true;
                TrainingController.OnMatchStart();
                ApplyDisplay();
                AddPauseButton();
                // Overlays sit BEHIND the pause menu and this page (moved just before PauseMenu in the UI tree).
                var ui = GetParent();
                foreach (Control hud in new Control[] { TrainingStatusHUD.Attach(ui), FrameMeterHUD.Attach(ui), InputDisplayHUD.Attach(ui) })
                    SendBehindPause(ui, hud);
            }

            if (_open && !gm.IsPaused) { _open = false; Visible = false; FunctionFighter.UI.PauseOverlay.Blocking = false; }
            if (!gm.IsPaused) CheckBackButtonReset(gm);

            // Holding Step Frame plays slowly: after RepeatDelay rendered frames, one step every RepeatEvery.
            if (!gm.IsPaused && FrameAdvance.Frozen && AnyHeld("frame_step"))
            {
                _stepHeld++;
                if (_stepHeld > FrameAdvance.RepeatDelay && (_stepHeld - FrameAdvance.RepeatDelay) % FrameAdvance.RepeatEvery == 0) FrameAdvance.Step();
            }
            else _stepHeld = 0;
        }

        /// <summary>
        /// Back button (each player's "options" action: Back/Select on a gamepad, Tab for keyboard 1) resets positions.
        /// The direction held picks where: see TrainingController.PositionForDirection.
        /// </summary>
        void CheckBackButtonReset(GameManager gm)
        {
            foreach (int device in new[] { gm.Match.P1SelectedDevice, gm.Match.P2SelectedDevice })
            {
                if (device < 0) continue;
                string p = SakugaEngine.Global.GlobalFunctions.GetPlayerPrefix(device);
                if (!InputMap.HasAction(p + "_options") || !Godot.Input.IsActionJustPressed(p + "_options")) continue;
                // The same key on a fighting button (old keyboard-2 default: Keypad . was both Back and H) is a button press, not a reset.
                bool clash = false;
                foreach (var b in new[] { "a", "b", "c", "d", "e", "f", "g", "h" })
                    if (InputMap.HasAction(p + "_face_" + b) && Godot.Input.IsActionJustPressed(p + "_face_" + b)) clash = true;
                if (clash) continue;
                bool left = Godot.Input.IsActionPressed(p + "_left"), right = Godot.Input.IsActionPressed(p + "_right"), down = Godot.Input.IsActionPressed(p + "_down");
                if (left && right) left = right = false;
                TrainingController.ResetPositions(gm.GetFighters(), TrainingController.PositionForDirection(left, right, down));
                return;
            }
        }

        /// <summary>Moves an overlay just before the pause menu, so the pause menu and settings pages draw on top of it.</summary>
        public static void SendBehindPause(Node ui, Node overlay)
        {
            if (ui == null || overlay == null) return;
            var pause = ui.GetNodeOrNull<Node>("PauseMenu");
            if (pause == null) return;
            ui.MoveChild(overlay, pause.GetIndex());
        }

        void AddPauseButton()
        {
            _pause = GetParent().GetNodeOrNull<PauseMenu>("PauseMenu");
            if (_pause == null) return;
            _pauseButton = FunctionFighter.UI.PauseOverlay.AddButton(_pause, "Training", 1);
            if (_pauseButton != null) _pauseButton.Pressed += Open;
            // Quick Character Select row above the buttons (not during online matches).
            if (GameManager.Instance != null && !GameManager.Instance.Match.SelectedModeSettings.UseLAN)
                QuickSelect = QuickCharacterSelect.Attach(_pause);
            ResetGuidePanel.Attach(_pause);
        }

        /// <summary>The Quick Character Select row in the pause menu (null outside Training).</summary>
        public QuickCharacterSelect QuickSelect { get; private set; }

        void Open()
        {
            if (_pause == null || !_pause.Visible) return;
            _open = true;
            FunctionFighter.UI.PauseOverlay.Blocking = true;
            Visible = true;
            _pause.Visible = false;
            GetViewport().GuiReleaseFocus();
            BuildRowsKeepSelection();
        }

        void BuildRowsKeepSelection()
        {
            if (_selected >= _rows.Count || _rows[_selected].Header) _selected = 0;
            Refresh();
        }

        void Close()
        {
            _open = false;
            FunctionFighter.UI.PauseOverlay.Blocking = false;
            Visible = false;
            TrainingSettings.Save();
            if (_pause != null && GameManager.Instance != null && GameManager.Instance.IsPaused)
            {
                _pause.CallPauseMenu(true);
                _pauseButton?.GrabFocus();
            }
        }

        void Resume()
        {
            _open = false;
            FunctionFighter.UI.PauseOverlay.Blocking = false;
            Visible = false;
            TrainingSettings.Save();
            GameManager.Instance?.PauseControl(false);
        }

        void ResetNow()
        {
            var gm = GameManager.Instance;
            if (gm != null) TrainingController.ResetPositions(gm.GetFighters());
        }

        void ApplyDisplay()
        {
            var ui = GetParent();
            var history = ui.GetNodeOrNull<Control>("GameHUD_Foreground/InputHistory");
            var info = ui.GetNodeOrNull<Control>("GameHUD_Foreground/TrainingInfo");
            if (history != null) history.Visible = TrainingSettings.ShowInputHistory;
            if (info != null) info.Visible = TrainingSettings.ShowTrainingInfo;
        }

        // ------------------------------------------------------------ input

        public override void _Input(InputEvent e)
        {
            if (!IsTraining) return;

            if (!_open)
            {
                if (GameManager.Instance == null || GameManager.Instance.IsPaused) return;
                if (e is InputEventKey k && k.Pressed && !k.Echo)
                {
                    if (k.Keycode == Key.F8) TrainingController.ToggleRecording();
                    else if (k.Keycode == Key.F9) TrainingController.TogglePlayback();
                    else if (k.Keycode == Key.F11) ResetNow();
                }
                // Save / Load State: bindable in the button config (keyboard 1 defaults: F5 / F6).
                if (!e.IsEcho())
                {
                    if (AnyPressed(e, "save_state")) SaveStates.Save();
                    else if (AnyPressed(e, "load_state")) SaveStates.Load();
                    else if (AnyPressed(e, "frame_advance")) FrameAdvance.Toggle();
                    else if (AnyPressed(e, "frame_step")) FrameAdvance.Step();
                }
                return;
            }

            int pageStep = PageInput(e);
            if (pageStep != 0)
            {
                GetViewport().SetInputAsHandled();
                ShowPage(_page + pageStep);
                _selected = 0;
                Refresh();
                return;
            }

            int move = 0, change = 0;
            bool accept = false, back = false;
            if (Pressed(e, "ui_up") || Pressed(e, "general_up")) move = -1;
            else if (Pressed(e, "ui_down") || Pressed(e, "general_down")) move = 1;
            else if (Pressed(e, "ui_left") || Pressed(e, "general_left")) change = -1;
            else if (Pressed(e, "ui_right") || Pressed(e, "general_right")) change = 1;
            else if (Pressed(e, "ui_accept") || Pressed(e, "general_accept")) accept = true;
            else if (Pressed(e, "ui_cancel") || Pressed(e, "general_return")) back = true;
            else return;

            GetViewport().SetInputAsHandled();
            var row = _rows[_selected];
            if (move != 0) _selected = NextSelectable(_selected, move);
            else if (change != 0 && row.Change != null) { row.Change(change); TrainingSettings.Save(); }
            else if (accept)
            {
                if (row.Activate != null) row.Activate();
                else if (row.Change != null) { row.Change(1); TrainingSettings.Save(); }
            }
            else if (back) { Close(); return; }
            if (_open) Refresh();
        }

        static bool Pressed(InputEvent e, string action) => InputMap.HasAction(action) && e.IsActionPressed(action, true);

        static readonly string[] DevicePrefixes = { "k1_", "k2_", "j1_", "j2_" };
        static bool AnyHeld(string action)
        {
            foreach (var p in DevicePrefixes)
                if (InputMap.HasAction(p + action) && Godot.Input.IsActionPressed(p + action)) return true;
            return false;
        }

        static bool AnyPressed(InputEvent e, string action)
        {
            foreach (var p in DevicePrefixes)
                if (InputMap.HasAction(p + action) && e.IsActionPressed(p + action, false)) return true;
            return false;
        }

        /// <summary>Shoulder buttons (LB / RB), PageUp / PageDown or Q / E switch pages.</summary>
        static int PageInput(InputEvent e)
        {
            if (e is InputEventJoypadButton j && j.Pressed)
            {
                if (j.ButtonIndex == JoyButton.LeftShoulder) return -1;
                if (j.ButtonIndex == JoyButton.RightShoulder) return 1;
            }
            if (e is InputEventKey k && k.Pressed && !k.Echo)
            {
                if (k.Keycode == Key.Pageup) return -1;
                if (k.Keycode == Key.Pagedown) return 1;
            }
            return 0;
        }
    }
}
