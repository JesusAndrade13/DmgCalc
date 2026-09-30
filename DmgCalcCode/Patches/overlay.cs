using Godot;

namespace DmgCalc.DmgCalcCode.Patches;

/// <summary>
/// In-game overlay showing hand damage previews. Background panel for
/// legibility over game art; RichTextLabel with BBCode so card names and
/// damage numbers can be styled, not just plain text.
///
/// Usage from MainFile.Initialize():
///   var overlay = new DamageOverlay();
///   var tree = (SceneTree)Engine.GetMainLoop();
///   tree.Root.AddChild(overlay);
///
/// SetText now expects BBCode-formatted content (see CombatStateCapturePatch).
/// </summary>
public partial class DamageOverlay : CanvasLayer
{
    public static DamageOverlay? Instance { get; private set; }

    private PanelContainer _panel = new();
    private RichTextLabel _richText = new();

    public override void _Ready()
    {
        Instance = this;

        Layer = 100; // draw above normal game UI

        _panel.Position = new Vector2(300, 20);
        _panel.CustomMinimumSize = new Vector2(320, 0);
        _panel.MouseFilter = Control.MouseFilterEnum.Ignore;

        // Semi-transparent dark background so text stays readable over any
        // game art behind it, without fully blocking the view.
        var bg = new StyleBoxFlat
        {
            BgColor = new Color(0f, 0f, 0f, 0.65f),
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6
        };
        _panel.AddThemeStyleboxOverride("panel", bg);

        _richText.BbcodeEnabled = true;
        _richText.FitContent = true; // panel grows/shrinks to fit content vertically
        _richText.ScrollActive = false;
        _richText.MouseFilter = Control.MouseFilterEnum.Ignore;
        _richText.CustomMinimumSize = new Vector2(300, 0);
        _richText.Text = "[b]DmgCalc[/b] loaded";

        _panel.AddChild(_richText);
        AddChild(_panel);
    }

    /// <param name="bbcode">BBCode-formatted content, e.g. from BuildOverlayText below.</param>
    public void SetText(string bbcode) => _richText.Text = bbcode;
}