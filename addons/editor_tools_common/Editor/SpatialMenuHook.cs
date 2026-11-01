#if TOOLS
using Godot;
using Godot.Collections;

namespace Godoodads.Common;

/// <summary>
/// Adds menu items to a built-in menu button in the 3D viewport toolbar (for example Godot's
/// "MultiMesh" menu), identified by its editor icon so the match survives translation.
/// If the button is not found, a menu button of our own is added to the same toolbar instead;
/// the plugin then has to drive its visibility through <see cref="SetFallbackVisible"/>.
///
/// A GodotObject with Variant-only state and method-name Callables, so the hook keeps working
/// after the editor reloads the C# assembly.
/// </summary>
[Tool]
public partial class SpatialMenuHook : RefCounted
{
    // Built-in menus use small ids; keep ours far away from them.
    private const int BaseId = 7300;
    private const int SeparatorId = BaseId - 1;

    private EditorPlugin _plugin;
    private PopupMenu _popup;
    private MenuButton _fallback;
    private bool _hasSeparator;
    private Dictionary<int, Callable> _callbacks = new();
    // Original texts of host items renamed by AppendToHostItems, keyed by item id.
    private Dictionary<int, string> _originalTexts = new();

    /// <summary>True when items were added to the built-in button rather than our own.</summary>
    public bool IsHooked => _fallback == null;

    public SpatialMenuHook() { }

    /// <param name="hostIcon">Editor icon of the built-in menu button, for example "MultiMeshInstance3D".</param>
    /// <param name="fallbackText">Text of our own button if the built-in one cannot be found.</param>
    public SpatialMenuHook(EditorPlugin plugin, string hostIcon, string fallbackText)
    {
        _plugin = plugin;
        var icon = EditorInterface.Singleton.GetEditorTheme().GetIcon(hostIcon, "EditorIcons");
        var host = FindHost(plugin, icon);
        if (host != null)
        {
            _popup = host.GetPopup();
        }
        else
        {
            _fallback = new MenuButton { Text = fallbackText, Icon = icon, Flat = true, Visible = false };
            plugin.AddControlToContainer(EditorPlugin.CustomControlContainer.SpatialEditorMenu, _fallback);
            _popup = _fallback.GetPopup();
        }
        _popup.Connect(PopupMenu.SignalName.IdPressed, new Callable(this, MethodName.OnIdPressed));
    }

    /// <summary>
    /// Adds an item; ids are offset so they never clash with the host's own items. Pass a
    /// method-name Callable (<c>new Callable(plugin, MethodName.X)</c>) so it survives reloads.
    /// </summary>
    public void AddItem(string text, Callable callback)
    {
        if (IsHooked && !_hasSeparator && _popup.ItemCount > 0)
        {
            _popup.AddSeparator("", SeparatorId);
            _hasSeparator = true;
        }
        int id = BaseId + _callbacks.Count;
        _popup.AddItem(text, id);
        _callbacks[id] = callback;
    }

    /// <summary>
    /// Appends <paramref name="suffix"/> to the host menu's own items, for example " (Godot)". The
    /// translated text is kept, and <see cref="Remove"/> restores the original texts.
    /// </summary>
    public void AppendToHostItems(string suffix)
    {
        if (!IsHooked)
            return;
        for (int i = 0; i < _popup.ItemCount; i++)
        {
            int id = _popup.GetItemId(i);
            if (id >= SeparatorId || _popup.IsItemSeparator(i) || _originalTexts.ContainsKey(id))
                continue;
            _originalTexts[id] = _popup.GetItemText(i);
            _popup.SetItemText(i, _popup.GetItemText(i) + suffix);
        }
    }

    public void SetFallbackVisible(bool visible)
    {
        if (_fallback != null)
            _fallback.Visible = visible;
    }

    /// <summary>Removes everything this hook added.</summary>
    public void Remove()
    {
        if (_popup == null)
            return;
        if (IsInstanceValid(_popup))
        {
            var handler = new Callable(this, MethodName.OnIdPressed);
            if (_popup.IsConnected(PopupMenu.SignalName.IdPressed, handler))
                _popup.Disconnect(PopupMenu.SignalName.IdPressed, handler);
        }
        if (_fallback != null)
        {
            _plugin.RemoveControlFromContainer(EditorPlugin.CustomControlContainer.SpatialEditorMenu, _fallback);
            _fallback.QueueFree();
        }
        else if (IsInstanceValid(_popup))
        {
            foreach (var (id, text) in _originalTexts)
            {
                int index = _popup.GetItemIndex(id);
                if (index >= 0)
                    _popup.SetItemText(index, text);
            }
            foreach (var id in _callbacks.Keys)
                RemoveById(id);
            if (_hasSeparator)
                RemoveById(SeparatorId);
        }
        // Drop every reference: this object can outlive the plugin until garbage collection, and an
        // assembly reload serializes it, which fails on references to freed objects.
        _popup = null;
        _fallback = null;
        _plugin = null;
        _callbacks.Clear();
        _originalTexts.Clear();
    }

    private void RemoveById(int id)
    {
        int index = _popup.GetItemIndex(id);
        if (index >= 0)
            _popup.RemoveItem(index);
    }

    private void OnIdPressed(long id)
    {
        if (_callbacks.TryGetValue((int)id, out var callback))
            callback.Call();
    }

    private static MenuButton FindHost(EditorPlugin plugin, Texture2D icon)
    {
        // Built-in 3D editor plugins add their menu buttons to the same container as
        // CustomControlContainer.SpatialEditorMenu; find it through a temporary probe control.
        var probe = new Control();
        plugin.AddControlToContainer(EditorPlugin.CustomControlContainer.SpatialEditorMenu, probe);
        var container = probe.GetParent();
        plugin.RemoveControlFromContainer(EditorPlugin.CustomControlContainer.SpatialEditorMenu, probe);
        probe.Free();

        if (container == null)
            return null;
        foreach (var child in container.GetChildren())
            if (child is MenuButton button && button.Icon == icon)
                return button;
        return null;
    }
}
#endif
