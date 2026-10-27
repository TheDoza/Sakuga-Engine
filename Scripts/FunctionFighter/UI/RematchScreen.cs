using Godot;
using SakugaEngine;
using SakugaEngine.Game;

namespace FunctionFighter.UI
{
    /// <summary>
    /// Post-match screen: the winner's victory render and quote, a centre strip with both players' portraits, WON / LOST and
    /// rounds won, and an option list per player (Rematch, Character Select, Leave Match).
    ///
    /// Rules:
    /// - Rematch only happens when both players pick it (a CPU side always picks Rematch).
    /// - Character Select from either player takes both there.
    /// - Leave Match from either player leaves (GameModeSettings.LeaveMatchScene, the main menu by default; an online mode
    ///   can point it at its lobby later).
    /// - Each player moves with their own up / down, confirms with Accept or A, and takes a pick back with Return or B.
    ///
    /// Built in code, so it needs no scene: GameMonitor.ShowResultScreen opens it ([FF PATCH: rematch]).
    /// Menu-only (runs after the match is over), so it isn't part of rollback.
    /// </summary>
    public partial class RematchScreen : Control
    {
        public enum Choice { None = -1, Rematch = 0, CharacterSelect = 1, LeaveMatch = 2 }

        static readonly string[] Labels = { "REMATCH", "CHARACTER SELECT", "LEAVE MATCH" };

        // Look
        public static Color AccentColor = new Color(0.93f, 0.33f, 0.12f);      // selected bar (orange-red)
        public static Color AccentGlow = new Color(1f, 0.62f, 0.2f);
        public static Color StripColor = new Color(0.08f, 0.11f, 0.18f, 0.92f);
        public static Color GoldColor = new Color(0.86f, 0.72f, 0.36f);
        public static Color WonColor = new Color(1f, 0.85f, 0.35f);
        public static Color LostColor = new Color(0.62f, 0.66f, 0.74f);
        public static Color ReadyColor = new Color(0.45f, 1f, 0.55f);
        /// <summary>Frames before input is read, so the button that skipped the outro doesn't pick something.</summary>
        public static int InputDelayFrames = 30;

        static RematchScreen _instance;
        /// <summary>True while the screen is open (GameManager skips its old "any button returns" on the results).</summary>
        public static bool Active => _instance != null && IsInstanceValid(_instance) && _instance.Visible;

        SakugaActor[] _fighters;
        GameMonitor _monitor;
        readonly int[] _cursor = { 0, 0 };
        readonly Choice[] _picked = { Choice.None, Choice.None };
        readonly bool[] _cpu = new bool[2];
        readonly Control[][] _items = new Control[2][];
        readonly ColorRect[][] _bars = new ColorRect[2][];
        readonly Label[][] _texts = new Label[2][];
        readonly Label[] _status = new Label[2];
        int _frames;
        bool _leaving;
        float _time;

        /// <summary>Opens the screen on <paramref name="parent"/> (the fighter UI). Returns false if it can't.</summary>
        public static bool Open(Node parent, SakugaActor[] fighters, GameMonitor monitor, int winner, string quote)
        {
            if (parent == null || fighters == null || fighters.Length < 2 || monitor == null) return false;
            if (Active) return true;
            var s = new RematchScreen { Name = "RematchScreen" };
            parent.AddChild(s);
            // The in-match HUD (health bars, timer, training panels) is hidden behind the results; the fade curtain stays.
            foreach (var c in parent.GetChildren())
                if (c is CanvasItem ci && c != s && c.Name != "FadeScreen") ci.Visible = false;
            s.Build(fighters, monitor, winner, quote);
            _instance = s;
            return true;
        }

        public override void _ExitTree() { if (_instance == this) _instance = null; }

