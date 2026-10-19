using Godot;
using System;

namespace SakugaEngine.Resources
{
    [GlobalClass]
    public partial class GameModeSettings : Resource
    {
        [Export] public string ModeName;
        [Export] public bool AutoStart;
        [Export] public Global.NetcodeMode NetcodeMode;
        [Export] public Global.PauseMode PauseMode;
        [Export] public bool UseLAN;
        [Export] public bool CanKO;
        [Export] public bool AutoFillResources;
        [Export] public bool ShowMatchIntro; // doesn't exist
        [Export] public bool ShowMatchCards; // doesn't exist
        [Export] public bool ShowTrainingInfo;
        [Export] public bool ShowOnlineInfo;
        //[Export] public bool ShowTimeline; // For replay mode (doesn't exist)
        //[Export] public bool ShowComboList; // For combo challenge mode (doesn't exist)
        [Export] public bool AllowShowHitboxes;
        [Export] public bool AllowShowInputs;
        [Export] public bool AllowShowDebugElements;
        [Export] public bool AllowUseDebugCommands;
        [Export] public bool NoAI;
        [Export] public string ReturnToScene;
        // [FF PATCH: rematch] Where "Leave Match" on the results screen goes (an online mode can point it at its lobby).
        [Export] public string LeaveMatchScene = "res://Scenes/MainMenu.tscn";
        /// <summary>[FF PATCH: training] Turns on the Training Settings page in the pause menu and the training hotkeys.</summary>
        [Export] public bool IsTraining;
    }
}
