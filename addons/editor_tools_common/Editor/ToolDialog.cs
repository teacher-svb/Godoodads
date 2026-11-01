#if TOOLS
using Godot;

namespace Godoodads.Common;

/// <summary>
/// Popup used by every tool in the suite: optional header rows, an embedded inspector that edits a
/// settings <see cref="Resource"/>, and a status area. The dialog stays open on its action button
/// and on errors so the user keeps their context.
/// </summary>
[Tool]
public partial class ToolDialog : ConfirmationDialog
{
    public enum Severity { Info, Success, Warning, Error }

    /// <summary>Emitted when the action button (for example "Populate") is pressed.</summary>
    [Signal] public delegate void ActionPressedEventHandler();

    /// <summary>Container for custom rows above the inspector, such as node pickers.</summary>
    public VBoxContainer Header { get; private set; }

    public EditorInspector Inspector { get; private set; }

    private RichTextLabel _status;
    private string _sizeKey;

    public ToolDialog() { }

    /// <param name="sizeKey">Project metadata key under which the dialog remembers its size.</param>
    public ToolDialog(string title, string actionText, string sizeKey = null)
    {
        _sizeKey = sizeKey;
        Title = title;
        OkButtonText = actionText;
        CancelButtonText = "Close";
        DialogHideOnOk = false;
        Unresizable = false;
        // Not exclusive: editor popups opened from here (such as the node selector) must be able to
        // become the exclusive child, and the editor stays usable. Transient keeps it on top.
        Exclusive = false;
        Transient = true;
        MinSize = new Vector2I(420, 360);
        Size = new Vector2I(520, 680);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 6);
        AddChild(root);

        Header = new VBoxContainer();
        root.AddChild(Header);

        Inspector = EditorInspector.CreateDefaultInspector();
        Inspector.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        Inspector.CustomMinimumSize = new Vector2(0, 200);
        root.AddChild(Inspector);

        root.AddChild(new HSeparator());

        _status = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            ScrollActive = false,
            SelectionEnabled = true,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        root.AddChild(_status);

        Confirmed += OnConfirmed;
        VisibilityChanged += OnVisibilityChanged;
    }

    /// <summary>Optional filter that hides generic Resource rows for the edited object.</summary>
    public DialogInspectorFilterPlugin Filter { get; set; }

    /// <summary>Shows <paramref name="settings"/> in the embedded inspector.</summary>
    public void Edit(Resource settings)
    {
        if (Filter != null)
            Filter.Target = settings;
        Inspector.Edit(settings);
    }

    public void ClearStatus() => _status.Clear();

    /// <summary>Replaces the status area with a single message.</summary>
    public void SetStatus(string text, Severity severity = Severity.Info)
    {
        _status.Clear();
        AddStatus(text, severity);
    }

    /// <summary>Appends a message line to the status area.</summary>
    public void AddStatus(string text, Severity severity = Severity.Info)
    {
        if (_status.GetParsedText().Length > 0)
            _status.Newline();
        _status.PushColor(ColorFor(severity));
        _status.AddText(text);
        _status.Pop();
    }

    public void SetActionEnabled(bool enabled) => GetOkButton().Disabled = !enabled;

    public void PopupForEditing()
    {
        if (_sizeKey != null)
        {
            var saved = EditorInterface.Singleton.GetEditorSettings().GetProjectMetadata(MetadataSection, _sizeKey, Size);
            if (saved.VariantType == Variant.Type.Vector2I)
                Size = saved.AsVector2I();
        }
        PopupCenteredClamped(Size, 0.9f);
    }

    private const string MetadataSection = "godoodads";

    private void OnConfirmed() => EmitSignal(SignalName.ActionPressed);

    // The inspector lists the edited class first, then inherited classes (Resource, RefCounted) and
    // the Add Metadata button. Godot has no option to hide those, so while the dialog is visible the
    // rows are walked in display order and every row from the second class header on is hidden.
    // Container nesting differs between Godot versions; display order does not. If no second class
    // header is found, nothing is hidden and the dialog still works.
    private const string CategoryClass = "EditorInspectorCategory";

    public override void _Process(double delta)
    {
        if (Visible)
            HideInheritedRows(Inspector, 0);
    }

    /// <summary>Returns the number of visible class headers seen so far, in display order.</summary>
    private static int HideInheritedRows(Node node, int categories)
    {
        foreach (var child in node.GetChildren(true))
        {
            // Invisible rows are skipped: Godot keeps hidden blocks (such as Favorites, with a class
            // header of its own) that must not count, and rows hidden here stay hidden.
            if (child is Control { Visible: false })
                continue;
            bool isCategory = child.IsClass(CategoryClass);
            if (isCategory)
                categories++;
            bool isRow = isCategory || child.IsClass("EditorInspectorSection") || child is EditorProperty or Button;
            if (categories >= 2 && isRow)
            {
                ((Control)child).Visible = false;
                continue; // its contents are hidden with it
            }
            categories = HideInheritedRows(child, categories);
        }
        return categories;
    }

    private void OnVisibilityChanged()
    {
        if (!Visible && _sizeKey != null)
            EditorInterface.Singleton.GetEditorSettings().SetProjectMetadata(MetadataSection, _sizeKey, Size);
    }

    private static Color ColorFor(Severity severity)
    {
        var theme = EditorInterface.Singleton.GetEditorTheme();
        return severity switch
        {
            Severity.Success => theme.GetColor("success_color", "Editor"),
            Severity.Warning => theme.GetColor("warning_color", "Editor"),
            Severity.Error => theme.GetColor("error_color", "Editor"),
            _ => theme.GetColor("font_color", "Editor"),
        };
    }
}
#endif