        void Build(SakugaActor[] fighters, GameMonitor monitor, int winner, string quote)
        {
            _fighters = fighters; _monitor = monitor;
            SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            MouseFilter = MouseFilterEnum.Stop;
            ZIndex = 50;

            AddChild(new ColorRect { Color = new Color(0, 0, 0, 0.72f), AnchorRight = 1, AnchorBottom = 1, MouseFilter = MouseFilterEnum.Ignore });

            // Victory render, fitted behind everything else.
            var render = fighters[Mathf.Clamp(winner, 0, 1)].Data?.Profile?.VictoryRender;
            if (winner >= 0 && render != null)
            {
                var tr = new TextureRect
                {
                    Texture = render, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    AnchorLeft = winner == 0 ? 0.04f : 0.46f, AnchorRight = winner == 0 ? 0.54f : 0.96f, AnchorTop = 0.02f, AnchorBottom = 0.70f,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                AddChild(tr);
            }

            // Title + quote
            string winnerName = winner >= 0 ? (fighters[winner].Data?.Profile?.ShortName ?? $"PLAYER {winner + 1}") : "";
            var title = MakeLabel(winner >= 0 ? $"PLAYER {winner + 1} WINS" : "DRAW", 46, Colors.White, HorizontalAlignment.Center);
            title.AnchorLeft = 0; title.AnchorRight = 1; title.AnchorTop = 0.03f; title.AnchorBottom = 0.12f;
            AddChild(title);
            if (winner >= 0)
            {
                var sub = MakeLabel(winnerName.ToUpper(), 26, WonColor, HorizontalAlignment.Center);
                sub.AnchorLeft = 0; sub.AnchorRight = 1; sub.AnchorTop = 0.11f; sub.AnchorBottom = 0.17f;
                AddChild(sub);
            }

            // Centre strip
            var strip = new Panel { AnchorLeft = 0.24f, AnchorRight = 0.76f, AnchorTop = 0.70f, AnchorBottom = 0.85f, MouseFilter = MouseFilterEnum.Ignore };
            var sb = new StyleBoxFlat { BgColor = StripColor, BorderColor = GoldColor, BorderWidthTop = 2, BorderWidthBottom = 2, ShadowColor = new Color(0, 0, 0, 0.6f), ShadowSize = 10 };
            strip.AddThemeStyleboxOverride("panel", sb);
            AddChild(strip);
            strip.AddChild(new ColorRect { Color = GoldColor, AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorBottom = 1, OffsetLeft = -1, OffsetRight = 1, MouseFilter = MouseFilterEnum.Ignore });
            for (int p = 0; p < 2; p++) BuildPlayerHalf(strip, p, winner);

            if (!string.IsNullOrEmpty(quote))
            {
                var q = MakeLabel($"“{quote}”", 22, Colors.White, HorizontalAlignment.Center);
                q.AnchorLeft = 0.24f; q.AnchorRight = 0.76f; q.AnchorTop = 0.86f; q.AnchorBottom = 0.92f;
                AddChild(q);
            }

            for (int p = 0; p < 2; p++) BuildMenu(p);

            // Slide in
            Modulate = new Color(1, 1, 1, 0);
            CreateTween().TweenProperty(this, "modulate:a", 1f, 0.25f);
            Refresh();
        }

        void BuildPlayerHalf(Control strip, int p, int winner)
        {
            bool left = p == 0;
            var f = _fighters[p];
            var half = new Control { AnchorLeft = left ? 0 : 0.5f, AnchorRight = left ? 0.5f : 1, AnchorBottom = 1, MouseFilter = MouseFilterEnum.Ignore };
            strip.AddChild(half);

            var portrait = new TextureRect
            {
                Texture = f.Data?.Profile?.Portrait, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                AnchorLeft = left ? 0.04f : 0.66f, AnchorRight = left ? 0.34f : 0.96f, AnchorTop = 0.08f, AnchorBottom = 0.92f, FlipH = !left,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            half.AddChild(portrait);

            bool won = winner == p;
            string result = winner < 0 ? "DRAW" : won ? "WON" : "LOST";
            var res = MakeLabel(result, 34, won ? WonColor : LostColor, left ? HorizontalAlignment.Right : HorizontalAlignment.Left);
            res.AnchorLeft = left ? 0.36f : 0.20f; res.AnchorRight = left ? 0.80f : 0.64f; res.AnchorTop = 0.05f; res.AnchorBottom = 0.65f;
            half.AddChild(res);

            int rounds = _monitor.VictoryCounter != null && p < _monitor.VictoryCounter.Length ? _monitor.VictoryCounter[p] : 0;
            var score = MakeLabel(rounds.ToString(), 40, Colors.White, HorizontalAlignment.Center);
            score.AnchorLeft = left ? 0.82f : 0.0f; score.AnchorRight = left ? 1.0f : 0.18f; score.AnchorTop = 0.05f; score.AnchorBottom = 0.65f;
            half.AddChild(score);

            var name = MakeLabel((f.Data?.Profile?.ShortName ?? "").ToUpper() + (f.UseAI ? "  (CPU)" : ""), 16, new Color(0.85f, 0.9f, 1f), left ? HorizontalAlignment.Right : HorizontalAlignment.Left);
            // Mirror of P1's span (0.36-0.98), so both names start / end at the centre line under the score.
            name.AnchorLeft = left ? 0.36f : 0.02f; name.AnchorRight = left ? 0.98f : 0.64f; name.AnchorTop = 0.62f; name.AnchorBottom = 0.95f;
            half.AddChild(name);
        }

        void BuildMenu(int p)
        {
            bool left = p == 0;
            _cpu[p] = _fighters[p].UseAI;
            if (_cpu[p]) { _picked[p] = Choice.Rematch; _cursor[p] = 0; }

            var col = new VBoxContainer { AnchorLeft = left ? 0.0f : 0.78f, AnchorRight = left ? 0.22f : 1.0f, AnchorTop = 0.66f, AnchorBottom = 0.96f, MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 6);
            AddChild(col);

            var header = MakeLabel(left ? $"    PLAYER {p + 1}" : $"PLAYER {p + 1}    ", 16, GoldColor, left ? HorizontalAlignment.Left : HorizontalAlignment.Right);
            header.CustomMinimumSize = new Vector2(0, 22);
            col.AddChild(header);

            _items[p] = new Control[Labels.Length]; _bars[p] = new ColorRect[Labels.Length]; _texts[p] = new Label[Labels.Length];
            for (int i = 0; i < Labels.Length; i++)
            {
                var item = new Control { CustomMinimumSize = new Vector2(0, 40), MouseFilter = MouseFilterEnum.Ignore };
                var bar = new ColorRect { Color = AccentColor, AnchorRight = 1, AnchorBottom = 1, MouseFilter = MouseFilterEnum.Ignore };
                var text = MakeLabel(Labels[i], 22, Colors.White, left ? HorizontalAlignment.Left : HorizontalAlignment.Right);
                text.AnchorRight = 1; text.AnchorBottom = 1; text.OffsetLeft = 18; text.OffsetRight = -18;
                item.AddChild(bar); item.AddChild(text);
                col.AddChild(item);
                _items[p][i] = item; _bars[p][i] = bar; _texts[p][i] = text;
            }
            _status[p] = MakeLabel("", 16, ReadyColor, left ? HorizontalAlignment.Left : HorizontalAlignment.Right);
            _status[p].CustomMinimumSize = new Vector2(0, 22);
            col.AddChild(_status[p]);
        }

        static Label MakeLabel(string text, int size, Color color, HorizontalAlignment align)
        {
            var l = new Label { Text = text, HorizontalAlignment = align, VerticalAlignment = VerticalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
            l.AddThemeFontSizeOverride("font_size", size);
            l.AddThemeColorOverride("font_color", color);
            l.AddThemeColorOverride("font_outline_color", Colors.Black);
            l.AddThemeConstantOverride("outline_size", Mathf.Max(4, size / 5));
            return l;
        }

        public override void _Process(double delta)
        {
            _time += (float)delta;
            if (_leaving) return;
            _frames++;
            if (_frames > InputDelayFrames)
                for (int p = 0; p < 2; p++) if (!_cpu[p]) ReadInput(p);
            Refresh();
        }

        void ReadInput(int p)
        {
            // The device this player picked for the match (keyboard 1/2 or gamepad 1/2), so a controller works here
            // too. Falls back to keyboard 1 / 2 when no device is set.
            var match = GameManager.Instance?.Match;
            int device = match == null ? -1 : (p == 0 ? match.P1SelectedDevice : match.P2SelectedDevice);
            string k = device >= 0 ? SakugaEngine.Global.GlobalFunctions.GetPlayerPrefix(device) + "_" : (p == 0 ? "k1_" : "k2_");
            bool Pressed(string a) => InputMap.HasAction(k + a) && Godot.Input.IsActionJustPressed(k + a);
            if (_picked[p] == Choice.None)
            {
                if (Pressed("up")) _cursor[p] = (_cursor[p] + Labels.Length - 1) % Labels.Length;
                if (Pressed("down")) _cursor[p] = (_cursor[p] + 1) % Labels.Length;
                if (Pressed("accept") || Pressed("face_a")) Pick(p, (Choice)_cursor[p]);
            }
            else if (Pressed("return") || Pressed("face_b")) _picked[p] = Choice.None;
        }

        /// <summary>A player picks an option (also used by tests).</summary>
        public void Pick(int p, Choice c)
        {
            if (_leaving || p < 0 || p > 1) return;
            _picked[p] = c;
            _cursor[p] = (int)c;
            Decide();
        }

        public Choice Picked(int p) => _picked[p];

        /// <summary>Where the screen is sending the players ("" while waiting). Set just before the scene changes.</summary>
        public string Destination { get; private set; } = "";

        void Decide()
        {
            if (_picked[0] == Choice.LeaveMatch || _picked[1] == Choice.LeaveMatch) Go(LeaveScene());
            else if (_picked[0] == Choice.CharacterSelect || _picked[1] == Choice.CharacterSelect) Go(CharacterSelectScene);
            else if (_picked[0] == Choice.Rematch && _picked[1] == Choice.Rematch) Go(RematchScene());
        }

        public const string CharacterSelectScene = "res://Scenes/SelectScreen.tscn";
        public const string MainMenuScene = "res://Scenes/MainMenu.tscn";

        string LeaveScene()
        {
            var mode = GameManager.Instance?.Match?.SelectedModeSettings;
            return mode != null && !string.IsNullOrEmpty(mode.LeaveMatchScene) ? mode.LeaveMatchScene : MainMenuScene;
        }

        string RematchScene()
        {
            var scene = GetTree()?.CurrentScene?.SceneFilePath;
            return string.IsNullOrEmpty(scene) ? "res://Scenes/TestScene.tscn" : scene;
        }

        /// <summary>Tests set this to stop the scene change.</summary>
        public static bool DryRun;

        void Go(string scene)
        {
            Destination = scene;
            Refresh();
            if (DryRun) return;
            _leaving = true;
            LoadingScreenManager.Instance.LoadScene(scene);
        }

        void Refresh()
        {
            float pulse = 0.75f + 0.25f * Mathf.Sin(_time * 6f);
            for (int p = 0; p < 2; p++)
            {
                if (_items[p] == null) continue;
                for (int i = 0; i < Labels.Length; i++)
                {
                    bool sel = _cursor[p] == i;
                    bool chosen = _picked[p] == (Choice)i;
                    var bar = _bars[p][i];
                    bar.Visible = sel || chosen;
                    bar.Color = chosen ? AccentGlow : new Color(AccentColor, 0.55f + 0.45f * pulse);
                    _texts[p][i].Text = (chosen ? "✔ " : "") + Labels[i];
                    _texts[p][i].Modulate = _picked[p] != Choice.None && !chosen ? new Color(1, 1, 1, 0.35f) : Colors.White;
                }
                string other = p == 0 ? "PLAYER 2" : "PLAYER 1";
                _status[p].Text = _picked[p] == Choice.None ? "" :
                    _cpu[p] ? "CPU READY" :
                    _picked[p] == Choice.Rematch && _picked[1 - p] != Choice.Rematch ? $"READY – WAITING FOR {other}" : "READY";
            }
        }
    }
}
