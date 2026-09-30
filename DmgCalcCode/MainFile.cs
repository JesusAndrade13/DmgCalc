using System.Reflection;
using DmgCalc.DmgCalcCode.Patches;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;


namespace DmgCalc.DmgCalcCode;

//You're recommended but not required to keep all your code in this package and all your assets in the DmgCalc folder.
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "DmgCalc"; //At the moment, this is used only for the Logger and harmony names.

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        Harmony harmony = new(ModId);
        harmony.PatchAll(assembly);

        var overlay = new DamageOverlay();
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.AddChild(overlay);
    }
}